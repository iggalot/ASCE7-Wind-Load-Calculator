using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Navigation;
using System.Windows.Threading;

namespace ASCE7WindLoadCalculator
{
    /// <summary>
    /// Guided wind input in three steps: Site, Building, Results. Every change recalculates immediately;
    /// the screen's own numbers and 3D view update at once, and <see cref="InputsChanged"/> follows after a
    /// short pause in typing so the host's heavier refreshes do not run on every keystroke.
    /// </summary>
    public partial class WindSetupControl : UserControl
    {
        public const int SiteStep = 0, BuildingStep = 1, ResultsStep = 2;
        private static readonly string[] StepNames = { "Site", "Building", "Results" };

        /// <summary>Raised (debounced) after any input changes. Read the new state from the properties.</summary>
        public event EventHandler InputsChanged;

        /// <summary>Raised by "Send loads to shear walls" with the load cases for the current inputs.</summary>
        public event EventHandler<WindLateralResult> SendLoadsRequested;

        public BuildingData Building { get; private set; }
        public WindParameters_Base Parameters { get; private set; }
        public ASCE7_Versions Version { get; private set; }

        /// <summary>The latest calculation, or null when the inputs cannot be calculated.</summary>
        public WindAnalysisResult Analysis { get; private set; }
        public WindSummary Summary { get; private set; }
        public IReadOnlyList<InputIssue> Issues { get; private set; } = new List<InputIssue>();

        /// <summary>Fig. 27.3-8 exception: only load cases 1 and 3.</summary>
        public bool OnlyCases1And3 => cbOnlyCases13.IsChecked == true;

        public int Step { get; private set; }

        /// <summary>Show the "Send loads to shear walls" button on the Results step.</summary>
        public bool ShowSendButton { get; set; } = true;

        private bool readOnly;

        /// <summary>
        /// The wind's results only, not its input pages: for a host that takes the wind from the project (the Shear Wall
        /// Calculator), where the Wind Load Calculator is the one place the wind is set up. It shows the Results page and
        /// stays there (no Site and Building pages, no step bar, no Back or Next); the choices on it are locked and "Send"
        /// is hidden.
        /// </summary>
        public bool ReadOnly
        {
            get => readOnly;
            set
            {
                readOnly = value;
                // only what this locked is unlocked again (the model's geometry and "Open" stay as they were)
                foreach (var c in lockedHere) { if (c is TextBox t) { t.IsReadOnly = false; t.IsTabStop = true; } else c.IsEnabled = true; }
                lockedHere.Clear();
                if (value) LockInputs();
                if (value) ShowSendButton = false;
                var steps = value ? Visibility.Collapsed : Visibility.Visible;
                spSteps.Visibility = steps;
                tbStepOf.Visibility = steps;
                GoToStep(value ? ResultsStep : Step);
                UpdateSendState();
            }
        }

        private readonly List<Control> lockedHere = new List<Control>();

        private void LockInputs()
        {
            foreach (var root in new DependencyObject[] { pnlSite, pnlBuilding, pnlResults })
                foreach (var c in LogicalDescendants(root))
                {
                    if (c is TextBox t && !t.IsReadOnly) { t.IsReadOnly = true; t.IsTabStop = false; lockedHere.Add(t); }
                    else if ((c is ComboBox || c is System.Windows.Controls.Primitives.ToggleButton) && ((Control)c).IsEnabled) { ((Control)c).IsEnabled = false; lockedHere.Add((Control)c); }
                }
        }

        private static IEnumerable<DependencyObject> LogicalDescendants(DependencyObject root)
        {
            foreach (var child in LogicalTreeHelper.GetChildren(root).OfType<DependencyObject>())
            {
                yield return child;
                foreach (var d in LogicalDescendants(child)) yield return d;
            }
        }

        /// <summary>The 3D view shown on the Building and Results steps.</summary>
        public Wind3DViewerControl Viewer { get; } = new Wind3DViewerControl();

        private readonly DispatcherTimer notifyTimer;
        private bool loading;
        private readonly List<Button> stepButtons = new List<Button>();

        private static readonly Brush ControlBrush = Frozen("#C3C2B7");
        private static readonly Brush WarnBrush = Frozen("#B45309");
        private static readonly Brush OkInk = Frozen("#006300");
        private static readonly Brush WarnInk = Frozen("#8A3B00");
        private static readonly Brush Ink = Frozen("#0B0B0B");
        private static readonly Brush InkSecondary = Frozen("#52514E");

        public WindSetupControl() : this(null, ASCE7_Versions.ASCE_VER_7_22, null) { }

        public WindSetupControl(BuildingData building, ASCE7_Versions version, WindParameters_Base parameters)
        {
            InitializeComponent();
            RadioGroups.MakeUnique(this); // the host window may hold other controls with the same group names
            Viewer.ShowSummaryCard = false; // the Results step shows these numbers itself

            notifyTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(350) };
            notifyTimer.Tick += (s, e) => { notifyTimer.Stop(); InputsChanged?.Invoke(this, EventArgs.Empty); };
            zeroSlopeTimer.Tick += (s, e) => SwitchZeroSlopeToFlat();
            ApplyResultsPanel(ViewerPreferences.ResultsPanelOpen);
            BuildStepIndicator();
            SetInputs(building, version, parameters);
            GoToStep(SiteStep);
        }

        /// <summary>Loads inputs into the screen without raising <see cref="InputsChanged"/>.</summary>
        public void SetInputs(BuildingData building, ASCE7_Versions version, WindParameters_Base parameters)
        {
            Building = building?.Clone() ?? new BuildingData();
            Parameters = parameters?.Clone() ?? new WindParameters_Base(); // the engine holds the starting values
            Version = version;

            loading = true;
            try
            {
                (Version == ASCE7_Versions.ASCE_VER_7_16 ? rbEd16 : rbEd22).IsChecked = true;
                switch (Parameters.RiskCategory)
                {
                    case "I": rbRisk1.IsChecked = true; break;
                    case "III": rbRisk3.IsChecked = true; break;
                    case "IV": rbRisk4.IsChecked = true; break;
                    default: rbRisk2.IsChecked = true; break;
                }
                tbV.Text = Format(Parameters.WindSpeed);
                switch (Parameters.ExposureCategory)
                {
                    case WindExposureCategories.WIND_EXP_CAT_B: rbExpB.IsChecked = true; break;
                    case WindExposureCategories.WIND_EXP_CAT_D: rbExpD.IsChecked = true; break;
                    default: rbExpC.IsChecked = true; break;
                }
                bool hill = Math.Abs(Parameters.Kzt - 1.0) > 1e-9;
                (hill ? rbHillYes : rbHillNo).IsChecked = true;
                tbKzt.Text = Parameters.Kzt.ToString("0.00", CultureInfo.CurrentCulture);
                (Parameters.LateralProcedure == LateralProcedure.Envelope ? rbProc28 : rbProc27).IsChecked = true;
                switch (Building.EnclosureType)
                {
                    case BuildingEnclosures.BLDG_PARTIALLY_ENCLOSED: rbEncPartEnc.IsChecked = true; break;
                    case BuildingEnclosures.BLDG_PARTIALLY_OPEN: rbEncPartOpen.IsChecked = true; break;
                    case BuildingEnclosures.BLDG_OPEN: rbEncOpen.IsChecked = true; break;
                    default: rbEncEnclosed.IsChecked = true; break;
                }
                switch (Building.RoofType)
                {
                    case RoofTypes.ROOF_TYPE_GABLE: rbRoofGable.IsChecked = true; break;
                    case RoofTypes.ROOF_TYPE_HIP: rbRoofHip.IsChecked = true; break;
                    default: rbRoofFlat.IsChecked = true; break;
                }
                tbL.Text = Format(Building.BuildingLength);
                tbB.Text = Format(Building.BuildingWidth);
                tbH.Text = Format(Building.BuildingHeight);
                cbStories.SelectedIndex = Building.NumberOfStories - 1;
                tbPlate1.Text = Format(Building.StoryPlateHeight(1));
                tbPlate2.Text = Format(Building.StoryPlateHeight(2));
                tbPlate3.Text = Format(Building.StoryPlateHeight(3));
                ShowPlateBoxes(Building.NumberOfStories);
                tbOverhang.Text = Format(Building.OverhangWidth);
                tbParapet.Text = Format(Building.ParapetHeight);
                cbParapetEnvelope.SelectedIndex = (int)Building.ParapetEnclosure;
                tbPitch.Text = Format(Building.RoofPitch);
                // Ridge choice as the user sees it; for a non-gable roof show the gable default.
                var asGable = Building.Clone();
                asGable.RoofType = RoofTypes.ROOF_TYPE_GABLE;
                (asGable.RidgeDirection == RidgeDirections.RIDGE_DIR_PERP_TO_BLDGLENGTH ? rbRidgeAcross : rbRidgeAlong).IsChecked = true;
                // Only the areas are saved: 10 sq ft shows as sheathing, anything else as an entered area.
                SetComponent(cbRoofComponent, tbRoofComp, Parameters.RoofComponentArea);
                SetComponent(cbWallComponent, tbWallComp, Parameters.WallComponentArea);
            }
            finally
            {
                loading = false;
            }
            Recalculate(notify: false);
        }

        // ---------------------------------------------------------------- steps

        public void GoToStep(int step)
        {
            Step = readOnly ? ResultsStep : Math.Max(SiteStep, Math.Min(ResultsStep, step)); // read-only: the results only
            pnlSite.Visibility = Step == SiteStep ? Visibility.Visible : Visibility.Collapsed;
            pnlBuilding.Visibility = Step == BuildingStep ? Visibility.Visible : Visibility.Collapsed;
            pnlResults.Visibility = Step == ResultsStep ? Visibility.Visible : Visibility.Collapsed;
            tbStepTitle.Text = StepTitles[Step];

            // One 3D view, moved to whichever step shows it (keeps the camera and the chosen view).
            ccViewerBuilding.Content = null;
            ccViewerResults.Content = null;
            Viewer.GeometryOnly = Step == BuildingStep;   // the Building page shows the building; its pressures are on Results
            ugLiveResults.Visibility = Step == BuildingStep ? Visibility.Collapsed : Visibility.Visible;
            if (Step == BuildingStep) ccViewerBuilding.Content = BuildingPreview ?? (object)Viewer;
            if (Step == ResultsStep) ccViewerResults.Content = Viewer;

            btnBack.Visibility = readOnly ? Visibility.Collapsed : Step == SiteStep ? Visibility.Hidden : Visibility.Visible;
            btnNext.Visibility = Step == ResultsStep ? Visibility.Collapsed : Visibility.Visible;
            btnNext.Content = Step == SiteStep ? "Next: Building" : "See results";
            tbStepOf.Text = $"Page {Step + 1} of 3";
            UpdateStepIndicator();
            UpdateSendState();
            StepChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>Raised when the page changes (Site, Building, Results): a host can show what depends on the results.</summary>
        public event EventHandler StepChanged;

        /// <summary>True on the Results page: the inputs have been gone through and the loads stand.</summary>
        public bool OnResults => Step == ResultsStep;

        private static readonly string[] StepTitles = { "Where is the building?", "Describe the building", "Wind results" };

        private void BuildStepIndicator()
        {
            spSteps.Children.Clear();
            stepButtons.Clear();
            for (int i = 0; i < StepNames.Length; i++)
            {
                int target = i;
                var b = new Button { Style = (Style)FindResource("StepButton") };
                AutomationPropertiesHelper.SetName(b, $"Step {i + 1}: {StepNames[i]}");
                b.Click += (s, e) => GoToStep(target);
                stepButtons.Add(b);
                spSteps.Children.Add(b);
                if (i < StepNames.Length - 1)
                    spSteps.Children.Add(new Border { Width = 28, Height = 1, Background = ControlBrush, VerticalAlignment = VerticalAlignment.Center });
            }
        }

        private void UpdateStepIndicator()
        {
            for (int i = 0; i < stepButtons.Count; i++)
            {
                bool current = i == Step, done = i < Step;
                var dot = new Border
                {
                    Width = 26, Height = 26, CornerRadius = new CornerRadius(13), Margin = new Thickness(0, 0, 8, 0),
                    Background = current ? Ink : done ? Frozen("#E1E0D9") : Brushes.Transparent,
                    BorderBrush = current || done ? Brushes.Transparent : ControlBrush,
                    BorderThickness = new Thickness(1),
                    Child = new TextBlock
                    {
                        Text = done ? "✓" : (i + 1).ToString(), FontSize = 13,
                        Foreground = current ? Brushes.White : done ? Ink : InkSecondary,
                        HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
                    },
                };
                var panel = new StackPanel { Orientation = Orientation.Horizontal };
                panel.Children.Add(dot);
                panel.Children.Add(new TextBlock
                {
                    Text = StepNames[i], VerticalAlignment = VerticalAlignment.Center,
                    Foreground = current ? Ink : InkSecondary, FontWeight = current ? FontWeights.SemiBold : FontWeights.Normal,
                });
                stepButtons[i].Content = panel;
            }
        }

        /// <summary>Raised by "Next: Shear walls" (shown on the Results page only when a host handles it).</summary>
        public event EventHandler ShearWallsRequested;

        private void ShearWalls_Click(object sender, RoutedEventArgs e) => ShearWallsRequested?.Invoke(this, EventArgs.Empty);

        /// <summary>Opens every coefficient, pressure and graph behind the results in its own window.</summary>
        private void Details_Click(object sender, RoutedEventArgs e)
        {
            if (Analysis == null) return;
            new WindDetailsWindow(Version, Parameters, Building, Analysis)
            {
                Owner = Window.GetWindow(this),
            }.Show();
        }

        private void Next_Click(object sender, RoutedEventArgs e) => GoToStep(Step + 1);
        private void Back_Click(object sender, RoutedEventArgs e) => GoToStep(Step - 1);

        private void Send_Click(object sender, RoutedEventArgs e)
        {
            if (Summary?.Lateral == null) return;
            SendLoadsRequested?.Invoke(this, Summary.Lateral);
        }

        /// <summary>Lets the host report what happened to the sent loads (shown next to the button).</summary>
        public void ShowSendStatus(string text) => tbSendStatus.Text = text ?? string.Empty;

        /// <summary>
        /// True once the host keeps the shear walls on these wind loads: any later change re-sends them
        /// automatically, so the button is no longer needed.
        /// </summary>
        public bool LinkedToShearWalls { get; private set; }

        /// <summary>MWFRS procedure for the shear-wall loads (also kept in <see cref="WindParameters_Base.LateralProcedure"/>).</summary>
        public LateralProcedure Procedure => rbProc28.IsChecked == true ? LateralProcedure.Envelope : LateralProcedure.Directional;
        private LateralProcedure? viewerProcedure;   // the procedure last given to the 3D view

        // ---------------------------------------------------------------- geometry from a building model

        private BuildingData modelGeometry;

        /// <summary>True when the size, stories and roof come from a building model (shown, not edited here).</summary>
        public bool GeometryFromModel => modelGeometry != null;

        /// <summary>
        /// The host's own view of the building for the Building page (e.g. the building model's 3D view), shown instead of
        /// the wind viewer's plain building; null: the plain building. The host keeps it up to date.
        /// </summary>
        public FrameworkElement BuildingPreview
        {
            get => buildingPreview;
            set { buildingPreview = value; if (Step == BuildingStep) GoToStep(Step); }
        }
        private FrameworkElement buildingPreview;

        /// <summary>"Edit building…" on the Building step (shown only when the host handles it and a model is loaded).</summary>
        public event EventHandler EditBuildingRequested;

        /// <summary>
        /// The building's size, stories, plate heights and roof from a building model (null: typed here again). They show
        /// on the Building step but cannot be edited; the wind app's own inputs (enclosure, parapet envelope, components,
        /// the site) stay editable. Does not notify the host.
        /// </summary>
        public void SetModelGeometry(BuildingData geometry, string note = null)
        {
            modelGeometry = geometry?.Clone();
            bool locked = modelGeometry != null;
            foreach (var t in new[] { tbL, tbB, tbOverhang, tbPlate1, tbPlate2, tbPlate3, tbPitch, tbParapet }) t.IsReadOnly = locked;
            foreach (var c in new Control[] { rbRoofFlat, rbRoofGable, rbRoofHip, cbStories, rbRidgeAlong, rbRidgeAcross }) c.IsEnabled = !locked;
            lockedHere.RemoveAll(c => c == tbL || c == tbB || c == tbOverhang || c == tbPlate1 || c == tbPlate2 || c == tbPlate3 || c == tbPitch || c == tbParapet ||
                                      c == rbRoofFlat || c == rbRoofGable || c == rbRoofHip || c == cbStories || c == rbRidgeAlong || c == rbRidgeAcross);
            if (readOnly) LockInputs(); // the geometry's own lock changed: read-only still holds
            bdModel.Visibility = locked ? Visibility.Visible : Visibility.Collapsed;
            btnEditBuilding.Visibility = locked && EditBuildingRequested != null ? Visibility.Visible : Visibility.Collapsed;
            // the model's geometry is shown as text, not as inputs that cannot be changed
            secRoof.Visibility = secSize.Visibility = pnlParapetHeight.Visibility = locked ? Visibility.Collapsed : Visibility.Visible;
            tbModel.Text = "Change these in the Building Model editor." + (string.IsNullOrEmpty(note) ? "" : Environment.NewLine + note);
            if (locked) SetInputs(WithWindInputs(modelGeometry, Building), Version, Parameters);
        }

        /// <summary>The model's geometry with this screen's own building inputs (enclosure, parapet envelope).</summary>
        private static BuildingData WithWindInputs(BuildingData geometry, BuildingData screen)
        {
            var b = geometry.Clone();
            if (screen != null)
            {
                b.EnclosureType = screen.EnclosureType;
                b.ParapetEnclosure = screen.ParapetEnclosure;
            }
            return b;
        }

        private void EditBuilding_Click(object sender, RoutedEventArgs e) => EditBuildingRequested?.Invoke(this, EventArgs.Empty);

        /// <summary>Sets the Fig. 27.3-8 exception (e.g. chosen in another dialog) without notifying the host.</summary>
        public void SetOnlyCases1And3(bool value)
        {
            if (OnlyCases1And3 == value) return;
            loading = true;
            cbOnlyCases13.IsChecked = value;
            loading = false;
            Recalculate(notify: false);
        }

        /// <summary>Shows whether (and how) the shear walls follow these wind loads.</summary>
        /// <remarks>
        /// Linked: green "Shear walls use these wind loads". Not linked with a reason: amber warning plus the
        /// button to use these loads. Not linked without a reason: just the button.
        /// </remarks>
        public void SetShearWallLink(bool linked, string detail)
        {
            LinkedToShearWalls = linked;
            tbLinked.Text = detail ?? string.Empty;
            bool show = linked || !string.IsNullOrEmpty(detail);
            bdLinked.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            var ink = linked ? Frozen("#004D00") : Frozen("#7A4B00");
            bdLinked.Background = linked ? Frozen("#EAF4EA") : Frozen("#FFF4E5");
            bdLinked.BorderBrush = linked ? Frozen("#7FB77F") : Frozen("#E8A33D");
            tbLinkedMark.Text = linked ? "✓" : "!";
            tbLinkedMark.Foreground = linked ? OkInk : WarnInk;
            tbLinkedTitle.Text = linked ? "Shear walls use these wind loads" : "Shear walls are NOT using these wind loads";
            tbLinkedTitle.Foreground = ink;
            tbLinked.Foreground = ink;
            UpdateSendState();
        }

        /// <summary>A sloped roof at 0 degrees is a flat roof: switch to Flat when the user leaves the box.</summary>
        private void Pitch_LostFocus(object sender, RoutedEventArgs e) => SwitchZeroSlopeToFlat();

        private void UpdateSendState()
        {
            bool results = Step == ResultsStep;
            btnSend.Visibility = results && ShowSendButton && !LinkedToShearWalls ? Visibility.Visible : Visibility.Collapsed;
            btnSend.IsEnabled = Summary?.Lateral != null;
            btnShearWalls.Visibility = results && LinkedToShearWalls && ShearWallsRequested != null ? Visibility.Visible : Visibility.Collapsed;
            btnDetails.Visibility = results ? Visibility.Visible : Visibility.Collapsed;
            btnDetails.IsEnabled = Analysis != null;
            if (LinkedToShearWalls || Step != ResultsStep) tbSendStatus.Text = string.Empty;
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            try { Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true }); }
            catch (Exception ex) when (ex is System.ComponentModel.Win32Exception || ex is InvalidOperationException) { }
            e.Handled = true;
        }

        // ---------------------------------------------------------------- inputs

        private void Input_Changed(object sender, RoutedEventArgs e)
        {
            if (loading || !IsInitialized) return;
            if (sender == rbHillNo && rbHillNo.IsChecked == true)
            {
                loading = true;
                tbKzt.Text = "1.00";
                loading = false;
            }
            // Choosing a sloped roof while the slope is 0 (e.g. coming from Flat): start from a typical slope.
            bool toSloped = (sender == rbRoofGable && rbRoofGable.IsChecked == true) || (sender == rbRoofHip && rbRoofHip.IsChecked == true);
            bool presetSlope = toSloped && !(Parse(tbPitch.Text) > 0);
            if (presetSlope)
            {
                loading = true;
                tbPitch.Text = DefaultSlope.ToString(CultureInfo.CurrentCulture);
                loading = false;
            }
            Recalculate(notify: true);
            if (presetSlope)
                SetMessage(msgPitch, $"Slope set to {DefaultSlope}°. Enter your roof's slope.");

            if (sender == rbHillYes && rbHillYes.IsChecked == true)
                Dispatcher.BeginInvoke(new Action(() => { tbKzt.Focus(); tbKzt.SelectAll(); }), DispatcherPriority.Input);

            // A 0 typed into the slope of a sloped roof means a flat roof. Wait for a pause (so "0.5" can be
            // typed), then switch; leaving the box switches at once (Pitch_LostFocus).
            zeroSlopeTimer.Stop();
            if (sender == tbPitch && IsZeroSlope()) zeroSlopeTimer.Start();
        }

        private const double DefaultSlope = 15;
        private readonly DispatcherTimer zeroSlopeTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(800) };

        private bool IsZeroSlope()
        {
            string t = tbPitch.Text.Trim();
            return Building.RoofTypeIsSloped() && Parse(t) == 0 && !t.EndsWith(".") && !t.EndsWith(",");
        }

        private void SwitchZeroSlopeToFlat()
        {
            zeroSlopeTimer.Stop();
            if (!IsZeroSlope()) return;
            rbRoofFlat.IsChecked = true; // recalculates through Input_Changed
            loading = true;
            tbPitch.Text = DefaultSlope.ToString(CultureInfo.CurrentCulture); // a sensible slope if they switch back
            loading = false;
            SetMessage(msgBuilding, "A 0° roof is a flat roof, so the roof shape was changed to Flat.");
        }

        private void ReadInputs()
        {
            Version = rbEd16.IsChecked == true ? ASCE7_Versions.ASCE_VER_7_16 : ASCE7_Versions.ASCE_VER_7_22;

            var p = Parameters.Clone();
            p.RiskCategory = rbRisk1.IsChecked == true ? "I" : rbRisk3.IsChecked == true ? "III" : rbRisk4.IsChecked == true ? "IV" : "II";
            p.WindSpeed = Parse(tbV.Text);
            p.ExposureCategory = rbExpB.IsChecked == true ? WindExposureCategories.WIND_EXP_CAT_B
                               : rbExpD.IsChecked == true ? WindExposureCategories.WIND_EXP_CAT_D
                               : WindExposureCategories.WIND_EXP_CAT_C;
            p.Kzt = rbHillYes.IsChecked == true ? Parse(tbKzt.Text) : 1.0;
            p.LateralProcedure = Procedure;
            p.RoofComponentArea = ComponentArea(cbRoofComponent, tbRoofComp, out roofComponentOk);
            p.WallComponentArea = ComponentArea(cbWallComponent, tbWallComp, out wallComponentOk);
            Parameters = p;

            var b = Building.Clone();
            b.EnclosureType = rbEncPartEnc.IsChecked == true ? BuildingEnclosures.BLDG_PARTIALLY_ENCLOSED
                            : rbEncPartOpen.IsChecked == true ? BuildingEnclosures.BLDG_PARTIALLY_OPEN
                            : rbEncOpen.IsChecked == true ? BuildingEnclosures.BLDG_OPEN
                            : BuildingEnclosures.BLDG_ENCLOSED;
            b.RoofType = rbRoofGable.IsChecked == true ? RoofTypes.ROOF_TYPE_GABLE
                       : rbRoofHip.IsChecked == true ? RoofTypes.ROOF_TYPE_HIP
                       : RoofTypes.ROOF_TYPE_FLAT;
            b.BuildingLength = Parse(tbL.Text);
            b.BuildingWidth = Parse(tbB.Text);
            b.NumberOfStories = cbStories.SelectedIndex + 1;
            b.PlateHeights = PlateHeightsTyped(b.NumberOfStories);
            ShowPlateBoxes(b.NumberOfStories);
            tbH.Text = double.IsNaN(b.BuildingHeight) ? "-" : Format(b.BuildingHeight); // the eave height is the sum of the plate heights
            // Blank = no overhang; a bad entry is flagged (ShowIssues) and counted as none.
            double overhang = string.IsNullOrWhiteSpace(tbOverhang.Text) ? 0 : Parse(tbOverhang.Text);
            b.OverhangWidth = double.IsNaN(overhang) || overhang < 0 ? 0 : overhang;
            double parapet = string.IsNullOrWhiteSpace(tbParapet.Text) ? 0 : Parse(tbParapet.Text);
            b.ParapetHeight = double.IsNaN(parapet) || parapet < 0 ? 0 : parapet;
            b.ParapetEnclosure = cbParapetEnvelope.SelectedIndex < 0 ? BuildingEnclosures.BLDG_ENCLOSED : (BuildingEnclosures)cbParapetEnvelope.SelectedIndex;
            double pitch = Parse(tbPitch.Text);
            // A flat roof ignores the pitch box; keep the last good value so switching back restores it.
            b.RoofPitch = double.IsNaN(pitch) && !b.RoofTypeIsSloped() ? Building.RoofPitch : pitch;
            b.RidgeDirection = rbRidgeAcross.IsChecked == true ? RidgeDirections.RIDGE_DIR_PERP_TO_BLDGLENGTH : RidgeDirections.RIDGE_DIR_PARALLEL_TO_BLDGLENGTH;
            if (modelGeometry != null) b = WithWindInputs(modelGeometry, b); // the model's size and roof, whatever the boxes say
            Building = b;
            // the floor heights show on the 3D view (and in the Floors tooltip)
            cbStories.ToolTip = b.NumberOfStories > 1 && b.BuildingHeight > 0
                ? "Floors and roof at " + string.Join(", ", b.StoryElevations.Select(z => Format(z) + " ft")) + " (the sums of the plate heights); shear walls are designed from the top story down." : null;
        }

        // ---------------------------------------------------------------- cladding component

        private const int SheathingItem = 0, FramingItem = 1, AreaItem = 2;

        /// <summary>
        /// The cladding component choices as typed: kind 0 = sheathing/fasteners, 1 = framing (entry = span, ft),
        /// 2 = other (entry = area, sq ft). Saved in project files, so "rafters, 14 ft span" comes back as typed
        /// instead of as its area.
        /// </summary>
        public ComponentChoice RoofComponent => new ComponentChoice(cbRoofComponent.SelectedIndex, Parse(tbRoofComp.Text));
        public ComponentChoice WallComponent => new ComponentChoice(cbWallComponent.SelectedIndex, Parse(tbWallComp.Text));

        /// <summary>Restores <see cref="RoofComponent"/> and <see cref="WallComponent"/> without notifying the host.</summary>
        public void SetComponents(ComponentChoice roof, ComponentChoice wall)
        {
            loading = true;
            try
            {
                SetChoice(cbRoofComponent, tbRoofComp, roof);
                SetChoice(cbWallComponent, tbWallComp, wall);
            }
            finally
            {
                loading = false;
            }
            Recalculate(notify: false);
        }

        private static void SetChoice(ComboBox kind, TextBox value, ComponentChoice choice)
        {
            if (choice == null) return;
            int k = choice.Kind == FramingItem || choice.Kind == AreaItem ? choice.Kind : SheathingItem;
            kind.SelectedIndex = k;
            value.Text = k == SheathingItem || !(choice.Entry > 0) ? string.Empty : Format(choice.Entry);
        }
        private bool roofComponentOk = true, wallComponentOk = true;

        /// <summary>
        /// Effective wind area (26.2) for the chosen component: sheathing and fasteners 10 sq ft; framing
        /// span × span/3 (the effective width need not be less than a third of the span); otherwise the area
        /// entered. An unusable entry falls back to 10 sq ft (the most conservative) and is marked.
        /// </summary>
        private static double ComponentArea(ComboBox kind, TextBox value, out bool ok)
        {
            ok = true;
            double v = Parse(value.Text);
            switch (kind.SelectedIndex)
            {
                case FramingItem:
                    if (v > 0) return v * v / 3.0;
                    break;
                case AreaItem:
                    if (v > 0) return v;
                    break;
                default:
                    return WindParameters_Base.DefaultComponentArea;
            }
            ok = false;
            return WindParameters_Base.DefaultComponentArea;
        }

        private void SetComponent(ComboBox kind, TextBox value, double area)
        {
            bool standard = !(area > 0) || Math.Abs(area - WindParameters_Base.DefaultComponentArea) < 1e-9;
            kind.SelectedIndex = standard ? SheathingItem : AreaItem;
            value.Text = standard ? string.Empty : Format(area);
        }

        private void Component_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (loading || !IsInitialized) return;
            loading = true;
            if (sender == cbRoofComponent) tbRoofComp.Text = string.Empty;
            if (sender == cbWallComponent) tbWallComp.Text = string.Empty;
            loading = false;
            Recalculate(notify: true);
            var box = sender == cbRoofComponent ? tbRoofComp : tbWallComp;
            if (((ComboBox)sender).SelectedIndex != SheathingItem)
                Dispatcher.BeginInvoke(new Action(() => box.Focus()), DispatcherPriority.Input);
        }

        private void ShowComponent(ComboBox kind, Border frame, TextBlock unit, TextBlock result, double area, bool ok)
        {
            frame.Visibility = kind.SelectedIndex == SheathingItem ? Visibility.Collapsed : Visibility.Visible;
            unit.Text = kind.SelectedIndex == FramingItem ? "ft" : "sq ft";
            frame.BorderBrush = ok ? ControlBrush : WarnBrush;
            result.Text = ok ? "= " + area.ToString("0.#") + " sq ft" : "enter a value";
            result.Foreground = ok ? Ink : WarnInk;
        }

        /// <summary>Reads the screen, recalculates and refreshes everything shown.</summary>
        private void Recalculate(bool notify)
        {
            ReadInputs();

            Issues = WindInputValidator.Validate(Parameters, Building);
            Analysis = null;
            Summary = null;
            string calcError = null;
            if (!WindInputValidator.HasErrors(Issues))
            {
                try
                {
                    Analysis = WindAnalysis.Run(Version, Parameters, Building);
                    Summary = WindSummary.Compute(Analysis, Building, OnlyCases1And3);
                }
                catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException || ex is NotSupportedException || ex is NotImplementedException)
                {
                    Analysis = null;
                    Summary = null;
                    calcError = ex.Message;
                }
            }

            ShowIssues(calcError);
            ShowBuildingNumbers();
            ShowResults(calcError);

            // the 3D view's MWFRS cases follow the procedure chosen here when that choice changes (the view can still show
            // the other procedure's cases in the meantime)
            if (viewerProcedure != Procedure) { viewerProcedure = Procedure; Viewer.CaseProcedure = Procedure; }
            if (Analysis != null)
                Viewer.SetResults(Building, Analysis.Cladding, Analysis.MwfrsAlongLength, Analysis.MwfrsAlongWidth, keepCamera: true);
            else
                Viewer.SetResults(Building, null, null, null, keepCamera: true); // no pressures: the building is still drawn (the Building page shows it as keyed)

            UpdateSendState();
            tbSendStatus.Text = string.Empty;

            if (notify)
            {
                notifyTimer.Stop();
                notifyTimer.Start();
            }
        }

        /// <summary>Raises <see cref="InputsChanged"/> now if a change is still waiting (e.g. before the host saves).</summary>
        public void FlushPendingChange()
        {
            if (!notifyTimer.IsEnabled) return;
            notifyTimer.Stop();
            InputsChanged?.Invoke(this, EventArgs.Empty);
        }

        private void ShowIssues(string calcError)
        {
            ShowField(msgV, bdV, WindInputField.WindSpeed);
            ShowField(msgKzt, bdKzt, WindInputField.Kzt);
            ShowField(msgPitch, bdPitch, WindInputField.RoofPitch);
            ShowField(msgSize, null, WindInputField.Length, WindInputField.Width, WindInputField.WallHeight);
            bdL.BorderBrush = Has(WindInputField.Length) ? WarnBrush : ControlBrush;
            bdB.BorderBrush = Has(WindInputField.Width) ? WarnBrush : ControlBrush;
            var plates = Building.PlateHeights ?? new List<double>();
            var plateFrames = new[] { bdPlate1, bdPlate2, bdPlate3 };
            for (int i = 0; i < plateFrames.Length; i++)
                plateFrames[i].BorderBrush = i < plates.Count && !(plates[i] > 0) ? WarnBrush : ControlBrush;
            double typedOverhang = Parse(tbOverhang.Text);
            bool overhangBad = !string.IsNullOrWhiteSpace(tbOverhang.Text) && (double.IsNaN(typedOverhang) || typedOverhang < 0);
            bdOverhang.BorderBrush = overhangBad ? WarnBrush : ControlBrush;
            if (overhangBad && string.IsNullOrEmpty(msgSize.Text)) SetMessage(msgSize, "Enter the eave overhang in feet (0 or more); it is taken as none until then.");

            var bldg = Issues.Where(i => i.Field == WindInputField.Building).Select(i => i.Message).ToList();
            if (calcError != null) bldg.Add("These inputs cannot be calculated: " + calcError);
            SetMessage(msgBuilding, string.Join(Environment.NewLine, bldg));

            bool sloped = Building.RoofTypeIsSloped();
            secSlope.Visibility = sloped && !GeometryFromModel ? Visibility.Visible : Visibility.Collapsed; // the model's: in its read-out
            ShowModelFacts();
            secParapet.Visibility = sloped ? Visibility.Collapsed : Visibility.Visible; // parapets are counted on flat roofs
            double typedParapet = Parse(tbParapet.Text);
            bool parapetBad = !string.IsNullOrWhiteSpace(tbParapet.Text) && (double.IsNaN(typedParapet) || typedParapet < 0);
            bdParapet.BorderBrush = parapetBad ? WarnBrush : ControlBrush;
            SetMessage(msgParapet, parapetBad ? "Enter the parapet height in feet (0 or more); it is taken as none until then." : null);
            pnlRidge.Visibility = Building.RoofType == RoofTypes.ROOF_TYPE_GABLE ? Visibility.Visible : Visibility.Collapsed;
            tbHipNote.Visibility = Building.RoofType == RoofTypes.ROOF_TYPE_HIP ? Visibility.Visible : Visibility.Collapsed;
            runRise.Text = WindInputValidator.RisePer12(Building.RoofPitch);
            ShowComponent(cbRoofComponent, bdRoofComp, tbRoofCompUnit, tbRoofCompArea, Parameters.RoofComponentArea, roofComponentOk);
            ShowComponent(cbWallComponent, bdWallComp, tbWallCompUnit, tbWallCompArea, Parameters.WallComponentArea, wallComponentOk);
            pnlKzt.Visibility = rbHillYes.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            tbKztFixed.Visibility = rbHillYes.IsChecked == true ? Visibility.Collapsed : Visibility.Visible;
            runRisk.Text = "Risk Category " + Parameters.RiskCategory;
        }

        private bool Has(WindInputField f) => Issues.Any(i => i.Field == f);

        private void ShowField(TextBlock message, Border frame, params WindInputField[] fields)
        {
            var list = Issues.Where(i => fields.Contains(i.Field)).ToList();
            SetMessage(message, string.Join(Environment.NewLine, list.Select(i => i.Message)));
            if (frame != null) frame.BorderBrush = list.Count > 0 ? WarnBrush : ControlBrush;
        }

        private static void SetMessage(TextBlock tb, string text)
        {
            tb.Text = text ?? string.Empty;
            tb.Visibility = string.IsNullOrEmpty(text) ? Visibility.Collapsed : Visibility.Visible;
        }

        /// <summary>The model's geometry as label / value rows (read only), on the Building page.</summary>
        private void ShowModelFacts()
        {
            gModelFacts.Children.Clear();
            gModelFacts.RowDefinitions.Clear();
            gModelFacts.ColumnDefinitions.Clear();
            if (!GeometryFromModel) return;
            var b = Building;
            gModelFacts.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
            gModelFacts.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            string roof = b.RoofType == RoofTypes.ROOF_TYPE_GABLE ? $"Gable, {b.RoofPitch:0.#}°, ridge {(b.RidgeDirection == RidgeDirections.RIDGE_DIR_PARALLEL_TO_BLDGLENGTH ? "along the length" : "across the width")}"
                        : b.RoofType == RoofTypes.ROOF_TYPE_HIP ? $"Hip, {b.RoofPitch:0.#}°" : "Flat";
            var rows = new List<(string, string)>
            {
                ("Size", $"{b.BuildingLength:0.#} × {b.BuildingWidth:0.#} ft  (L W-E × B S-N)"),
                ("Floors", $"{b.NumberOfStories}  ·  plates {string.Join(" + ", b.StoryPlateHeights.Select(h => h.ToString("0.#")))} ft  ·  eave {b.BuildingHeight:0.#} ft"),
                ("Roof", roof),
                ("Eave overhang", $"{b.OverhangWidth:0.#} ft"),
            };
            if (!b.RoofTypeIsSloped()) rows.Add(("Parapet", $"{b.ParapetHeight:0.#} ft"));
            foreach (var (label, value) in rows)
            {
                int r = gModelFacts.RowDefinitions.Count;
                gModelFacts.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                var l = new TextBlock { Text = label, Foreground = new SolidColorBrush(Color.FromRgb(0x52, 0x51, 0x4E)), Margin = new Thickness(0, 2, 8, 2) };
                var v = new TextBlock { Text = value, FontWeight = FontWeights.SemiBold, Foreground = Brushes.Black, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 2, 0, 2) };
                Grid.SetRow(l, r); Grid.SetRow(v, r); Grid.SetColumn(v, 1);
                gModelFacts.Children.Add(l); gModelFacts.Children.Add(v);
            }
        }

        private void ShowBuildingNumbers()
        {
            bool sizeOk = !Has(WindInputField.Length) && !Has(WindInputField.Width) && !Has(WindInputField.WallHeight) &&
                          !Issues.Any(i => i.Field == WindInputField.RoofPitch && i.Severity == InputSeverity.Error);
            valMeanH.Text = sizeOk ? Building.MeanRoofHeight.ToString("0.#") + " ft" : "-";
            valRidgeH.Text = sizeOk ? Building.RidgeHeight.ToString("0.#") + " ft" : "-";
            double hf = Building.MwfrsFigureHeight;
            valFigH.Text = sizeOk ? hf.ToString("0.#") + " ft" + (hf != Building.MeanRoofHeight ? " (eave, θ ≤ 10°)" : "") : "-";
            // each wind direction its own L (along the wind) and B (normal to it), 26.3
            string Direction(double l, double b) => $"L {l:0.#}, B {b:0.#}: L/B {l / b:0.00}";
            valWindW.Text = sizeOk ? Direction(Building.BuildingLength, Building.BuildingWidth) : "-";
            valWindS.Text = sizeOk ? Direction(Building.BuildingWidth, Building.BuildingLength) : "-";

            // Zone width a from the cladding calculation (it only exists while the cladding method applies).
            var zones = Analysis?.Cladding?.WallAreaCalculator_BldgLength;
            bool hasA = sizeOk && zones != null && zones.HasCritDim && Analysis.Cladding.extGCpCurve_Wall != null;
            valA.Text = hasA ? zones.CritDim_a.ToString("0.#") + " ft" : sizeOk ? "n/a (h > 60 ft)" : "-";
            valA.Foreground = hasA || !sizeOk ? Ink : WarnInk;
        }

        private void ShowResults(string calcError)
        {
            string ed = Version == ASCE7_Versions.ASCE_VER_7_16 ? "ASCE 7-16" : "ASCE 7-22";
            string exp = Parameters.ExposureCategory == WindExposureCategories.WIND_EXP_CAT_B ? "B"
                       : Parameters.ExposureCategory == WindExposureCategories.WIND_EXP_CAT_D ? "D" : "C";
            string roof = Building.RoofType == RoofTypes.ROOF_TYPE_GABLE ? "gable" : Building.RoofType == RoofTypes.ROOF_TYPE_HIP ? "hip" : "flat";
            tbResultsSub.Text = $"{ed}, {Format(Parameters.WindSpeed)} mph, Exposure {exp}, Risk Category {Parameters.RiskCategory}. " +
                                $"{Format(Building.BuildingLength)} × {Format(Building.BuildingWidth)} ft {roof}-roof building, {Format(Building.BuildingHeight)} ft walls.";

            var s = Summary;
            SetStat(valRoof, unitRoof, subRoof, s?.RoofUpliftPsf, s?.RoofUpliftZone, $"{ComponentName(cbRoofComponent)}, {Parameters.RoofComponentArea:0.#} sq ft");
            SetStat(valWall, unitWall, subWall, s?.WallSuctionPsf, s?.WallSuctionZone, $"{ComponentName(cbWallComponent)}, {Parameters.WallComponentArea:0.#} sq ft");
            SetForce(valWest, unitWest, subWest, s?.FromWest);
            SetForce(valSouth, unitSouth, subSouth, s?.FromSouth);

            // The Building page shows the same numbers, one detail line each, so edits there give feedback at once.
            SetLive(liveRoof, liveRoofUnit, liveRoofSub, s?.RoofUpliftPsf, $"{ComponentName(cbRoofComponent)}, {Parameters.RoofComponentArea:0.#} sq ft");
            SetLive(liveWall, liveWallUnit, liveWallSub, s?.WallSuctionPsf, $"{ComponentName(cbWallComponent)}, {Parameters.WallComponentArea:0.#} sq ft");
            SetLive(liveWest, liveWestUnit, liveWestSub, s?.FromWest?.DesignKips, s?.FromWest == null ? null : s.FromWest.MinimumGoverns ? "Minimum case larger" : "Calculated wind larger");
            SetLive(liveSouth, liveSouthUnit, liveSouthSub, s?.FromSouth?.DesignKips, s?.FromSouth == null ? null : s.FromSouth.MinimumGoverns ? "Minimum case larger" : "Calculated wind larger");

            var rows = new List<CheckRow>();
            // Building-level limits (e.g. over 60 ft) are also reported by the engine in s.Checks; show them once.
            foreach (var i in Issues.Where(i => s == null || i.Field != WindInputField.Building))
                rows.Add(new CheckRow(false, i.Message));
            if (calcError != null)
                rows.Add(new CheckRow(false, "These inputs cannot be calculated: " + calcError));
            if (s != null)
            {
                // The two directions usually agree; say it once to keep the list short.
                bool same = s.FromWest != null && s.FromSouth != null && s.FromWest.MinimumGoverns == s.FromSouth.MinimumGoverns;
                foreach (var check in s.Checks)
                {
                    if (same && check.Text.StartsWith("Wind from the south")) continue;
                    string text = same && check.Text.StartsWith("Wind from the west")
                        ? "Wind from the west and south" + check.Text.Substring("Wind from the west".Length)
                        : check.Text;
                    rows.Add(new CheckRow(check.Ok, text));
                }
            }
            rows.Add(new CheckRow(false, "Confirm the wind speed V against the map for your site and risk category."));
            // only rigid buildings are calculated (G = 0.85, 26.11.1; torsion at ±0.15B, Fig. 27.3-8)
            rows.Add(new CheckRow(false, "Rigid building assumed (G = 0.85, 26.11); flexible buildings are not handled at this time."));
            icChecks.ItemsSource = rows;

            // the wind cases the shear walls and the Load Manager take (WindCases.For: every case of the code and commentary)
            int cases = 0;
            if (s?.Lateral != null && Analysis != null)
            {
                try { cases = ASCE7WindLoadCalculator.WindCases.For(Analysis, OnlyCases1And3).Count; }
                catch (ArgumentException) { cases = s.Lateral.LoadCases.Count; }
            }
            bool envelope = s?.Lateral?.Procedure == LateralProcedure.Envelope;
            tbCaseCount.Text = s?.Lateral == null
                ? "Load cases are not available until the inputs can be calculated."
                : s.Lateral.ProcedureNote != null
                    ? $"Ch. 28 does not apply here (see details); Ch. 27, {cases} wind cases:"
                    : $"{cases} wind cases ({(envelope ? "Figs. 28.3-1, 28.3-2" : "Fig. 27.3-8")}, every direction); each wall uses its worst case:";
            // one choice, the exception of the procedure in use: Ch. 27 (27.3.5, Appendix D) drops Fig. 27.3-8 Cases 2 and 4;
            // Ch. 28 (28.3.2 in 7-22, Fig. 28.3-1 note 5 in 7-16) drops its torsional Load Cases 3 and 4
            tbOnlyCasesTitle.Text = envelope ? "Skip the torsional Load Cases 3 and 4" : "Use Cases 1 and 3 only (skip 2 and 4)";
            tbOnlyCasesHint.Text = envelope
                ? $"{Analysis?.MwfrsAlongLength?.Edition?.EnvelopeTorsionExemption ?? "28.3.2 exception"}: allowed for 1 story with h ≤ 30 ft, or up to 2 stories light-frame or with flexible diaphragms."
                : "27.3.5 exception (Appendix D): allowed for 1 story with h ≤ 30 ft, or up to 2 stories light-frame or with flexible diaphragms.";
            // Ch. 27: Fig. 27.3-8 Cases 1 to 4; Ch. 28: Load Cases 1 and 2 (basic, Fig. 28.3-1) and 3 and 4 (torsional, Fig.
            // 28.3-2); each with its separate minimum (27.1.5 / 28.3.6). The skipped cases fade.
            tbCase1a.Text = envelope ? "LC 1, 2" : "Case 1";
            tbCase1b.Text = envelope ? "Basic (Fig. 28.3-1): LC 1 wind across the ridge, LC 2 along it, each corner as the windward corner"
                                     : "Full wind, each direction alone";
            tbCase2a.Text = envelope ? "LC 3, 4" : "Case 2";
            tbCase2b.Text = envelope ? (s?.Lateral?.EnvelopeAlongLength?.PrintedTorsional == true
                                           ? "Torsional (Fig. 28.3-2): far half at the printed T values (twists the building)"
                                           : "Torsional (Fig. 28.3-2): far half at 25% (twists the building)")
                                     : "75%, 15% off-center (twists the building)";
            tbCase3a.Visibility = tbCase3b.Visibility = tbCase4a.Visibility = tbCase4b.Visibility = envelope ? Visibility.Collapsed : Visibility.Visible;
            string minSection = envelope ? Analysis?.MwfrsAlongLength?.Edition?.EnvelopeMinimumSection : "27.1.5";
            tbCaseMinB.Text = "16 psf walls + 8 psf roof, separate" + (minSection != null ? $" ({minSection})" : "");
            double faded = OnlyCases1And3 ? 0.35 : 1.0;
            tbCase2a.Opacity = tbCase2b.Opacity = tbCase4a.Opacity = tbCase4b.Opacity = faded;
        }

        private static void SetLive(TextBlock value, TextBlock unit, TextBlock sub, double? v, string detail)
        {
            value.Text = v.HasValue ? v.Value.ToString("0.0") : "-";
            unit.Visibility = v.HasValue ? Visibility.Visible : Visibility.Hidden;
            sub.Text = v.HasValue ? detail : "Not available";
        }

        private static string ComponentName(ComboBox c)
        {
            switch (c.SelectedIndex)
            {
                case FramingItem: return c.Name == "cbRoofComponent" ? "Rafters/trusses" : "Studs";
                case AreaItem: return "Component";
                default: return c.Name == "cbRoofComponent" ? "Sheathing, fasteners" : "Sheathing, siding";
            }
        }

        /// <summary>Cladding card: number, unit, then the zone and the component it is for.</summary>
        private static void SetStat(TextBlock value, TextBlock unit, TextBlock sub, double? psf, string where, string usedFor)
        {
            value.Text = psf.HasValue ? psf.Value.ToString("0.0") : "-";
            unit.Visibility = psf.HasValue ? Visibility.Visible : Visibility.Hidden;
            sub.Text = psf.HasValue ? where + Environment.NewLine + usedFor + Environment.NewLine + "Click: show the cladding negative pressures" : "Not available for these inputs";
        }

        /// <summary>Shear-wall card: number, unit, then how it is made up and the minimum.</summary>
        private static void SetForce(TextBlock value, TextBlock unit, TextBlock sub, DirectionalWindForce f)
        {
            unit.Visibility = f != null ? Visibility.Visible : Visibility.Hidden;
            if (f == null) { value.Text = "-"; sub.Text = "Not available for these inputs"; return; }
            value.Text = f.DesignKips.ToString("0.0");
            sub.Text = $"Walls {f.WallsKips:0.0} + roof {f.RoofKips:0.0} = {f.TotalKips:0.0} kips" + Environment.NewLine +
                       $"Minimum {f.MinimumKips:0.0} kips (own case" + (f.MinimumGoverns ? ", larger)" : ")") + Environment.NewLine +
                       "Click: show the wind case that gives the largest shear";
        }

        // ---------------------------------------------------------------- the summary cards, clicked

        private void Chip_Click(object sender, System.Windows.Input.MouseButtonEventArgs e) =>
            ShowSourceOf(sender == chipRoof ? "roof" : sender == chipWall ? "wall" : sender == chipWest ? "W" : "S");

        /// <summary>
        /// Shows in the 3D view where a summary card's number comes from: the roof uplift and the wall suction (cladding) the
        /// cladding negative pressures; a base shear ("W", "S") the wind case of the procedure in use, from that direction,
        /// with the largest shear along it (the minimum case when it governs). Returns the case drawn (null for cladding).
        /// </summary>
        public WindCase ShowSourceOf(string card)
        {
            if (Analysis == null) return null;
            if (card == "roof" || card == "wall")
            {
                Viewer.Mode = WindViewMode.CladdingSuction;
                return null;
            }
            bool alongLength = card == "W";
            Viewer.CaseProcedure = Summary?.Lateral?.Procedure ?? Procedure;
            var worst = Viewer.WindCases.Where(c => c.From == card)
                              .OrderByDescending(c => Math.Abs(alongLength ? c.Vx : c.Vy)).FirstOrDefault();
            if (worst != null) Viewer.SelectedCase = worst;
            return worst;
        }

        // ---------------------------------------------------------------- the RESULTS flyout

        /// <summary>Whether the Results page's panel (checks, shear walls) is open; closed, the model takes its width. Remembered between sessions.</summary>
        public bool ResultsPanelOpen { get; private set; } = true;

        private void EdgeResults_Click(object sender, RoutedEventArgs e)
        {
            ApplyResultsPanel(!ResultsPanelOpen);
            ViewerPreferences.ResultsPanelOpen = ResultsPanelOpen;
        }

        /// <summary>Opens or closes the RESULTS flyout.</summary>
        public void ApplyResultsPanel(bool open)
        {
            ResultsPanelOpen = open;
            svResultsSide.Visibility = open ? Visibility.Visible : Visibility.Collapsed;
            btnEdgeResults.Content = open ? "▼  RESULTS" : "▲  RESULTS";
            btnEdgeResults.ToolTip = open ? "Close the results panel (checks, shear walls): the model takes the whole width"
                                          : "Open the results panel: the checks and the shear walls";
        }

        // ---------------------------------------------------------------- helpers

        private static double Parse(string text) =>
            double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out double v) ? v : double.NaN;

        /// <summary>The plate heights typed for the first <paramref name="stories"/> stories (NaN where not a number); a blank box takes the story below.</summary>
        private List<double> PlateHeightsTyped(int stories)
        {
            var boxes = new[] { tbPlate1, tbPlate2, tbPlate3 };
            var list = new List<double>();
            for (int i = 0; i < stories && i < boxes.Length; i++)
            {
                double h = string.IsNullOrWhiteSpace(boxes[i].Text) && i > 0 ? list[i - 1] : Parse(boxes[i].Text);
                list.Add(double.IsNaN(h) || h <= 0 ? double.NaN : h);
            }
            return list;
        }

        /// <summary>One plate-height box per story.</summary>
        private void ShowPlateBoxes(int stories)
        {
            var boxes = new[] { bdPlate1, bdPlate2, bdPlate3 };
            for (int i = 0; i < boxes.Length; i++) boxes[i].Visibility = i < stories ? Visibility.Visible : Visibility.Hidden; // hidden keeps the columns
        }

        private static string Format(double v) => double.IsNaN(v) ? string.Empty : v.ToString("0.###", CultureInfo.CurrentCulture);

        private static Brush Frozen(string hex)
        {
            var b = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
            b.Freeze();
            return b;
        }

        /// <summary>One row of the results checklist (bound in XAML).</summary>
        public sealed class CheckRow
        {
            public CheckRow(bool ok, string text) { Ok = ok; Text = text; }
            public bool Ok { get; }
            public string Text { get; }
            public string Mark => Ok ? "✓" : "!";
            public string Lead => Ok ? "OK:" : "Check:";
            public Brush MarkBrush => Ok ? OkInk : WarnInk;
        }

        private static class AutomationPropertiesHelper
        {
            public static void SetName(DependencyObject o, string name) => System.Windows.Automation.AutomationProperties.SetName(o, name);
        }
    }
}
