using System;
using System.Linq;
using ASCE7WindLoadCalculator;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ASCE7Wind.Tests
{
    /// <summary>Zone layouts must follow the real roof geometry (ridge direction, hip planes, low slopes).</summary>
    [TestClass]
    public class ZoningOrientationTests
    {
        /// <summary>
        /// The C&amp;C ridge strip (width 2a centered on the ridge) must lie along the ridge the user chose.
        /// Zones entirely inside that band must cover it completely.
        /// </summary>
        [DataTestMethod]
        [DataRow(ASCE7_Versions.ASCE_VER_7_16, RidgeDirections.RIDGE_DIR_PARALLEL_TO_BLDGLENGTH, 15.0)]
        [DataRow(ASCE7_Versions.ASCE_VER_7_16, RidgeDirections.RIDGE_DIR_PERP_TO_BLDGLENGTH, 15.0)]
        [DataRow(ASCE7_Versions.ASCE_VER_7_16, RidgeDirections.RIDGE_DIR_PERP_TO_BLDGLENGTH, 25.0)]
        [DataRow(ASCE7_Versions.ASCE_VER_7_22, RidgeDirections.RIDGE_DIR_PARALLEL_TO_BLDGLENGTH, 15.0)]
        [DataRow(ASCE7_Versions.ASCE_VER_7_22, RidgeDirections.RIDGE_DIR_PERP_TO_BLDGLENGTH, 15.0)]
        [DataRow(ASCE7_Versions.ASCE_VER_7_22, RidgeDirections.RIDGE_DIR_PERP_TO_BLDGLENGTH, 25.0)]
        public void CladdingGable_RidgeStripFollowsRidge(ASCE7_Versions v, RidgeDirections ridge, double slope)
        {
            double L = 60, B = 40;
            var c = Bldg.Run(v, WindLoadCalculationTypes.COMPONENT_AND_CLADDING, Bldg.Make(L, B, 15, RoofTypes.ROOF_TYPE_GABLE, slope, ridge));
            var zones = (AreaCalculator_CC_Base)c.RoofAreaCalculator;
            double a = zones.CritDim_a;
            bool alongY = ridge == RidgeDirections.RIDGE_DIR_PERP_TO_BLDGLENGTH;
            double mid = alongY ? L / 2 : B / 2, span = alongY ? B : L;

            double inBand = zones.effWindAreas.Values
                .Where(z => z.OuterBoundary.All(p => Math.Abs((alongY ? p.X : p.Y) - mid) <= a + 1e-9))
                .Sum(z => z.Area);
            Assert.AreEqual(2 * a * span, inBand, 1e-6, "ridge strip must run along the ridge");
        }

        [DataTestMethod]
        [DataRow(RoofTypes.ROOF_TYPE_GABLE, RidgeDirections.RIDGE_DIR_PERP_TO_BLDGLENGTH)]
        [DataRow(RoofTypes.ROOF_TYPE_GABLE, RidgeDirections.RIDGE_DIR_PARALLEL_TO_BLDGLENGTH)]
        [DataRow(RoofTypes.ROOF_TYPE_HIP, RidgeDirections.RIDGE_DIR_NONE)]
        public void Mwfrs_LowSlope_UsesDistanceZonesEverywhere_WithPressures(RoofTypes roof, RidgeDirections ridge)
        {
            foreach (ASCE7_Versions v in Enum.GetValues(typeof(ASCE7_Versions)))
            {
                var c = Bldg.Run(v, WindLoadCalculationTypes.MWFRS, Bldg.Make(60, 40, 15, roof, 5, ridge));
                Assert.IsFalse(c.RoofAreaCalculator.effWindAreas.Values.Any(z => z.Label_Full == "ZoneWWR" || z.Label_Full == "ZoneLWR"),
                    "below 10 deg Fig. 27.3-1 uses the distance table");
                Assert.AreEqual(60 * 40, Bldg.SumArea(c.RoofAreaCalculator), 1e-6);
                foreach (var id in c.RoofAreaCalculator.effWindAreas.Keys)
                    Assert.IsTrue(c.windPressureRoof_Neg_External.ContainsKey(id) && c.windPressureRoof_Pos_External.ContainsKey(id));
            }
        }

        /// <summary>
        /// 30 x 30 ft square hip (pyramid), 15 deg: h = 17.01 ft. The 0-h/2 band cuts a triangle
        /// (h/2)^2/2 from each side slope; the hipped ends are windward/leeward roof.
        /// </summary>
        [TestMethod]
        public void Mwfrs_SquareHip_BandAreas()
        {
            var b = Bldg.Make(30, 30, 15, RoofTypes.ROOF_TYPE_HIP, 15);
            var c = Bldg.Run(ASCE7_Versions.ASCE_VER_7_16, WindLoadCalculationTypes.MWFRS, b);
            double h = b.MeanRoofHeight, x1 = h / 2;
            double zone4 = 2 * (x1 * x1 / 2);
            double zone3 = 2 * ((15 * 15 - x1 * x1) / 2 + (30 * (h - 15) - (h * h - 225) / 2));
            Assert.AreEqual(zone4, Bldg.Zones(c.RoofAreaCalculator, "Zone4").Sum(z => z.Area), 1e-6);
            Assert.AreEqual(zone3, Bldg.Zones(c.RoofAreaCalculator, "Zone3").Sum(z => z.Area), 1e-6);
            Assert.AreEqual(225, Bldg.Zones(c.RoofAreaCalculator, "ZoneWWR").Sum(z => z.Area), 1e-6, "west triangle, plan (30 x 15 / 2)");
            Assert.AreEqual(225, Bldg.Zones(c.RoofAreaCalculator, "ZoneLWR").Sum(z => z.Area), 1e-6, "east triangle, plan (30 x 15 / 2)");
        }
    }
}
