using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ASCE7WindLoadCalculator
{
    /// <summary>
    /// Display choices remembered between sessions (not engineering inputs, so they are not saved in project files):
    /// one "name=value" line each in %AppData%\ASCE7WindLoadCalculator\viewer.txt. Reading or writing never throws;
    /// a missing or unreadable file gives the defaults.
    /// </summary>
    public static class ViewerPreferences
    {
        /// <summary>Where the choices are kept. Tests point it at a temporary file.</summary>
        public static string FilePath { get => filePath; set { filePath = value; values = null; } }

        private static string filePath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ASCE7WindLoadCalculator", "viewer.txt");

        private static Dictionary<string, string> values;

        /// <summary>3D view: write each zone's psf on the zone (default off).</summary>
        public static bool PsfLabels
        {
            get => Get(nameof(PsfLabels)) == "1";
            set => Set(nameof(PsfLabels), value ? "1" : "0");
        }

        private static string Get(string name)
        {
            Load();
            return values.TryGetValue(name, out var v) ? v : null;
        }

        private static void Set(string name, string value)
        {
            Load();
            if (values.TryGetValue(name, out var old) && old == value) return;
            values[name] = value;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
                File.WriteAllLines(FilePath, values.Select(kv => kv.Key + "=" + kv.Value));
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                // a display preference that cannot be saved is simply not remembered
            }
        }

        private static void Load()
        {
            if (values != null) return;
            values = new Dictionary<string, string>(StringComparer.Ordinal);
            try
            {
                if (!File.Exists(FilePath)) return;
                foreach (var line in File.ReadAllLines(FilePath))
                {
                    int i = line.IndexOf('=');
                    if (i > 0) values[line.Substring(0, i).Trim()] = line.Substring(i + 1).Trim();
                }
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
            }
        }
    }
}
