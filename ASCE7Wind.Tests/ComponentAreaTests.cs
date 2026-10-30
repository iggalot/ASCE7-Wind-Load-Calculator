using System;
using System.Linq;
using ASCE7WindLoadCalculator;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ASCE7Wind.Tests
{
    /// <summary>
    /// C&amp;C GCp is read at the effective wind area of the component being designed (26.2), not at the area of the
    /// zone, and every C&amp;C net design pressure is at least 16 psf in either direction (30.2.2).
    /// </summary>
    [TestClass]
    public class ComponentAreaTests
    {
        private static WindAnalysisResult Run(BuildingData b, double v = 115, double roofArea = 10, double wallArea = 10,
                                              ASCE7_Versions ed = ASCE7_Versions.ASCE_VER_7_22)
        {
            var p = new WindParameters_Base
            {
                WindSpeed = v, ExposureCategory = WindExposureCategories.WIND_EXP_CAT_C, RiskCategory = "II",
                RoofComponentArea = roofArea, WallComponentArea = wallArea,
            };
            return WindAnalysis.Run(ed, p, b);
        }

        private static BuildingData Flat() => new BuildingData { BuildingLength = 60, BuildingWidth = 40, BuildingHeight = 15, RoofType = RoofTypes.ROOF_TYPE_FLAT };
        private static BuildingData Gable() => new BuildingData { BuildingLength = 60, BuildingWidth = 40, BuildingHeight = 15, RoofType = RoofTypes.ROOF_TYPE_GABLE, RoofPitch = 22 };

        /// <summary>
        /// Hand calculation, ASCE 7-22, flat roof, Zone 3 suction at 10 sq ft (Fig. 30.3-2A: GCp = -3.2):
        /// Kh = 2.41 (15/2460)^(2/9.8) = 0.851; qh = 0.00256 Kh V^2 = 28.8 psf (Kzt = Ke = 1);
        /// p = qh Kd [(GCp) - (+GCpi)] = 28.8 × 0.85 × (-3.2 - 0.18) = -82.8 psf (Eq. 30.3-1).
        /// </summary>
        [TestMethod]
        public void FlatRoofZone3_At10SqFt_MatchesHandCalculation()
        {
            var cc = Run(Flat()).Cladding;
            double kh = 2.41 * Math.Pow(15.0 / 2460.0, 2.0 / 9.8);
            double qh = 0.00256 * kh * 115 * 115;
            double expected = qh * 0.85 * (-3.2 - 0.18);
            Assert.AreEqual(-82.8, expected, 0.1, "hand calculation");

            var zone3 = cc.RoofAreaCalculator.effWindAreas.First(a => a.Value.Label_Full == "Zone3");
            Assert.AreEqual(expected, cc.windPressureRoof_Neg_External[zone3.Key].NetPressure, 0.05);
        }

        /// <summary>Every zone reads its curve at the component's area, whatever the zone's own size.</summary>
        [DataTestMethod]
        [DataRow(10.0)]
        [DataRow(50.0)]
        [DataRow(300.0)]
        public void RoofGCp_IsReadAtTheComponentArea_InEveryZone(double area)
        {
            var cc = Run(Gable(), roofArea: area).Cladding;
            foreach (var z in cc.RoofAreaCalculator.effWindAreas)
            {
                double expected = cc.extGCpCurve_Roof.RoofCurves_Neg[z.Value.Label_Full].Evaluate(area);
                Assert.AreEqual(expected, cc.windPressureRoof_Neg_External[z.Key].GCp, 1e-12, $"{z.Value.Label_Full} (zone area {z.Value.Area:0} sq ft)");
            }
        }

        [TestMethod]
        public void LargerComponent_GivesSmallerSuction_AndWallsUseTheirOwnArea()
        {
            var small = Run(Gable(), roofArea: 10, wallArea: 10).Cladding;
            var large = Run(Gable(), roofArea: 100, wallArea: 10).Cladding;
            double Min(System.Collections.Generic.Dictionary<int, PressureData> d) => d.Values.Min(p => p.NetPressure);
            Assert.IsTrue(Min(large.windPressureRoof_Neg_External) > Min(small.windPressureRoof_Neg_External), "roof suction drops with area");
            Assert.AreEqual(Min(small.windPressureBuildingLengthWall_Neg_External), Min(large.windPressureBuildingLengthWall_Neg_External), 1e-12,
                "the wall area did not change, so the wall pressures do not");
        }

        /// <summary>Default is 10 sq ft: the old behaviour (GCp at the zone area) understated a large Zone 1.</summary>
        [TestMethod]
        public void DefaultComponentArea_Is10SqFt()
        {
            var p = new WindParameters_Base();
            Assert.AreEqual(10.0, p.RoofComponentArea);
            Assert.AreEqual(10.0, p.WallComponentArea);
            var cc = WindAnalysis.Run(ASCE7_Versions.ASCE_VER_7_22, new WindParameters_Base { ExposureCategory = WindExposureCategories.WIND_EXP_CAT_C }, Gable()).Cladding;
            var zone1 = cc.RoofAreaCalculator.effWindAreas.First(a => a.Value.Label_Full == "Zone1");
            Assert.AreEqual(cc.extGCpCurve_Roof.RoofCurves_Neg["Zone1"].Evaluate(10), cc.windPressureRoof_Neg_External[zone1.Key].GCp, 1e-12);
        }

        /// <summary>30.2.2: at least 16 psf net in either direction; larger calculated values are unchanged.</summary>
        [DataTestMethod]
        [DataRow(ASCE7_Versions.ASCE_VER_7_16)]
        [DataRow(ASCE7_Versions.ASCE_VER_7_22)]
        public void CladdingNet_IsAtLeast16Psf_EitherDirection(ASCE7_Versions ed)
        {
            var cc = Run(Gable(), v: 95, roofArea: 100, wallArea: 100, ed: ed).Cladding;
            var all = new[]
            {
                cc.windPressureRoof_Pos_External, cc.windPressureRoof_Neg_External,
                cc.windPressureBuildingLengthWall_Pos_External, cc.windPressureBuildingLengthWall_Neg_External,
                cc.windPressureBuildingWidthWall_Pos_External, cc.windPressureBuildingWidthWall_Neg_External,
            }.SelectMany(d => d.Values).ToList();

            Assert.IsTrue(all.Any(p => p.MinimumGoverns), "a low wind speed and large area should hit the minimum somewhere");
            foreach (var p in all)
            {
                Assert.IsTrue(Math.Abs(p.NetPressure) >= 16 - 1e-9, $"|{p.NetPressure:0.0}| >= 16");
                Assert.AreEqual(Math.Sign(p.ExternalPressure < 0 ? -1 : 1), Math.Sign(p.NetPressure), "the minimum keeps the direction");
                if (!p.MinimumGoverns) Assert.AreEqual(p.CalculatedNetPressure, p.NetPressure, 1e-12);
            }
        }

        [TestMethod]
        public void Mwfrs_HasNoCladdingMinimum()
        {
            var r = Run(Gable(), v: 95);
            Assert.IsTrue(r.MwfrsAlongLength.windPressureWall_Pos_External_MWFRS.Values.All(p => p.MinimumNet == 0));
            Assert.IsTrue(r.MwfrsAlongLength.windPressureRoof_Neg_External.Values.All(p => p.MinimumNet == 0));
        }
    }
}
