using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ASCE7WindLoadCalculator.ModelAdapter;
using BuildingModel.Diagnostics;
using BuildingModel.Model;
using BuildingModel.Serialization;
using BuildingModel.Sketch;
using Microsoft.Win32;

namespace ASCE7WindLoadCalculator.App
{
    /// <summary>
    /// The Wind Load Calculator: the wind library's page (Site, Building, Results) in a window of its own.
    /// <list type="bullet">
    /// <item>Quick check (no file): the building's size and roof are typed on the Building step.</item>
    /// <item>Project (.bldg.json): the building comes from the model (shown, not edited: Building > Edit building opens the
    /// Building Model editor); the wind inputs are the file's Wind section and saving writes only that section.</item>
    /// </list>
    /// </summary>
    public sealed class MainWindow : Window
    {
        private const string AppName = "Wind Load Calculator";

        private readonly ContentControl host = new ContentControl();
        /// <summary>The suite's issues button (owner 2026-11-11): the building model's errors and warnings; a row opens the Building Model at its member.</summary>
        private readonly IssuesButton issuesButton = new IssuesButton { Scope = "the building model" };
        private readonly TextBlock modeText = new TextBlock { VerticalAlignment = VerticalAlignment.Center, Foreground = new SolidColorBrush(Color.FromRgb(0x52, 0x51, 0x4E)), Margin = new Thickness(12, 0, 6, 0), TextTrimming = TextTrimming.CharacterEllipsis };
        private readonly TextBlock status = new TextBlock { VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis, Margin = new Thickness(10, 0, 10, 0) };
        private readonly MenuItem saveItem, closeItem, editItem, reloadItem;

        private WindSetupControl setup;
        private ModelDocument doc;          // the project as opened or last saved (null: a quick check)
        private string path;
        /// <summary>This app's hold on the open project (one Wind Load Calculator per project).</summary>
        private ProjectLock projectLock;
        private string savedWind;           // the Wind section's text when opened or saved
        private Process editor;             // the Building Model editor, while it is open on the project

        public MainWindow()
        {
            FontFamily = new FontFamily("Segoe UI");
            FontSize = 12;
            Background = Brushes.White;
            Width = 1440; Height = 900; MinWidth = 900; MinHeight = 600;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            // a laptop: the whole screen (the model and the results want every pixel)
            var area = SystemParameters.WorkArea;
            if (area.Width < Width + 40 || area.Height < Height + 40) WindowState = WindowState.Maximized;

            var menu = new Menu { Background = Brushes.White, VerticalAlignment = VerticalAlignment.Center };
            MenuItem Item(ItemsControl parent, string header, string keys, Action a, string tip = null)
            {
                var m = new MenuItem { Header = header, InputGestureText = keys ?? "", ToolTip = tip };
                if (a != null) m.Click += (s, e) => a();
                parent.Items.Add(m);
                return m;
            }
            var file = Item(menu, "_File", null, null);
            Item(file, "_New quick check", "Ctrl+N", () => { if (ConfirmLeave()) NewQuickCheck(); }, "Type a building's size and roof and read the pressures (no file)");
            Item(file, "_Open project…", "Ctrl+O", OpenDialog, "A project's building model (.bldg.json): the building from the model, the wind inputs from its Wind section");
            saveItem = Item(file, "_Save", "Ctrl+S", () => Save(), "Writes the wind inputs into the project file (its Wind section; the building and everything else are left as they are)");
            closeItem = Item(file, "_Close project", null, () => { if (ConfirmLeave()) NewQuickCheck(); }, "Back to a quick check");
            file.Items.Add(new Separator());
            Item(file, "E_xit", "Alt+F4", Close);
            var building = Item(menu, "_Building", null, null);
            editItem = Item(building, "_Edit building…", null, EditBuilding, "Opens the project in the Building Model editor, the one place the size, levels and roof are changed; this window updates when it saves");
            reloadItem = Item(building, "_Reload the building", null, () => ReloadBuilding(true), "Reads the building from the file again (another app has changed it); your wind inputs are kept");
            menu.Items.Add(HelpMenu.For(this, AppName, "Wind loads: ASCE 7-22 Chapters 26 to 30 (the main wind force resisting system and the components and cladding)."));

            var bar = new DockPanel { LastChildFill = true };
            var name = new TextBlock { Text = AppName, FontWeight = FontWeights.SemiBold, FontSize = 13, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4, 0, 12, 0) };
            DockPanel.SetDock(name, Dock.Left);
            DockPanel.SetDock(menu, Dock.Left);
            bar.Children.Add(name);
            bar.Children.Add(menu);
            DockPanel.SetDock(issuesButton, Dock.Right);
            bar.Children.Add(issuesButton);
            issuesButton.Picked += (s, i) => EditBuilding(i.Ids.FirstOrDefault()); // no plan here: the Building Model, at the member
            bar.Children.Add(modeText);
            var line = new SolidColorBrush(Color.FromRgb(0xE1, 0xE0, 0xD9));
            var appBar = new Border { Child = bar, BorderBrush = line, BorderThickness = new Thickness(0, 0, 0, 1), Padding = new Thickness(6, 3, 6, 3) };
            var statusBar = new Border { Child = status, BorderBrush = line, BorderThickness = new Thickness(0, 1, 0, 0), Padding = new Thickness(0, 3, 0, 3), Background = new SolidColorBrush(Color.FromRgb(0xFA, 0xFA, 0xF7)) };

            var root = new DockPanel();
            DockPanel.SetDock(appBar, Dock.Top);
            DockPanel.SetDock(statusBar, Dock.Bottom);
            DockPanel.SetDock(viewOnlyBanner, Dock.Top);
            root.Children.Add(appBar);
            root.Children.Add(viewOnlyBanner);
            root.Children.Add(statusBar);
            root.Children.Add(host);
            Content = root;

            InputBindings.Add(new KeyBinding(new Command(() => { if (ConfirmLeave()) NewQuickCheck(); }), Key.N, ModifierKeys.Control));
            InputBindings.Add(new KeyBinding(new Command(OpenDialog), Key.O, ModifierKeys.Control));
            InputBindings.Add(new KeyBinding(new Command(() => Save()), Key.S, ModifierKeys.Control));
            Closing += (s, e) => { if (!ConfirmLeave()) e.Cancel = true; };
            Closed += (s, e) => { watcher?.Dispose(); projectLock?.Dispose(); projectLock = null; };
            Activated += (s, e) => UpdateTitle();

            NewQuickCheck();
        }

        // ------------------------------------------------------------------ quick check / project

        private bool HasProject => path != null;

        /// <summary>
        /// Someone else's project, opened to look at only (the suite's view-only mode): no lock, the Results page only
        /// (<see cref="WindSetupControl.ReadOnly"/>, as the Shear Wall Calculator shows it), no saves, no "save first?", no
        /// Edit building; their saves are read again whole, the wind too. Opening it again once they have closed it works on it.
        /// </summary>
        private bool viewOnly;
        private bool reopeningViewOnly;
        private readonly Border viewOnlyBanner = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(0x3F, 0x4A, 0x5C)), Padding = new Thickness(10, 5, 10, 5), Visibility = Visibility.Collapsed,
            Child = new TextBlock
            {
                Text = "View only: someone else is working on this project. You can look at the wind's results; nothing can be changed or saved here. Their saves show here as they make them. Open it again once they have closed it.",
                Foreground = Brushes.White, TextWrapping = TextWrapping.Wrap,
            },
        };

        private void SetViewOnly(bool on)
        {
            viewOnly = on;
            viewOnlyBanner.Visibility = on ? Visibility.Visible : Visibility.Collapsed;
            if (on) Log.Info("App", "View only: someone else is working on this project.");
        }

        /// <summary>Says no to a change for the view-only mode; true when it said it.</summary>
        private bool RefusedViewOnly(string what)
        {
            if (!viewOnly) return false;
            MessageBox.Show(this, $"View only: someone else is working on this project, so {what}.", AppName, MessageBoxButton.OK, MessageBoxImage.Information);
            return true;
        }

        /// <summary>A fresh page for a quick check: the size and roof typed on the Building step, nothing saved.</summary>
        private void NewQuickCheck()
        {
            issuesButton.Show(null);
            doc = null;
            path = savedWind = null;
            watcher?.Dispose();
            watcher = null;
            projectLock?.Dispose();
            projectLock = null;
            SetViewOnly(false);
            ProjectFolder.Close();
            Show(new WindSetupControl());
            modeText.Text = "Quick check: type the building's size and roof on the Building page (nothing is saved).";
            ShowStatus("A quick check. To work on a project's building, use File > Open project.");
            Log.Info("App", "New quick check.");
        }

        private void Show(WindSetupControl page)
        {
            setup = page;
            setup.ShowSendButton = false;          // no shear walls in this window: they read the saved Wind section
            setup.InputsChanged += (s, e) => UpdateTitle();
            host.Content = setup;
            UpdateTitle();
        }

        private void OpenDialog()
        {
            if (!ConfirmLeave()) return;
            var d = new OpenFileDialog
            {
                Title = "Open a project",
                Filter = "Building model (*.bldg.json)|*.bldg.json|All files (*.*)|*.*",
                InitialDirectory = ProjectFolder.DialogFolder,
            };
            if (d.ShowDialog(this) == true) Open(d.FileName);
        }

        /// <summary>Opens a project: the building from its model, the wind inputs from its Wind section (the starting ones without).</summary>
        public void Open(string file)
        {
            ModelDocument opened;
            // one Wind Load Calculator per project (the same project again keeps this app's own lock)
            bool refused = false, lookOnly = viewOnly && reopeningViewOnly; // their save read again: still view only, not asked again
            var taken = lookOnly ? null : projectLock != null && string.Equals(projectLock.ModelPath, file, StringComparison.OrdinalIgnoreCase)
                ? projectLock : ProjectDialogs.TakeLock(this, file, "wind", "Wind Load Calculator", true, out refused, out lookOnly);
            if (refused) return;
            try
            {
                ProjectFolder.OpenFor(file); // first, so the opening is in the project's log
                opened = ModelFile.Load(file);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is InvalidDataException ||
                                       ex is System.Runtime.Serialization.SerializationException || ex is ArgumentException)
            {
                if (taken != projectLock) taken?.Dispose();
                Log.Warning("App", "Could not open " + file, ex);
                MessageBox.Show(this, "The project could not be opened:\n" + ex.Message, "Open", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var b = opened.Building;
            var wind = WindSection.Read(b);
            bool had = wind != null;
            wind = wind ?? WindSection.New();

            var page = new WindSetupControl(new BuildingData { EnclosureType = wind.Enclosure, ParapetEnclosure = wind.ParapetEnclosure }, wind.Edition, wind.ToParameters());
            page.EditBuildingRequested += (s, e) => EditBuilding();   // before the geometry, so its "Edit building…" shows
            page.SetComponents(wind.RoofComponent, wind.WallComponent);
            page.SetOnlyCases1And3(wind.OnlyCases1And3);
            if (projectLock != taken) projectLock?.Dispose();
            projectLock = taken;
            doc = opened;
            path = file;
            SetViewOnly(lookOnly);
            Show(page);
            bool ok = ApplyGeometry(b);
            savedWind = had ? CurrentWind().ToJson() : string.Empty;   // no Wind section yet: unsaved until saved
            if (ok) setup.GoToStep(WindSetupControl.ResultsStep);
            if (lookOnly) setup.ReadOnly = true; // the results only, the choices on them locked
            Watch();
            modeText.Text = $"Project: {Path.GetFileName(file)} (the building from the model; wind inputs saved in the file)";
            Log.Info("App", $"Opened {file}; " + (had ? $"wind {wind.WindSpeed:0} mph, exposure {wind.Exposure}, {wind.Edition}." : "no Wind section yet: the starting inputs."));
            UpdateTitle();
        }

        /// <summary>The model's size, stories and roof into the page (shown, not editable). False when the model gives none.</summary>
        private bool ApplyGeometry(Building b)
        {
            issuesButton.Show(BuildingModel.Checks.ModelChecks.Run(b));
            // a model that is not valid gives no wind (owner 2026-11-10: nothing is calculated anywhere)
            var errors = BuildingModel.Checks.ModelChecks.Run(b).Where(i => i.Severity == BuildingModel.Checks.Severity.Error).ToList();
            if (errors.Count > 0)
            {
                ShowStatus($"The building model has {errors.Count} error{(errors.Count == 1 ? "" : "s")}, so no wind is calculated: {errors[0].Message} Fix it in the Building Model (Building > Edit building, or click an error under the red issues button at the top right).");
                Log.Warning("Wind", "Model errors, no wind: " + string.Join(" | ", errors.Select(e => e.Message)), null);
                return false;
            }
            try
            {
                var input = WindAdapter.From(b, new WindOptions { Enclosure = setup.Building?.EnclosureType ?? BuildingEnclosures.BLDG_ENCLOSED,
                                                                  ParapetEnclosure = setup.Building?.ParapetEnclosure ?? BuildingEnclosures.BLDG_ENCLOSED });
                string note = input.Notes.Count > 0 ? string.Join(" ", input.Notes) : null;
                setup.SetModelGeometry(input.Building, note);
                var g = input.Building;
                ShowStatus($"Building from the model: {g.BuildingLength:0.#} × {g.BuildingWidth:0.#} ft, {g.NumberOfStories} stor{(g.NumberOfStories == 1 ? "y" : "ies")}, mean roof height {g.MeanRoofHeight:0.#} ft." + (note == null ? "" : " " + note));
                Log.Info("Wind", $"Building from the model: L {g.BuildingLength:0.##} ft, B {g.BuildingWidth:0.##} ft, {g.NumberOfStories} stories, roof {g.RoofType} {g.RoofPitch:0.#}°, h {g.MeanRoofHeight:0.##} ft" + (note == null ? "." : ". " + note));
                return true;
            }
            catch (InvalidOperationException ex)
            {
                ShowStatus("No wind geometry from the model: " + ex.Message + " Draw the building in the Building Model editor (Building > Edit building).");
                Log.Warning("Wind", "No wind geometry from the model: " + ex.Message, null);
                return false;
            }
        }

        private WindSection CurrentWind() =>
            WindSection.From(setup.Version, setup.Parameters, setup.Building.EnclosureType, setup.Building.ParapetEnclosure,
                             setup.OnlyCases1And3, setup.RoofComponent, setup.WallComponent);

        private bool Dirty => HasProject && !viewOnly && savedWind != null && CurrentWind().ToJson() != savedWind;

        /// <summary>
        /// Writes the wind inputs into the project file: the file is read again and only its Wind section is replaced, so what
        /// the other apps saved meanwhile (the building, the shear walls, the loads) is kept.
        /// </summary>
        private bool Save()
        {
            if (!HasProject)
            {
                ShowStatus("A quick check is not saved. Open a project (File > Open project) to keep the wind inputs with a building.");
                return false;
            }
            if (RefusedViewOnly("the wind is not saved from here")) return false;
            setup.FlushPendingChange(); // a value still being typed counts
            try
            {
                var latest = ModelFile.Load(path);
                var wind = CurrentWind();
                wind.Write(latest.Building);
                ModelFile.Save(path, latest);
                watcher?.SavedByMe();
                doc = latest;
                savedWind = wind.ToJson();
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is InvalidDataException ||
                                       ex is System.Runtime.Serialization.SerializationException)
            {
                Log.Warning("App", "Could not save " + path, ex);
                MessageBox.Show(this, "The project could not be saved:\n" + ex.Message, "Save", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            Log.Info("App", $"Saved the wind inputs in {path} (revision {doc.Revision}).");
            ShowStatus($"Saved the wind inputs in {Path.GetFileName(path)}. The Shear Wall Calculator and the Load Manager use them when they open the project.");
            UpdateTitle();
            return true;
        }

        /// <summary>Asks to save unsaved wind inputs; false: stay.</summary>
        private bool ConfirmLeave()
        {
            if (!Dirty) return true;
            return ProjectDialogs.SaveChangesFirst(this, $"Save the wind inputs in {Path.GetFileName(path)}?", AppName, Save);
        }

        // ------------------------------------------------------------------ the project file, watched while it is open

        private ProjectWatcher watcher;

        /// <summary>
        /// The project's file is watched the whole time it is open: when another app saves it (the building changed in the
        /// Building Model), the building is read again and the wind calculated for it at once; the wind inputs on the screen
        /// are kept. Nothing is saved in answer.
        /// </summary>
        private void Watch()
        {
            watcher?.Dispose();
            watcher = null;
            if (!HasProject) return;
            watcher = new ProjectWatcher(path);
            watcher.ChangedOnDisk += (s, e) =>
            {
                if (!HasProject || (editor != null && !editor.HasExited)) return; // Edit building: read again when it closes
                if (viewOnly)
                {
                    // nothing here to keep: their project read again whole, the wind too
                    reopeningViewOnly = true;
                    try { Open(path); }
                    finally { reopeningViewOnly = false; }
                    ShowStatus("They saved this project: it was read again (view only).");
                    return;
                }
                ReloadBuilding(false);
                ShowStatus("Another app saved this project: the building was read again and the wind calculated for it." + (Dirty ? " Your unsaved wind inputs are kept." : ""));
            };
        }

        // ------------------------------------------------------------------ the building: edited in the Building Model editor

        private static string EditorPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BuildingModel.exe");

        /// <summary>
        /// Saves the wind inputs and opens the project in the Building Model editor. This window waits while it is open and
        /// reads the building again when it closes (the wind inputs are kept).
        /// </summary>
        private void EditBuilding() => EditBuilding(null);

        /// <summary>The Building Model on the project, at a member when one is given (an issue clicked: <c>--select</c>, as the hub does).</summary>
        private void EditBuilding(string select)
        {
            if (!HasProject) { ShowStatus("A quick check has no building model: its size and roof are typed on the Building page."); return; }
            if (RefusedViewOnly("the Building Model is not opened on it from here")) return;
            if (editor != null && !editor.HasExited) return;
            if (!File.Exists(EditorPath))
            {
                MessageBox.Show(this, "The Building Model editor (BuildingModel.exe) is not next to this program:\n" + EditorPath, "Edit building", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (Dirty && !Save()) return;
            try
            {
                editor = Process.Start(new ProcessStartInfo(EditorPath, "\"" + path + "\"" + (string.IsNullOrEmpty(select) ? "" : " --select " + select)) { UseShellExecute = false });
            }
            catch (Exception ex) when (ex is System.ComponentModel.Win32Exception || ex is InvalidOperationException)
            {
                Log.Error("App", "Could not start the Building Model editor", ex);
                MessageBox.Show(this, "The Building Model editor could not be started:\n" + ex.Message, "Edit building", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            Log.Info("App", "Building Model editor opened on " + path);
            host.IsEnabled = false;
            ShowStatus("The building is open in the Building Model editor. Edit and save it there, then close it: this window reads the building again.");
            editor.EnableRaisingEvents = true;
            editor.Exited += (s, a) => Dispatcher.BeginInvoke(new Action(() =>
            {
                editor = null;
                host.IsEnabled = true;
                ReloadBuilding(false);
            }));
        }

        /// <summary>Reads the building from the file again; the wind inputs on the screen are kept.</summary>
        private void ReloadBuilding(bool ask)
        {
            if (!HasProject) return;
            try
            {
                doc = ModelFile.Load(path);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is InvalidDataException ||
                                       ex is System.Runtime.Serialization.SerializationException)
            {
                MessageBox.Show(this, "The building could not be read again:\n" + ex.Message, "Reload", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            ApplyGeometry(doc.Building);
            Log.Info("App", "Building read again from " + path);
            UpdateTitle();
        }

        // ------------------------------------------------------------------ the frame

        private void UpdateTitle()
        {
            Title = HasProject ? $"{Path.GetFileName(path).Replace(ProjectFolder.ModelExtension, "")}{(Dirty ? " *" : "")}{(viewOnly ? " (view only)" : "")} - {AppName}" : $"Quick check - {AppName}";
            saveItem.IsEnabled = HasProject && !viewOnly;
            closeItem.IsEnabled = HasProject;
            editItem.IsEnabled = HasProject && !viewOnly;
            reloadItem.IsEnabled = HasProject;
        }

        private void ShowStatus(string text) => status.Text = text ?? "";

        private void OpenFolder(string folder)
        {
            try
            {
                Directory.CreateDirectory(folder);
                Process.Start(new ProcessStartInfo("explorer.exe", "\"" + folder + "\"") { UseShellExecute = true });
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is System.ComponentModel.Win32Exception)
            {
                ShowStatus("The folder could not be opened: " + ex.Message);
            }
        }

        private sealed class Command : ICommand
        {
            private readonly Action run;
            public Command(Action run) => this.run = run;
            public event EventHandler CanExecuteChanged { add { } remove { } }
            public bool CanExecute(object parameter) => true;
            public void Execute(object parameter) => run();
        }
    }
}
