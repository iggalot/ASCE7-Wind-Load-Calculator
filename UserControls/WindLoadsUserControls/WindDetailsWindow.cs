using System.Windows;

namespace ASCE7WindLoadCalculator
{
    /// <summary>The detailed calculations (<see cref="WindDetailsView"/>) in their own resizable window.</summary>
    public class WindDetailsWindow : Window
    {
        public const string InputsPage = WindDetailsView.InputsPage, CladdingPage = WindDetailsView.CladdingPage,
                            WestPage = WindDetailsView.WestPage, SouthPage = WindDetailsView.SouthPage,
                            CasesPage = WindDetailsView.CasesPage;

        public WindDetailsView View { get; } = new WindDetailsView();

        public WindDetailsWindow(ASCE7_Versions version, WindParameters_Base parameters, BuildingData building, WindAnalysisResult analysis)
        {
            Title = "Detailed wind calculations";
            Width = 1400;
            Height = 900;
            MinWidth = 900;
            MinHeight = 600;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Content = View;
            View.SetResults(version, parameters, building, analysis);
        }

        public string CurrentPage => View.CurrentPage;
        public FrameworkElement CurrentContent => View.CurrentContent;
        public void ShowPage(string name) => View.ShowPage(name);
    }
}
