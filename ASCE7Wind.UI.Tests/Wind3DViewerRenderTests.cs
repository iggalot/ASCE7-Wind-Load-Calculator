using System;
using System.IO;
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
        public void OutOfLimits_ShowsWarning()
        {
            var b = new BuildingData { BuildingLength = 100, BuildingWidth = 50, BuildingHeight = 70 };
            var v = Viewer(b, WindViewMode.CladdingSuction);
            Render(v, "out_of_limits");
            var warn = (FrameworkElement)v.FindName("bdWarnings");
            Assert.AreEqual(Visibility.Visible, warn.Visibility);
        }
    }
}
