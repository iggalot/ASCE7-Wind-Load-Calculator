using System;
using System.Linq;
using ASCE7WindLoadCalculator;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ASCE7Wind.Tests
{
    /// <summary>
    /// Wind on each diaphragm of a 1-3 story building: strips of wall from mid-story to mid-story (level 1 from the ground,
    /// the roof to the top with the roof and parapet forces), Fig. 27.3-8 cases per floor, Chapter 28 split by area.
    /// </summary>
    [TestClass]
    public class StoryWindTests
    {
        private static WindAnalysisResult Analysis(BuildingData b, double v = 115, LateralProcedure procedure = LateralProcedure.Directional) =>
            WindAnalysis.Run(ASCE7_Versions.ASCE_VER_7_22,
                new WindParameters_Base { WindSpeed = v, ExposureCategory = WindExposureCategories.WIND_EXP_CAT_C, LateralProcedure = procedure }, b);

        [TestMethod]
        public void Stories_Clamped_1To3_StoryHeight()
        {
            var b = Bldg.Make(60, 40, 30);
            Assert.AreEqual(1, b.NumberOfStories);
            b.NumberOfStories = 0; Assert.AreEqual(1, b.NumberOfStories);
            b.NumberOfStories = 7; Assert.AreEqual(3, b.NumberOfStories);
            Assert.AreEqual(10, b.StoryHeight, 1e-12);
        }

        [TestMethod]
        public void Strips_MidStoryToMidStory()
        {
            var s = StoryWindLoads.Strips(30, 3);
            Assert.AreEqual((0.0, 15.0, 10.0), s[0]);
            Assert.AreEqual((15.0, 25.0, 20.0), s[1]);
            Assert.AreEqual(25.0, s[2].bottom);
            Assert.IsTrue(double.IsPositiveInfinity(s[2].top));
            Assert.AreEqual(30.0, s[2].elevation);
        }

        [TestMethod]
        public void OneStory_SameLoadsAsTheBuilding_BothChapters()
        {
            foreach (var proc in new[] { LateralProcedure.Directional, LateralProcedure.Envelope })
            {
                var a = Analysis(Bldg.Make(60, 40, 15), procedure: proc);
                var r = StoryWindLoads.Compute(a, false, 1);
                var whole = WindLateralLoads.Compute(a, false);
                Assert.AreEqual(1, r.Levels.Count);
                Assert.AreEqual(whole.LoadCases.Count, r.Levels[0].LoadCases.Count, proc.ToString());
                for (int i = 0; i < whole.LoadCases.Count; i++)
                {
                    Assert.AreEqual(whole.LoadCases[i].Name, r.Levels[0].LoadCases[i].Name);
                    Assert.AreEqual(whole.LoadCases[i].Vx, r.Levels[0].LoadCases[i].Vx, 1e-9);
                    Assert.AreEqual(whole.LoadCases[i].Vy, r.Levels[0].LoadCases[i].Vy, 1e-9);
                    Assert.AreEqual(whole.LoadCases[i].Mt, r.Levels[0].LoadCases[i].Mt, 1e-9);
                }
            }
        }

        /// <summary>
        /// Flat 60 × 40 ft box, 30 ft high, two stories of 15 ft: level 1 takes 0 to 22.5 ft, the roof 22.5 to 30 ft.
        /// By hand: windward ∫ p(z) B dz (fine midpoint rule on p_WW(z) = qz Cp G Kd), leeward |p_LW| B Δz.
        /// </summary>
        [TestMethod]
        public void TwoStories_FlatBox_StripsByHand()
        {
            double L = 60, B = 40, H = 30;
            var a = Analysis(Bldg.Make(L, B, H));
            var mx = (WindLoadCalculator_MWFRS)a.MwfrsAlongLength;
            double Windward(double z0, double z1)
            {
                int n = 6000; double dz = (z1 - z0) / n, sum = 0;
                for (int i = 0; i < n; i++) sum += mx.WindwardWallExternalPressureAt(z0 + (i + 0.5) * dz) * B * dz;
                return sum / 1000;
            }
            double total = MwfrsLateralLoad.WallBaseShearKips(mx);
            double leewardPerFoot = (total - Windward(0, H)) / H; // kips per ft of height, uniform

            var r = StoryWindLoads.Compute(a, false, 2);
            Assert.AreEqual(15, r.StoryHeight, 1e-12);
            var l1 = r.Levels[0]; var l2 = r.Levels[1];
            Assert.AreEqual(Windward(0, 22.5) + leewardPerFoot * 22.5, l1.AlongLengthKips, 0.002 * total);
            Assert.AreEqual(Windward(22.5, 30) + leewardPerFoot * 7.5, l2.AlongLengthKips, 0.002 * total);
            Assert.AreEqual(total, l1.AlongLengthKips + l2.AlongLengthKips, 1e-9, "the strips add up to the base shear");
            Assert.IsTrue(l2.AlongLengthKips / 7.5 > l1.AlongLengthKips / 22.5, "qz grows with height: more per foot up top");

            // each floor gets every case with its own forces
            var whole = r.Building;
            Assert.AreEqual(whole.LoadCases.Count, l1.LoadCases.Count);
            var c1 = l1.LoadCases.First(c => c.Name == "Case 1, along length, + wind");
            Assert.AreEqual(l1.AlongLengthDesignKips, c1.Vx, 1e-12);
            var c2 = l2.LoadCases.First(c => c.Name == "Case 2, along length, + wind, + torsion");
            Assert.AreEqual(0.75 * l2.AlongLengthDesignKips * 0.15 * B, c2.Mt, 1e-9, "0.75 V e, e = 0.15 B of the building");
            foreach (var wc in whole.LoadCases)
            {
                var parts = r.Levels.Select(l => l.LoadCases.Single(c => c.Name == wc.Name)).ToList();
                Assert.AreEqual(wc.Vx, parts.Sum(p => p.Vx), 1e-9, wc.Name);
                Assert.AreEqual(wc.Vy, parts.Sum(p => p.Vy), 1e-9, wc.Name);
                Assert.AreEqual(wc.Mt, parts.Sum(p => p.Mt), 1e-9, wc.Name);
            }
        }

        [TestMethod]
        public void Gable_RoofForceGoesToTheRoofLevel()
        {
            var a = Analysis(Bldg.Make(60, 40, 20, RoofTypes.ROOF_TYPE_GABLE, 25, RidgeDirections.RIDGE_DIR_PERP_TO_BLDGLENGTH));
            var d = WindLateralLoads.DirectionalForce(a.MwfrsAlongLength, "x");
            var r = StoryWindLoads.Compute(a, false, 2);
            Assert.AreEqual(MwfrsLateralLoad.WallShearKipsBetween(a.MwfrsAlongLength, 0, 15), r.Levels[0].AlongLengthKips, 1e-12, "level 1: walls only");
            Assert.AreEqual(d.TotalKips, r.Levels.Sum(l => l.AlongLengthKips), 1e-9);
            Assert.AreEqual(MwfrsLateralLoad.WallShearKipsBetween(a.MwfrsAlongLength, 15, double.PositiveInfinity) + d.RoofKips + d.ParapetKips,
                r.Levels[1].AlongLengthKips, 1e-9);
        }

        /// <summary>27.1.5 governs at a low wind speed: 16 psf on each floor's wall strip, 8 psf roof at the roof.</summary>
        [TestMethod]
        public void Minimum_SplitByStrip()
        {
            double B = 40, H = 30;
            var a = Analysis(Bldg.Make(60, B, H), v: 85);
            var r = StoryWindLoads.Compute(a, false, 3);
            var whole = r.Building.AlongLength;
            Assert.IsTrue(whole.MinimumGoverns, "the minimum should govern at 85 mph for this test");
            Assert.AreEqual(16 * B * 15 / 1000, r.Levels[0].AlongLengthMinimumKips, 1e-9);
            Assert.AreEqual(16 * B * 10 / 1000, r.Levels[1].AlongLengthMinimumKips, 1e-9);
            Assert.AreEqual(16 * B * 5 / 1000, r.Levels[2].AlongLengthMinimumKips, 1e-9);
            Assert.AreEqual(whole.DesignKips, r.Levels.Sum(l => l.AlongLengthDesignKips), 1e-9);
            Assert.AreEqual(r.Levels[0].AlongLengthMinimumKips, r.Levels[0].AlongLengthDesignKips, 1e-12);
        }

        [TestMethod]
        public void Chapter28_CasesSplitByArea_AddUp()
        {
            var a = Analysis(Bldg.Make(60, 40, 30), procedure: LateralProcedure.Envelope);
            var r = StoryWindLoads.Compute(a, false, 3);
            Assert.AreEqual(LateralProcedure.Envelope, r.Building.Procedure);
            foreach (var wc in r.Building.LoadCases)
            {
                var parts = r.Levels.Select(l => l.LoadCases.Single(c => c.Name == wc.Name)).ToList();
                Assert.AreEqual(wc.Vx, parts.Sum(p => p.Vx), 1e-9, wc.Name);
                Assert.AreEqual(wc.Vy, parts.Sum(p => p.Vy), 1e-9, wc.Name);
                Assert.AreEqual(wc.Mt, parts.Sum(p => p.Mt), 1e-9, wc.Name);
            }
            // uniform zone pressures, flat roof: the wall strips are 15, 10 and 5 ft of 30
            var first = r.Building.LoadCases.First(c => c.Name.Contains("along length") && !c.Name.Contains("minimum"));
            Assert.AreEqual(first.Vx * 0.5, r.Levels[0].LoadCases.Single(c => c.Name == first.Name).Vx, 1e-9);
            Assert.AreEqual(first.Vx / 6, r.Levels[2].LoadCases.Single(c => c.Name == first.Name).Vx, 1e-9);
        }
    }
}
