using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ASCE7WindLoadCalculator
{
    /// <summary>
    /// Every number behind the guided results, organised by topic: the inputs and factors, the cladding
    /// (Chapter 30) tables and GCp graphs, and the whole-building (Chapter 27) tables for each wind direction.
    /// The graphs are drawn from the curves the calculation picked for this building, with this building's
    /// zones marked where they read the curves. It replaces the CC Wind Calcs / MWFRS 1 / MWFRS 2 tabs, and is
    /// shown both as a tab and in its own window (<see cref="WindDetailsWindow"/>).
    /// </summary>
    public partial class WindDetailsView : UserControl
    {
        public const string InputsPage = "Inputs and factors", CladdingPage = "Cladding (C&C)",
                            WestPage = "Shear walls: wind from W", SouthPage = "Shear walls: wind from S",
                            CasesPage = "Shear-wall load cases";

        private ASCE7_Versions version;
        private WindParameters_Base parameters;
        private BuildingData building;
        private WindAnalysisResult analysis;
        private WindLateralResult lateral;
        private readonly Dictionary<string, Func<FrameworkElement>> pages = new Dictionary<string, Func<FrameworkElement>>();
        private readonly Dictionary<string, RadioButton> navButtons = new Dictionary<string, RadioButton>();

        private static readonly Brush Ink = Frozen("#0B0B0B"), InkSecondary = Frozen("#52514E"), Hairline = Frozen("#E1E0D9"),
                                      Surface = Frozen("#FCFCFB"), HeaderFill = Frozen("#F0EFEC"), WarnFill = Frozen("#FFF4E5"),
                                      WarnBorder = Frozen("#E8A33D"), WarnInk = Frozen("#7A4B00"), Suction = Frozen("#C0362C"),
                                      PushIn = Frozen("#2A78D6");

        public WindDetailsView()
        {
            InitializeComponent();
            AddPage(InputsPage, "Velocity pressure, gust and internal pressure factors", BuildInputsPage);
            AddPage(CladdingPage, "Roofing, siding and windows (Chapter 30)", BuildCladdingPage);
            AddPage(WestPage, "Whole building, wind along the length (Chapter 27)", () => BuildMwfrsPage(analysis.MwfrsAlongLength, lateral?.AlongLength, "west"));
            AddPage(SouthPage, "Whole building, wind along the width (Chapter 27)", () => BuildMwfrsPage(analysis.MwfrsAlongWidth, lateral?.AlongWidth, "south"));
            AddPage(CasesPage, "Fig. 27.3-8 cases sent to the shear walls", BuildCasesPage);
            CurrentPage = InputsPage;
            navButtons[InputsPage].IsChecked = true;
            SetResults(version, null, null, null);
        }

        /// <summary>
        /// Shows these results, staying on the current page. Null analysis (inputs that cannot be calculated)
        /// shows a note instead.
        /// </summary>
        public void SetResults(ASCE7_Versions version, WindParameters_Base parameters, BuildingData building, WindAnalysisResult analysis)
        {
            this.version = version;
            this.parameters = parameters;
            this.building = building;
            this.analysis = analysis;
            lateral = analysis?.MwfrsAlongLength != null && analysis.MwfrsAlongWidth != null
                ? WindLateralLoads.Compute(analysis, onlyCases1And3: false) : null;

            tbInputs.Text = HasResults
                ? $"{Asce7Edition.For(version).Name} · V = {parameters.WindSpeed:0.#} mph · Risk Category {parameters.RiskCategory} · " +
                  $"Exposure {ExposureLetter(parameters.ExposureCategory)} · Kzt = {parameters.Kzt:0.00} · " +
                  $"{building.BuildingLength:0.#} × {building.BuildingWidth:0.#} ft, {building.BuildingHeight:0.#} ft walls, " +
                  $"{RoofName(building)} roof, mean roof height h = {building.MeanRoofHeight:0.#} ft"
                : "No results: the wind inputs cannot be calculated yet.";
            double offset = svPage.VerticalOffset;
            ShowPage(CurrentPage);
            svPage.ScrollToVerticalOffset(offset); // a live update keeps the reader's place
        }

        private bool HasResults => analysis != null && parameters != null && building != null;

        /// <summary>The page currently shown (for tests and for opening on a given topic).</summary>
        public string CurrentPage { get; private set; }

        public FrameworkElement CurrentContent => ccPage.Content as FrameworkElement;

        /// <summary>The line of key inputs shown at the top (also the report's summary line).</summary>
        public string InputsSummary => tbInputs.Text;

        private static readonly string[] PageOrder = { InputsPage, CladdingPage, WestPage, SouthPage, CasesPage };

        /// <summary>
        /// Every page, freshly built and split into its blocks (title, paragraphs, figures, tables), for the printed
        /// report: the report is built by the same code as the screen, so the two cannot disagree. Empty when
        /// there are no results.
        /// </summary>
        public IReadOnlyList<ReportSection> BuildReportSections()
        {
            var sections = new List<ReportSection>();
            if (!HasResults) return sections;
            foreach (var name in PageOrder)
            {
                var content = pages[name]();
                var blocks = new List<FrameworkElement>();
                if (content is Panel panel)
                {
                    var children = panel.Children.OfType<FrameworkElement>().ToList();
                    panel.Children.Clear();
                    blocks.AddRange(children);
                }
                else
                {
                    blocks.Add(content);
                }
                sections.Add(new ReportSection(name, blocks));
            }
            return sections;
        }

        public void ShowPage(string name)
        {
            if (!pages.TryGetValue(name, out var build)) return;
            CurrentPage = name;
            navButtons[name].IsChecked = true;
            ccPage.Content = HasResults ? build() : Note("Enter wind inputs that can be calculated to see the details.");
            svPage.ScrollToTop();
        }

        private void AddPage(string name, string sub, Func<FrameworkElement> build)
        {
            pages[name] = build;
            var label = new StackPanel();
            label.Children.Add(new TextBlock { Text = name, FontWeight = FontWeights.SemiBold });
            label.Children.Add(new TextBlock { Text = sub, FontSize = 11, Opacity = 0.75, TextWrapping = TextWrapping.Wrap });
            var rb = new RadioButton { Style = (Style)FindResource("NavItem"), Content = label, GroupName = "nav" + GetHashCode() };
            rb.Checked += (s, e) => { if (CurrentPage != name) ShowPage(name); };
            navButtons[name] = rb;
            spNav.Children.Add(rb);
        }

        // ------------------------------------------------------------------ pages

        private FrameworkElement BuildInputsPage()
        {
            var page = new StackPanel { MaxWidth = 1000, HorizontalAlignment = HorizontalAlignment.Left };
            page.Children.Add(Title("Inputs and factors"));
            var edition = Asce7Edition.For(version);
            var (alpha, zg) = edition.TerrainConstants(parameters.ExposureCategory);
            var calc = analysis.Cladding ?? analysis.MwfrsAlongLength;
            double h = building.MeanRoofHeight;

            var rows = new List<string[]>
            {
                new[] { "Code edition", edition.Name, "" },
                new[] { "Basic wind speed V", $"{parameters.WindSpeed:0.#} mph", "Fig. 26.5-1 (map for the risk category)" },
                new[] { "Risk Category", parameters.RiskCategory, "Table 1.5-1" },
                new[] { "Exposure", ExposureLetter(parameters.ExposureCategory), "Section 26.7" },
                new[] { "Terrain constants α, zg", $"{alpha:0.##}, {zg:0} ft", "Table 26.11-1" },
                new[] { "Mean roof height h", $"{h:0.##} ft", "Section 26.2" },
                new[] { "Plan ratio L/B (wind from W)", (building.BuildingLength / building.BuildingWidth).ToString("0.00"), "Fig. 27.3-1 leeward wall Cp" },
                new[] { "Height ratios h/L, h/B", $"{h / building.BuildingLength:0.00}, {h / building.BuildingWidth:0.00}", "Fig. 27.3-1 roof Cp (h/L in the wind direction)" },
            };
            if (calc != null)
            {
                rows.Add(new[] { "Velocity pressure coefficient Kh", calc.GetKz(h, parameters.ExposureCategory).ToString("0.000"),
                                 $"Table 26.10-1: {edition.KzCoefficient:0.##} (z/zg)^(2/α), z ≥ 15 ft" });
                rows.Add(new[] { "Topographic factor Kzt", parameters.Kzt.ToString("0.00"), "Section 26.8" });
                rows.Add(new[] { "Ground elevation factor Ke", "1.00", "Section 26.9 (1.0 permitted)" });
                rows.Add(new[] { "Directionality factor Kd", calc.GetKd(WindLoadCalculationTypes.MWFRS).ToString("0.00"),
                                 edition.KdInVelocityPressure ? "Table 26.6-1 (in qh)" : "Table 26.6-1 (in the pressure equations)" });
                string qEq = edition.KdInVelocityPressure ? "0.00256 Kh Kzt Ke Kd V²" : "0.00256 Kh Kzt Ke V²";
                rows.Add(new[] { "Velocity pressure qh", $"{calc.CalculateDynamicWindPressure(h):0.00} psf", "Eq. 26.10-1: qh = " + qEq });
                rows.Add(new[] { "Internal pressure GCpi", "±" + calc.GetGCpi().ToString("0.00"), "Table 26.13-1 (" + EnclosureName(building) + ")" });
            }
            rows.Add(new[] { "Gust-effect factor G", parameters.GustFactor.ToString("0.00"), "Section 26.11 (rigid building)" });
            if (analysis.Cladding?.RoofAreaCalculator?.HasCritDim == true)
                rows.Add(new[] { "Cladding zone width a", $"{analysis.Cladding.RoofAreaCalculator.CritDim_a:0.##} ft", "Fig. 30.3-2 notes" });
            page.Children.Add(Card(KeyValueTable(rows)));
            return page;
        }

        private FrameworkElement BuildCladdingPage()
        {
            var cc = analysis.Cladding;
            var page = new StackPanel();
            page.Children.Add(Title("Cladding (components and cladding, Chapter 30)"));
            if (cc == null) { page.Children.Add(Note("Cladding results are not available for these inputs.")); return page; }
            AddWarnings(page, cc);
            if (cc.extGCpCurve_Roof == null || cc.extGCpCurve_Wall == null) return page;
            double roofArea = (cc as WindLoadCalculator_CC)?.RoofComponentArea ?? WindParameters_Base.DefaultComponentArea;
            double wallArea = (cc as WindLoadCalculator_CC)?.WallComponentArea ?? WindParameters_Base.DefaultComponentArea;

            page.Children.Add(Para($"Pressure p = qh [(GCp) − (±GCpi)]. qh = {QhOf(cc):0.00} psf, GCpi = ±{cc.GetGCpi():0.00}." +
                                   (cc.RoofAreaCalculator.HasCritDim ? $" Zone width a = {cc.RoofAreaCalculator.CritDim_a:0.##} ft." : "") +
                                   $" GCp is read at the component's effective wind area (26.2): roof {roofArea:0.#} sq ft, walls {wallArea:0.#} sq ft; " +
                                   $"the zones only set where each curve applies. Net pressures are at least {WindLoadCalculator_CC.MinimumNetPressure:0} psf either way (30.2.2)."));

            // Roof: zones + graph, then the table
            var roofRows = CladdingRows(cc.RoofAreaCalculator.effWindAreas, cc.windPressureRoof_Pos_External, cc.windPressureRoof_Neg_External, roofArea);
            page.Children.Add(Section("Roof"));
            page.Children.Add(FigureRow(
                ZoneDrawing(cc.RoofAreaCalculator, cc.buildingData, "Roof zones (plan)"),
                GCpGraph(cc.extGCpCurve_Roof, roofRows)));
            page.Children.Add(Card(CladdingTable(roofRows)));
            AddOverhangSection(page, cc as WindLoadCalculator_CC);

            // Walls: one graph, a table for each pair of walls
            page.Children.Add(Section("Walls"));
            var lenRows = CladdingRows(cc.WallAreaCalculator_BldgLength.effWindAreas, cc.windPressureBuildingLengthWall_Pos_External,
                                       cc.windPressureBuildingLengthWall_Neg_External, wallArea);
            var widRows = CladdingRows(cc.WallAreaCalculator_BldgWidth.effWindAreas, cc.windPressureBuildingWidthWall_Pos_External,
                                       cc.windPressureBuildingWidthWall_Neg_External, wallArea);
            page.Children.Add(FigureRow(
                ZoneDrawing(cc.WallAreaCalculator_BldgLength, cc.buildingData, "Long walls (S and N faces)"),
                ZoneDrawing(cc.WallAreaCalculator_BldgWidth, cc.buildingData, "End walls (W and E faces)"),
                GCpGraph(cc.extGCpCurve_Wall, lenRows.Concat(widRows).ToList())));
            page.Children.Add(Sub("Long walls (along the length, S and N faces)" + NoteSuffix(cc.WallAreaCalculator_BldgLength.Note)));
            page.Children.Add(Card(CladdingTable(lenRows)));
            page.Children.Add(Sub("End walls (along the width, W and E faces)" + NoteSuffix(cc.WallAreaCalculator_BldgWidth.Note)));
            page.Children.Add(Card(CladdingTable(widRows)));
            return page;
        }

        private FrameworkElement BuildMwfrsPage(WindLoadCalculator_Base m, DirectionalWindForce force, string from)
        {
            var page = new StackPanel();
            page.Children.Add(Title($"Shear walls: wind from the {from} (whole building, Chapter 27)"));
            if (m == null) { page.Children.Add(Note("Whole-building results are not available for these inputs.")); return page; }
            AddWarnings(page, m);

            page.Children.Add(Para($"Pressure p = q G Cp − qh (±GCpi). qh = {QhOf(m):0.00} psf, G = {parameters.GustFactor:0.00}, GCpi = ±{m.GetGCpi():0.00}. " +
                                   "Internal pressure cancels in the total force on the building."));

            // The ratios Fig. 27.3-1 reads for this direction (L is the plan dimension along the wind).
            var dir = m.buildingData;
            string lw = m is WindLoadCalculator_MWFRS mw ? $" → leeward wall Cp = {mw.Cp_LW:0.00}" : string.Empty;
            page.Children.Add(Section("Ratios for this wind direction (Fig. 27.3-1)"));
            page.Children.Add(Card(KeyValueTable(new List<string[]>
            {
                new[] { "Dimension along the wind L", $"{dir.BuildingLength:0.#} ft", "parallel to the wind" },
                new[] { "Dimension across the wind B", $"{dir.BuildingWidth:0.#} ft", "normal to the wind" },
                new[] { "L/B", dir.L_Over_B.ToString("0.00"), "leeward wall Cp" + lw },
                new[] { "h/L", dir.h_Over_L.ToString("0.00"), "roof Cp: windward/leeward roof and distance zones from the windward edge" },
            })));

            if (force != null)
            {
                page.Children.Add(Section("Force on the shear walls"));
                var forceRows = new List<string[]>
                {
                    new[] { "Walls (windward + leeward)", $"{force.WallsKips:0.00} kips", $"projected wall area {force.ProjectedWallArea:0} sq ft" },
                    new[] { "Roof (horizontal part)", $"{force.RoofKips:0.00} kips", $"projected roof area {force.ProjectedRoofArea:0} sq ft, roof Cp: {(force.GoverningRoofCase == MwfrsRoofCase.Alternate ? "other value" : "larger uplift")} governs" },
                };
                if (force.ParapetKips > 0 && m is WindLoadCalculator_MWFRS mp)
                    forceRows.Add(new[] { "Parapets (windward + leeward)", $"{force.ParapetKips:0.00} kips",
                        $"(1.5 + 1.0) × qp {mp.ParapetVelocityPressure:0.00} psf" + (m.Edition.KdInVelocityPressure ? "" : $" × Kd {m.GetKd(m.Parameters.AnalysisType):0.00}") +
                        $" × {m.buildingData.ParapetHeight:0.##} ft × {m.buildingData.BuildingWidth:0.#} ft; qp at the parapet top, {mp.ParapetTopHeight:0.#} ft (27.3.4)" });
                forceRows.AddRange(new List<string[]>
                {
                    new[] { "Calculated total", $"{force.TotalKips:0.00} kips", "" },
                    new[] { "Minimum (27.1.5)", $"{force.MinimumKips:0.00} kips", $"{WindLateralLoads.MinimumWallPressure:0} psf walls + {WindLateralLoads.MinimumRoofPressure:0} psf roof" },
                    new[] { "Design force", $"{force.DesignKips:0.00} kips", force.MinimumGoverns ? "the minimum governs" : "the calculated wind governs" },
                });
                page.Children.Add(Card(KeyValueTable(forceRows)));
                if (force.ParapetKips > 0 && m is WindLoadCalculator_MWFRS md)
                    page.Children.Add(new Border { Margin = new Thickness(0, 14, 0, 0), Child = FigureRow(Drawing("Parapet pressures (27.3.4), section along the wind",
                        c => ParapetDiagram.DrawMwfrs(c, md.buildingData.BuildingHeight, md.buildingData.ParapetHeight, md.ParapetVelocityPressure,
                                                      md.ParapetPressureWindward, md.ParapetPressureLeeward), width: 460)) });
            }

            page.Children.Add(Section("Zones"));
            var bd = m.buildingData;
            page.Children.Add(FigureRow(
                ZoneDrawing(m.RoofAreaCalculator, bd, "Roof zones"),
                WallZoneDrawing(m, "WW", "Windward wall"),
                WallZoneDrawing(m, "LW", "Leeward wall"),
                WallZoneDrawing(m, "SW", "Side walls")));
            page.Children.Add(FigureRow(
                Drawing("Plan", c => BuildingDrawer.DrawPlan(c, bd), width: 260),
                Drawing("Elevation along the length", c => BuildingDrawer.DrawElevation_BuildingLength(c, bd), width: 300),
                Drawing("Elevation along the width", c => BuildingDrawer.DrawElevation_BuildingWidth(c, bd), width: 300)));

            page.Children.Add(Section("Roof (Fig. 27.3-1; two Cp values where the figure gives two)"));
            page.Children.Add(Card(MwfrsRoofTable(m)));
            if (m is WindLoadCalculator_MWFRS mo && mo.buildingData.OverhangWidth > 0 && mo.WindwardOverhangTop != null)
            {
                page.Children.Add(Section($"Windward roof overhang, {mo.buildingData.OverhangWidth:0.##} ft (27.3.3)"));
                page.Children.Add(Card(KeyValueTable(new List<string[]>
                {
                    new[] { "Bottom surface (soffit)", $"+{mo.OverhangBottomPressure:0.0} psf up", $"qh G Cp, Cp = {WindLoadCalculator_MWFRS.Cp_OverhangBottom:0.0}; no internal pressure" },
                    new[] { "Top surface", $"{mo.WindwardOverhangTop.ExternalPressure:0.0} psf", $"Cp = {mo.WindwardOverhangTop.GCp:0.00}, windward roof at the eave (Fig. 27.3-1)" },
                    new[] { "Net on the overhang", $"{mo.WindwardOverhangNetPressure:0.0} psf", "top and bottom combined (negative = up)" },
                    new[] { "Uplift per foot of eave", $"{mo.WindwardOverhangUpliftPerFoot:0} lb/ft", "net × overhang width; roof uplift only, it does not change the shear-wall forces" },
                })));
            }
            page.Children.Add(Section("Walls (Fig. 27.3-1)" + NoteSuffix(m.WallAreaCalculator_BldgLength.Note)));
            page.Children.Add(Card(MwfrsWallTable(m)));
            return page;
        }

        /// <summary>
        /// Roof overhang cladding pressures (shown when the building has an overhang): one row per roof zone, read at the
        /// roof component area, no internal pressure, and the uplift per foot of eave over the overhang width.
        /// </summary>
        private static void AddOverhangSection(StackPanel page, WindLoadCalculator_CC cc)
        {
            if (cc == null) return;
            double width = cc.buildingData.OverhangWidth;
            if (!(width > 0))
            {
                page.Children.Add(Note("No eave overhang entered (Building page), so overhang pressures are not listed."));
                return;
            }
            bool anySummed = cc.OverhangFromRoofPlusWall.Values.Any(s => s);
            page.Children.Add(Section($"Roof overhangs, {width:0.##} ft"));
            page.Children.Add(Para("Overhang GCp covers the top and bottom surfaces together, so no internal pressure is added (the soffit is outside). " +
                                   (anySummed
                                       ? "ASCE 7-22 has no overhang curves for this roof: GCp = roof-zone GCp + the positive wall GCp below it " +
                                         "(Zone 5 under corner zones, Zone 4 elsewhere; to be checked against the printed standard). "
                                       : "GCp from the overhang curves of the roof figure. ") +
                                   $"Net pressures are at least {WindLoadCalculator_CC.MinimumNetPressure:0} psf (30.2.2)."));
            var rows = cc.windPressureOverhang_External
                .Select(kv => (zone: cc.RoofAreaCalculator.effWindAreas[kv.Key], p: kv.Value, summed: cc.OverhangFromRoofPlusWall.TryGetValue(kv.Key, out var s) && s))
                .GroupBy(x => x.zone.Label_Short)
                .Select(grp => grp.First())
                .OrderBy(x => x.zone.Label_Short, StringComparer.Ordinal)
                .Select(x => new
                {
                    Zone = x.zone.Label_Short, Swatch = EffectiveWindAreaRenderer.GetColorForRegion(x.zone.Label_Short),
                    Area = cc.RoofComponentArea, Qh = x.p.qh, GCp = x.p.GCp,
                    Source = x.summed ? (x.zone.Label_Full.StartsWith("Zone3", StringComparison.Ordinal) ? "roof + wall Zone 5" : "roof + wall Zone 4") : "overhang curve",
                    Press = x.p.ExternalPressure, Net = x.p.NetPressure, PerFoot = -x.p.NetPressure * width,
                    Note = x.p.MinimumGoverns ? "16 psf minimum governs" : string.Empty,
                }).ToList();
            var g = NewGrid(new[]
            {
                ("Zone", "Zone", (string)null), ("Component area (sq ft)", "Area", "0"), ("qh (psf)", "Qh", "0.00"),
                ("GCp", "GCp", "0.00"), ("From", "Source", (string)null), ("External (psf)", "Press", "0.0"),
                ("Design (psf)", "Net", "0.0"), ("Uplift per ft of eave (lb/ft)", "PerFoot", "0"), ("Note", "Note", (string)null),
            }, swatch: true);
            g.ItemsSource = rows;
            page.Children.Add(Card(g));
        }

        private FrameworkElement BuildCasesPage()
        {
            var page = new StackPanel { MaxWidth = 1000, HorizontalAlignment = HorizontalAlignment.Left };
            page.Children.Add(Title("Shear-wall load cases (Fig. 27.3-8)"));
            if (lateral == null) { page.Children.Add(Note("Load cases are not available for these inputs.")); return page; }
            page.Children.Add(Para($"Px = {lateral.AlongLength.DesignKips:0.00} kips (wind from W), Py = {lateral.AlongWidth.DesignKips:0.00} kips (wind from S). " +
                                   $"Eccentricity e = {WindLateralLoads.CaseEccentricity:0.00} × the building width normal to the wind. " +
                                   "Each shear wall is designed for its worst case. Cases 2 and 4 may be skipped under the Fig. 27.3-8 exception."));
            var grid = NewGrid(new[] { ("Case", "Name", (string)null), ("Vx (kips)", "Vx", "0.00"), ("Vy (kips)", "Vy", "0.00"), ("Torsion Mt (kip-ft)", "Mt", "0.0") });
            grid.ItemsSource = lateral.LoadCases.Select(c => new { c.Name, c.Vx, c.Vy, c.Mt }).ToList();
            page.Children.Add(Card(grid));
            return page;
        }

        // ------------------------------------------------------------------ tables

        public sealed class CladdingRow
        {
            public string Zone { get; set; }
            public Brush Swatch { get; set; }
            public double Area { get; set; }
            public int Count { get; set; } = 1;
            public string Note { get; set; }
            public double Qh { get; set; }
            public double GCpPos { get; set; }
            public double GCpNeg { get; set; }
            public double PressPos { get; set; }
            public double PressNeg { get; set; }
            public double NetPos { get; set; }
            public double NetNeg { get; set; }
        }

        /// <summary>
        /// One row per zone: GCp at the component's effective wind area (the engine reads every zone there),
        /// the pressures, and a note where the 16 psf minimum (30.2.2) governs.
        /// </summary>
        private static List<CladdingRow> CladdingRows(Dictionary<int, EffectiveWindArea> areas,
            Dictionary<int, PressureData> pos, Dictionary<int, PressureData> neg, double componentArea)
        {
            var rows = new List<CladdingRow>();
            foreach (var a in areas)
            {
                var r = new CladdingRow { Zone = a.Value.Label_Short, Swatch = EffectiveWindAreaRenderer.GetColorForRegion(a.Value.Label_Short), Area = componentArea };
                var notes = new List<string>();
                if (pos.TryGetValue(a.Key, out var p))
                {
                    r.Qh = p.qh;
                    r.GCpPos = p.GCp;
                    r.PressPos = p.ExternalPressure;
                    r.NetPos = p.NetPressure;
                    if (p.MinimumGoverns) notes.Add("push-in");
                }
                if (neg.TryGetValue(a.Key, out var n))
                {
                    r.Qh = n.qh;
                    r.GCpNeg = n.GCp;
                    r.PressNeg = n.ExternalPressure;
                    r.NetNeg = n.NetPressure;
                    if (n.MinimumGoverns) notes.Add("suction");
                }
                r.Note = notes.Count == 0 ? string.Empty : $"16 psf minimum governs ({string.Join(", ", notes)})";
                rows.Add(r);
            }
            // Every piece of a zone now has the same values: list each zone once, with how many pieces it has.
            return rows.GroupBy(r => r.Zone)
                       .Select(g => { var first = g.First(); first.Count = g.Count(); return first; })
                       .OrderBy(r => r.Zone, StringComparer.Ordinal)
                       .ToList();
        }

        private static DataGrid CladdingTable(List<CladdingRow> rows)
        {
            var g = NewGrid(new[]
            {
                ("Zone", "Zone", (string)null), ("Pieces", "Count", "0"), ("Component area (sq ft)", "Area", "0"), ("qh (psf)", "Qh", "0.00"),
                ("GCp push-in", "GCpPos", "0.00"), ("GCp suction", "GCpNeg", "0.00"),
                ("External push-in (psf)", "PressPos", "0.0"), ("External suction (psf)", "PressNeg", "0.0"),
                ("Design push-in (psf)", "NetPos", "0.0"), ("Design suction (psf)", "NetNeg", "0.0"), ("Note", "Note", (string)null),
            }, swatch: true);
            g.ItemsSource = rows;
            return g;
        }

        private DataGrid MwfrsRoofTable(WindLoadCalculator_Base m)
        {
            var g = NewGrid(new[]
            {
                ("Zone", "Zone", (string)null), ("Areas", "Count", "0"), ("Area (sq ft)", "Area", "0"), ("qh (psf)", "Qh", "0.00"),
                ("Cp, first value", "CpA", "0.00"), ("External (psf)", "PressA", "0.0"), ("Net (psf)", "NetA", "0.0"),
                ("Cp, second value", "CpB", "0.00"), ("External (psf) ", "PressB", "0.0"), ("Net (psf) ", "NetB", "0.0"),
            }, swatch: true);
            g.ItemsSource = m.RoofAreaCalculator.effWindAreas
                .GroupBy(a => (a.Value.Label_Short, Math.Round(a.Value.Area, 1)))
                .Select(grp => (a: grp.First(), count: grp.Count()))
                .Select(x =>
            {
                var a = x.a;
                m.windPressureRoof_Pos_External.TryGetValue(a.Key, out var p);
                m.windPressureRoof_Neg_External.TryGetValue(a.Key, out var n);
                return new
                {
                    Zone = a.Value.Label_Short, Swatch = EffectiveWindAreaRenderer.GetColorForRegion(a.Value.Label_Short), Area = a.Value.Area, Count = x.count,
                    Qh = (p ?? n)?.qh ?? 0,
                    CpA = p?.GCp ?? 0, PressA = p?.ExternalPressure ?? 0, NetA = p?.NetPressure ?? 0,
                    CpB = n?.GCp ?? 0, PressB = n?.ExternalPressure ?? 0, NetB = n?.NetPressure ?? 0,
                };
            }).ToList();
            return g;
        }

        private static DataGrid MwfrsWallTable(WindLoadCalculator_Base m)
        {
            var g = NewGrid(new[]
            {
                ("Wall", "Zone", (string)null), ("Area (sq ft)", "Area", "0"), ("q (psf)", "Qh", "0.00"),
                ("Cp", "Cp", "0.00"), ("External (psf)", "Press", "0.0"), ("Net (psf)", "Net", "0.0"),
            }, swatch: true);
            g.ItemsSource = m.WallAreaCalculator_BldgLength.effWindAreas.Select(a =>
            {
                m.windPressureWall_Pos_External_MWFRS.TryGetValue(a.Key, out var p);
                return new
                {
                    Zone = WallName(a.Value.Label_Short), Swatch = EffectiveWindAreaRenderer.GetColorForRegion(a.Value.Label_Short), Area = a.Value.Area,
                    Qh = p?.qh ?? 0, Cp = p?.GCp ?? 0, Press = p?.ExternalPressure ?? 0, Net = p?.NetPressure ?? 0,
                };
            }).ToList();
            return g;
        }

        private static DataGrid NewGrid(IEnumerable<(string header, string path, string format)> columns, bool swatch = false)
        {
            var g = new DataGrid
            {
                AutoGenerateColumns = false, IsReadOnly = true, CanUserAddRows = false, HeadersVisibility = DataGridHeadersVisibility.Column,
                GridLinesVisibility = DataGridGridLinesVisibility.Horizontal, HorizontalGridLinesBrush = Hairline,
                BorderThickness = new Thickness(0), Background = Brushes.White, RowBackground = Brushes.White,
                AlternatingRowBackground = Frozen("#FAFAF8"), FontSize = 13, RowHeight = 26,
                HorizontalAlignment = HorizontalAlignment.Left, SelectionUnit = DataGridSelectionUnit.FullRow,
                ColumnHeaderStyle = HeaderStyle(),
            };
            bool first = true;
            foreach (var (header, path, format) in columns)
            {
                if (first && swatch)
                {
                    var f = new FrameworkElementFactory(typeof(Rectangle));
                    f.SetValue(WidthProperty, 14.0);
                    f.SetValue(HeightProperty, 14.0);
                    f.SetValue(Shape.StrokeProperty, InkSecondary);
                    f.SetValue(Shape.StrokeThicknessProperty, 0.5);
                    f.SetBinding(Shape.FillProperty, new Binding("Swatch"));
                    g.Columns.Add(new DataGridTemplateColumn { Header = "", CellTemplate = new DataTemplate { VisualTree = f }, Width = 26 });
                }
                var binding = new Binding(path);
                if (format != null) binding.StringFormat = format;
                var col = new DataGridTextColumn { Header = header, Binding = binding, MinWidth = first ? 70 : 90 };
                if (format != null)
                    col.ElementStyle = new Style(typeof(TextBlock)) { Setters = { new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Right), new Setter(MarginProperty, new Thickness(8, 0, 8, 0)) } };
                else
                    col.ElementStyle = new Style(typeof(TextBlock)) { Setters = { new Setter(MarginProperty, new Thickness(6, 0, 6, 0)), new Setter(TextBlock.FontWeightProperty, FontWeights.SemiBold) } };
                g.Columns.Add(col);
                first = false;
            }
            return g;
        }

        private static Style HeaderStyle()
        {
            var s = new Style(typeof(System.Windows.Controls.Primitives.DataGridColumnHeader));
            s.Setters.Add(new Setter(BackgroundProperty, HeaderFill));
            s.Setters.Add(new Setter(ForegroundProperty, InkSecondary));
            s.Setters.Add(new Setter(FontWeightProperty, FontWeights.SemiBold));
            s.Setters.Add(new Setter(FontSizeProperty, 12.0));
            s.Setters.Add(new Setter(PaddingProperty, new Thickness(8, 6, 8, 6)));
            s.Setters.Add(new Setter(BorderBrushProperty, Hairline));
            s.Setters.Add(new Setter(BorderThicknessProperty, new Thickness(0, 0, 1, 1)));
            s.Setters.Add(new Setter(HorizontalContentAlignmentProperty, HorizontalAlignment.Right));
            return s;
        }

        private static FrameworkElement KeyValueTable(List<string[]> rows)
        {
            var g = new Grid();
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(260) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(160) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            for (int i = 0; i < rows.Count; i++)
            {
                g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                for (int c = 0; c < 3; c++)
                {
                    var tb = new TextBlock
                    {
                        Text = rows[i][c], Margin = new Thickness(0, 5, 12, 5), TextWrapping = TextWrapping.Wrap,
                        FontWeight = c == 1 ? FontWeights.SemiBold : FontWeights.Normal,
                        Foreground = c == 2 ? InkSecondary : Ink,
                    };
                    Grid.SetRow(tb, i);
                    Grid.SetColumn(tb, c);
                    g.Children.Add(tb);
                }
                if (i > 0)
                {
                    var line = new Border { BorderBrush = Hairline, BorderThickness = new Thickness(0, 1, 0, 0) };
                    Grid.SetRow(line, i);
                    Grid.SetColumnSpan(line, 3);
                    g.Children.Add(line);
                }
            }
            return g;
        }

        // ------------------------------------------------------------------ figures

        private const double GraphW = 460, GraphH = 300;
        private const double XMin = 1, XMax = 1000, YMin = -4, YMax = 1; // the axes FigureDrawer uses

        /// <summary>The GCp curves the calculation used, with this building's zones marked at their effective area.</summary>
        private static FrameworkElement GCpGraph(Chapter27and30_GCpCurveBase figure, List<CladdingRow> rows)
        {
            var canvas = new Canvas { Width = GraphW, Height = GraphH, Background = Brushes.White, ClipToBounds = false };
            FigureDrawer.DrawCurvesOnCanvas(canvas, figure);
            foreach (var r in rows)
            {
                Mark(canvas, r.Area, r.GCpNeg, Suction);
                Mark(canvas, r.Area, r.GCpPos, PushIn);
            }
            var panel = new StackPanel { Margin = new Thickness(0, 0, 24, 0) };
            panel.Children.Add(new TextBlock { Text = figure?.ChartTitle, FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap, MaxWidth = GraphW });
            panel.Children.Add(new TextBlock { Text = figure?.ChartCriteria, Foreground = InkSecondary, FontSize = 12, TextWrapping = TextWrapping.Wrap, MaxWidth = GraphW, Margin = new Thickness(0, 0, 0, 6) });
            panel.Children.Add(new Border { BorderBrush = Hairline, BorderThickness = new Thickness(1), Padding = new Thickness(28, 10, 28, 22), Background = Brushes.White, Child = canvas, HorizontalAlignment = HorizontalAlignment.Left });
            var legend = new TextBlock { FontSize = 12, Foreground = InkSecondary, Margin = new Thickness(0, 6, 0, 0), TextWrapping = TextWrapping.Wrap, MaxWidth = GraphW };
            legend.Inlines.Add(new System.Windows.Documents.Run("● ") { Foreground = Suction });
            legend.Inlines.Add("suction and ");
            legend.Inlines.Add(new System.Windows.Documents.Run("● ") { Foreground = PushIn });
            legend.Inlines.Add("push-in GCp of this building's zones, at the component's effective wind area (sq ft, log scale). Negative GCp is up.");
            panel.Children.Add(legend);
            return panel;
        }

        private static void Mark(Canvas canvas, double area, double gcp, Brush brush)
        {
            if (!(area > 0) || gcp == 0) return;
            double x = Math.Max(XMin, Math.Min(XMax, area));
            double px = (Math.Log10(x) - Math.Log10(XMin)) / (Math.Log10(XMax) - Math.Log10(XMin)) * GraphW;
            double py = (gcp - YMin) / (YMax - YMin) * GraphH;
            var dot = new Ellipse { Width = 9, Height = 9, Fill = brush, Stroke = Brushes.White, StrokeThickness = 1.5 };
            Canvas.SetLeft(dot, px - 4.5);
            Canvas.SetTop(dot, py - 4.5);
            canvas.Children.Add(dot);
        }

        private static FrameworkElement ZoneDrawing(AreaCalculator_Base zones, BuildingData bd, string title)
        {
            return Drawing(title, c =>
            {
                if (zones?.effWindAreas == null || zones.effWindAreas.Count == 0) return;
                Rect box = zones.GetBoundingExtents(zones.effWindAreas).ToRect();
                var (ox, oy) = Centered(c, box);
                foreach (var a in zones.effWindAreas)
                    EffectiveWindAreaRenderer.DrawEffectiveWindArea(c, bd, a.Value, box, ox, oy,
                        EffectiveWindAreaRenderer.GetColorForRegion(a.Value.Label_Short), Brushes.Black, 1);
            }, ZoneLegend(zones));
        }

        private static FrameworkElement WallZoneDrawing(WindLoadCalculator_Base m, string label, string title)
        {
            var zones = m.WallAreaCalculator_BldgLength;
            return Drawing(title, c =>
            {
                var all = zones?.effWindAreas;
                if (all == null || all.Count == 0) return;
                Rect box = zones.GetBoundingExtents(all).ToRect();
                var (ox, oy) = Centered(c, box);
                foreach (var a in all.Where(a => a.Value.Label_Short == label))
                    EffectiveWindAreaRenderer.DrawEffectiveWindArea(c, m.buildingData, a.Value, box, ox, oy,
                        EffectiveWindAreaRenderer.GetColorForRegion(a.Value.Label_Short), Brushes.Black, 1);
            });
        }

        /// <summary>Offsets that centre the zone drawing (the renderer scales to 80% of the canvas, from the origin).</summary>
        private static (double x, double y) Centered(Canvas c, Rect box)
        {
            const double usable = 0.8; // EffectiveWindAreaRenderer's default margin ratio of 0.1 on each side
            if (box.Width <= 0 || box.Height <= 0) return (0, 0);
            double scale = Math.Min(usable * c.Width / box.Width, usable * c.Height / box.Height);
            return ((c.Width - box.Width * scale) / 2 - box.X * scale, (c.Height - box.Height * scale) / 2 - box.Y * scale);
        }

        private static FrameworkElement ZoneLegend(AreaCalculator_Base zones)
        {
            var wrap = new WrapPanel { MaxWidth = 200, Margin = new Thickness(0, 4, 0, 0) };
            if (zones?.effWindAreas == null) return wrap;
            foreach (var name in zones.effWindAreas.Values.Select(a => a.Label_Short).Distinct())
            {
                var sp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 10, 2) };
                sp.Children.Add(new Rectangle { Width = 10, Height = 10, Fill = EffectiveWindAreaRenderer.GetColorForRegion(name), Stroke = InkSecondary, StrokeThickness = 0.5, Margin = new Thickness(0, 0, 4, 0), VerticalAlignment = VerticalAlignment.Center });
                sp.Children.Add(new TextBlock { Text = name, FontSize = 11, Foreground = InkSecondary });
                wrap.Children.Add(sp);
            }
            return wrap;
        }

        private static FrameworkElement Drawing(string title, Action<Canvas> draw, FrameworkElement below = null, double width = 200)
        {
            var canvas = new Canvas { Width = width, Height = 200, Background = Brushes.White };
            try { draw(canvas); }
            catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException || ex is NullReferenceException) { }
            var panel = new StackPanel { Margin = new Thickness(0, 0, 16, 0) };
            panel.Children.Add(new TextBlock { Text = title, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 6) });
            panel.Children.Add(new Border { BorderBrush = Hairline, BorderThickness = new Thickness(1), Child = canvas, HorizontalAlignment = HorizontalAlignment.Left });
            if (below != null) panel.Children.Add(below);
            return panel;
        }

        // ------------------------------------------------------------------ layout pieces

        private static TextBlock Title(string text) => new TextBlock { Text = text, FontSize = 20, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 8) };
        private static TextBlock Section(string text) => new TextBlock { Text = text, FontSize = 15, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 18, 0, 8) };
        private static TextBlock Sub(string text) => new TextBlock { Text = text, FontWeight = FontWeights.SemiBold, Foreground = InkSecondary, Margin = new Thickness(0, 12, 0, 6) };
        private static TextBlock Para(string text) => new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap, MaxWidth = 1000, HorizontalAlignment = HorizontalAlignment.Left, Foreground = InkSecondary, Margin = new Thickness(0, 0, 0, 4) };

        private static Border Card(UIElement child) => new Border
        {
            Background = Surface, BorderBrush = Hairline, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8),
            Padding = new Thickness(14, 8, 14, 8), Child = child, HorizontalAlignment = HorizontalAlignment.Left,
        };

        private static Border Note(string text) => new Border
        {
            Background = WarnFill, BorderBrush = WarnBorder, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(6),
            Padding = new Thickness(10, 8, 10, 8), Margin = new Thickness(0, 0, 0, 8), HorizontalAlignment = HorizontalAlignment.Left, MaxWidth = 1000,
            Child = new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap, Foreground = WarnInk },
        };

        private static void AddWarnings(Panel page, WindLoadCalculator_Base calc)
        {
            if (calc.ValidationMessages.Count > 0) page.Children.Add(Note(string.Join(Environment.NewLine, calc.ValidationMessages)));
        }

        private static WrapPanel FigureRow(params FrameworkElement[] items)
        {
            var w = new WrapPanel { Margin = new Thickness(0, 0, 0, 10) };
            foreach (var i in items) w.Children.Add(i);
            return w;
        }

        // ------------------------------------------------------------------ helpers

        private double QhOf(WindLoadCalculator_Base calc) => calc.CalculateDynamicWindPressure(building.MeanRoofHeight);

        private static string NoteSuffix(string note) => string.IsNullOrWhiteSpace(note) ? string.Empty : " · " + note;

        private static string WallName(string shortLabel)
        {
            switch (shortLabel)
            {
                case "WW": return "WW (windward)";
                case "LW": return "LW (leeward)";
                case "SW": return "SW (side walls)";
                default: return shortLabel;
            }
        }

        private static string ExposureLetter(WindExposureCategories e) =>
            e == WindExposureCategories.WIND_EXP_CAT_B ? "B" : e == WindExposureCategories.WIND_EXP_CAT_D ? "D" : "C";

        private static string RoofName(BuildingData b) =>
            b.RoofType == RoofTypes.ROOF_TYPE_GABLE ? $"gable {b.RoofPitch:0.#}°" : b.RoofType == RoofTypes.ROOF_TYPE_HIP ? $"hip {b.RoofPitch:0.#}°" : "flat";

        private static string EnclosureName(BuildingData b)
        {
            switch (b.EnclosureType)
            {
                case BuildingEnclosures.BLDG_PARTIALLY_ENCLOSED: return "partially enclosed";
                case BuildingEnclosures.BLDG_PARTIALLY_OPEN: return "partially open";
                case BuildingEnclosures.BLDG_OPEN: return "open";
                default: return "enclosed";
            }
        }

        private static Brush Frozen(string hex)
        {
            var b = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
            b.Freeze();
            return b;
        }
    }
}
