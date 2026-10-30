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
            Width = 1200;
            Height = 900;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Viewer.Document = report;
            Content = Viewer;
            Loaded += (s, e) => Viewer.FitToMaxPagesAcross(1);
        }
    }
}
