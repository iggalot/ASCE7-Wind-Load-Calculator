using System;
using System.Windows;
using System.Windows.Input;

namespace ASCE7WindLoadCalculator
{
    /// <summary>The detailed calculations (<see cref="WindDetailsView"/>) in their own resizable window.</summary>
    public class WindDetailsWindow : Window
    {
        public const string InputsPage = WindDetailsView.InputsPage, CladdingPage = WindDetailsView.CladdingPage,
                            WestPage = WindDetailsView.WestPage, SouthPage = WindDetailsView.SouthPage,
                            CasesPage = WindDetailsView.CasesPage, EnvelopePage = WindDetailsView.EnvelopePage;

        public WindDetailsView View { get; } = new WindDetailsView();

        public WindDetailsWindow(ASCE7_Versions version, WindParameters_Base parameters, BuildingData building, WindAnalysisResult analysis)
        {
            Title = "Detailed wind calculations";
            // up to 1400 × 900, never larger than the screen's work area: a window taller than the screen opened centred on
            // its owner put its title bar (and its close button) above the top of the screen
            var area = SystemParameters.WorkArea;
            Width = FitSize(1400, area.Width);
            Height = FitSize(900, area.Height);
            MinWidth = Math.Min(900, Width);
            MinHeight = Math.Min(600, Height);
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Content = View;
            View.SetResults(version, parameters, building, analysis);
            Loaded += (s, e) => KeepOnScreen(SystemParameters.WorkArea);
            KeyDown += (s, e) => { if (e.Key == Key.Escape) Close(); }; // Esc closes it too
        }

        /// <summary>The wanted size, or 95% of the room there is when that is less.</summary>
        public static double FitSize(double wanted, double room) => room > 0 ? Math.Min(wanted, Math.Floor(room * 0.95)) : wanted;

        /// <summary>Moves the window inside <paramref name="area"/>; the top-left last, so the title bar (and its close button) is always on screen.</summary>
        public void KeepOnScreen(Rect area) => KeepOnScreen(this, area);

        /// <summary>Moves <paramref name="win"/> inside <paramref name="area"/>, its top-left last (also used by the report preview).</summary>
        public static void KeepOnScreen(Window win, Rect area)
        {
            double w = win.ActualWidth > 0 ? win.ActualWidth : win.Width, h = win.ActualHeight > 0 ? win.ActualHeight : win.Height;
            if (win.Left + w > area.Right) win.Left = area.Right - w;
            if (win.Top + h > area.Bottom) win.Top = area.Bottom - h;
            if (win.Left < area.Left) win.Left = area.Left;
            if (win.Top < area.Top) win.Top = area.Top;
        }

        public string CurrentPage => View.CurrentPage;
        public FrameworkElement CurrentContent => View.CurrentContent;
        public void ShowPage(string name) => View.ShowPage(name);
    }
}
