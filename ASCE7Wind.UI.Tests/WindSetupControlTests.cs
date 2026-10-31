using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ASCE7WindLoadCalculator;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ASCE7Wind.UI.Tests
{
    /// <summary>
    /// M5 guided wind input: the three steps render, typing recalculates at once, messages appear at the
    /// field, and the host is told about changes. Set SNAPSHOT_DIR to save a PNG of each step.
    /// </summary>
    [TestClass]
    public class WindSetupControlTests
    {
        // Smallest case: the tab beside the 750 px drawing canvas on a maximized 1920 x 1080 window (measured
        // in the running app). On the wind tabs the app hides the canvas, so the real space is larger.
        private const int W = 1134, H = 754;

        private static WindSetupControl Make(BuildingData b = null)
        {
            var c = new WindSetupControl(b ?? new BuildingData(), ASCE7_Versions.ASCE_VER_7_22,
                new WindParameters_Base { WindSpeed = 115, ExposureCategory = WindExposureCategories.WIND_EXP_CAT_C, RiskCategory = "II" })
            { Width = W, Height = H };
            Layout(c);
            return c;
        }

        private static void Layout(FrameworkElement c)
        {
            c.Measure(new Size(W, H));
            c.Arrange(new Rect(0, 0, W, H));
            c.UpdateLayout();
        }

        private static T Find<T>(WindSetupControl c, string name) where T : class => (T)c.FindName(name);

        private static void Snapshot(FrameworkElement e, string name)
        {
            var bmp = new RenderTargetBitmap(W, H, 96, 96, PixelFormats.Pbgra32);
            bmp.Render(e);
            string dir = Environment.GetEnvironmentVariable("SNAPSHOT_DIR");
            if (string.IsNullOrEmpty(dir)) return;
            Directory.CreateDirectory(dir);
            var enc = new PngBitmapEncoder();
            enc.Frames.Add(BitmapFrame.Create(bmp));
            using (var fs = File.Create(Path.Combine(dir, name + ".png"))) enc.Save(fs);
        }

        [STATestMethod]
        public void EachStep_ShowsOnlyItsOwnPanel_AndRenders()
        {
            var c = Make(new BuildingData { BuildingLength = 60, BuildingWidth = 40, BuildingHeight = 15, RoofType = RoofTypes.ROOF_TYPE_GABLE, RoofPitch = 22 });
            string[] panels = { "pnlSite", "pnlBuilding", "pnlResults" };
            for (int step = 0; step < 3; step++)
            {
                c.GoToStep(step);
                Layout(c);
                for (int p = 0; p < 3; p++)
                    Assert.AreEqual(p == step ? Visibility.Visible : Visibility.Collapsed, Find<UIElement>(c, panels[p]).Visibility, $"step {step} panel {p}");
                var viewerHost = step == 1 ? "ccViewerBuilding" : step == 2 ? "ccViewerResults" : null;
                if (viewerHost != null) Assert.AreSame(c.Viewer, Find<ContentControl>(c, viewerHost).Content, "the 3D view moves to the current step");
                Snapshot(c, "setup_step" + (step + 1));
            }
            Assert.AreEqual(Visibility.Visible, Find<UIElement>(c, "btnSend").Visibility);
            Assert.IsTrue(Find<UIElement>(c, "btnSend").IsEnabled);
        }

        /// <summary>
        /// Every step fits the tab with nothing to scroll, including the worst case for each step: a gable
        /// roof (slope, ridge choice) and the longest checklist (over 60 ft, warnings).
        /// </summary>
        [STATestMethod]
        [DataRow(15.0)]
        [DataRow(70.0)]
        public void EveryStep_FitsWithoutScrolling(double wallHeight)
        {
            var c = Make(new BuildingData { BuildingLength = 60, BuildingWidth = 40, BuildingHeight = wallHeight, RoofType = RoofTypes.ROOF_TYPE_GABLE, RoofPitch = 22 });
            Find<ToggleButton>(c, "rbHillYes").IsChecked = true; // shows the Kzt box
            c.SetShearWallLink(true, "16 load cases (from W 92.6 k, from S 149.5 k), updated automatically."); // as the app shows it
            var scrollers = new[] { "pnlSite", "svBuildingInputs", "svResultsSide" };
            for (int step = 0; step < 3; step++)
            {
                c.GoToStep(step);
                Layout(c);
                var sv = Find<ScrollViewer>(c, scrollers[step]);
                Assert.AreEqual(0, sv.ScrollableHeight, 0.5, $"step {step + 1} must not scroll (h = {wallHeight} ft)");
                Snapshot(c, $"fit_step{step + 1}_h{wallHeight:0}");
            }
        }

        /// <summary>Chapter 28 chosen: the Results step still fits, and the loads come from the envelope procedure
        /// (or fall back to Chapter 27, with a note, for a building Chapter 28 does not cover).</summary>
        [STATestMethod]
        [DataRow(15.0, true)]
        [DataRow(70.0, false)]
        public void Chapter28_ResultsStep_FitsAndUsesEnvelope(double wallHeight, bool applies)
        {
            var c = Make(new BuildingData { BuildingLength = 60, BuildingWidth = 40, BuildingHeight = wallHeight, RoofType = RoofTypes.ROOF_TYPE_GABLE, RoofPitch = 22 });
            c.SetShearWallLink(true, "10 load cases (from W 12.0 k, from S 18.0 k), updated automatically.");
            Find<ToggleButton>(c, "rbProc28").IsChecked = true;
            c.GoToStep(WindSetupControl.ResultsStep);
            Layout(c);
            Assert.AreEqual(LateralProcedure.Envelope, c.Parameters.LateralProcedure);
            Assert.AreEqual(applies ? LateralProcedure.Envelope : LateralProcedure.Directional, c.Summary.Lateral.Procedure);
            Assert.AreEqual(applies, c.Summary.Lateral.ProcedureNote == null);
            Assert.AreEqual(0, Find<ScrollViewer>(c, "svResultsSide").ScrollableHeight, 0.5, $"results step must not scroll (h = {wallHeight} ft)");
            Snapshot(c, $"fit_ch28_h{wallHeight:0}");
        }

        [STATestMethod]
        public void LinkedToShearWalls_HidesSend_AndShowsTheLink()
        {
            var c = Make();
            c.GoToStep(WindSetupControl.ResultsStep);
            c.SetShearWallLink(true, "Shear walls use these wind loads.");
            Assert.AreEqual(Visibility.Collapsed, Find<UIElement>(c, "btnSend").Visibility);
            Assert.AreEqual(Visibility.Visible, Find<UIElement>(c, "bdLinked").Visibility);
            c.SetShearWallLink(false, null);
            Assert.AreEqual(Visibility.Visible, Find<UIElement>(c, "btnSend").Visibility);
            Assert.AreEqual(Visibility.Collapsed, Find<UIElement>(c, "bdLinked").Visibility);

            // Not linked for a reason (manual load, opened file): a warning stays visible with the button.
            c.SetShearWallLink(false, "They use your manual load.");
            Assert.AreEqual(Visibility.Visible, Find<UIElement>(c, "bdLinked").Visibility);
            Assert.AreEqual(Visibility.Visible, Find<UIElement>(c, "btnSend").Visibility);
            StringAssert.Contains(Find<TextBlock>(c, "tbLinkedTitle").Text, "NOT");
            Layout(c);
            Snapshot(c, "setup_results_unlinked");
        }

        /// <summary>A gable or hip at 0 degrees is a flat roof: leaving the pitch box switches the shape.</summary>
        [STATestMethod]
        public void ZeroPitch_OnSlopedRoof_SwitchesToFlat()
        {
            var c = Make(new BuildingData { BuildingLength = 60, BuildingWidth = 40, BuildingHeight = 15, RoofType = RoofTypes.ROOF_TYPE_GABLE, RoofPitch = 22 });
            var pitch = Find<TextBox>(c, "tbPitch");
            pitch.Text = "0";
            Assert.AreEqual(RoofTypes.ROOF_TYPE_GABLE, c.Building.RoofType, "no switch while typing (e.g. on the way to 0.5)");
            pitch.RaiseEvent(new RoutedEventArgs(UIElement.LostFocusEvent));
            Assert.AreEqual(RoofTypes.ROOF_TYPE_FLAT, c.Building.RoofType);
            Assert.IsNotNull(c.Analysis);
        }

        [STATestMethod]
        public void SetOnlyCases1And3_UpdatesWithoutNotifying()
        {
            var c = Make();
            int raised = 0;
            c.InputsChanged += (s, e) => raised++;
            c.SetOnlyCases1And3(true);
            c.FlushPendingChange();
            Assert.IsTrue(c.OnlyCases1And3);
            Assert.AreEqual(4, c.Summary.Lateral.LoadCases.Count);
            Assert.AreEqual(0, raised);
        }

        [STATestMethod]
        public void Typing_RecalculatesImmediately()
        {
            var c = Make();
            double before = c.Summary.FromWest.DesignKips;
            Find<TextBox>(c, "tbV").Text = "150";
            Assert.AreEqual(150, c.Parameters.WindSpeed);
            Assert.IsTrue(c.Summary.FromWest.DesignKips > before, "higher wind speed must raise the force");
            Find<TextBox>(c, "tbL").Text = "90";
            Assert.AreEqual(90, c.Building.BuildingLength);
        }

        [STATestMethod]
        public void BadDimension_ShowsMessageAtTheField_AndBlocksSending()
        {
            var c = Make();
            c.GoToStep(WindSetupControl.BuildingStep);
            Find<TextBox>(c, "tbB").Text = "0";
            Layout(c);
            Assert.IsNull(c.Analysis);
            var msg = Find<TextBlock>(c, "msgSize");
            Assert.AreEqual(Visibility.Visible, msg.Visibility);
            StringAssert.Contains(msg.Text, "Width");
            Assert.IsFalse(Find<UIElement>(c, "btnSend").IsEnabled);
            Snapshot(c, "setup_bad_width");

            Find<TextBox>(c, "tbB").Text = "40";
            Assert.IsNotNull(c.Analysis);
            Assert.AreEqual(Visibility.Collapsed, msg.Visibility);
        }

        [STATestMethod]
        public void RoofShape_ShowsSlopeAndRidgeOnlyWhereTheyApply()
        {
            var c = Make();
            var slope = Find<UIElement>(c, "secSlope");
            var ridge = Find<UIElement>(c, "pnlRidge");
            Assert.AreEqual(Visibility.Collapsed, slope.Visibility, "flat roof");

            Find<ToggleButton>(c, "rbRoofGable").IsChecked = true;
            Assert.AreEqual(RoofTypes.ROOF_TYPE_GABLE, c.Building.RoofType);
            Assert.AreEqual(Visibility.Visible, slope.Visibility);
            Assert.AreEqual(Visibility.Visible, ridge.Visibility);

            Find<ToggleButton>(c, "rbRidgeAcross").IsChecked = true;
            Assert.AreEqual(RidgeDirections.RIDGE_DIR_PERP_TO_BLDGLENGTH, c.Building.RidgeDirection);

            Find<ToggleButton>(c, "rbRoofHip").IsChecked = true;
            Assert.AreEqual(Visibility.Collapsed, ridge.Visibility, "hip ridge follows the longer side");
        }

        [STATestMethod]
        public void Hill_No_ResetsKztToOne()
        {
            var c = Make();
            var fixedKzt = Find<UIElement>(c, "tbKztFixed");
            Assert.AreEqual(Visibility.Visible, fixedKzt.Visibility, "flat ground still shows Kzt = 1.00");
            Find<ToggleButton>(c, "rbHillYes").IsChecked = true;
            Assert.AreEqual(Visibility.Collapsed, fixedKzt.Visibility);
            Find<TextBox>(c, "tbKzt").Text = "1.30";
            Assert.AreEqual(1.30, c.Parameters.Kzt, 1e-12);
            Find<ToggleButton>(c, "rbHillNo").IsChecked = true;
            Assert.AreEqual(1.0, c.Parameters.Kzt, 1e-12);
            Assert.AreEqual(Visibility.Visible, fixedKzt.Visibility);
        }

        [STATestMethod]
        public void SiteChoices_ReachTheParameters()
        {
            var c = Make();
            Find<ToggleButton>(c, "rbExpD").IsChecked = true;
            Find<ToggleButton>(c, "rbRisk4").IsChecked = true;
            Find<ToggleButton>(c, "rbEd16").IsChecked = true;
            Find<ToggleButton>(c, "rbEncPartEnc").IsChecked = true;
            Assert.AreEqual(WindExposureCategories.WIND_EXP_CAT_D, c.Parameters.ExposureCategory);
            Assert.AreEqual("IV", c.Parameters.RiskCategory);
            Assert.AreEqual(ASCE7_Versions.ASCE_VER_7_16, c.Version);
            Assert.AreEqual(BuildingEnclosures.BLDG_PARTIALLY_ENCLOSED, c.Building.EnclosureType);
        }

        [STATestMethod]
        public void SetInputs_RoundTrips_AndDoesNotNotifyTheHost()
        {
            var c = Make();
            int raised = 0;
            c.InputsChanged += (s, e) => raised++;
            var b = new BuildingData { BuildingLength = 72, BuildingWidth = 36, BuildingHeight = 12, RoofType = RoofTypes.ROOF_TYPE_GABLE, RoofPitch = 30, RidgeDirection = RidgeDirections.RIDGE_DIR_PERP_TO_BLDGLENGTH };
            var p = new WindParameters_Base { WindSpeed = 140, Kzt = 1.2, ExposureCategory = WindExposureCategories.WIND_EXP_CAT_B, RiskCategory = "III" };
            c.SetInputs(b, ASCE7_Versions.ASCE_VER_7_16, p);
            c.FlushPendingChange();

            Assert.AreEqual(0, raised);
            Assert.AreEqual(72, c.Building.BuildingLength);
            Assert.AreEqual(RidgeDirections.RIDGE_DIR_PERP_TO_BLDGLENGTH, c.Building.RidgeDirection);
            Assert.AreEqual(140, c.Parameters.WindSpeed);
            Assert.AreEqual(1.2, c.Parameters.Kzt, 1e-12);
            Assert.AreEqual(WindExposureCategories.WIND_EXP_CAT_B, c.Parameters.ExposureCategory);
            Assert.AreEqual("III", c.Parameters.RiskCategory);
            Assert.AreEqual(ASCE7_Versions.ASCE_VER_7_16, c.Version);
            Assert.AreNotSame(b, c.Building, "the control keeps its own copy");
        }

        [STATestMethod]
        public void UserChange_NotifiesTheHost_AndSendRaisesTheLoadCases()
        {
            var c = Make();
            int raised = 0;
            WindLateralResult sent = null;
            c.InputsChanged += (s, e) => raised++;
            c.SendLoadsRequested += (s, r) => sent = r;

            Find<TextBox>(c, "tbH").Text = "18";
            c.FlushPendingChange();
            Assert.AreEqual(1, raised);

            Find<ToggleButton>(c, "cbOnlyCases13").IsChecked = true;
            c.GoToStep(WindSetupControl.ResultsStep);
            Find<Button>(c, "btnSend").RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            Assert.IsNotNull(sent);
            Assert.AreEqual(4, sent.LoadCases.Count);
        }

        /// <summary>
        /// Choosing Gable from Flat with a 0 slope (the usual flat-roof value) starts from a typical slope
        /// instead of showing an error the user did not cause.
        /// </summary>
        [STATestMethod]
        public void FlatToGable_WithZeroSlope_StartsFromATypicalSlope()
        {
            var c = Make(new BuildingData { BuildingLength = 60, BuildingWidth = 40, BuildingHeight = 15, RoofType = RoofTypes.ROOF_TYPE_FLAT, RoofPitch = 0 });
            Find<ToggleButton>(c, "rbRoofGable").IsChecked = true;
            Assert.AreEqual(RoofTypes.ROOF_TYPE_GABLE, c.Building.RoofType);
            Assert.IsTrue(c.Building.RoofPitch > 0);
            Assert.IsFalse(WindInputValidator.HasErrors(c.Issues));
            StringAssert.Contains(Find<TextBlock>(c, "msgPitch").Text, "Slope set to");
        }

        /// <summary>WPF groups radio buttons by name across a window; each control keeps its own groups.</summary>
        [STATestMethod]
        public void TwoControlsInOneWindow_DoNotUncheckEachOther()
        {
            var a = Make();
            var b = Make();
            var viewer = new Wind3DViewerControl();
            var root = new StackPanel();
            var w = new Window { Content = root, Width = 400, Height = 300, ShowInTaskbar = false, WindowStyle = WindowStyle.None, Left = -10000 };
            root.Children.Add(a);
            root.Children.Add(b);
            root.Children.Add(viewer);
            w.Show();
            try
            {
                Find<ToggleButton>(a, "rbRoofGable").IsChecked = true;
                Find<ToggleButton>(b, "rbRoofHip").IsChecked = true;
                Assert.AreEqual(true, Find<ToggleButton>(a, "rbRoofGable").IsChecked, "the other setup control's roof shape");
                var caseA = (ToggleButton)viewer.FindName("rbCaseA");
                Assert.AreEqual(true, caseA.IsChecked, "the 3D view's roof Cp choice (it also used the group name 'roof')");
                viewer.Mode = WindViewMode.CladdingSuction;
                Assert.AreEqual(true, ((ToggleButton)viewer.FindName("rbCladSuction")).IsChecked);
                Assert.AreEqual(true, Find<ToggleButton>(a, "rbRoofGable").IsChecked);
            }
            finally { w.Close(); }
        }

        [STATestMethod]
        public void OnlyCases1And3_FadesCases2And4_AndResultsShowDetailsButton()
        {
            var c = Make();
            c.GoToStep(WindSetupControl.ResultsStep);
            Assert.AreEqual(1.0, Find<UIElement>(c, "tbCase2b").Opacity);
            Find<ToggleButton>(c, "cbOnlyCases13").IsChecked = true;
            Assert.IsTrue(Find<UIElement>(c, "tbCase2b").Opacity < 1);
            Assert.IsTrue(Find<UIElement>(c, "tbCase4b").Opacity < 1);
            Assert.AreEqual(Visibility.Visible, Find<UIElement>(c, "btnDetails").Visibility);
            Assert.IsTrue(Find<UIElement>(c, "btnDetails").IsEnabled);
        }

        [STATestMethod]
        public void ShearWallsButton_ShowsOnlyWhenLinkedAndHandled()
        {
            var c = Make();
            int asked = 0;
            c.ShearWallsRequested += (s, e) => asked++;
            c.GoToStep(WindSetupControl.ResultsStep);
            c.SetShearWallLink(true, "linked");
            var btn = Find<Button>(c, "btnShearWalls");
            Assert.AreEqual(Visibility.Visible, btn.Visibility);
            btn.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            Assert.AreEqual(1, asked);
            c.SetShearWallLink(false, "manual");
            Assert.AreEqual(Visibility.Collapsed, btn.Visibility);
        }

        [STATestMethod]
        public void ResultCards_ShowNumberAndUnitSeparately()
        {
            var c = Make(new BuildingData { BuildingLength = 60, BuildingWidth = 40, BuildingHeight = 15, RoofType = RoofTypes.ROOF_TYPE_GABLE, RoofPitch = 22 });
            c.GoToStep(WindSetupControl.ResultsStep);
            foreach (var n in new[] { "Roof", "Wall", "West", "South" })
            {
                Assert.IsTrue(double.TryParse(Find<TextBlock>(c, "val" + n).Text, out _), n + " shows just the number");
                Assert.AreEqual(Visibility.Visible, Find<UIElement>(c, "unit" + n).Visibility);
                Assert.AreEqual(2, Find<TextBlock>(c, "sub" + n).Text.Split('\n').Length, n + " has two detail lines");
            }
        }

        /// <summary>Framing uses span × span/3 (26.2); an entered area is used as is; it all reaches the parameters.</summary>
        [STATestMethod]
        public void ComponentChoice_SetsTheEffectiveWindArea()
        {
            var c = Make(new BuildingData { BuildingLength = 60, BuildingWidth = 40, BuildingHeight = 15, RoofType = RoofTypes.ROOF_TYPE_GABLE, RoofPitch = 22 });
            Assert.AreEqual(10.0, c.Parameters.RoofComponentArea, "default: sheathing and fasteners");
            Assert.AreEqual(Visibility.Collapsed, Find<UIElement>(c, "bdRoofComp").Visibility);

            Find<ComboBox>(c, "cbRoofComponent").SelectedIndex = 1;   // rafters
            Assert.AreEqual(Visibility.Visible, Find<UIElement>(c, "bdRoofComp").Visibility);
            Find<TextBox>(c, "tbRoofComp").Text = "12";               // 12 ft span
            Assert.AreEqual(48.0, c.Parameters.RoofComponentArea, 1e-9);
            StringAssert.Contains(Find<TextBlock>(c, "tbRoofCompArea").Text, "48");

            Find<ComboBox>(c, "cbWallComponent").SelectedIndex = 2;   // window
            Find<TextBox>(c, "tbWallComp").Text = "20";
            Assert.AreEqual(20.0, c.Parameters.WallComponentArea, 1e-9);

            // GCp follows the area: bigger rafters, smaller roof suction than sheathing.
            double rafter = c.Summary.RoofUpliftPsf.Value;
            Find<ComboBox>(c, "cbRoofComponent").SelectedIndex = 0;
            Assert.IsTrue(c.Summary.RoofUpliftPsf.Value < rafter, "sheathing (10 sq ft) sees more uplift than a 48 sq ft rafter");
        }

        [STATestMethod]
        public void ComponentArea_RoundTrips_AndBadEntryFallsBackTo10()
        {
            var c = Make();
            c.SetInputs(new BuildingData(), ASCE7_Versions.ASCE_VER_7_22,
                new WindParameters_Base { WindSpeed = 115, ExposureCategory = WindExposureCategories.WIND_EXP_CAT_C, RoofComponentArea = 48, WallComponentArea = 10 });
            Assert.AreEqual(2, Find<ComboBox>(c, "cbRoofComponent").SelectedIndex, "a saved non-default area shows as an entered area");
            Assert.AreEqual(48.0, c.Parameters.RoofComponentArea, 1e-9);
            Assert.AreEqual(0, Find<ComboBox>(c, "cbWallComponent").SelectedIndex);

            Find<TextBox>(c, "tbRoofComp").Text = "x";
            Assert.AreEqual(10.0, c.Parameters.RoofComponentArea, "unusable entry: the conservative 10 sq ft");
            StringAssert.Contains(Find<TextBlock>(c, "tbRoofCompArea").Text, "enter");
        }

        [STATestMethod]
        public void ComponentChoices_RestoreAsTyped_ForProjectFiles()
        {
            var c = Make();
            c.SetComponents(new ComponentChoice(ComponentChoice.Framing, 14), new ComponentChoice(ComponentChoice.Area, 21));
            Assert.AreEqual(ComponentChoice.Framing, c.RoofComponent.Kind);
            Assert.AreEqual(14.0, c.RoofComponent.Entry);
            Assert.AreEqual(14 * 14 / 3.0, c.Parameters.RoofComponentArea, 1e-9, "rafters: span x span/3");
            Assert.AreEqual(ComponentChoice.Area, c.WallComponent.Kind);
            Assert.AreEqual(21.0, c.Parameters.WallComponentArea, 1e-9);

            c.SetComponents(new ComponentChoice(ComponentChoice.Sheathing, 0), new ComponentChoice(ComponentChoice.Sheathing, 0));
            Assert.AreEqual(10.0, c.Parameters.RoofComponentArea);
            Assert.AreEqual(string.Empty, Find<TextBox>(c, "tbRoofComp").Text);
        }
    }
}
