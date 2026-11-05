using System;
using System.IO;
using System.Windows;
using BuildingModel.Diagnostics;

namespace ASCE7WindLoadCalculator.App
{
    internal static class Program
    {
        [STAThread]
        public static int Main(string[] args)
        {
            // the log: %AppData%\ASCE7WindLoadCalculator\logs until a project is open, then the project's logs folder
            Log.Start("ASCE7WindLoadCalculator", "wind");
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                Log.Error("App", "Unhandled error (the program is closing)" + (e.IsTerminating ? "" : " (not terminating)"), e.ExceptionObject as Exception);
            var app = new Application { ShutdownMode = ShutdownMode.OnMainWindowClose };
            app.DispatcherUnhandledException += (s, e) =>
            {
                Log.Error("App", "Unexpected error; the action was cancelled", e.Exception);
                MessageBox.Show("Something went wrong and that action was cancelled, but your work is still open.\n\n" + e.Exception.Message +
                                "\n\nThe details are in the log:\n" + Log.TodayFile, "Unexpected problem", MessageBoxButton.OK, MessageBoxImage.Warning);
                e.Handled = true;
            };
            app.Exit += (s, e) => Log.Info("App", "Wind Load Calculator closed.");
            var w = new MainWindow();
            if (args.Length > 0)
            {
                Log.Info("App", "Started with " + args[0]);
                if (File.Exists(args[0])) w.Loaded += (s, e) => w.Open(args[0]);
                else Log.Warning("App", "The file to open does not exist: " + args[0], null);
            }
            return app.Run(w);
        }
    }
}
