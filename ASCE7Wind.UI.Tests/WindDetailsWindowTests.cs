using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ASCE7WindLoadCalculator;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ASCE7Wind.UI.Tests
{
    /// <summary>
    /// The "Detailed calculations" window (replacement for the CC Wind Calcs / MWFRS 1 / MWFRS 2 tabs):
    /// every page builds, its tables hold the calculated values, and the GCp graphs are drawn from the
    /// curves for this building with its zones marked. Set SNAPSHOT_DIR to save a PNG of each page.
    /// </summary>
    [TestClass]
    public class WindDetailsWindowTests
    {
        private static readonly string[] Pages =
        {
            WindDetailsWindow.InputsPage, WindDetailsWindow.CladdingPage, WindDetailsWindow.WestPage,
            WindDetailsWindow.SouthPage, WindDetailsWindow.CasesPage,
        };

        private static WindDetailsWindow Make(BuildingData b, out WindAnalysisResult analysis)
        {
            var p = new WindParameters_Base { WindSpeed = 115, ExposureCategory = WindExposureCategories.WIND_EXP_CAT_C, RiskCategory = "II" };
            analysis = WindAnalysis.Run(ASCE7_Versions.ASCE_VER_7_22, p, b);
            return new WindDetailsWindow(ASCE7_Versions.ASCE_VER_7_22, p, b, analysis);
        }

        private static IEnumerable<DependencyObject> Descendants(DependencyObject root)
        {
            foreach (object child in LogicalTreeHelper.GetChildren(root))
            {
                if (!(child is DependencyObject d)) continue;
                yield return d;
                foreach (var x in Descendants(d)) yield return x;
            }
        }

        [STATestMethod]
        [DataRow(RoofTypes.ROOF_TYPE_GABLE, 22.0)]
        [DataRow(RoofTypes.ROOF_TYPE_FLAT, 0.0)]
        [DataRow(RoofTypes.ROOF_TYPE_HIP, 25.0)]
        public void EveryPage_Builds_WithTablesAndDrawings(RoofTypes roof, double pitch)
        {
            var w = Make(new BuildingData { BuildingLength = 60, BuildingWidth = 40, BuildingHeight = 15, RoofType = roof, RoofPitch = pitch }, out _);
            foreach (var page in Pages)
            {
                w.ShowPage(page);
                Assert.AreEqual(page, w.CurrentPage);
                var content = w.CurrentContent;
                Assert.IsNotNull(content, page);
                var all = Descendants(content).ToList();

                if (page != WindDetailsWindow.InputsPage)
                    Assert.IsTrue(all.OfType<DataGrid>().Any(g => g.Items.Count > 0), page + " has a filled table");
                if (page == WindDetailsWindow.CladdingPage || page == WindDetailsWindow.WestPage || page == WindDetailsWindow.SouthPage)
                    Assert.IsTrue(all.OfType<Canvas>().Count(cv => cv.Children.Count > 0) >= 3, page + " has its drawings");

                Snapshot(content, $"details_{roof}_{Array.IndexOf(Pages, page) + 1}");
            }
        }

        /// <summary>The cladding graphs come from this building's curves and mark each zone at its area.</summary>
        [STATestMethod]
        public void CladdingGraphs_MarkEveryZone()
        {
            var w = Make(new BuildingData { BuildingLength = 60, BuildingWidth = 40, BuildingHeight = 15, RoofType = RoofTypes.ROOF_TYPE_GABLE, RoofPitch = 22 }, out var a);
            w.ShowPage(WindDetailsWindow.CladdingPage);
            var graphs = Descendants(w.CurrentContent).OfType<Canvas>().Where(c => c.Width == 460).ToList();
            Assert.AreEqual(2, graphs.Count, "roof and wall GCp graphs");
            int roofZones = a.Cladding.RoofAreaCalculator.effWindAreas.Values.Select(z => (z.Label_Short, Math.Round(z.Area, 1))).Distinct().Count();
            int roofDots = graphs[0].Children.OfType<Ellipse>().Count();
            Assert.IsTrue(roofDots >= roofZones, $"each roof zone is marked ({roofDots} dots, {roofZones} zones)");
            Assert.IsTrue(graphs[0].Children.OfType<Polyline>().Any(), "the Fig. 30.3 curves are drawn");
        }

        [STATestMethod]
        public void InputsPage_ShowsTheVelocityPressure()
        {
            var w = Make(new BuildingData { BuildingLength = 60, BuildingWidth = 40, BuildingHeight = 15, RoofType = RoofTypes.ROOF_TYPE_FLAT }, out var a);
            w.ShowPage(WindDetailsWindow.InputsPage);
            string qh = a.Cladding.CalculateDynamicWindPressure(15).ToString("0.00") + " psf";
            Assert.IsTrue(Descendants(w.CurrentContent).OfType<TextBlock>().Any(t => t.Text == qh), "qh = " + qh);
        }

        private static void Snapshot(FrameworkElement e, string name)
        {
            string dir = Environment.GetEnvironmentVariable("SNAPSHOT_DIR");
            if (string.IsNullOrEmpty(dir)) return;
            e.Measure(new Size(1150, double.PositiveInfinity));
            e.Arrange(new Rect(0, 0, 1150, e.DesiredSize.Height));
            e.UpdateLayout();
            var bmp = new RenderTargetBitmap(1150, (int)Math.Ceiling(e.DesiredSize.Height), 96, 96, PixelFormats.Pbgra32);
            bmp.Render(e);
            Directory.CreateDirectory(dir);
            var enc = new PngBitmapEncoder();
            enc.Frames.Add(BitmapFrame.Create(bmp));
            using (var fs = File.Create(System.IO.Path.Combine(dir, name + ".png"))) enc.Save(fs);
        }
    }
}
