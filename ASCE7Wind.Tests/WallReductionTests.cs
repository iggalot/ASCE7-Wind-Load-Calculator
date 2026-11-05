using ASCE7WindLoadCalculator;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ASCE7Wind.Tests
{
    /// <summary>
    /// Fig. 30.3-1 note 5 (7-16 and 7-22): values of (GCp) for walls are reduced by 10% when the roof angle θ ≤ 10°.
    /// Figure values: Zones 4 and 5 positive +1.0 at 10 sq ft, +0.7 at 500 sq ft; Zone 4 negative -1.1 / -0.8, Zone 5
    /// -1.4 / -0.8.
    /// </summary>
    [TestClass]
    public class WallReductionTests
    {
        private static WindLoadCalculator_CC Cc(ASCE7_Versions v, RoofTypes roof, double slope) =>
            (WindLoadCalculator_CC)Bldg.Run(v, WindLoadCalculationTypes.COMPONENT_AND_CLADDING,
                Bldg.Make(60, 40, 15, roof, slope, RidgeDirections.RIDGE_DIR_PARALLEL_TO_BLDGLENGTH));

        [DataTestMethod]
        [DataRow(ASCE7_Versions.ASCE_VER_7_16, RoofTypes.ROOF_TYPE_FLAT, 0.0)]
        [DataRow(ASCE7_Versions.ASCE_VER_7_22, RoofTypes.ROOF_TYPE_FLAT, 0.0)]
        [DataRow(ASCE7_Versions.ASCE_VER_7_16, RoofTypes.ROOF_TYPE_GABLE, 10.0)]
        [DataRow(ASCE7_Versions.ASCE_VER_7_22, RoofTypes.ROOF_TYPE_GABLE, 10.0)]
        public void RoofAt10DegOrLess_WallsReduced10Percent(ASCE7_Versions v, RoofTypes roof, double slope)
        {
            var c = Cc(v, roof, slope);
            Assert.IsTrue(c.WallReductionApplied);
            var w = c.extGCpCurve_Wall;
            Assert.AreEqual(0.90, w.WallCurves_Pos["Zone4"].Evaluate(10), 1e-9);
            Assert.AreEqual(0.90, w.WallCurves_Pos["Zone5"].Evaluate(10), 1e-9);
            Assert.AreEqual(0.63, w.WallCurves_Pos["Zone4"].Evaluate(500), 1e-9);
            Assert.AreEqual(-0.99, w.WallCurves_Neg["Zone4"].Evaluate(10), 1e-9);
            Assert.AreEqual(-1.26, w.WallCurves_Neg["Zone5"].Evaluate(10), 1e-9);
            Assert.AreEqual(-0.72, w.WallCurves_Neg["Zone5"].Evaluate(500), 1e-9);
            // the wall pressure tables use the reduced values
            foreach (var kv in c.WallAreaCalculator_BldgLength.effWindAreas)
                if (kv.Value.Label_Full == "Zone5")
                    Assert.AreEqual(w.WallCurves_Neg["Zone5"].Evaluate(c.WallComponentArea), c.windPressureBuildingLengthWall_Neg_External[kv.Key].GCp, 1e-9);
        }

        [DataTestMethod]
        [DataRow(ASCE7_Versions.ASCE_VER_7_16)]
        [DataRow(ASCE7_Versions.ASCE_VER_7_22)]
        public void RoofSteeperThan10Deg_WallsUnreduced(ASCE7_Versions v)
        {
            var c = Cc(v, RoofTypes.ROOF_TYPE_GABLE, 15);
            Assert.IsFalse(c.WallReductionApplied);
            Assert.AreEqual(1.0, c.extGCpCurve_Wall.WallCurves_Pos["Zone4"].Evaluate(10), 1e-9);
            Assert.AreEqual(-1.4, c.extGCpCurve_Wall.WallCurves_Neg["Zone5"].Evaluate(10), 1e-9);
        }
    }
}
