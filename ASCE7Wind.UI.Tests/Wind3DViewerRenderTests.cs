using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ASCE7WindLoadCalculator;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ASCE7Wind.UI.Tests
{
    /// <summary>
    /// Builds the 3D viewer offscreen and renders it to a bitmap. Checks that zones actually draw in the
    /// expected colors and that hover picking finds them. Set SNAPSHOT_DIR to also save PNGs for review.
    /// </summary>
    [TestClass]
    public class Wind3DViewerRenderTests
    {
        private const int W = 1280, H = 760;

        private static (BuildingData b, WindLoadCalculator_Base cc, WindLoadCalculator_Base mL, WindLoadCalculator_Base mW) Results(BuildingData b)
        {
            WindLoadCalculator_Base Run(BuildingData x, WindLoadCalculationTypes t)
            {
                var p = new WindParameters_Base { WindSpeed = 115, ExposureCategory = WindExposureCategories.WIND_EXP_CAT_C, AnalysisType = t };
                var c = WindLoadCalculatorFactory.Create(ASCE7_Versions.ASCE_VER_7_22, t, p, x);
                c.Initialize();
                return c;
            }
            var rotated = b.Clone();
            rotated.RotateBuilding();
            return (b, Run(b, WindLoadCalculationTypes.COMPONENT_AND_CLADDING), Run(b, WindLoadCalculationTypes.MWFRS), Run(rotated, WindLoadCalculationTypes.MWFRS));
        }

        private static Wind3DViewerControl Viewer(BuildingData b, WindViewMode mode)
        {
            var r = Results(b);
            var v = new Wind3DViewerControl(r.b, r.cc, r.mL, r.mW) { Width = W, Height = H };
            v.Mode = mode;
            v.Measure(new Size(W, H));
            v.Arrange(new Rect(0, 0, W, H));
            v.UpdateLayout();
            v.ResetView();
            v.UpdateLayout();
            return v;
        }

        private static byte[] Render(FrameworkElement e, string name)
        {
            var bmp = new RenderTargetBitmap(W, H, 96, 96, PixelFormats.Pbgra32);
            bmp.Render(e);
            var pixels = new byte[W * H * 4];
            bmp.CopyPixels(pixels, W * 4, 0);

            string dir = Environment.GetEnvironmentVariable("SNAPSHOT_DIR");
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
                var enc = new PngBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(bmp));
                using (var fs = File.Create(Path.Combine(dir, name + ".png"))) enc.Save(fs);
            }
            return pixels;
        }

        /// <summary>Counts pixels that are clearly red (suction arm) or clearly blue (pressure arm).</summary>
        private static (int red, int blue) CountArms(byte[] bgra)
        {
            int red = 0, blue = 0;
            for (int i = 0; i < bgra.Length; i += 4)
            {
                int b = bgra[i], g = bgra[i + 1], r = bgra[i + 2];
                if (r > g + 40 && r > b + 40) red++;
                if (b > r + 40 && b > g + 10) blue++;
            }
            return (red, blue);
        }

        public static System.Collections.Generic.IEnumerable<object[]> Cases()
        {
            yield return new object[] { "flat", RoofTypes.ROOF_TYPE_FLAT, 0.0, RidgeDirections.RIDGE_DIR_NONE };
            yield return new object[] { "gable_parallel", RoofTypes.ROOF_TYPE_GABLE, 22.0, RidgeDirections.RIDGE_DIR_PARALLEL_TO_BLDGLENGTH };
            yield return new object[] { "gable_perp", RoofTypes.ROOF_TYPE_GABLE, 30.0, RidgeDirections.RIDGE_DIR_PERP_TO_BLDGLENGTH };
            yield return new object[] { "hip", RoofTypes.ROOF_TYPE_HIP, 20.0, RidgeDirections.RIDGE_DIR_PARALLEL_TO_BLDGLENGTH };
        }

        [STATestMethod]
        [DynamicData(nameof(Cases))]
        public void Cladding_Suction_RendersRedZones(string name, RoofTypes roof, double pitch, RidgeDirections ridge)
        {
            var b = new BuildingData { BuildingLength = 60, BuildingWidth = 40, BuildingHeight = 15, RoofType = roof, RoofPitch = pitch, RidgeDirection = ridge };
            var v = Viewer(b, WindViewMode.CladdingSuction);
            var (red, blue) = CountArms(Render(v, name + "_cladding_suction"));
            Assert.IsTrue(red > W * H / 50, $"expected suction-colored zones, got {red} red pixels");
            Assert.IsTrue(blue < red / 4, "suction view should be mostly the red arm (legend only for blue)");
        }

        [STATestMethod]
        [DynamicData(nameof(Cases))]
        public void Mwfrs_RendersPressureAndSuction(string name, RoofTypes roof, double pitch, RidgeDirections ridge)
        {
            var b = new BuildingData { BuildingLength = 60, BuildingWidth = 40, BuildingHeight = 15, RoofType = roof, RoofPitch = pitch, RidgeDirection = ridge };
            foreach (var mode in new[] { WindViewMode.MwfrsAlongLength, WindViewMode.MwfrsAlongWidth })
            {
                var v = Viewer(b, mode);
                var (red, blue) = CountArms(Render(v, $"{name}_{mode}"));
                Assert.IsTrue(red > W * H / 100, $"{mode}: expected suction zones");
                Assert.IsTrue(blue > 1000, $"{mode}: expected the windward wall in the pressure color");
            }
        }

        [STATestMethod]
        public void Hover_FindsZoneAtCenter()
        {
            var b = new BuildingData { BuildingLength = 60, BuildingWidth = 40, BuildingHeight = 15, RoofType = RoofTypes.ROOF_TYPE_GABLE, RoofPitch = 22, RidgeDirection = RidgeDirections.RIDGE_DIR_PARALLEL_TO_BLDGLENGTH };
            var v = Viewer(b, WindViewMode.CladdingSuction);
            Render(v, "hover_probe");
            // The 3D area starts below the toolbar; the model is framed at its center.
            var host = (FrameworkElement)v.FindName("viewHost");
            var zone = v.ZoneAt(new Point(host.ActualWidth / 2, host.ActualHeight / 2));
            Assert.IsNotNull(zone, "no zone under the view center");
            Assert.IsTrue(zone.HasPressure);
        }

        [STATestMethod]
        public void InvalidDimensions_DoNotCrash()
        {
            var v = new Wind3DViewerControl(new BuildingData { BuildingLength = 0, BuildingWidth = 40, BuildingHeight = 15 }, null, null, null)
            { Width = W, Height = H };
            v.Measure(new Size(W, H));
            v.Arrange(new Rect(0, 0, W, H));
            Render(v, "invalid_dimensions");
            Assert.IsNull(v.Scene);
            StringAssert.Contains(((System.Windows.Controls.TextBlock)v.FindName("tbSubtitle")).Text, "Cannot draw");
        }

        [STATestMethod]
        public void NoResultsYet_ShowsPrompt()
        {
            var v = new Wind3DViewerControl(new BuildingData(), null, null, null) { Width = W, Height = H };
            v.Measure(new Size(W, H));
            v.Arrange(new Rect(0, 0, W, H));
            Render(v, "no_results");
            var warn = (FrameworkElement)v.FindName("bdWarnings");
            Assert.AreEqual(Visibility.Visible, warn.Visibility, "tells the user to enter wind inputs");
        }

        [STATestMethod]
        public void OpensOnWholeBuildingWindFromWest()
        {
            var r = Results(new BuildingData { BuildingLength = 60, BuildingWidth = 40, BuildingHeight = 15 });
            var v = new Wind3DViewerControl(r.b, r.cc, r.mL, r.mW);
            Assert.AreEqual(WindViewMode.MwfrsAlongLength, v.Mode);
            Assert.IsTrue(v.Scene.Zones.Any(z => z.HasPressure && z.Kind == SurfaceKind.Wall));
        }

        /// <summary>A cladding view with no valid pressures must not leave the designer looking at a gray building.</summary>
        [STATestMethod]
        public void CladdingNotApplicable_SwitchesToWholeBuilding_AndSaysWhy()
        {
            var r = Results(new BuildingData { BuildingLength = 100, BuildingWidth = 50, BuildingHeight = 70 });
            var v = new Wind3DViewerControl(r.b, r.cc, r.mL, r.mW) { Width = W, Height = H };
            v.Mode = WindViewMode.CladdingSuction;
            v.Measure(new Size(W, H)); v.Arrange(new Rect(0, 0, W, H));
            Render(v, "auto_switch_70ft");
            Assert.AreEqual(WindViewMode.MwfrsAlongLength, v.Mode);
            Assert.IsTrue(v.Scene.Zones.Any(z => z.HasPressure), "whole-building pressures shown");
            StringAssert.Contains(v.Scene.Messages[0], "do not apply");
        }

        /// <summary>Refreshing with new results (e.g. a changed height) replaces the scene; nothing stays stale.</summary>
        [STATestMethod]
        public void SetResults_ReplacesStaleBuilding()
        {
            var a = Results(new BuildingData { BuildingLength = 60, BuildingWidth = 40, BuildingHeight = 15 });
            var v = new Wind3DViewerControl(a.b, a.cc, a.mL, a.mW);
            var b = Results(new BuildingData { BuildingLength = 60, BuildingWidth = 40, BuildingHeight = 40 });
            v.SetResults(b.b, b.cc, b.mL, b.mW, keepCamera: true);
            double top = v.Scene.Zones.Where(z => z.Kind == SurfaceKind.Wall).SelectMany(z => z.Triangles).Max(p => p.Z);
            Assert.AreEqual(40, top, 1e-9, "walls colored to the new 40 ft height");
        }

        [STATestMethod]
        public void OutOfLimits_ShowsWarning()
        {
            var b = new BuildingData { BuildingLength = 100, BuildingWidth = 50, BuildingHeight = 70 };
            var v = Viewer(b, WindViewMode.CladdingSuction);
            Render(v, "out_of_limits");
            var warn = (FrameworkElement)v.FindName("bdWarnings");
            Assert.AreEqual(Visibility.Visible, warn.Visibility);
        }

        /// <summary>
        /// The wind cases view: the list starts with the envelope, then every case; the Max/Min toggle shows the largest
        /// pressure or the largest suction; selecting a case draws that case's own zones and hides the roof Cp choice.
        /// </summary>
        [STATestMethod]
        public void WindCases_ListEnvelopeToggleAndOneCase()
        {
            var b = new BuildingData { BuildingLength = 60, BuildingWidth = 40, BuildingHeight = 15, RoofType = RoofTypes.ROOF_TYPE_GABLE, RoofPitch = 22, RidgeDirection = RidgeDirections.RIDGE_DIR_PARALLEL_TO_BLDGLENGTH };
            var v = Viewer(b, WindViewMode.WindCases);
            Assert.AreEqual(WindViewMode.WindCases, v.Mode);
            Assert.AreEqual(Visibility.Visible, ((FrameworkElement)v.FindName("bdCases")).Visibility);
            Assert.AreEqual(Visibility.Collapsed, ((FrameworkElement)v.FindName("spRoofCase")).Visibility, "each case has its own roof value");

            Assert.AreEqual(208 + 4, v.WindCases.Count);   // and the four 27.1.5 minimum cases (C27.1.5)
            Assert.AreEqual(v.WindCases.Count, v.ShownCaseCount, "the envelope, then every case");
            Assert.IsNull(v.SelectedCase, "the envelope is drawn first");

            v.EnvelopeMax = true;
            double maxView = v.Scene.MaxPsf;
            v.EnvelopeMax = false;
            Assert.IsTrue(v.Scene.Title.Contains("suction"));
            Assert.IsTrue(v.Scene.MinPsf < 0 && maxView > 0);
            Assert.IsTrue(v.Scene.MaxPsf < maxView, "the min view shows each piece's smallest value");

            var quartering = v.WindCases.First(c => c.FigureCase == 3);
            v.SelectedCase = quartering;
            Assert.AreEqual(quartering.Name, v.Scene.Title);
            Assert.AreEqual(quartering.Zones.Count, v.Scene.Zones.Count);
            Assert.IsTrue(v.Scene.WindDirection.Value.X > 0 && v.Scene.WindDirection.Value.Y > 0, "from the SW: toward +x and +y");

            v.EnvelopeMax = true; // the toggle goes back to the envelope
            Assert.IsNull(v.SelectedCase);
            Assert.AreEqual(maxView, v.Scene.MaxPsf, 1e-9);

            var filter = (System.Windows.Controls.TextBox)v.FindName("tbCaseFilter");
            filter.Text = "Case 3";
            Assert.AreEqual(32, v.ShownCaseCount);
        }

        /// <summary>The Ch. 28 list: its own cases and its own envelope; back to Ch. 27 gives the Ch. 27 list again.</summary>
        [STATestMethod]
        public void WindCases_Chapter28_OwnListAndEnvelope()
        {
            var b = new BuildingData { BuildingLength = 60, BuildingWidth = 40, BuildingHeight = 15, RoofType = RoofTypes.ROOF_TYPE_GABLE, RoofPitch = 22, RidgeDirection = RidgeDirections.RIDGE_DIR_PARALLEL_TO_BLDGLENGTH };
            var v = Viewer(b, WindViewMode.WindCases);
            v.CaseProcedure = LateralProcedure.Envelope;
            Assert.AreEqual(32 + 4, v.WindCases.Count, "2 axes × 4 corners × LC basic and torsional × ±GCpi, and the four minimum cases");
            Assert.IsTrue(v.WindCases.All(c => c.Procedure == LateralProcedure.Envelope));
            Assert.AreEqual(32 + 4, v.ShownCaseCount);
            Assert.IsNull(v.SelectedCase);
            Assert.IsTrue(v.Scene.Subtitle.Contains("36 wind cases"));

            var lc3 = v.WindCases.First(c => c.FigureCase == 3);
            v.SelectedCase = lc3;
            Assert.AreEqual(lc3.Name, v.Scene.Title);
            Assert.IsTrue(v.Scene.Subtitle.Contains("reference corner"));

            v.CaseProcedure = LateralProcedure.Directional;
            Assert.AreEqual(208 + 4, v.WindCases.Count);   // and the four 27.1.5 minimum cases (C27.1.5)
            Assert.IsNull(v.SelectedCase, "a new list starts at its envelope");
        }

        /// <summary>
        /// The wind cases grouped: the envelope, then a group per wind direction (W, E, S, N, then the quartering winds), in
        /// each a group per load case. Ch. 27 here (60 x 40 gable): each principal wind holds Case 1 (2 roof values x 2 GCpi
        /// signs = 4) and Case 2 (x 2 torsions = 8), and its minimum case alone (no group of one); each quartering wind Case 3
        /// (32 / 4 = 8) and Case 4 (128 / 4 = 32). 4 x (4 + 8 + 1) + 4 x (8 + 32) = 52 + 160 = 212.
        /// </summary>
        [STATestMethod]
        public void WindCases_GroupedByDirectionThenCase()
        {
            var b = new BuildingData { BuildingLength = 60, BuildingWidth = 40, BuildingHeight = 15, RoofType = RoofTypes.ROOF_TYPE_GABLE, RoofPitch = 22, RidgeDirection = RidgeDirections.RIDGE_DIR_PARALLEL_TO_BLDGLENGTH };
            var v = Viewer(b, WindViewMode.WindCases);
            var groups = v.CaseGroups;
            CollectionAssert.AreEqual(new[] { "Envelope of all 212 cases", "From the west (W)", "From the east (E)", "From the south (S)", "From the north (N)",
                                              "From the southwest (SW)", "From the southeast (SE)", "From the northwest (NW)", "From the northeast (NE)" },
                                      groups.Where(g => !g.Contains("|")).ToList());
            // each principal wind lists every Fig. 27.3-8 case it takes part in: Cases 1 and 2 (its own), then Cases 3 and 4
            // with each of the two winds across it (the same cases as under the quartering direction)
            CollectionAssert.AreEqual(new[] { "From the west (W)|Envelope", "From the west (W)|Case 1", "From the west (W)|Case 2",
                                              "From the west (W)|Cases 3 and 4 with the south wind (from SW)", "From the west (W)|Cases 3 and 4 with the north wind (from NW)" },
                                      groups.Where(g => g.StartsWith("From the west (W)|")).ToList());
            CollectionAssert.AreEqual(new[] { "From the southwest (SW)|Envelope", "From the southwest (SW)|Case 3", "From the southwest (SW)|Case 4" }, groups.Where(g => g.StartsWith("From the southwest (SW)|")).ToList());
            Assert.AreEqual(212, v.ShownCaseCount, "every case (a quartering case is listed under its own direction and under its two winds)");
            var west = ((System.Windows.Controls.TreeView)v.FindName("tvCases")).Items.OfType<System.Windows.Controls.TreeViewItem>()
                        .First(i => ((System.Windows.Controls.TextBlock)i.Header).Inlines.OfType<System.Windows.Documents.Run>().First().Text == "From the west (W)");
            Assert.AreEqual((13 + 2 * 40).ToString(), ((System.Windows.Controls.TextBlock)west.Header).Inlines.OfType<System.Windows.Documents.Run>().Last().Text.Trim(),
                            "W: its own 13 and the 40 of SW and of NW");
            foreach (var d in new[] { "W", "E", "S", "N" })
            {
                Assert.AreEqual(4, v.WindCases.Count(c => c.From == d && c.FigureCase == 1));
                Assert.AreEqual(8, v.WindCases.Count(c => c.From == d && c.FigureCase == 2));
            }

            // the name split at its direction: the group, then the rest
            var c4 = v.WindCases.First(c => c.FigureCase == 4 && c.From == "NE");
            var (group, item) = Wind3DViewerControl.SplitName(c4);
            Assert.AreEqual("Case 4", group);
            Assert.AreEqual(c4.Name, group + ", from NE, " + item);
            var min = v.WindCases.First(c => c.IsMinimumCase && c.From == "S");
            Assert.AreEqual(("Minimum (27.1.5)", "16 psf walls + 8 psf roof (projected)"), Wind3DViewerControl.SplitName(min));

            // a case picked from a closed group draws it; the filter keeps the groups with a match
            v.SelectedCase = c4;
            Assert.AreEqual(c4.Name, v.Scene.Title);
            ((System.Windows.Controls.TextBox)v.FindName("tbCaseFilter")).Text = "torsion ++";
            Assert.AreEqual(v.WindCases.Count(c => c.Name.Contains("torsion ++")), v.ShownCaseCount);
            Assert.IsTrue(v.CaseGroups.Where(g => !g.Contains("|") && !g.StartsWith("Envelope")).All(g => new[] { "(SW)", "(SE)", "(NW)", "(NE)" }.Any(g.Contains)), "only the quartering winds have torsion ++");
        }

        /// <summary>The VIEW flyout: its tab closes and opens the panel (remembered); the building-only view has neither.</summary>
        [STATestMethod]
        public void ViewFlyout_OpensAndCloses_NoneForTheBuildingOnly()
        {
            string prefs = Path.Combine(Path.GetTempPath(), "w7-prefs-" + Guid.NewGuid().ToString("N") + ".txt");
            ViewerPreferences.FilePath = prefs;     // never touch the user's own preferences
            try
            {
                var b = new BuildingData { BuildingLength = 60, BuildingWidth = 40, BuildingHeight = 15 };
                var v = Viewer(b, WindViewMode.WindCases);
                var panel = (FrameworkElement)v.FindName("bdView");
                var tab = (System.Windows.Controls.Button)v.FindName("btnEdgeView");
                var host = (FrameworkElement)v.FindName("viewHost");
                Assert.AreEqual(Visibility.Visible, panel.Visibility);
                double narrow = host.ActualWidth;
                tab.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
                v.UpdateLayout();
                Assert.AreEqual(Visibility.Collapsed, panel.Visibility);
                Assert.IsFalse(ViewerPreferences.ViewPanelOpen, "remembered");
                Assert.IsTrue(host.ActualWidth > narrow + 250, "the model takes the panel's width");
                tab.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
                Assert.AreEqual(Visibility.Visible, panel.Visibility);
                v.GeometryOnly = true;
                Assert.AreEqual(Visibility.Collapsed, panel.Visibility);
                Assert.AreEqual(Visibility.Collapsed, tab.Visibility);
            }
            finally { if (File.Exists(prefs)) File.Delete(prefs); }
        }

        /// <summary>
        /// The envelopes: of every case at the top (Max +, Max −) and of each wind direction's cases inside its group. A
        /// direction's envelope is of that direction's cases alone (its title and arrow say which); the envelope of every case
        /// is the largest of the eight: its Max + is the largest of the directions' Max +, its Max − the most negative of
        /// their Max −.
        /// </summary>
        [STATestMethod]
        public void WindCases_EnvelopePerDirectionAndOfEveryCase()
        {
            var b = new BuildingData { BuildingLength = 60, BuildingWidth = 40, BuildingHeight = 15, RoofType = RoofTypes.ROOF_TYPE_GABLE, RoofPitch = 22, RidgeDirection = RidgeDirections.RIDGE_DIR_PARALLEL_TO_BLDGLENGTH };
            var v = Viewer(b, WindViewMode.WindCases);
            Assert.IsNull(v.SelectedCase);
            Assert.IsTrue(v.SelectedEnvelope.From == null && v.SelectedEnvelope.Max, "the envelope of every case, Max +, first");
            double allMax = v.Scene.MaxPsf;
            v.EnvelopeMax = false;
            double allMin = v.Scene.MinPsf;

            double dirMax = double.MinValue, dirMin = double.MaxValue;
            foreach (var d in new[] { "W", "E", "S", "N", "SW", "SE", "NW", "NE" })
            {
                int n = v.WindCases.Count(c => c.From == d);
                v.SelectedEnvelope = new Wind3DViewerControl.EnvelopeChoice(d, true);
                Assert.IsNull(v.SelectedCase);
                StringAssert.Contains(v.Scene.Subtitle, $"the {n} wind cases from the {d}");
                StringAssert.Contains(v.Scene.Title, "wind from the " + WindSceneBuilder.DirectionWord(d));
                Assert.AreEqual(WindSceneBuilder.Toward(d).Value.X, v.Scene.WindDirection.Value.X, 1e-12, "the arrow of that wind");
                Assert.IsTrue(v.Scene.MaxPsf <= allMax + 1e-9, d + ": no more than the envelope of every case");
                dirMax = Math.Max(dirMax, v.Scene.MaxPsf);
                v.SelectedEnvelope = new Wind3DViewerControl.EnvelopeChoice(d, false);
                StringAssert.Contains(v.Scene.Title, "suction");
                Assert.IsTrue(v.Scene.MinPsf >= allMin - 1e-9, d + ": no more suction than the envelope of every case");
                dirMin = Math.Min(dirMin, v.Scene.MinPsf);
            }
            Assert.AreEqual(allMax, dirMax, 1e-9, "every case's Max + is the largest of the directions' Max +");
            Assert.AreEqual(allMin, dirMin, 1e-9, "every case's Max − is the most negative of the directions' Max −");

            // a case, then back to the envelope of every case
            var c1 = v.WindCases.First(c => c.From == "E" && c.FigureCase == 1);
            v.SelectedCase = c1;
            Assert.AreEqual(c1.Name, v.Scene.Title);
            Assert.IsNull(v.SelectedEnvelope);
            v.EnvelopeMax = true;
            Assert.IsNull(v.SelectedCase);
            Assert.AreEqual(allMax, v.Scene.MaxPsf, 1e-9);
        }
    }
}
