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
    }
}
