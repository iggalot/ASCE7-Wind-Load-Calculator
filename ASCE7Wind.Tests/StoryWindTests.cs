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
            Assert.AreEqual(l1.AlongLengthKips, c1.Vx, 1e-12);   // the calculated force (the minimum is its own case)
            var c2 = l2.LoadCases.First(c => c.Name == "Case 2, along length, + wind, + torsion");
            Assert.AreEqual(0.75 * l2.AlongLengthKips * 0.15 * B, c2.Mt, 1e-9, "0.75 V e, e = 0.15 B of the building");
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
            // each floor's cases: Case 1 on the calculated strip force, the minimum as its own case (C27.1.5)
            Assert.AreEqual(r.Levels[0].AlongLengthKips, r.Levels[0].LoadCases.First(c => c.CaseNumber == 1 && c.Name.Contains("along length")).Vx, 1e-12);
            var min0 = r.Levels[0].LoadCases.First(c => c.CaseNumber == WindLateralLoads.MinimumCaseNumber && c.Name.Contains("along length"));
            Assert.AreEqual(16 * B * 15 / 1000, min0.Vx, 1e-9);
            Assert.AreEqual(0, min0.Mt, 1e-12);
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
            var first = r.Building.LoadCases.First(c => c.Name.Contains("along length") && c.CaseNumber != WindLateralLoads.MinimumCaseNumber);
            Assert.AreEqual(first.Vx * 0.5, r.Levels[0].LoadCases.Single(c => c.Name == first.Name).Vx, 1e-9);
            Assert.AreEqual(first.Vx / 6, r.Levels[2].LoadCases.Single(c => c.Name == first.Name).Vx, 1e-9);
        }

        /// <summary>The calculator's own wind cases on each floor: every case on every floor under its own name, and the floors
        /// add up to the building's case (both chapters, 1 to 3 stories); one story is the building's case itself.</summary>
        [TestMethod]
        public void ComputeCases_EveryCalculatorCase_FloorsAddUp_BothChapters()
        {
            foreach (var proc in new[] { LateralProcedure.Directional, LateralProcedure.Envelope })
                foreach (int n in new[] { 1, 2, 3 })
                {
                    var a = Analysis(Bldg.Make(60, 40, 30, RoofTypes.ROOF_TYPE_GABLE, 20, RidgeDirections.RIDGE_DIR_PARALLEL_TO_BLDGLENGTH), procedure: proc);
                    var r = StoryWindLoads.ComputeCases(a, false, n);
                    var all = WindCases.For(a);
                    Assert.AreEqual(proc, r.Procedure);
                    Assert.AreEqual(n, r.Levels.Count);
                    CollectionAssert.AreEqual(all.Select(c => c.Name).ToList(), r.Cases.Select(c => c.Name).ToList());
                    for (int k = 0; k < all.Count; k++)
                    {
                        var c = all[k];
                        Assert.IsTrue(r.Levels.All(l => l[k].Name == c.Name && l[k].CaseNumber == c.FigureCase), c.Name);
                        Assert.AreEqual(c.Vx, r.Levels.Sum(l => l[k].Vx), 1e-9, $"{proc} {n}: {c.Name}");
                        Assert.AreEqual(c.Vy, r.Levels.Sum(l => l[k].Vy), 1e-9, $"{proc} {n}: {c.Name}");
                        Assert.AreEqual(c.Mt, r.Levels.Sum(l => l[k].Mt), 1e-9, $"{proc} {n}: {c.Name}");
                    }
                }
        }

        /// <summary>
        /// By hand, a gable 60 × 40 ft, 20 ft walls, two 10 ft stories: level 1 takes the walls from 0 to 15 ft (qz up the windward
        /// wall), the roof the rest of the walls and all of the roof. Case 1 from W, roof Case A: level 1 Vx = the wall force on
        /// 0-15 ft. Case 2: 0.75 of that, Mt = 0.75 × 0.15 B × that force. The minimum from W: 16 psf × B × 15 ft on level 1.
        /// </summary>
        [TestMethod]
        public void ComputeCases_TwoStories_ByHand()
        {
            double B = 40;
            var a = Analysis(Bldg.Make(60, B, 20, RoofTypes.ROOF_TYPE_GABLE, 25, RidgeDirections.RIDGE_DIR_PERP_TO_BLDGLENGTH));
            var r = StoryWindLoads.ComputeCases(a, false, 2);
            double walls1 = MwfrsLateralLoad.WallShearKipsBetween(a.MwfrsAlongLength, 0, 15);
            int Of(string name) => r.Cases.FindIndex(c => c.Name == name);

            int c1 = Of("Case 1, from W, roof Case A, +GCpi");
            Assert.AreEqual(walls1, r.Levels[0][c1].Vx, 1e-9);
            Assert.AreEqual(r.Cases[c1].Vx - walls1, r.Levels[1][c1].Vx, 1e-9, "the roof level: the rest of the walls and the roof");
            Assert.AreEqual(0, r.Levels[0][c1].Vy, 1e-12);

            int e1 = Of("Case 1, from E, roof Case A, +GCpi");
            Assert.AreEqual(-walls1, r.Levels[0][e1].Vx, 1e-9, "from E: the same, toward −x");

            int c2 = Of("Case 2, from W, roof Case A, +GCpi, torsion +");
            Assert.AreEqual(0.75 * walls1, r.Levels[0][c2].Vx, 1e-9);
            Assert.AreEqual(0.75 * walls1 * 0.15 * B, r.Levels[0][c2].Mt, 1e-9, "0.75 V e on the floor's own force, e = 0.15 B");

            int m = r.Cases.FindIndex(c => c.IsMinimumCase && c.From == "W");
            Assert.AreEqual(16 * B * 15 / 1000, r.Levels[0][m].Vx, 1e-9, "16 psf on the 0-15 ft strip");
        }

        /// <summary>Plate heights per story: the eave height is their sum, and the floors sit at the running sums.</summary>
        [TestMethod]
        public void PlateHeights_EaveIsTheSum_ElevationsAreRunningSums()
        {
            var b = Bldg.Make(60, 40, 30);
            b.NumberOfStories = 3;
            Assert.AreEqual(10, b.StoryPlateHeight(2), 1e-12, "not given: the eave split equally");
            b.PlateHeights = new System.Collections.Generic.List<double> { 12, 10, 9 };
            Assert.AreEqual(31, b.BuildingHeight, 1e-12);
            CollectionAssert.AreEqual(new[] { 12.0, 22.0, 31.0 }, b.StoryElevations);
            Assert.IsTrue(b.UnequalStories);
            var c = b.Clone();
            c.PlateHeights = new System.Collections.Generic.List<double> { 8 };
            Assert.AreEqual(31, b.BuildingHeight, 1e-12, "a clone does not share the plate heights");
            Assert.AreEqual(24, c.BuildingHeight, 1e-12, "fewer entries than stories repeat the last one");
            b.NumberOfStories = 2;
            Assert.AreEqual(22, b.BuildingHeight, 1e-12, "only the stories in use count");
            b.BuildingHeight = 20;
            Assert.IsNull(b.PlateHeights, "entering the eave height clears the plate heights");
            Assert.AreEqual(10, b.StoryPlateHeight(1), 1e-12);
        }

        /// <summary>Unequal stories: each strip runs from mid-height of the story below to mid-height of the story above.</summary>
        [TestMethod]
        public void Strips_UnequalPlateHeights()
        {
            var s = StoryWindLoads.Strips(new[] { 12.0, 10.0, 8.0 });
            Assert.AreEqual((0.0, 17.0, 12.0), s[0]);
            Assert.AreEqual((17.0, 26.0, 22.0), s[1]);
            Assert.AreEqual(26.0, s[2].bottom, 1e-12);
            Assert.IsTrue(double.IsPositiveInfinity(s[2].top));
            Assert.AreEqual(30.0, s[2].elevation, 1e-12);
        }

        /// <summary>The building's plate heights drive the floor forces, and the floors still add up to the base shear.</summary>
        [TestMethod]
        public void Compute_UsesThePlateHeights_FloorsAddUp()
        {
            var b = Bldg.Make(60, 40, 30);
            b.NumberOfStories = 2;
            b.PlateHeights = new System.Collections.Generic.List<double> { 18, 12 };
            var r = StoryWindLoads.Compute(Analysis(b), false, 2);
            Assert.AreEqual(24.0, r.Levels[0].StripTop, 1e-12, "18 + 12/2");
            Assert.AreEqual(18.0, r.Levels[0].Elevation, 1e-12);
            Assert.AreEqual(r.Building.AlongLength.DesignKips, r.Levels.Sum(l => l.AlongLengthDesignKips), 1e-6);
            Assert.AreEqual(r.Building.AlongWidth.DesignKips, r.Levels.Sum(l => l.AlongWidthDesignKips), 1e-6);
        }
    }
}
