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
        private const int W = 1360, H = 900;

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
                Assert.AreEqual(step == 0 ? Visibility.Collapsed : Visibility.Visible, Find<UIElement>(c, "pnlRight").Visibility);
                Snapshot(c, "setup_step" + (step + 1));
            }
            Assert.AreEqual(Visibility.Visible, Find<UIElement>(c, "btnSend").Visibility);
            Assert.IsTrue(Find<UIElement>(c, "btnSend").IsEnabled);
        }

        /// <summary>Beside the shear-wall drawing the screen is narrow: columns stack instead of squeezing.</summary>
        [STATestMethod]
        public void NarrowWidth_StacksTheColumns()
        {
            var c = Make(new BuildingData { BuildingLength = 60, BuildingWidth = 40, BuildingHeight = 15, RoofType = RoofTypes.ROOF_TYPE_HIP, RoofPitch = 25 });
            var right = Find<UIElement>(c, "pnlRight");
            Assert.AreEqual(0, Grid.GetRow(right), "wide: the 3D view sits beside the inputs");

            c.Width = 820;
            c.Measure(new Size(820, H));
            c.Arrange(new Rect(0, 0, 820, H));
            c.UpdateLayout();
            Assert.AreEqual(1, Grid.GetRow(right), "narrow: the 3D view goes below the inputs");
            Assert.AreEqual(2, Grid.GetRow(Find<UIElement>(c, "siteRight")), "narrow: the site cards stack");

            c.GoToStep(WindSetupControl.BuildingStep);
            c.UpdateLayout();
            Snapshot(c, "setup_narrow_building");
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
            Find<ToggleButton>(c, "rbHillYes").IsChecked = true;
            Find<TextBox>(c, "tbKzt").Text = "1.30";
            Assert.AreEqual(1.30, c.Parameters.Kzt, 1e-12);
            Find<ToggleButton>(c, "rbHillNo").IsChecked = true;
            Assert.AreEqual(1.0, c.Parameters.Kzt, 1e-12);
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
