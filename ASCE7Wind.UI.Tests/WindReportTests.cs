using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ASCE7WindLoadCalculator;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ASCE7Wind.UI.Tests
{
    /// <summary>
    /// The printable report: built from the Detailed calculations pages, paginated onto landscape Letter pages with
    /// header and footer, nothing wider or taller than the printable area. Set SNAPSHOT_DIR to save each page as a PNG.
    /// </summary>
    [TestClass]
    public class WindReportTests
    {
        private static FixedDocument Build(RoofTypes roof, double pitch, IEnumerable<ReportSection> extra = null)
        {
            var p = new WindParameters_Base { WindSpeed = 115, ExposureCategory = WindExposureCategories.WIND_EXP_CAT_C, RiskCategory = "II" };
            var b = new BuildingData { BuildingLength = 60, BuildingWidth = 40, BuildingHeight = 15, RoofType = roof, RoofPitch = pitch };
            var a = WindAnalysis.Run(ASCE7_Versions.ASCE_VER_7_22, p, b);
            return WindReport.Build(new ReportInfo { Project = "Test project", Engineer = "J. Engineer" }, ASCE7_Versions.ASCE_VER_7_22, p, b, a, extra);
        }

        private static IEnumerable<string> Texts(DependencyObject root)
        {
            if (root is TextBlock t) yield return t.Text;
            foreach (var child in LogicalTreeHelper.GetChildren(root).OfType<DependencyObject>())
                foreach (var s in Texts(child)) yield return s;
        }

        private static FixedPage Page(FixedDocument doc, int i) => doc.Pages[i].Child;

        [STATestMethod]
        [DataRow(RoofTypes.ROOF_TYPE_GABLE, 22.0)]
        [DataRow(RoofTypes.ROOF_TYPE_FLAT, 0.0)]
        [DataRow(RoofTypes.ROOF_TYPE_HIP, 25.0)]
        public void Report_HasCoverAndEveryWindSection_NumberedPages(RoofTypes roof, double pitch)
        {
            var doc = Build(roof, pitch);
            Assert.IsTrue(doc.Pages.Count >= 6, "cover + 5 sections");
            var all = Enumerable.Range(0, doc.Pages.Count).Select(i => string.Join("\n", Texts(Page(doc, i)))).ToList();

            StringAssert.Contains(all[0], "Wind load and shear wall calculations");
            StringAssert.Contains(all[0], "J. Engineer");
            foreach (var section in new[] { WindDetailsView.InputsPage, WindDetailsView.CladdingPage, WindDetailsView.WestPage,
                                            WindDetailsView.SouthPage, WindDetailsView.CasesPage })
                Assert.IsTrue(all.Any(t => t.Contains("Test project  ·  " + section)), section);
            for (int i = 0; i < all.Count; i++)
                StringAssert.Contains(all[i], $"Page {i + 1} of {all.Count}");
            Assert.IsTrue(all.Any(t => t.Contains("qh")), "velocity pressure is in the report");
        }

        [STATestMethod]
        public void EveryBlock_FitsInsideThePrintableArea()
        {
            var doc = Build(RoofTypes.ROOF_TYPE_GABLE, 22);
            for (int i = 0; i < doc.Pages.Count; i++)
            {
                var page = Page(doc, i);
                foreach (var child in page.Children.OfType<FrameworkElement>())
                {
                    double left = FixedPage.GetLeft(child), top = FixedPage.GetTop(child);
                    Assert.IsTrue(left + child.ActualWidth <= WindReport.PageWidth - WindReport.Margin + 1, $"page {i + 1} too wide");
                    Assert.IsTrue(top + child.ActualHeight <= WindReport.PageHeight - WindReport.Margin + 1,
                                  $"page {i + 1}: content runs into the bottom margin ({top + child.ActualHeight:0})");
                }
            }
            Snapshot(doc, "report_gable");
        }

        [STATestMethod]
        public void TablesPrintEveryRow()
        {
            var doc = Build(RoofTypes.ROOF_TYPE_GABLE, 22);
            var grids = Enumerable.Range(0, doc.Pages.Count).SelectMany(i => Descendants(Page(doc, i))).OfType<DataGrid>().ToList();
            Assert.IsTrue(grids.Count > 0);
            foreach (var g in grids)
            {
                int items = g.Items.Count;
                int rows = Enumerable.Range(0, items).Count(r => g.ItemContainerGenerator.ContainerFromIndex(r) != null);
                Assert.AreEqual(items, rows, "every row is generated (no virtualization) so the paper shows it");
            }
        }

        [STATestMethod]
        public void HostSections_AreAddedAtTheEnd_LongTablesSplit()
        {
            var rows = Enumerable.Range(1, 60).Select(i => new[] { "W" + i, (i * 1.5).ToString("0.0") }).ToList();
            var blocks = new List<FrameworkElement> { WindReport.Heading("Shear walls"), WindReport.Para("Rigid diaphragm.") };
            blocks.AddRange(WindReport.Tables(new[] { "Wall", "V (k)" }, rows));
            var doc = Build(RoofTypes.ROOF_TYPE_FLAT, 0, new[] { new ReportSection("Shear walls", blocks) });

            var last = string.Join("\n", Texts(Page(doc, doc.Pages.Count - 1)));
            StringAssert.Contains(last, "Shear walls");
            StringAssert.Contains(last, "W60");
            var text = string.Join("\n", Enumerable.Range(0, doc.Pages.Count).Select(i => string.Join("\n", Texts(Page(doc, i)))));
            Assert.AreEqual(3, text.Split('\n').Count(s => s == "Wall"), "60 rows in tables of 26 repeat the header 3 times");
        }

        [STATestMethod]
        public void NoResults_StillGivesAReport()
        {
            var doc = WindReport.Build(new ReportInfo(), ASCE7_Versions.ASCE_VER_7_22, null, null, null);
            Assert.AreEqual(2, doc.Pages.Count);
            StringAssert.Contains(string.Join("\n", Texts(Page(doc, 1))), "No wind results");
        }

        /// <summary>
        /// Each wind direction's page works its wall pressures out by hand: its own L (along the wind) and B (normal to it),
        /// the leeward Cp from that L/B (60 x 40 ft: 1.5 from the west, -0.40 interpolated; 0.667 from the south, -0.5), qz up
        /// the windward wall to qh at h, qh on the leeward and side walls; and the block fits the printed page.
        /// </summary>
        [STATestMethod]
        [DataRow(ASCE7_Versions.ASCE_VER_7_16)]
        [DataRow(ASCE7_Versions.ASCE_VER_7_22)]
        public void MwfrsPages_HandCalculation_ThisDirectionsLAndB(ASCE7_Versions v)
        {
            var p = new WindParameters_Base { WindSpeed = 115, ExposureCategory = WindExposureCategories.WIND_EXP_CAT_C, RiskCategory = "II" };
            var b = new BuildingData { BuildingLength = 60, BuildingWidth = 40, BuildingHeight = 25, RoofType = RoofTypes.ROOF_TYPE_GABLE, RoofPitch = 22 };
            var a = WindAnalysis.Run(v, p, b);
            var doc = WindReport.Build(new ReportInfo { Project = "Test project" }, v, p, b, a, null);
            var blocks = Enumerable.Range(0, doc.Pages.Count).SelectMany(i => Descendants(Page(doc, i))).OfType<TextBlock>()
                                   .Where(t => t.Text.StartsWith("1. Dimensions for wind from the", StringComparison.Ordinal)).ToList();
            Assert.AreEqual(2, blocks.Count, "one on each direction's page");
            string west = blocks.Single(t => t.Text.Contains("from the west")).Text, south = blocks.Single(t => t.Text.Contains("from the south")).Text;

            StringAssert.Contains(west, "L = 60 ft");
            StringAssert.Contains(west, "B = 40 ft");
            StringAssert.Contains(west, "Cp = −0.5 + (1.500 − 1) × (−0.3 + 0.5) / (2 − 1) = -0.400");
            StringAssert.Contains(south, "L = 40 ft");
            StringAssert.Contains(south, "B = 60 ft");
            StringAssert.Contains(south, "L/B = 0.667 ≤ 1: Cp = −0.5");

            var m = (WindLoadCalculator_MWFRS)a.MwfrsAlongLength;
            double h = b.MwfrsFigureHeight, qh = m.CalculateDynamicWindPressure(h);
            StringAssert.Contains(west, $"q = qh = {qh:0.00} psf");
            StringAssert.Contains(west, "0 to 15");
            StringAssert.Contains(west, $"{h:0.##} (h)");
            StringAssert.Contains(west, $"{m.WindwardWallExternalPressureAt(15):0.00}", "qz G Cp at 15 ft");
            StringAssert.Contains(west, $"{m.WindwardWallExternalPressureAt(h):0.00}", "qh G Cp at h");
            Assert.IsTrue(m.WindwardWallExternalPressureAt(15) < m.WindwardWallExternalPressureAt(h), "qz grows up the wall");
            foreach (var t in blocks)
            {
                t.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                Assert.IsTrue(t.DesiredSize.Width <= WindReport.BodyWidth, $"{t.DesiredSize.Width:0} px wide");
            }
            Snapshot(doc, "report_handcalc_" + v);
        }

        private static IEnumerable<DependencyObject> Descendants(DependencyObject root)
        {
            foreach (var child in LogicalTreeHelper.GetChildren(root).OfType<DependencyObject>())
            {
                yield return child;
                foreach (var d in Descendants(child)) yield return d;
            }
        }

        private static void Snapshot(FixedDocument doc, string name)
        {
            string dir = Environment.GetEnvironmentVariable("SNAPSHOT_DIR");
            if (string.IsNullOrEmpty(dir)) return;
            Directory.CreateDirectory(dir);
            for (int i = 0; i < doc.Pages.Count; i++)
            {
                var page = Page(doc, i);
                var bmp = new RenderTargetBitmap((int)WindReport.PageWidth, (int)WindReport.PageHeight, 96, 96, PixelFormats.Pbgra32);
                bmp.Render(page);
                var enc = new PngBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(bmp));
                using (var f = File.Create(Path.Combine(dir, $"{name}_{i + 1:00}.png"))) enc.Save(f);
            }
        }
    }
}
