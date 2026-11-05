using System;
using System.Linq;
using ASCE7WindLoadCalculator;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ASCE7Wind.Tests
{
    /// <summary>
    /// M6 lateral loads for the shear walls: roof horizontal components, the 27.1.5 minimum, and the
    /// Fig. 27.3-8 design wind load cases.
    /// </summary>
    [TestClass]
    public class LateralLoadTests
    {
        private static WindAnalysisResult Analysis(BuildingData b, ASCE7_Versions v = ASCE7_Versions.ASCE_VER_7_22) =>
            WindAnalysis.Run(v, new WindParameters_Base { WindSpeed = 115, ExposureCategory = WindExposureCategories.WIND_EXP_CAT_C }, b);

        private static double Tan(double d) => Math.Tan(d * Math.PI / 180);

        [TestMethod]
        public void FlatRoof_HasNoHorizontalRoofForce()
        {
            var f = WindLateralLoads.DirectionalForce(Analysis(Bldg.Make(60, 40, 15)).MwfrsAlongLength, "x");
            Assert.AreEqual(0, f.RoofKips, 1e-12);
            Assert.AreEqual(f.WallsKips, f.TotalKips, 1e-12);
        }

        /// <summary>
        /// Wind normal to the ridge: F = tan(theta) A_plan (p_windward - p_leeward) for each roof Cp case;
        /// the larger total governs.
        /// </summary>
        [TestMethod]
        public void GableNormalToRidge_RoofForce_MatchesHandCalc()
        {
            double L = 60, B = 40, slope = 25;
            var a = Analysis(Bldg.Make(L, B, 15, RoofTypes.ROOF_TYPE_GABLE, slope, RidgeDirections.RIDGE_DIR_PERP_TO_BLDGLENGTH));
            var c = a.MwfrsAlongLength;
            double Force(System.Collections.Generic.Dictionary<int, PressureData> p)
            {
                var zones = c.RoofAreaCalculator.effWindAreas;
                double pw = p[zones.Single(z => z.Value.Label_Full == "ZoneWWR").Key].ExternalPressure;
                double pl = p[zones.Single(z => z.Value.Label_Full == "ZoneLWR").Key].ExternalPressure;
                return Tan(slope) * (L / 2 * B) * (pw - pl) / 1000;
            }
            double best = Math.Max(Force(c.windPressureRoof_Neg_External), Force(c.windPressureRoof_Pos_External));
            var f = WindLateralLoads.DirectionalForce(c, "x");
            Assert.AreEqual(best, f.RoofKips, 1e-9);
            Assert.IsTrue(f.RoofKips > 0, "leeward suction and windward pressure both push downwind");
        }

        [TestMethod]
        public void GableParallelToRidge_RoofAddsNothingAlongTheRidge()
        {
            var a = Analysis(Bldg.Make(60, 40, 15, RoofTypes.ROOF_TYPE_GABLE, 25, RidgeDirections.RIDGE_DIR_PARALLEL_TO_BLDGLENGTH));
            Assert.AreEqual(0, WindLateralLoads.DirectionalForce(a.MwfrsAlongLength, "x").RoofKips, 1e-12);
        }

        /// <summary>27.1.5: 16 psf x wall area + 8 psf x roof area, both projected normal to the wind.</summary>
        [TestMethod]
        public void Minimum_FlatBox()
        {
            var f = WindLateralLoads.DirectionalForce(Analysis(Bldg.Make(60, 40, 15)).MwfrsAlongLength, "x");
            Assert.AreEqual(40 * 15, f.ProjectedWallArea, 1e-6);
            Assert.AreEqual(0, f.ProjectedRoofArea, 1e-6);
            Assert.AreEqual(16 * 600 / 1000.0, f.MinimumKips, 1e-6);
        }

        [TestMethod]
        public void Minimum_GableNormalToRidge_RoofSilhouette()
        {
            double L = 60, B = 40, H = 15, slope = 25;
            var f = WindLateralLoads.DirectionalForce(Analysis(Bldg.Make(L, B, H, RoofTypes.ROOF_TYPE_GABLE, slope, RidgeDirections.RIDGE_DIR_PERP_TO_BLDGLENGTH)).MwfrsAlongLength, "x");
            double rise = Tan(slope) * L / 2;
            Assert.AreEqual(B * H, f.ProjectedWallArea, 1e-6);
            Assert.AreEqual(B * rise, f.ProjectedRoofArea, 1e-6);
            Assert.AreEqual((16 * B * H + 8 * B * rise) / 1000, f.MinimumKips, 1e-6);
        }

        [TestMethod]
        public void Minimum_GableEnd_TriangleIsWall()
        {
            double L = 60, B = 40, H = 15, slope = 25;
            var f = WindLateralLoads.DirectionalForce(Analysis(Bldg.Make(L, B, H, RoofTypes.ROOF_TYPE_GABLE, slope, RidgeDirections.RIDGE_DIR_PARALLEL_TO_BLDGLENGTH)).MwfrsAlongLength, "x");
            Assert.AreEqual(B * H + 0.5 * B * (Tan(slope) * B / 2), f.ProjectedWallArea, 1e-3);
            Assert.AreEqual(0, f.ProjectedRoofArea, 1e-3);
        }

        [TestMethod]
        public void Minimum_Hip_RoofSilhouetteIsTrapezoid()
        {
            double L = 60, B = 40, H = 15, slope = 20;
            var f = WindLateralLoads.DirectionalForce(Analysis(Bldg.Make(L, B, H, RoofTypes.ROOF_TYPE_HIP, slope)).MwfrsAlongLength, "x");
            double rise = Tan(slope) * B / 2;
            // wind along the length sees the hip end: triangle of base B and height = rise
            Assert.AreEqual(0.5 * B * rise, f.ProjectedRoofArea, 1e-2);
        }

        [TestMethod]
        public void DesignForce_IsLargerOfCalculatedAndMinimum()
        {
            var f = WindLateralLoads.DirectionalForce(Analysis(Bldg.Make(60, 40, 15)).MwfrsAlongLength, "x");
            Assert.AreEqual(Math.Max(f.TotalKips, f.MinimumKips), f.DesignKips, 1e-12);

            // Low wind speed makes the minimum govern.
            var low = WindAnalysis.Run(ASCE7_Versions.ASCE_VER_7_22,
                new WindParameters_Base { WindSpeed = 85, ExposureCategory = WindExposureCategories.WIND_EXP_CAT_B }, Bldg.Make(60, 40, 15));
            var fl = WindLateralLoads.DirectionalForce(low.MwfrsAlongLength, "x");
            Assert.IsTrue(fl.MinimumGoverns);
            Assert.AreEqual(fl.MinimumKips, fl.DesignKips, 1e-12);
        }

        // ---- Fig. 27.3-8 load cases --------------------------------------------------------------

        [TestMethod]
        public void LoadCases_AllFour()
        {
            var b = Bldg.Make(60, 40, 15);
            var r = WindLateralLoads.Compute(Analysis(b));
            // Cases 1-4 are built on the calculated force (the minimum is a separate case, C27.1.5)
            double vx = r.AlongLength.TotalKips, vy = r.AlongWidth.TotalKips;
            Assert.AreEqual(40, r.AlongLength.WidthNormalToWind, 1e-12, "B for wind along x is the width");
            Assert.AreEqual(60, r.AlongWidth.WidthNormalToWind, 1e-12, "B for wind along y is the length");

            // The wind blows from either side along each axis (the 180° direction reverses the forces).
            var c1 = r.LoadCases.Where(c => c.CaseNumber == 1).ToList();
            Assert.AreEqual(4, c1.Count);
            foreach (int s in new[] { 1, -1 })
            {
                Assert.IsTrue(c1.Any(c => c.Vx == s * vx && c.Vy == 0 && c.Mt == 0));
                Assert.IsTrue(c1.Any(c => c.Vx == 0 && c.Vy == s * vy && c.Mt == 0));
            }

            // Case 2: each wind side with both torsion signs
            var c2 = r.LoadCases.Where(c => c.CaseNumber == 2).ToList();
            Assert.AreEqual(8, c2.Count);
            foreach (int s in new[] { 1, -1 })
                foreach (int t in new[] { 1, -1 })
                {
                    Assert.IsTrue(c2.Any(c => Math.Abs(c.Vx - s * 0.75 * vx) < 1e-12 && c.Vy == 0 && Math.Abs(c.Mt - t * 0.75 * vx * 0.15 * 40) < 1e-9));
                    Assert.IsTrue(c2.Any(c => c.Vx == 0 && Math.Abs(c.Vy - s * 0.75 * vy) < 1e-12 && Math.Abs(c.Mt - t * 0.75 * vy * 0.15 * 60) < 1e-9));
                }

            // Case 3: all four diagonal (oblique) directions
            var c3 = r.LoadCases.Where(c => c.CaseNumber == 3).ToList();
            Assert.AreEqual(4, c3.Count);
            foreach (int sx in new[] { 1, -1 })
                foreach (int sy in new[] { 1, -1 })
                    Assert.IsTrue(c3.Any(c => Math.Abs(c.Vx - sx * 0.75 * vx) < 1e-12 && Math.Abs(c.Vy - sy * 0.75 * vy) < 1e-12 && c.Mt == 0));

            // Case 4: four diagonal directions, each with the four torsion sign combinations
            var c4 = r.LoadCases.Where(c => c.CaseNumber == 4).ToList();
            Assert.AreEqual(16, c4.Count);
            double mMax = 0.563 * (vx * 0.15 * 40 + vy * 0.15 * 60);
            foreach (int sx in new[] { 1, -1 })
                foreach (int sy in new[] { 1, -1 })
                {
                    var q = c4.Where(c => Math.Abs(c.Vx - sx * 0.563 * vx) < 1e-12 && Math.Abs(c.Vy - sy * 0.563 * vy) < 1e-12).ToList();
                    Assert.AreEqual(4, q.Count);
                    Assert.AreEqual(mMax, q.Max(c => c.Mt), 1e-9);
                    Assert.AreEqual(-mMax, q.Min(c => c.Mt), 1e-9);
                }
        }

        [TestMethod]
        public void LoadCases_Exception_OnlyCases1And3()
        {
            var r = WindLateralLoads.Compute(Analysis(Bldg.Make(60, 40, 15)), onlyCases1And3: true);
            CollectionAssert.AreEquivalent(new[] { 1, 3, WindLateralLoads.MinimumCaseNumber }, r.LoadCases.Select(c => c.CaseNumber).Distinct().ToArray());
            Assert.IsTrue(r.LoadCases.All(c => c.Mt == 0));
        }

        [TestMethod]
        public void Minimum_IsASeparateLoadCase_C27_1_5()
        {
            // C27.1.5 (ASCE 7-16 and 7-22): the minimum is "applied as a separate load case in addition to the normal load
            // cases". 85 mph, Exp. B: the minimum is larger than the calculated wind, yet Cases 1-4 keep the calculated force.
            var low = WindAnalysis.Run(ASCE7_Versions.ASCE_VER_7_22,
                new WindParameters_Base { WindSpeed = 85, ExposureCategory = WindExposureCategories.WIND_EXP_CAT_B }, Bldg.Make(60, 40, 15));
            var r = WindLateralLoads.Compute(low);
            Assert.IsTrue(r.AlongLength.MinimumGoverns && r.AlongWidth.MinimumGoverns);
            double tx = r.AlongLength.TotalKips, ty = r.AlongWidth.TotalKips, ex = 0.15 * 40, ey = 0.15 * 60;
            double mx = r.AlongLength.MinimumKips, my = r.AlongWidth.MinimumKips;

            Assert.IsTrue(r.LoadCases.Any(c => c.CaseNumber == 1 && Math.Abs(c.Vx - tx) < 1e-12 && c.Vy == 0));
            Assert.IsTrue(r.LoadCases.Any(c => c.CaseNumber == 2 && Math.Abs(c.Vx - 0.75 * tx) < 1e-12 && Math.Abs(c.Mt - 0.75 * tx * ex) < 1e-9));
            Assert.IsTrue(r.LoadCases.Any(c => c.CaseNumber == 4 && Math.Abs(c.Mt - 0.563 * (tx * ex + ty * ey)) < 1e-9));

            // the minimum: 16 psf × 40 × 15 = 9.6 k along the length, 16 × 60 × 15 = 14.4 k along the width, either side, no torsion
            Assert.AreEqual(9.6, mx, 1e-9);
            Assert.AreEqual(14.4, my, 1e-9);
            var min = r.LoadCases.Where(c => c.CaseNumber == WindLateralLoads.MinimumCaseNumber).ToList();
            Assert.AreEqual(4, min.Count);
            foreach (int s in new[] { 1, -1 })
            {
                Assert.IsTrue(min.Any(c => c.Vx == s * mx && c.Vy == 0 && c.Mt == 0));
                Assert.IsTrue(min.Any(c => c.Vx == 0 && c.Vy == s * my && c.Mt == 0));
            }
            Assert.AreEqual(32 + 4, r.LoadCases.Count, "Cases 1-4 and the four minimum cases");
            Assert.AreEqual(mx, r.AlongLength.DesignKips, 1e-12, "DesignKips reports the larger of the two");
        }
    }
}
