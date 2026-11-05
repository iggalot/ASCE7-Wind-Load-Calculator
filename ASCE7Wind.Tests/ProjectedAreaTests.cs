using System;
using ASCE7WindLoadCalculator;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ASCE7Wind.Tests
{
    /// <summary>
    /// The areas projected normal to the wind (along +x) for the 16/8 psf minimum (27.1.5), by hand. Eave h = 10 ft, 30° roofs
    /// (tan 30° = 0.57735). The heights across the width are straight lines between break points, so the sums are exact.
    /// </summary>
    [TestClass]
    public class ProjectedAreaTests
    {
        private static readonly double Tan30 = Math.Tan(30 * Math.PI / 180);

        private static (double wall, double roof) Areas(BuildingData b) => WindLateralLoads.ProjectedAreas(new WindGeometry(b));

        [TestMethod]
        public void Flat_WallOnly()
        {
            var (wall, roof) = Areas(Bldg.Make(60, 40, 10));
            Assert.AreEqual(10 * 40, wall, 1e-9);
            Assert.AreEqual(0, roof, 1e-9);
        }

        /// <summary>Ridge along the length (parallel to the wind): the windward wall is a gable end, h B + tan θ B² / 4; no roof above it.</summary>
        [TestMethod]
        public void Gable_RidgeAlongTheWind_GableEndIsWall()
        {
            var (wall, roof) = Areas(Bldg.Make(60, 40, 10, RoofTypes.ROOF_TYPE_GABLE, 30, RidgeDirections.RIDGE_DIR_PARALLEL_TO_BLDGLENGTH));
            Assert.AreEqual(400 + Tan30 * 400, wall, 1e-9);   // 400 + 230.94 = 630.94 sq ft
            Assert.AreEqual(0, roof, 1e-9);
        }

        /// <summary>Ridge across the wind: the wall h B, the roof rising to tan θ × L/2 over the whole width.</summary>
        [TestMethod]
        public void Gable_RidgeAcrossTheWind_RoofAboveTheWall()
        {
            var (wall, roof) = Areas(Bldg.Make(60, 40, 10, RoofTypes.ROOF_TYPE_GABLE, 30, RidgeDirections.RIDGE_DIR_PERP_TO_BLDGLENGTH));
            Assert.AreEqual(400, wall, 1e-9);
            Assert.AreEqual(Tan30 * 30 * 40, roof, 1e-9);     // 692.82 sq ft
        }

        /// <summary>
        /// Hip, 60 × 40: the silhouette is tan θ × min(L/2, y, B − y), a triangle (L/2 = 30 never governs): tan θ × B² / 4 = 230.94.
        /// Hip, 20 × 40: min(10, y, 40 − y) is a trapezoid, 10² + 10 × 20 = 300, × tan θ = 173.21.
        /// </summary>
        [TestMethod]
        public void Hip_SilhouetteTriangleOrTrapezoid()
        {
            var (wall, roof) = Areas(Bldg.Make(60, 40, 10, RoofTypes.ROOF_TYPE_HIP, 30));
            Assert.AreEqual(400, wall, 1e-9);
            Assert.AreEqual(Tan30 * 400, roof, 1e-9);
            var (wall2, roof2) = Areas(Bldg.Make(20, 40, 10, RoofTypes.ROOF_TYPE_HIP, 30));
            Assert.AreEqual(400, wall2, 1e-9);
            Assert.AreEqual(Tan30 * 300, roof2, 1e-9);
        }
    }
}
