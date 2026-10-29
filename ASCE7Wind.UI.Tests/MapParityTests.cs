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
    /// Renders the Wind Input tab's 2D zone maps and the 3D viewer's top view for the same building,
    /// from the very same calculators, so the two can be compared side by side (SNAPSHOT_DIR).
    /// </summary>
    [TestClass]
    public class MapParityTests
    {
        public static System.Collections.Generic.IEnumerable<object[]> Cases()
        {
            yield return new object[] { "gable_par22", 60.0, 40.0, RoofTypes.ROOF_TYPE_GABLE, 22.0, RidgeDirections.RIDGE_DIR_PARALLEL_TO_BLDGLENGTH };
            yield return new object[] { "gable_perp30", 60.0, 40.0, RoofTypes.ROOF_TYPE_GABLE, 30.0, RidgeDirections.RIDGE_DIR_PERP_TO_BLDGLENGTH };
            yield return new object[] { "hip20", 60.0, 40.0, RoofTypes.ROOF_TYPE_HIP, 20.0, RidgeDirections.RIDGE_DIR_NONE };
            yield return new object[] { "hip20_narrow", 40.0, 60.0, RoofTypes.ROOF_TYPE_HIP, 20.0, RidgeDirections.RIDGE_DIR_NONE };
        }

        private static void Save(FrameworkElement e, int w, int h, string name)
        {
            string dir = Environment.GetEnvironmentVariable("SNAPSHOT_DIR");
            if (string.IsNullOrEmpty(dir)) return;
            Directory.CreateDirectory(dir);
            var bmp = new RenderTargetBitmap(w, h, 96, 96, PixelFormats.Pbgra32);
            bmp.Render(e);
            var enc = new PngBitmapEncoder();
            enc.Frames.Add(BitmapFrame.Create(bmp));
            using (var fs = File.Create(Path.Combine(dir, name + ".png"))) enc.Save(fs);
        }

        [STATestMethod]
        [DynamicData(nameof(Cases))]
        public void Maps_And3DTopView_FromSameCalculators(string name, double L, double B, RoofTypes roof, double pitch, RidgeDirections ridge)
        {
            var b = new BuildingData { BuildingLength = L, BuildingWidth = B, BuildingHeight = 15, RoofType = roof, RoofPitch = pitch, RidgeDirection = ridge };
            var p = new WindParameters_Base { WindSpeed = 115, ExposureCategory = WindExposureCategories.WIND_EXP_CAT_C };
            var input = new WindLoadInputControl(b, ASCE7_Versions.ASCE_VER_7_22, p);

            WindLoadCalculator_Base cc = null, mL = null, mW = null;
            input.WindInputComplete += (s, e) => { cc = e._windLoadCalculator_CC; mL = e._windLoadCalculator_MWFRS_Length; mW = e._windLoadCalculator_MWFRS_Width; };

            var win = new Window
            {
                Content = input, Width = 1400, Height = 1000, Left = -20000, Top = -20000,
                ShowInTaskbar = false, ShowActivated = false, WindowStyle = WindowStyle.None,
            };
            win.Show();
            input.UpdateLayout();
            input.Update();          // compute and draw the maps
            input.UpdateLayout();
            Save(input, (int)input.ActualWidth, (int)input.ActualHeight, name + "_2Dmaps");

            Assert.IsNotNull(cc, "wind input did not produce calculators");
            foreach (var mode in new[] { WindViewMode.CladdingSuction, WindViewMode.MwfrsAlongLength, WindViewMode.MwfrsAlongWidth })
            {
                var v = new Wind3DViewerControl(b, cc, mL, mW) { Width = 900, Height = 700 };
                v.Mode = mode;
                v.Measure(new Size(900, 700));
                v.Arrange(new Rect(0, 0, 900, 700));
                v.UpdateLayout();
                v.ShowTopView();
                v.UpdateLayout();
                Save(v, 900, 700, $"{name}_3Dtop_{mode}");
            }
            win.Close();
        }
    }
}
