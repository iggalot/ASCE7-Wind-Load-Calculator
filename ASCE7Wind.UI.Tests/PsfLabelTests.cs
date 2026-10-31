using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ASCE7WindLoadCalculator;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ASCE7Wind.UI.Tests
{
    /// <summary>
    /// The 3D viewer's "psf labels" option: each zone that is big enough on screen shows its net pressure, the text is the
    /// zone's own value, labels never overlap, and the choice is remembered. Set SNAPSHOT_DIR to save a PNG.
    /// </summary>
    [TestClass]
    public class PsfLabelTests
    {
        private const int W = 1280, H = 760;
        private string prefs;

        [TestInitialize]
        public void Init()
        {
            prefs = Path.Combine(Path.GetTempPath(), "w7-prefs-" + Guid.NewGuid().ToString("N") + ".txt");
            ViewerPreferences.FilePath = prefs;     // never touch the user's own preferences
        }

        [TestCleanup]
        public void Cleanup() { if (File.Exists(prefs)) File.Delete(prefs); }

        private static Wind3DViewerControl Viewer(WindViewMode mode, bool labels)
        {
            var b = new BuildingData { BuildingLength = 80, BuildingWidth = 50, BuildingHeight = 20, RoofType = RoofTypes.ROOF_TYPE_GABLE, RoofPitch = 25};
            WindLoadCalculator_Base Run(BuildingData x, WindLoadCalculationTypes t)
            {
                var p = new WindParameters_Base { WindSpeed = 115, ExposureCategory = WindExposureCategories.WIND_EXP_CAT_C, AnalysisType = t };
                var c = WindLoadCalculatorFactory.Create(ASCE7_Versions.ASCE_VER_7_22, t, p, x);
                c.Initialize();
                return c;
            }
            var rotated = b.Clone();
            rotated.RotateBuilding();
            var v = new Wind3DViewerControl(b, Run(b, WindLoadCalculationTypes.COMPONENT_AND_CLADDING), Run(b, WindLoadCalculationTypes.MWFRS),
                                            Run(rotated, WindLoadCalculationTypes.MWFRS)) { Width = W, Height = H };
            v.Mode = mode;
            ((CheckBox)v.FindName("cbPsfLabels")).IsChecked = labels;
            v.Measure(new Size(W, H));
            v.Arrange(new Rect(0, 0, W, H));
            v.UpdateLayout();
            v.ResetView();
            v.UpdateLayout();
            return v;
        }

        private static TextBlock[] Shown(Wind3DViewerControl v) =>
            ((Canvas)v.FindName("labelLayer")).Children.OfType<TextBlock>()
                .Where(t => t.Name != "lblNorth" && t.Name != "lblWind" && t.Visibility == Visibility.Visible).ToArray();

        [STATestMethod]
        public void Labels_ShowZonePressures_WithoutOverlap_AndAreRemembered()
        {
            var v = Viewer(WindViewMode.MwfrsAlongLength, labels: true);
            var shown = Shown(v);
            Assert.IsTrue(shown.Length >= 3, $"only {shown.Length} labels placed");

            var values = v.Scene.Zones.Where(z => z.HasPressure).Select(z => z.PressurePsf.ToString("+0.0;−0.0")).ToList();
            foreach (var t in shown) CollectionAssert.Contains(values, t.Text, "a label is a zone's own pressure");

            var rects = shown.Select(t => new Rect(Canvas.GetLeft(t), Canvas.GetTop(t), t.ActualWidth > 0 ? t.ActualWidth : t.DesiredSize.Width, t.DesiredSize.Height)).ToList();
            for (int i = 0; i < rects.Count; i++)
                for (int j = i + 1; j < rects.Count; j++)
                    Assert.IsFalse(rects[i].IntersectsWith(rects[j]), $"'{shown[i].Text}' overlaps '{shown[j].Text}'");

            Assert.IsTrue(ViewerPreferences.PsfLabels, "the choice is remembered");
            Snapshot(v, "psf_labels_mwfrs");

            var cc = Viewer(WindViewMode.CladdingSuction, labels: true);
            Assert.IsTrue(Shown(cc).Length >= 3, "cladding zones are labelled too");
            Snapshot(cc, "psf_labels_cladding");
        }

        [STATestMethod]
        public void LabelsOff_ShowsNone()
        {
            var v = Viewer(WindViewMode.MwfrsAlongLength, labels: false);
            Assert.AreEqual(0, Shown(v).Length);
            Assert.IsFalse(ViewerPreferences.PsfLabels);
        }

        private static void Snapshot(FrameworkElement e, string name)
        {
            string dir = Environment.GetEnvironmentVariable("SNAPSHOT_DIR");
            if (string.IsNullOrEmpty(dir)) return;
            Directory.CreateDirectory(dir);
            var bmp = new RenderTargetBitmap(W, H, 96, 96, PixelFormats.Pbgra32);
            bmp.Render(e);
            var enc = new PngBitmapEncoder();
            enc.Frames.Add(BitmapFrame.Create(bmp));
            using (var fs = File.Create(Path.Combine(dir, name + ".png"))) enc.Save(fs);
        }
    }
}
