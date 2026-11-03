using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace ASCE7WindLoadCalculator
{
    /// <summary>
    /// Shows the report before printing. The viewer's Print button (Ctrl+P) opens the Windows print dialog; choose
    /// "Microsoft Print to PDF" there to save a PDF.
    /// </summary>
    public class ReportPreviewWindow : Window
    {
        public DocumentViewer Viewer { get; } = new DocumentViewer();

        public ReportPreviewWindow(FixedDocument report, string title)
        {
            Title = "Report preview: " + title + "  (Print: Ctrl+P; choose \"Microsoft Print to PDF\" for a PDF)";
            Width = WindDetailsWindow.FitSize(1200, SystemParameters.WorkArea.Width); // never larger than the screen
            Height = WindDetailsWindow.FitSize(900, SystemParameters.WorkArea.Height);
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Viewer.Document = report;
            Content = Viewer;
            Loaded += (s, e) => { WindDetailsWindow.KeepOnScreen(this, SystemParameters.WorkArea); Viewer.FitToMaxPagesAcross(1); };
        }
    }
}
