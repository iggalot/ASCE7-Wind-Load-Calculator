using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using ASCE7WindLoadCalculator;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ASCE7Wind.Tests
{
    /// <summary>
    /// Calculation-only timing (no output formatting) over the golden-master matrix, plus a UI-like pass
    /// that reads every zone's area and centroid repeatedly, as the results tables and drawings do.
    /// Writes to BENCH_OUT when set; uses only API that existed before the refactor so the same file can
    /// time the original engine.
    /// </summary>
    [TestClass]
    public class Benchmark
    {
        [TestMethod, TestCategory("Benchmark")]
        public void Time_Calculation_And_ResultReads()
        {
            string path = Environment.GetEnvironmentVariable("BENCH_OUT");
            if (string.IsNullOrEmpty(path))
                Assert.Inconclusive("Set BENCH_OUT to run the benchmark.");

            var cases = GoldenMaster.Matrix().ToList();
            const int repeats = 5;

            double Run(bool reads)
            {
                var sw = Stopwatch.StartNew();
                double sink = 0;
                for (int r = 0; r < repeats; r++)
                    foreach (var m in cases)
                    {
                        try
                        {
                            var calc = WindLoadCalculatorFactory.Create(m.ver, m.type, m.p.Clone(), m.bldg.Clone());
                            calc.Initialize();
                            if (reads)
                                foreach (var zones in new[] { calc.RoofAreaCalculator, calc.WallAreaCalculator_BldgLength, calc.WallAreaCalculator_BldgWidth })
                                    if (zones != null)
                                        for (int k = 0; k < 10; k++)
                                            foreach (var z in zones.effWindAreas.Values)
                                                sink += z.Area + z.Centroid.X;
                        }
                        catch (Exception) { }
                    }
                sw.Stop();
                if (sink == double.MinValue) Console.WriteLine(sink); // keep the reads live
                return 1000.0 * sw.Elapsed.TotalMilliseconds / (repeats * cases.Count);
            }

            Run(true); // warm-up / JIT
            double calcOnly = new[] { Run(false), Run(false), Run(false) }.Min();
            double withReads = new[] { Run(true), Run(true), Run(true) }.Min();
            File.WriteAllText(path, $"cases={cases.Count} calc_only_us={calcOnly:F1} calc_plus_ui_reads_us={withReads:F1}\n");
        }
    }
}
