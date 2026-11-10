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
