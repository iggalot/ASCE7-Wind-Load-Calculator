using System.Linq;
using ASCE7WindLoadCalculator;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ASCE7Wind.Tests
{
    /// <summary>
    /// The cladding figures' h (Figs. 30.3-1 and 30.3-2A to 2I, 7-16 and 7-22 alike): "h = Mean roof height, except that
    /// eave height shall be used for θ ≤ 10°", for qh and the zone widths; and the walls figure's exception, a ≤ 0.8h for
    /// θ = 0° to 7° and a least horizontal dimension over 300 ft (Fig. 30.3-1).
    /// </summary>
    [TestClass]
    public class CcFigureHeightTests
    {
        private static WindLoadCalculator_CC Cc(ASCE7_Versions v, double l, double b, double eave, RoofTypes roof, double slope) =>
            (WindLoadCalculator_CC)Bldg.Run(v, WindLoadCalculationTypes.COMPONENT_AND_CLADDING,
                Bldg.Make(l, b, eave, roof, slope, RidgeDirections.RIDGE_DIR_PARALLEL_TO_BLDGLENGTH));

        [DataTestMethod]
        [DataRow(ASCE7_Versions.ASCE_VER_7_16, 5.0)]
        [DataRow(ASCE7_Versions.ASCE_VER_7_22, 5.0)]
        [DataRow(ASCE7_Versions.ASCE_VER_7_16, 10.0)]
        [DataRow(ASCE7_Versions.ASCE_VER_7_22, 10.0)]
        public void RoofAt10DegOrLess_QhAndZoneWidth_AtTheEaveHeight(ASCE7_Versions v, double slope)
        {
            var c = Cc(v, 60, 40, 15, RoofTypes.ROOF_TYPE_GABLE, slope);
            var bd = c.buildingData;
            Assert.IsTrue(bd.MeanRoofHeight > 15, "a gable: the mean roof height is above the eave");
            Assert.AreEqual(15, bd.CcFigureHeight, 1e-9, "the eave height");
            double qEave = c.CalculateDynamicWindPressure(15);
            Assert.IsTrue(c.windPressureRoof_Neg_External.Values.All(p => System.Math.Abs(p.qh - qEave) < 1e-9), "qh at the eave height");
            Assert.IsTrue(c.windPressureBuildingLengthWall_Neg_External.Values.All(p => System.Math.Abs(p.qh - qEave) < 1e-9));
            // a = min(0.4h, 0.1 x 40) = min(6, 4) = 4: the least dimension governs here, so try a narrow h case below
            Assert.AreEqual(4.0, c.WallAreaCalculator_BldgLength.CritDim_a, 1e-9);
        }

        [TestMethod]
        public void RoofAt10DegOrLess_ZoneWidthFromTheEave_WhenHGoverns()
        {
            // 200 x 120 ft, 8 ft eave, 10°: 0.1 x 120 = 12, 0.4 x 8 = 3.2 (eave) vs 0.4 x mean (larger); not under 4.8 (4%)
            var c = Cc(ASCE7_Versions.ASCE_VER_7_16, 200, 120, 8, RoofTypes.ROOF_TYPE_GABLE, 10);
            Assert.AreEqual(4.8, c.WallAreaCalculator_BldgLength.CritDim_a, 1e-9, "0.4 x 8 = 3.2, raised to 4% of 120");
            var steep = Cc(ASCE7_Versions.ASCE_VER_7_16, 200, 120, 8, RoofTypes.ROOF_TYPE_GABLE, 15);
            double h = steep.buildingData.MeanRoofHeight;
            Assert.AreEqual(h, steep.buildingData.CcFigureHeight, 1e-9, "over 10°: the mean roof height");
            Assert.AreEqual(System.Math.Min(0.4 * h, 12), steep.WallAreaCalculator_BldgLength.CritDim_a, 1e-9);
            Assert.AreEqual(steep.CalculateDynamicWindPressure(h), steep.windPressureRoof_Neg_External.Values.First().qh, 1e-9);
        }

        /// <summary>
        /// 7-16 Fig. 30.3-2E note 7: B for Zone 3 is the least horizontal dimension. A 40 ft long, 60 ft wide hip at 15°:
        /// h/40, not h/60 (which read the h/B ≤ 0.5 curve); Zone 3 at 10 sq ft between -1.8 (h/B ≤ 0.5) and -2.6 (≥ 0.8).
        /// </summary>
        [TestMethod]
        public void Hip7_16_HOverB_FromTheLeastDimension()
        {
            var c = Cc(ASCE7_Versions.ASCE_VER_7_16, 40, 60, 20, RoofTypes.ROOF_TYPE_HIP, 15);
            var bd = c.buildingData;
            double r = bd.CcFigureHeight / 40;
            Assert.AreEqual(r, bd.HipCladdingHOverB, 1e-9);
            Assert.IsTrue(r > 0.5 && r < 0.8, $"between the two curves: {r:0.000}");
            Assert.IsTrue(bd.MeanRoofHeight / 60 < 0.5, "the width alone read the lower curve");
            double expected = -1.8 + (r - 0.5) / 0.3 * (-2.6 + 1.8);
            Assert.AreEqual(expected, c.extGCpCurve_Roof.RoofCurves_Neg["Zone3"].Evaluate(10), 1e-6);
        }

        /// <summary>
        /// The same hip, zone by zone (26.3: B normal to the wind; note 7): Zone 3 at h/40 (the least dimension); a Zone 2e
        /// along the 40 ft eaves at h/40 (wind normal to them, B their length); a Zone 2e along the 60 ft eaves at h/60
        /// (the h/B ≤ 0.5 curve: -1.8 at 10 sq ft); Zone 2r the larger of the two.
        /// </summary>
        [TestMethod]
        public void Hip7_16_EachZone_ItsOwnHOverB_BNormalToTheWind()
        {
            var c = Cc(ASCE7_Versions.ASCE_VER_7_16, 40, 60, 20, RoofTypes.ROOF_TYPE_HIP, 15);
            Assert.IsTrue(c.HipHOverBApplies);
            double h = c.buildingData.CcFigureHeight, r40 = h / 40, r60 = h / 60;
            Assert.IsTrue(r60 <= 0.5 && r40 > 0.5 && r40 < 0.8, $"{r40:0.000}, {r60:0.000}");
            double Curve(double r, double low, double high) => r <= 0.5 ? low : r >= 0.8 ? high : low + (r - 0.5) / 0.3 * (high - low);
            var zones = c.RoofAreaCalculator.effWindAreas;
            PressureData Neg(int key) => c.windPressureRoof_Neg_External[key];
            int nZone2eOnLength = 0, nZone2eOnWidth = 0;
            foreach (var kv in zones)
            {
                string label = kv.Value.Label_Full;
                var cen = kv.Value.Centroid;
                if (label == "Zone3")
                    Assert.AreEqual(Curve(r40, -1.8, -2.6), Neg(kv.Key).GCp, 1e-6, "Zone 3: the least dimension");
                else if (label == "Zone2e")
                {
                    bool onLengthEave = System.Math.Min(cen.Y, 60 - cen.Y) <= System.Math.Min(cen.X, 40 - cen.X);
                    if (onLengthEave) { nZone2eOnLength++; Assert.AreEqual(Curve(r40, -1.8, -2.6), Neg(kv.Key).GCp, 1e-6, "2e on a 40 ft eave"); }
                    else { nZone2eOnWidth++; Assert.AreEqual(-1.8, Neg(kv.Key).GCp, 1e-6, "2e on a 60 ft eave: h/60 ≤ 0.5"); }
                    StringAssert.Contains(c.HipHOverBByZone[kv.Key], onLengthEave ? "/ 40" : "/ 60");
                }
                else if (label == "Zone2r")
                    Assert.AreEqual(-2.4, Neg(kv.Key).GCp, 1e-6, "2r: -2.4 at 10 sq ft on both curves");
            }
            Assert.IsTrue(nZone2eOnLength > 0 && nZone2eOnWidth > 0, "both kinds of eave");
            Assert.AreEqual(Neg(1).qh * Neg(1).GCp * c.PressureKd, Neg(1).ExternalPressure, 1e-9, "the pressure from the zone's own GCp");
        }

        /// <summary>
        /// Fig. 27.3-1's leeward wall Cp by L/B, L along the wind and B normal to it (26.3), for each wind direction: a 60 x 40
        /// ft building, wind along the 60 ft side L/B = 1.5 (-0.4); along the 40 ft side L/B = 0.67 (-0.5).
        /// </summary>
        [DataTestMethod]
        [DataRow(ASCE7_Versions.ASCE_VER_7_16)]
        [DataRow(ASCE7_Versions.ASCE_VER_7_22)]
        public void Mwfrs_LeewardWallCp_ByEachWindDirectionsLOverB(ASCE7_Versions v)
        {
            var b = Bldg.Make(60, 40, 15, RoofTypes.ROOF_TYPE_GABLE, 20, RidgeDirections.RIDGE_DIR_PARALLEL_TO_BLDGLENGTH);
            var a = WindAnalysis.Run(v, new WindParameters_Base(), b);
            var x = (WindLoadCalculator_MWFRS)a.MwfrsAlongLength;
            var y = (WindLoadCalculator_MWFRS)a.MwfrsAlongWidth;
            Assert.AreEqual(60, x.buildingData.BuildingLength, 1e-9, "L along the wind");
            Assert.AreEqual(40, x.buildingData.BuildingWidth, 1e-9, "B normal to it");
            Assert.AreEqual(1.5, x.buildingData.L_Over_B, 1e-9);
            Assert.AreEqual(-0.4, x.Cp_LW, 1e-9);
            Assert.AreEqual(40, y.buildingData.BuildingLength, 1e-9, "the other direction: L the 40 ft side");
            Assert.AreEqual(60, y.buildingData.BuildingWidth, 1e-9);
            Assert.AreEqual(-0.5, y.Cp_LW, 1e-9, "L/B = 0.67 ≤ 1");
            Assert.AreEqual(y.buildingData.MwfrsFigureHeight / 40, y.buildingData.h_Over_L, 1e-9, "h/L with that direction's L");
        }

        [DataTestMethod]
        [DataRow(ASCE7_Versions.ASCE_VER_7_16)]
        [DataRow(ASCE7_Versions.ASCE_VER_7_22)]
        public void Walls_LowSlopeOver300Ft_ZoneWidthAtMost0_8h(ASCE7_Versions v)
        {
            // 400 x 350 ft, flat, 12 ft: a = max(min(4.8, 35), max(14, 3)) = 14 ft; the walls' exception: at most 0.8h = 9.6 ft
            var c = Cc(v, 400, 350, 12, RoofTypes.ROOF_TYPE_FLAT, 0);
            Assert.AreEqual(9.6, c.WallAreaCalculator_BldgLength.CritDim_a, 1e-9);
            Assert.AreEqual(9.6, c.WallAreaCalculator_BldgWidth.CritDim_a, 1e-9);
            Assert.AreEqual(14.0, c.RoofAreaCalculator.CritDim_a, 1e-9, "the walls figure's exception only");
            // not over 300 ft: no exception
            var small = Cc(v, 300, 300, 12, RoofTypes.ROOF_TYPE_FLAT, 0);
            Assert.AreEqual(12.0, small.WallAreaCalculator_BldgLength.CritDim_a, 1e-9, "4% of 300");
            // steeper than 7°: no exception
            var gable = Cc(v, 400, 350, 12, RoofTypes.ROOF_TYPE_GABLE, 8);
            Assert.AreEqual(14.0, gable.WallAreaCalculator_BldgLength.CritDim_a, 1e-9);
        }
    }
}
