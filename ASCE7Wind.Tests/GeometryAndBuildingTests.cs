using System;
using System.Collections.Generic;
using System.Linq;
using ASCE7WindLoadCalculator;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ASCE7Wind.Tests
{
    /// <summary>Building geometry (ASCE 7 26.2 definitions) and zone polygon math.</summary>
    [TestClass]
    public class GeometryAndBuildingTests
    {
        private static double Tan(double deg) => Math.Tan(deg * Math.PI / 180);

        // ---- 26.2: mean roof height h = average of eave and ridge heights ---------------------------

        [TestMethod]
        public void MeanRoofHeight_Flat_IsEaveHeight()
        {
            var b = Bldg.Make(60, 40, 15);
            Assert.AreEqual(15, b.MeanRoofHeight, Tol.Tight);
            Assert.AreEqual(15, b.RidgeHeight, Tol.Tight);
        }

        [TestMethod]
        public void MeanRoofHeight_Gable_UsesSpanAcrossRidge()
        {
            var parallel = Bldg.Make(60, 40, 15, RoofTypes.ROOF_TYPE_GABLE, 20, RidgeDirections.RIDGE_DIR_PARALLEL_TO_BLDGLENGTH);
            Assert.AreEqual(15 + Tan(20) * 20, parallel.RidgeHeight, 1e-9);
            Assert.AreEqual(15 + Tan(20) * 20 / 2, parallel.MeanRoofHeight, 1e-9);

            var perp = Bldg.Make(60, 40, 15, RoofTypes.ROOF_TYPE_GABLE, 20, RidgeDirections.RIDGE_DIR_PERP_TO_BLDGLENGTH);
            Assert.AreEqual(15 + Tan(20) * 30, perp.RidgeHeight, 1e-9);
        }

        [TestMethod]
        public void MeanRoofHeight_Hip_UsesShorterSide()
        {
            var b = Bldg.Make(60, 40, 15, RoofTypes.ROOF_TYPE_HIP, 20);
            Assert.AreEqual(15 + Tan(20) * 20, b.RidgeHeight, 1e-9);
        }

        // ---- Ridge direction rules, independent of property assignment order ------------------------

        [TestMethod]
        public void RidgeDirection_FlatRoof_IsAlwaysNone()
        {
            var b = new BuildingData { RidgeDirection = RidgeDirections.RIDGE_DIR_PERP_TO_BLDGLENGTH, RoofType = RoofTypes.ROOF_TYPE_FLAT };
            Assert.AreEqual(RidgeDirections.RIDGE_DIR_NONE, b.RidgeDirection);
        }

        [TestMethod]
        public void RidgeDirection_Hip_FollowsLongerSide()
        {
            var b = new BuildingData { RoofType = RoofTypes.ROOF_TYPE_HIP, BuildingLength = 30, BuildingWidth = 50 };
            Assert.AreEqual(RidgeDirections.RIDGE_DIR_PERP_TO_BLDGLENGTH, b.RidgeDirection);
            b.BuildingLength = 80;
            Assert.AreEqual(RidgeDirections.RIDGE_DIR_PARALLEL_TO_BLDGLENGTH, b.RidgeDirection);
        }

        [TestMethod]
        public void RidgeDirection_Gable_KeepsUserChoice_RegardlessOfAssignmentOrder()
        {
            var a = new BuildingData { RidgeDirection = RidgeDirections.RIDGE_DIR_PERP_TO_BLDGLENGTH, RoofType = RoofTypes.ROOF_TYPE_GABLE };
            var b = new BuildingData { RoofType = RoofTypes.ROOF_TYPE_GABLE, RidgeDirection = RidgeDirections.RIDGE_DIR_PERP_TO_BLDGLENGTH };
            Assert.AreEqual(RidgeDirections.RIDGE_DIR_PERP_TO_BLDGLENGTH, a.RidgeDirection);
            Assert.AreEqual(RidgeDirections.RIDGE_DIR_PERP_TO_BLDGLENGTH, b.RidgeDirection);
        }

        [TestMethod]
        public void RidgeDirection_Gable_DefaultsAlongLongerSide()
        {
            var b = new BuildingData { RoofType = RoofTypes.ROOF_TYPE_GABLE, BuildingLength = 60, BuildingWidth = 40 };
            Assert.AreEqual(RidgeDirections.RIDGE_DIR_PARALLEL_TO_BLDGLENGTH, b.RidgeDirection);
        }

        [TestMethod]
        public void Clone_And_Rotate()
        {
            var b = Bldg.Make(60, 40, 15, RoofTypes.ROOF_TYPE_GABLE, 20, RidgeDirections.RIDGE_DIR_PERP_TO_BLDGLENGTH, BuildingEnclosures.BLDG_PARTIALLY_ENCLOSED);
            var c = b.Clone();
            Assert.AreEqual(b.RidgeDirection, c.RidgeDirection);
            Assert.AreEqual(b.MeanRoofHeight, c.MeanRoofHeight, Tol.Tight);
            Assert.AreEqual(b.EnclosureType, c.EnclosureType);

            c.RotateBuilding();
            Assert.AreEqual(40, c.BuildingLength);
            Assert.AreEqual(60, c.BuildingWidth);
            Assert.AreEqual(RidgeDirections.RIDGE_DIR_PARALLEL_TO_BLDGLENGTH, c.RidgeDirection);
            Assert.AreEqual(b.RidgeHeight, c.RidgeHeight, 1e-9, "rotation must not change the physical building");
        }

        // ---- Polygons -------------------------------------------------------------------------------

        private static List<Vec2> Rect(double x0, double y0, double x1, double y1) =>
            new List<Vec2> { new Vec2(x0, y0), new Vec2(x1, y0), new Vec2(x1, y1), new Vec2(x0, y1) };

        [TestMethod]
        public void Polygon_AreaAndCentroid()
        {
            var z = new EffectiveWindArea("Zone1", Rect(0, 0, 10, 4));
            Assert.AreEqual(40, z.Area, Tol.Tight);
            Assert.AreEqual(5, z.Centroid.X, Tol.Tight);
            Assert.AreEqual(2, z.Centroid.Y, Tol.Tight);
        }

        [TestMethod]
        public void Polygon_WithHole()
        {
            var z = new EffectiveWindArea("Zone1", Rect(0, 0, 10, 10), new[] { Rect(0, 0, 2, 2) });
            Assert.AreEqual(96, z.Area, Tol.Tight);
            // centroid = (100*(5,5) - 4*(1,1)) / 96
            Assert.AreEqual((500 - 4) / 96.0, z.Centroid.X, 1e-12);
            Assert.AreEqual((500 - 4) / 96.0, z.Centroid.Y, 1e-12);
        }

        [TestMethod]
        public void Polygon_ClockwiseInput_IsNormalized()
        {
            var cw = Rect(0, 0, 10, 4);
            cw.Reverse();
            var z = new EffectiveWindArea("Zone1", cw);
            Assert.AreEqual(40, z.Area, Tol.Tight);
        }

        [TestMethod]
        public void Polygon_Degenerate_Throws()
        {
            Assert.Throws<ArgumentException>(() =>
                new EffectiveWindArea("Zone1", new[] { new Vec2(0, 0), new Vec2(1, 1), new Vec2(0, 0) }));
        }

        [TestMethod]
        public void Label_ShortForm()
        {
            Assert.AreEqual("2e", new EffectiveWindArea("Zone2e", Rect(0, 0, 1, 1)).Label_Short);
        }

        // ---- Net pressure sign convention -----------------------------------------------------------

        [TestMethod]
        public void NetPressure_CombinesWithTheWorseInternalPressure()
        {
            Assert.AreEqual(-13, new PressureData { ExternalPressure = -10, InternalPressure = 3 }.NetPressure, Tol.Tight);
            Assert.AreEqual(13, new PressureData { ExternalPressure = 10, InternalPressure = 3 }.NetPressure, Tol.Tight);
        }
    }

    /// <summary>MWFRS lateral load summary that feeds the shear-wall calculator.</summary>
    [TestClass]
    public class MwfrsBaseShearTests
    {
        /// <summary>
        /// 60 x 40 x 15 ft flat box, V = 115 mph, Exp C, ASCE 7-16, wind along the 60 ft length:
        /// qh = 24.43 psf; WW = qh G (0.8); LW = qh G (-0.4) for L/B = 1.5; face area 40 x 15 = 600 sf.
        /// Base shear = (16.61 + 8.31) x 600 = 14.95 kips. Internal pressure cancels on a closed box.
        /// </summary>
        [DataTestMethod]
        [DataRow(BuildingEnclosures.BLDG_ENCLOSED)]
        [DataRow(BuildingEnclosures.BLDG_PARTIALLY_ENCLOSED)]
        public void BaseShear_FlatBox_Kips_InternalPressureCancels(BuildingEnclosures enc)
        {
            var c = Bldg.Run(ASCE7_Versions.ASCE_VER_7_16, WindLoadCalculationTypes.MWFRS, Bldg.Make(60, 40, 15, enclosure: enc));
            double qh = c.CalculateDynamicWindPressure(15);
            double expectedLb = (qh * 0.85 * 0.8 + qh * 0.85 * 0.4) * 600;
            Assert.AreEqual(expectedLb / 1000.0, MwfrsLateralLoad.WallBaseShearKips(c), 1e-9);
            Assert.AreEqual(14.95, MwfrsLateralLoad.WallBaseShearKips(c), 0.01);
        }

        [TestMethod]
        public void BaseShear_RequiresMwfrsCalculator()
        {
            var cc = Bldg.Run(ASCE7_Versions.ASCE_VER_7_16, WindLoadCalculationTypes.COMPONENT_AND_CLADDING, Bldg.Make(60, 40, 15));
            Assert.Throws<ArgumentException>(() => MwfrsLateralLoad.WallBaseShearKips(cc));
        }
    }

    /// <summary>Performance guard: a full building analysis must stay well under interactive budgets.</summary>
    [TestClass]
    public class PerformanceTests
    {
        [TestMethod, TestCategory("Performance")]
        public void FullMatrix_RunsFast()
        {
            GoldenMaster.Capture(); // JIT warm-up
            var (_, ms, runs, _) = GoldenMaster.Capture();
            double perRunMs = (double)ms / runs;
            Console.WriteLine($"{runs} analyses in {ms} ms = {1000 * perRunMs:F0} us each");
            Assert.IsTrue(perRunMs < 2.0, $"{perRunMs:F3} ms per analysis exceeds the 2 ms budget");
        }
    }
}
