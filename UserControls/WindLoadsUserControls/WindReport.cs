using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace ASCE7WindLoadCalculator
{
    /// <summary>One part of the printed report: a heading (shown in the page header) and its blocks, in order.</summary>
    public sealed class ReportSection
    {
        public ReportSection(string title, IList<FrameworkElement> blocks)
        {
            Title = title;
            Blocks = blocks ?? new List<FrameworkElement>();
        }

        public string Title { get; }
        public IList<FrameworkElement> Blocks { get; }
    }

    /// <summary>What goes on the cover and in the page header and footer.</summary>
    public sealed class ReportInfo
    {
        public string Project { get; set; } = "Untitled project";
        public string Program { get; set; } = "Shear Wall Calculator";
        public string Engineer { get; set; } = string.Empty;
        public DateTime Date { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// The printable calculation report: a cover, the Detailed calculations pages (built by
    /// <see cref="WindDetailsView"/>, so the report matches the screen), then any sections the host adds
    /// (e.g. the shear-wall results). Landscape Letter pages; each section starts on a new page, and a block
    /// too large for a page is scaled down to fit.
    /// </summary>
    public static class WindReport
    {
        public const double PageWidth = 11 * 96, PageHeight = 8.5 * 96, Margin = 48;
        private const double HeaderHeight = 30, FooterHeight = 26, BlockGap = 10, HeadingAllowance = 80;

        public static double BodyWidth => PageWidth - 2 * Margin;
        public static double BodyHeight => PageHeight - 2 * Margin - HeaderHeight - FooterHeight;

        private static readonly Brush Ink = Frozen("#0B0B0B"), InkSecondary = Frozen("#52514E"), Hairline = Frozen("#C9C8C1"),
                                      HeaderFill = Frozen("#F0EFEC");

        public static FixedDocument Build(ReportInfo info, ASCE7_Versions version, WindParameters_Base parameters,
            BuildingData building, WindAnalysisResult analysis, IEnumerable<ReportSection> extraSections = null)
        {
            info = info ?? new ReportInfo();
            var view = new WindDetailsView();
            view.SetResults(version, parameters, building, analysis);

            var sections = new List<ReportSection> { Cover(info, view.InputsSummary, version) };
            var wind = view.BuildReportSections();
            if (wind.Count == 0)
                sections.Add(new ReportSection("Wind", new List<FrameworkElement> { Para("No wind results: the wind inputs cannot be calculated.") }));
            sections.AddRange(wind);
            if (extraSections != null) sections.AddRange(extraSections);

            return Paginate(info, Asce7Edition.For(version).Name, sections);
        }

        /// <summary>
        /// A document of any sections (no cover, no wind pages) on the same pages as the report: landscape Letter, the
        /// project and section in the header, <paramref name="subtitle"/> at the header's right (where the report names
        /// the ASCE 7 edition), date and page numbers in the footer. The shear-wall designer prints its hold-down catalog so.
        /// </summary>
        public static FixedDocument Document(ReportInfo info, string subtitle, IEnumerable<ReportSection> sections) =>
            Paginate(info ?? new ReportInfo(), subtitle ?? string.Empty, sections ?? Enumerable.Empty<ReportSection>());

        // ------------------------------------------------------------------ pagination

        private sealed class PageBlocks
        {
            public string Section;
            public readonly List<FrameworkElement> Blocks = new List<FrameworkElement>();
        }

        private static FixedDocument Paginate(ReportInfo info, string edition, IEnumerable<ReportSection> sections)
        {
            var pages = new List<PageBlocks>();
            foreach (var section in sections)
            {
                var blocks = section.Blocks.Select(FitToPage).ToList();
                var heights = blocks.Select(b => LaidOutHeight(b, BodyWidth)).ToList();

                var page = new PageBlocks { Section = section.Title };
                pages.Add(page);
                double used = 0;
                for (int i = 0; i < blocks.Count; i++)
                {
                    // A heading stays with the block after it (never alone at the foot of a page).
                    double needed = heights[i];
                    if (IsHeading(blocks[i]) && i + 1 < blocks.Count) needed += BlockGap + heights[i + 1];
                    if (used > 0 && used + needed > BodyHeight)
                    {
                        page = new PageBlocks { Section = section.Title };
                        pages.Add(page);
                        used = 0;
                    }
                    page.Blocks.Add(blocks[i]);
                    used += heights[i] + BlockGap;
                }
            }

            var doc = new FixedDocument();
            doc.DocumentPaginator.PageSize = new Size(PageWidth, PageHeight);
            for (int i = 0; i < pages.Count; i++)
            {
                var pc = new PageContent();
                ((System.Windows.Markup.IAddChild)pc).AddChild(BuildPage(info, edition, pages[i], i + 1, pages.Count));
                doc.Pages.Add(pc);
            }
            return doc;
        }

        private static bool IsHeading(FrameworkElement block) =>
            block is TextBlock t && t.FontWeight.ToOpenTypeWeight() >= FontWeights.SemiBold.ToOpenTypeWeight();

        /// <summary>
        /// Lays the block out at the body width. Content wider than that (a table with many columns is clipped by its
        /// own scroll area rather than reporting its width) is laid out at its full width and scaled down to fit, as
        /// is anything taller than a page.
        /// </summary>
        private static FrameworkElement FitToPage(FrameworkElement block)
        {
            PrepareForPrint(block);
            if (block.HorizontalAlignment == HorizontalAlignment.Stretch && !(block is Panel))
                block.HorizontalAlignment = HorizontalAlignment.Left;

            double width = BodyWidth, height = 0;
            for (int pass = 0; pass < 4; pass++)
            {
                height = LaidOutHeight(block, width);
                double needed = Math.Max(block.ActualWidth, block.DesiredSize.Width) + Overflow(block);
                if (needed <= width + 1) break;
                width = needed;
            }

            // Tall blocks leave room for the heading that introduces them, so the two fit on one page.
            double scale = Math.Min(1.0, Math.Min(BodyWidth / width, (BodyHeight - HeadingAllowance) / Math.Max(height, 1)));
            if (scale >= 0.999) return block;

            // The block keeps the size it was laid out at; the Viewbox (given its final size) scales it.
            var inner = new Border { Width = width, Height = height, Child = block };
            return new Viewbox
            {
                Stretch = Stretch.Uniform,
                Width = width * scale,
                Height = height * scale,
                HorizontalAlignment = HorizontalAlignment.Left,
                Child = inner,
            };
        }

        /// <summary>How much wider a scroll area's content is than the area (tables clip instead of growing).</summary>
        private static double Overflow(DependencyObject root)
        {
            double extra = 0;
            if (root is ScrollViewer sv && sv.ViewportWidth > 0)
                extra = Math.Max(0, sv.ExtentWidth - sv.ViewportWidth);
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
                extra = Math.Max(extra, Overflow(VisualTreeHelper.GetChild(root, i)));
            return extra;
        }

        /// <summary>
        /// The height the block takes at this width. It is laid out in a host a few times, because a table only
        /// creates its rows during layout (a single Measure of a detached table under-reports its height).
        /// </summary>
        private static double LaidOutHeight(FrameworkElement block, double width)
        {
            var host = new StackPanel { Width = width };
            host.Children.Add(block);
            for (int i = 0; i < 3; i++)
            {
                host.Measure(new Size(width, double.PositiveInfinity));
                host.Arrange(new Rect(host.DesiredSize));
                host.UpdateLayout();
            }
            double height = Math.Max(block.ActualHeight, block.DesiredSize.Height);
            host.Children.Remove(block);
            return height;
        }

        /// <summary>Tables show every row (no virtualization, no scroll bars) so nothing is cut off on paper.</summary>
        private static void PrepareForPrint(DependencyObject root)
        {
            if (root is DataGrid g)
            {
                g.EnableRowVirtualization = false;
                g.EnableColumnVirtualization = false;
                VirtualizingPanel.SetIsVirtualizing(g, false);
                g.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
                g.HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden; // not Disabled: that squeezes the columns and hides the overflow
                g.MaxHeight = double.PositiveInfinity;
            }
            foreach (var child in LogicalTreeHelper.GetChildren(root).OfType<DependencyObject>())
                PrepareForPrint(child);
        }

        private static FixedPage BuildPage(ReportInfo info, string edition, PageBlocks content, int number, int count)
        {
            var page = new FixedPage { Width = PageWidth, Height = PageHeight, Background = Brushes.White };

            var header = new Grid { Width = BodyWidth };
            header.ColumnDefinitions.Add(new ColumnDefinition());
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            header.Children.Add(Text($"{info.Project}  ·  {content.Section}", 11, FontWeights.SemiBold, Ink));
            var right = Text(edition, 11, FontWeights.Normal, InkSecondary);
            Grid.SetColumn(right, 1);
            header.Children.Add(right);
            var headerLine = new Border { BorderBrush = Hairline, BorderThickness = new Thickness(0, 0, 0, 1), Child = header, Width = BodyWidth, Padding = new Thickness(0, 0, 0, 4) };
            FixedPage.SetLeft(headerLine, Margin);
            FixedPage.SetTop(headerLine, Margin);
            page.Children.Add(headerLine);

            var body = new StackPanel { Width = BodyWidth };
            foreach (var block in content.Blocks)
            {
                block.Margin = new Thickness(0, 0, 0, BlockGap);
                body.Children.Add(block);
            }
            FixedPage.SetLeft(body, Margin);
            FixedPage.SetTop(body, Margin + HeaderHeight);
            page.Children.Add(body);

            var footer = new Grid { Width = BodyWidth };
            footer.ColumnDefinitions.Add(new ColumnDefinition());
            footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            footer.Children.Add(Text($"{info.Program}  ·  {info.Date:yyyy-MM-dd HH:mm}" +
                                     (string.IsNullOrWhiteSpace(info.Engineer) ? "" : $"  ·  {info.Engineer}"), 10, FontWeights.Normal, InkSecondary));
            var pageNo = Text($"Page {number} of {count}", 10, FontWeights.Normal, InkSecondary);
            Grid.SetColumn(pageNo, 1);
            footer.Children.Add(pageNo);
            FixedPage.SetLeft(footer, Margin);
            FixedPage.SetTop(footer, PageHeight - Margin - FooterHeight + 8);
            page.Children.Add(footer);

            var size = new Size(PageWidth, PageHeight);
            page.Measure(size);
            page.Arrange(new Rect(size));
            page.UpdateLayout();
            return page;
        }

        // ------------------------------------------------------------------ cover and building blocks for hosts

        private static ReportSection Cover(ReportInfo info, string inputs, ASCE7_Versions version)
        {
            var blocks = new List<FrameworkElement>
            {
                Text("Wind load and shear wall calculations", 26, FontWeights.SemiBold, Ink),
                Text(info.Project, 18, FontWeights.Normal, Ink),
                Para(inputs),
                Table(new[] { "Item", "Value" }, new[]
                {
                    new[] { "Wind loads", Asce7Edition.For(version).Name + ", Chapters 26, 27 (MWFRS, directional) and 30 (C&C, Part 1)" },
                    new[] { "Date", info.Date.ToString("MMMM d, yyyy") },
                    new[] { "Program", info.Program },
                    new[] { "Prepared by", string.IsNullOrWhiteSpace(info.Engineer) ? "______________________" : info.Engineer },
                    new[] { "Checked by", "______________________" },
                }),
                Para("Pressures are strength-level (ASCE 7-16/7-22 wind maps); allowable stress design uses 0.6W. " +
                     "Section, table and figure numbers refer to the ASCE 7 edition named above unless another standard is named. " +
                     "These calculations are a design aid: the engineer of record is responsible for checking them."),
            };
            return new ReportSection("Cover", blocks);
        }

        /// <summary>A report heading, for host sections.</summary>
        public static TextBlock Heading(string text) => Text(text, 18, FontWeights.SemiBold, Ink);

        /// <summary>A wrapped paragraph, for host sections.</summary>
        public static TextBlock Para(string text)
        {
            var t = Text(text ?? string.Empty, 12, FontWeights.Normal, Ink);
            t.TextWrapping = TextWrapping.Wrap;
            t.MaxWidth = BodyWidth;
            return t;
        }

        /// <summary>A simple ruled table (all text), for host sections. Long tables are split across pages.</summary>
        public static FrameworkElement Table(string[] headers, IEnumerable<string[]> rows)
        {
            var grid = new Grid { HorizontalAlignment = HorizontalAlignment.Left };
            for (int c = 0; c < headers.Length; c++)
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var all = new List<string[]> { headers };
            all.AddRange(rows);
            for (int r = 0; r < all.Count; r++)
            {
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                for (int c = 0; c < headers.Length; c++)
                {
                    string value = c < all[r].Length ? all[r][c] : string.Empty;
                    var cell = new Border
                    {
                        BorderBrush = Hairline,
                        BorderThickness = new Thickness(0, 0, 0, 1),
                        Background = r == 0 ? HeaderFill : Brushes.Transparent,
                        Padding = new Thickness(8, 3, 14, 3),
                        Child = Text(value, 12, r == 0 ? FontWeights.SemiBold : FontWeights.Normal, Ink),
                    };
                    Grid.SetRow(cell, r);
                    Grid.SetColumn(cell, c);
                    grid.Children.Add(cell);
                }
            }
            return grid;
        }

        /// <summary>Splits a long table into page-sized tables that each repeat the header row.</summary>
        public static IEnumerable<FrameworkElement> Tables(string[] headers, IList<string[]> rows, int rowsPerTable = 26)
        {
            if (rows.Count == 0) yield return Table(headers, rows);
            for (int i = 0; i < rows.Count; i += rowsPerTable)
                yield return Table(headers, rows.Skip(i).Take(rowsPerTable));
        }

        private static TextBlock Text(string text, double size, FontWeight weight, Brush brush) =>
            new TextBlock { Text = text, FontSize = size, FontWeight = weight, Foreground = brush, FontFamily = new FontFamily("Segoe UI") };

        private static Brush Frozen(string hex)
        {
            var b = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
            b.Freeze();
            return b;
        }
    }
}
