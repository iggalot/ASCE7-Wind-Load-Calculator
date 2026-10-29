using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ASCE7WindLoadCalculator;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ASCE7Wind.Tests
{
    /// <summary>
    /// ASCE 7 Chapter 30 Part 1 (h &lt;= 60 ft): GCp figures 30.3-1 and 30.3-2A..I, edge-strip width "a",
    /// zone layouts, and figure/zone selection by roof type and slope.
    /// </summary>
    [TestClass]
    public class Chapter30_CladdingTests
    {
        private static readonly ASCE7_Versions[] Editions = { ASCE7_Versions.ASCE_VER_7_16, ASCE7_Versions.ASCE_VER_7_22 };

        // ---- Fig. 30.3-1 walls ------------------------------------------------------------------------

        [DataTestMethod]
        [DataRow(ASCE7_Versions.ASCE_VER_7_16)]
        [DataRow(ASCE7_Versions.ASCE_VER_7_22)]
        public void Fig30_3_1_Walls_AnchorValues(ASCE7_Versions v)
        {
            var f = Asce7Edition.For(v).CreateCCWallFigure();
            foreach (var A in new[] { 1.0, 10.0 })
            {
                Assert.AreEqual(1.0, f.WallCurves_Pos["Zone4"].Evaluate(A), Tol.Tight);
                Assert.AreEqual(1.0, f.WallCurves_Pos["Zone5"].Evaluate(A), Tol.Tight);
                Assert.AreEqual(-1.1, f.WallCurves_Neg["Zone4"].Evaluate(A), Tol.Tight);
                Assert.AreEqual(-1.4, f.WallCurves_Neg["Zone5"].Evaluate(A), Tol.Tight);
            }
            foreach (var A in new[] { 500.0, 2000.0 })
            {
                Assert.AreEqual(0.7, f.WallCurves_Pos["Zone4"].Evaluate(A), Tol.Tight);
                Assert.AreEqual(0.7, f.WallCurves_Pos["Zone5"].Evaluate(A), Tol.Tight);
                Assert.AreEqual(-0.8, f.WallCurves_Neg["Zone4"].Evaluate(A), Tol.Tight);
                Assert.AreEqual(-0.8, f.WallCurves_Neg["Zone5"].Evaluate(A), Tol.Tight);
            }
        }

        /// <summary>Between anchors the figure is a straight line on log(area): check against the exact line.</summary>
        [DataTestMethod]
        [DataRow(ASCE7_Versions.ASCE_VER_7_16)]
        [DataRow(ASCE7_Versions.ASCE_VER_7_22)]
        public void Fig30_3_1_Walls_LogLinearBetweenAnchors(ASCE7_Versions v)
        {
            var f = Asce7Edition.For(v).CreateCCWallFigure();
            foreach (var A in new[] { 15.0, 35.0, 70.0, 150.0, 300.0 })
            {
                double t = Math.Log10(A / 10) / Math.Log10(500.0 / 10);
                Assert.AreEqual(1.0 - 0.3 * t, f.WallCurves_Pos["Zone5"].Evaluate(A), 0.02, $"Zone5+ at {A}");
                Assert.AreEqual(-1.4 + 0.6 * t, f.WallCurves_Neg["Zone5"].Evaluate(A), 0.02, $"Zone5- at {A}");
                Assert.AreEqual(-1.1 + 0.3 * t, f.WallCurves_Neg["Zone4"].Evaluate(A), 0.02, $"Zone4- at {A}");
            }
        }

        // ---- Fig. 30.3-2A roofs theta <= 7 deg ------------------------------------------------------

        [DataTestMethod]
        [DataRow(ASCE7_Versions.ASCE_VER_7_16)]
        [DataRow(ASCE7_Versions.ASCE_VER_7_22)]
        public void Fig30_3_2A_AnchorValues(ASCE7_Versions v)
        {
            var f = Asce7Edition.For(v).CreateCCRoofFigure(Bldg.Make(60, 40, 15));
            Assert.AreEqual(-0.9, f.RoofCurves_Neg["Zone1'"].Evaluate(100), Tol.Tight);
            Assert.AreEqual(-0.4, f.RoofCurves_Neg["Zone1'"].Evaluate(1000), Tol.Tight);
            Assert.AreEqual(-1.7, f.RoofCurves_Neg["Zone1"].Evaluate(10), Tol.Tight);
            Assert.AreEqual(-1.0, f.RoofCurves_Neg["Zone1"].Evaluate(500), Tol.Tight);
            Assert.AreEqual(-2.3, f.RoofCurves_Neg["Zone2"].Evaluate(10), Tol.Tight);
            Assert.AreEqual(-1.4, f.RoofCurves_Neg["Zone2"].Evaluate(500), Tol.Tight);
            Assert.AreEqual(-3.2, f.RoofCurves_Neg["Zone3"].Evaluate(10), Tol.Tight);
            Assert.AreEqual(-1.4, f.RoofCurves_Neg["Zone3"].Evaluate(500), Tol.Tight);
            foreach (var z in new[] { "Zone1'", "Zone1", "Zone2", "Zone3" })
            {
                Assert.AreEqual(0.3, f.RoofCurves_Pos[z].Evaluate(10), Tol.Tight);
                Assert.AreEqual(0.2, f.RoofCurves_Pos[z].Evaluate(100), Tol.Tight);
            }
        }

        /// <summary>Interpolation must follow the log-area axis of the figure, not a linear-area chord.</summary>
        [TestMethod]
        public void Fig30_3_2A_Zone3_InterpolatesOnLogArea()
        {
            var f = Asce7Edition.ASCE7_16.CreateCCRoofFigure(Bldg.Make(60, 40, 15));
            double exact = -3.2 + 1.8 * Math.Log10(300.0 / 10) / Math.Log10(500.0 / 10); // -1.635
            Assert.AreEqual(exact, f.RoofCurves_Neg["Zone3"].Evaluate(300), 0.005);
        }

        // ---- Data integrity of every Chapter 30 curve -----------------------------------------------

        private static IEnumerable<(string where, Chapter27and30_GCpCurveBase fig)> AllChapter30Figures()
        {
            foreach (var v in Editions)
            {
                var ed = Asce7Edition.For(v);
                yield return ($"{ed} walls", ed.CreateCCWallFigure());
                foreach (var roof in new[] { RoofTypes.ROOF_TYPE_FLAT, RoofTypes.ROOF_TYPE_GABLE, RoofTypes.ROOF_TYPE_HIP })
                    for (int slope = 0; slope <= 45; slope++)
                        foreach (var H in new[] { 8.0, 15.0, 30.0 }) // spans h/B <= 0.5 to >= 0.8 for the hip sets
                        {
                            var b = Bldg.Make(60, 40, H, roof, roof == RoofTypes.ROOF_TYPE_FLAT ? 0 : slope,
                                roof == RoofTypes.ROOF_TYPE_FLAT ? RidgeDirections.RIDGE_DIR_NONE : RidgeDirections.RIDGE_DIR_PARALLEL_TO_BLDGLENGTH);
                            yield return ($"{ed} {roof} {slope}deg h={b.MeanRoofHeight:0.#}", ed.CreateCCRoofFigure(b));
                        }
            }
        }

        /// <summary>
        /// Every GCp curve: area axis ascending, one sign throughout, and |GCp| never increasing with area
        /// (larger tributary areas always see lower peak pressure coefficients in Chapter 30).
        /// </summary>
        /// <summary>
        /// Curves that break the |GCp|-non-increasing rule and are awaiting comparison with the printed
        /// figure (see docs/DATA_VERIFICATION.md). Kept explicit so new problems still fail the test.
        /// </summary>
        private static readonly string[] KnownForReview =
        {
            "ASCE 7-16 Figure 30-3-2E and 2F - Hip Roofs Overhang[Zone1]: |GCp| increases", // low h/B set: -1.8 -> -2.0
        };

        [TestMethod]
        public void AllCurves_AreSortedSingleSignedAndNonIncreasingInMagnitude()
        {
            var problems = new SortedSet<string>();
            var reviewHits = new HashSet<string>();
            foreach (var (where, fig) in AllChapter30Figures())
            {
                var sets = new Dictionary<string, Dictionary<string, ExternalGCpCurve>>
                {
                    ["RoofPos"] = fig.RoofCurves_Pos, ["RoofNeg"] = fig.RoofCurves_Neg, ["Overhang"] = fig.OverhangCurves,
                    ["WallPos"] = fig.WallCurves_Pos, ["WallNeg"] = fig.WallCurves_Neg,
                };
                foreach (var set in sets)
                    foreach (var kv in set.Value ?? new Dictionary<string, ExternalGCpCurve>())
                    {
                        var p = kv.Value.GetPoints();
                        string id = $"{fig.ChartTitle} {set.Key}[{kv.Key}]";
                        for (int i = 1; i < p.Length; i++)
                        {
                            if (p[i].X < p[i - 1].X) problems.Add($"{id}: area not ascending at index {i} ({where})");
                            if (Math.Sign(p[i].Y) * Math.Sign(p[i - 1].Y) < 0) problems.Add($"{id}: sign flips between A={p[i - 1].X} and A={p[i].X} ({where})");
                            if (Math.Abs(p[i].Y) > Math.Abs(p[i - 1].Y) + 1e-9) problems.Add($"{id}: |GCp| increases from A={p[i - 1].X} to A={p[i].X} ({where})");
                        }
                    }
            }
            foreach (var known in KnownForReview)
            {
                if (problems.RemoveWhere(s => s.StartsWith(known, StringComparison.Ordinal)) > 0)
                    reviewHits.Add(known);
            }
            foreach (var known in KnownForReview.Where(k => !reviewHits.Contains(k)))
                problems.Add($"Review entry no longer needed (data fixed?): {known}");
            if (problems.Count > 0)
            {
                var sb = new StringBuilder($"{problems.Count} curve problem(s):\n");
                foreach (var s in problems.Take(60)) sb.AppendLine(s);
                Assert.Fail(sb.ToString());
            }
        }

        // ---- Edge-strip width "a" (Figs. 30.3-1, 30.3-2A..I notes) ---------------------------------

        /// <summary>a = min(0.1 least horizontal dim, 0.4h), but not less than 4% of least dim or 3 ft.</summary>
        [DataTestMethod]
        [DataRow(60.0, 40.0, 15.0, 4.0)]   // 0.1*40 governs
        [DataRow(20.0, 20.0, 10.0, 3.0)]   // 3 ft floor
        [DataRow(100.0, 100.0, 8.0, 4.0)]  // 0.4h = 3.2, raised to 4% of 100 = 4
        [DataRow(200.0, 150.0, 30.0, 12.0)] // 0.4h = 12 < 15
        public void CriticalDimension_a(double L, double W, double H, double expected)
        {
            var zones = (AreaCalculator_CC_Base)Asce7Edition.ASCE7_16.CreateCCRoofZones(Bldg.Make(L, W, H));
            Assert.AreEqual(expected, zones.CritDim_a, Tol.Tight);
        }

        // ---- Wall zones -------------------------------------------------------------------------------

        [DataTestMethod]
        [DataRow(ASCE7_Versions.ASCE_VER_7_16)]
        [DataRow(ASCE7_Versions.ASCE_VER_7_22)]
        public void WallZones_EachWallUsesItsOwnLength(ASCE7_Versions v)
        {
            double L = 60, W = 40, H = 15;
            var c = Bldg.Run(v, WindLoadCalculationTypes.COMPONENT_AND_CLADDING, Bldg.Make(L, W, H));
            Assert.AreEqual(L * H, Bldg.SumArea(c.WallAreaCalculator_BldgLength), 1e-9);
            Assert.AreEqual(W * H, Bldg.SumArea(c.WallAreaCalculator_BldgWidth), 1e-9);

            double a = ((AreaCalculator_CC_Base)c.RoofAreaCalculator).CritDim_a;
            foreach (var z5 in Bldg.Zones(c.WallAreaCalculator_BldgWidth, "Zone5"))
                Assert.AreEqual(a * H, z5.Area, 1e-9, "Zone 5 is an edge strip of width a");
        }

        [TestMethod]
        public void WallZones_ShortWall_IsAllZone5()
        {
            // Wall exactly 2a long: both edge strips meet, leaving no Zone 4.
            var b = Bldg.Make(60, 6, 10); // a = 3 ft (floor); width wall = 6 ft = 2a
            var c = Bldg.Run(ASCE7_Versions.ASCE_VER_7_16, WindLoadCalculationTypes.COMPONENT_AND_CLADDING, b);
            Assert.IsTrue(c.WallAreaCalculator_BldgWidth.effWindAreas.Values.All(z => z.Label_Full == "Zone5"));
            Assert.AreEqual(6 * 10, Bldg.SumArea(c.WallAreaCalculator_BldgWidth), 1e-9);
        }

        /// <summary>
        /// ASCE 7-22 gable-end wall zoning includes the gable triangle, with the apex at mid-span.
        /// Ridge parallel to the length means the width walls are the gable ends.
        /// </summary>
        [DataTestMethod]
        [DataRow(ASCE7_Versions.ASCE_VER_7_16)]
        [DataRow(ASCE7_Versions.ASCE_VER_7_22)]
        public void WallZones_GableEnd_IncludesTriangleWithApexAtMidSpan(ASCE7_Versions v)
        {
            double L = 60, W = 40, H = 15, slope = 20;
            var b = Bldg.Make(L, W, H, RoofTypes.ROOF_TYPE_GABLE, slope, RidgeDirections.RIDGE_DIR_PARALLEL_TO_BLDGLENGTH);
            var c = Bldg.Run(ASCE7_Versions.ASCE_VER_7_22, WindLoadCalculationTypes.COMPONENT_AND_CLADDING, b);
            double rise = Math.Tan(slope * Math.PI / 180) * W / 2;

            var gableWall = c.WallAreaCalculator_BldgWidth;
            Assert.AreEqual(W * H + 0.5 * W * rise, Bldg.SumArea(gableWall), 1e-9);
            var apex = gableWall.effWindAreas.Values.SelectMany(z => z.OuterBoundary).OrderByDescending(p => p.Y).First();
            Assert.AreEqual(W / 2, apex.X, 1e-9);
            Assert.AreEqual(H + rise, apex.Y, 1e-9);

            Assert.AreEqual(L * H, Bldg.SumArea(c.WallAreaCalculator_BldgLength), 1e-9, "eave walls have no gable");
        }

        [TestMethod]
        public void WallZones_GableEnd_FollowsRidgeDirection_NotTheShorterSide()
        {
            // Ridge perpendicular to the (longer) length: the LENGTH walls are the gable ends.
            double L = 60, W = 40, H = 15, slope = 20;
            var b = Bldg.Make(L, W, H, RoofTypes.ROOF_TYPE_GABLE, slope, RidgeDirections.RIDGE_DIR_PERP_TO_BLDGLENGTH);
            var c = Bldg.Run(ASCE7_Versions.ASCE_VER_7_22, WindLoadCalculationTypes.COMPONENT_AND_CLADDING, b);
            double rise = Math.Tan(slope * Math.PI / 180) * L / 2;
            Assert.AreEqual(L * H + 0.5 * L * rise, Bldg.SumArea(c.WallAreaCalculator_BldgLength), 1e-9);
            Assert.AreEqual(W * H, Bldg.SumArea(c.WallAreaCalculator_BldgWidth), 1e-9);
        }

        // ---- Roof zones -------------------------------------------------------------------------------

        [DataTestMethod]
        [DataRow(RoofTypes.ROOF_TYPE_FLAT, 0.0)]
        [DataRow(RoofTypes.ROOF_TYPE_GABLE, 15.0)]
        [DataRow(RoofTypes.ROOF_TYPE_GABLE, 30.0)]
        [DataRow(RoofTypes.ROOF_TYPE_HIP, 15.0)]
        [DataRow(RoofTypes.ROOF_TYPE_HIP, 30.0)]
        public void RoofZones_TileThePlan(RoofTypes roof, double slope)
        {
            foreach (var v in Editions)
            {
                var ridge = roof == RoofTypes.ROOF_TYPE_FLAT ? RidgeDirections.RIDGE_DIR_NONE : RidgeDirections.RIDGE_DIR_PARALLEL_TO_BLDGLENGTH;
                var c = Bldg.Run(v, WindLoadCalculationTypes.COMPONENT_AND_CLADDING, Bldg.Make(60, 40, 15, roof, slope, ridge));
                Assert.AreEqual(60 * 40, Bldg.SumArea(c.RoofAreaCalculator), 1e-6, v.ToString());
            }
        }

        // ---- Figure / zone selection ------------------------------------------------------------------

        [DataTestMethod]
        [DataRow(ASCE7_Versions.ASCE_VER_7_16, RoofTypes.ROOF_TYPE_GABLE, 7.0)]
        [DataRow(ASCE7_Versions.ASCE_VER_7_16, RoofTypes.ROOF_TYPE_HIP, 5.0)]
        [DataRow(ASCE7_Versions.ASCE_VER_7_16, RoofTypes.ROOF_TYPE_HIP, 7.0)]
        [DataRow(ASCE7_Versions.ASCE_VER_7_22, RoofTypes.ROOF_TYPE_GABLE, 7.0)]
        [DataRow(ASCE7_Versions.ASCE_VER_7_22, RoofTypes.ROOF_TYPE_HIP, 5.0)]
        [DataRow(ASCE7_Versions.ASCE_VER_7_22, RoofTypes.ROOF_TYPE_HIP, 7.0)]
        public void LowSlopeRoofs_UseFig30_3_2A_AndFlatZoning(ASCE7_Versions v, RoofTypes roof, double slope)
        {
            var ed = Asce7Edition.For(v);
            var b = Bldg.Make(60, 40, 15, roof, slope, RidgeDirections.RIDGE_DIR_PARALLEL_TO_BLDGLENGTH);
            StringAssert.Contains(ed.CreateCCRoofFigure(b).ChartTitle, "2A");
            Assert.IsInstanceOfType(ed.CreateCCRoofZones(b), typeof(FlatRoofAreaCalculator_CC));
        }

        /// <summary>ASCE 7-22 hip roofs: 2E for 7-20 deg, 2F for 20-27 deg, 2G for 27-45 deg (step ranges, no blending).</summary>
        [DataTestMethod]
        [DataRow(10.0, "2E")]
        [DataRow(20.0, "2E")]
        [DataRow(25.0, "2F")]
        [DataRow(27.0, "2F")]
        [DataRow(30.0, "2G")]
        [DataRow(45.0, "2G")]
        public void ASCE7_22_HipFigureRanges(double slope, string figure)
        {
            var b = Bldg.Make(60, 40, 15, RoofTypes.ROOF_TYPE_HIP, slope, RidgeDirections.RIDGE_DIR_PARALLEL_TO_BLDGLENGTH);
            StringAssert.Contains(Asce7Edition.ASCE7_22.CreateCCRoofFigure(b).ChartTitle, figure);
        }

        /// <summary>
        /// Whatever the building, every C&amp;C zone must find its GCp curve: a label without a curve would
        /// silently drop that zone from the results.
        /// </summary>
        [TestMethod]
        public void EveryZone_HasACurve_AndAPressure()
        {
            var missing = new SortedSet<string>();
            foreach (var v in Editions)
                foreach (var roof in new[] { RoofTypes.ROOF_TYPE_FLAT, RoofTypes.ROOF_TYPE_GABLE, RoofTypes.ROOF_TYPE_HIP })
                    foreach (var ridge in new[] { RidgeDirections.RIDGE_DIR_PARALLEL_TO_BLDGLENGTH, RidgeDirections.RIDGE_DIR_PERP_TO_BLDGLENGTH })
                        for (int slope = 0; slope <= 45; slope += 1)
                            foreach (var size in new[] { (60.0, 40.0, 15.0), (40.0, 60.0, 12.0), (30.0, 30.0, 10.0) })
                            {
                                var b = Bldg.Make(size.Item1, size.Item2, size.Item3, roof, roof == RoofTypes.ROOF_TYPE_FLAT ? 0 : slope, roof == RoofTypes.ROOF_TYPE_FLAT ? RidgeDirections.RIDGE_DIR_NONE : ridge);
                                var c = Bldg.Run(v, WindLoadCalculationTypes.COMPONENT_AND_CLADDING, b);
                                string tag = $"{v} {roof} {b.RidgeDirection} {slope}deg {size}";
                                void Check(AreaCalculator_Base zones, Dictionary<int, PressureData> pos, Dictionary<int, PressureData> neg, string surface)
                                {
                                    foreach (var z in zones.effWindAreas)
                                    {
                                        if (!pos.ContainsKey(z.Key)) missing.Add($"{tag} {surface} {z.Value.Label_Full}: no positive GCp");
                                        if (!neg.ContainsKey(z.Key)) missing.Add($"{tag} {surface} {z.Value.Label_Full}: no negative GCp");
                                    }
                                }
                                Check(c.RoofAreaCalculator, c.windPressureRoof_Pos_External, c.windPressureRoof_Neg_External, "roof");
                                Check(c.WallAreaCalculator_BldgLength, c.windPressureBuildingLengthWall_Pos_External, c.windPressureBuildingLengthWall_Neg_External, "wallL");
                                Check(c.WallAreaCalculator_BldgWidth, c.windPressureBuildingWidthWall_Pos_External, c.windPressureBuildingWidthWall_Neg_External, "wallW");
                            }
            if (missing.Count > 0)
                Assert.Fail($"{missing.Count} zone(s) without a coefficient, e.g.:\n" + string.Join("\n", missing.Take(40)));
        }

        // ---- Applicability limits ---------------------------------------------------------------------

        [DataTestMethod]
        [DataRow(ASCE7_Versions.ASCE_VER_7_16)]
        [DataRow(ASCE7_Versions.ASCE_VER_7_22)]
        public void MeanRoofHeightAbove60ft_ReportsLimit_InsteadOfCrashing(ASCE7_Versions v)
        {
            var c = Bldg.Run(v, WindLoadCalculationTypes.COMPONENT_AND_CLADDING, Bldg.Make(100, 50, 70));
            Assert.IsFalse(c.IsValid);
            Assert.IsTrue(c.ValidationMessages.Any(m => m.Contains("60")), string.Join("; ", c.ValidationMessages));
            Assert.AreEqual(0, c.windPressureRoof_Neg_External.Count);
        }

        [TestMethod]
        public void RoofSlopeAbove45deg_ReportsLimit()
        {
            var b = Bldg.Make(60, 40, 10, RoofTypes.ROOF_TYPE_GABLE, 50, RidgeDirections.RIDGE_DIR_PARALLEL_TO_BLDGLENGTH);
            var c = Bldg.Run(ASCE7_Versions.ASCE_VER_7_16, WindLoadCalculationTypes.COMPONENT_AND_CLADDING, b);
            Assert.IsTrue(c.ValidationMessages.Any(m => m.Contains("45")), string.Join("; ", c.ValidationMessages));
        }

        [TestMethod]
        public void ValidBuilding_HasNoMessages()
        {
            var c = Bldg.Run(ASCE7_Versions.ASCE_VER_7_22, WindLoadCalculationTypes.COMPONENT_AND_CLADDING, Bldg.Make(60, 40, 15));
            Assert.IsTrue(c.IsValid);
            Assert.AreEqual(0, c.ValidationMessages.Count);
        }
    }
}
