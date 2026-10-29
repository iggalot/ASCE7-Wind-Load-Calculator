using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using ASCE7WindLoadCalculator;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ASCE7Wind.Tests
{
    /// <summary>
    /// Golden-master capture of every zone area and pressure for a matrix of buildings.
    /// Used to prove that refactoring steps do not change results; any intentional change
    /// (a code-correctness fix) must show up as an explained diff between two captures.
    /// Writes only when GOLDEN_OUT is set, so a normal test run is unaffected.
    /// </summary>
    [TestClass]
    public class GoldenMaster
    {
        internal static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        internal static IEnumerable<(ASCE7_Versions ver, WindLoadCalculationTypes type, BuildingData bldg, WindParameters_Base p, string key)> Matrix()
        {
            var sizes = new (double L, double W, double H)[]
            {
                (60, 40, 15), (40, 60, 15), (40, 40, 10), (80, 30, 25), (30, 30, 15), (100, 50, 40)
            };
            var roofs = new (RoofTypes type, RidgeDirections ridge)[]
            {
                (RoofTypes.ROOF_TYPE_FLAT, RidgeDirections.RIDGE_DIR_NONE),
                (RoofTypes.ROOF_TYPE_GABLE, RidgeDirections.RIDGE_DIR_PARALLEL_TO_BLDGLENGTH),
                (RoofTypes.ROOF_TYPE_GABLE, RidgeDirections.RIDGE_DIR_PERP_TO_BLDGLENGTH),
                (RoofTypes.ROOF_TYPE_HIP, RidgeDirections.RIDGE_DIR_PARALLEL_TO_BLDGLENGTH),
            };
            var pitches = new[] { 5.0, 7.0, 10.0, 15.0, 22.0, 30.0, 40.0 };
            var exposures = new[] { WindExposureCategories.WIND_EXP_CAT_B, WindExposureCategories.WIND_EXP_CAT_C, WindExposureCategories.WIND_EXP_CAT_D };
            var enclosures = new[] { BuildingEnclosures.BLDG_ENCLOSED, BuildingEnclosures.BLDG_PARTIALLY_ENCLOSED };

            foreach (ASCE7_Versions ver in new[] { ASCE7_Versions.ASCE_VER_7_16, ASCE7_Versions.ASCE_VER_7_22 })
                foreach (WindLoadCalculationTypes type in new[] { WindLoadCalculationTypes.COMPONENT_AND_CLADDING, WindLoadCalculationTypes.MWFRS })
                    foreach (var s in sizes)
                        foreach (var r in roofs)
                            foreach (var pitch in (r.type == RoofTypes.ROOF_TYPE_FLAT ? new[] { 0.0 } : pitches))
                                foreach (var exp in exposures)
                                    foreach (var enc in enclosures)
                                    {
                                        var b = new BuildingData
                                        {
                                            BuildingLength = s.L,
                                            BuildingWidth = s.W,
                                            BuildingHeight = s.H,
                                            RoofPitch = pitch,
                                            RoofType = r.type,
                                            RidgeDirection = r.ridge,
                                            EnclosureType = enc,
                                        };
                                        b.ValidateRidgeDirection();
                                        var p = new WindParameters_Base
                                        {
                                            WindSpeed = 115,
                                            ExposureCategory = exp,
                                            Kzt = 1.0,
                                            AnalysisType = type,
                                        };
                                        string key = string.Format(Inv, "{0}|{1}|L{2}W{3}H{4}|{5}|{6}|p{7}|{8}|{9}",
                                            ver, type, s.L, s.W, s.H, r.type, b.RidgeDirection, pitch, exp, enc);
                                        yield return (ver, type, b, p, key);
                                    }
        }

        internal static string Fmt(double d) => d.ToString("R", Inv);

        static double GetXY(object pt, string name) =>
            Convert.ToDouble(pt.GetType().GetProperty(name).GetValue(pt), Inv);

        internal static void Dump(StringBuilder sb, string key, WindLoadCalculator_Base calc)
        {
            void Areas(string tag, AreaCalculator_Base ac)
            {
                if (ac == null) { sb.Append(key).Append("|AREA|").Append(tag).Append("|null\n"); return; }
                foreach (var kv in ac.effWindAreas.OrderBy(k => k.Key))
                {
                    var c = kv.Value.Centroid;
                    sb.Append(key).Append("|AREA|").Append(tag).Append('|').Append(kv.Key).Append('|')
                      .Append(kv.Value.Label_Full).Append('|').Append(Fmt(kv.Value.Area)).Append('|')
                      .Append(Fmt(GetXY(c, "X"))).Append('|').Append(Fmt(GetXY(c, "Y"))).Append('\n');
                }
            }
            Areas("roof", calc.RoofAreaCalculator);
            Areas("wallL", calc.WallAreaCalculator_BldgLength);
            Areas("wallW", calc.WallAreaCalculator_BldgWidth);

            foreach (var prop in typeof(WindLoadCalculator_Base).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                                            .Where(pi => pi.PropertyType == typeof(Dictionary<int, PressureData>))
                                                            .OrderBy(pi => pi.Name, StringComparer.Ordinal))
            {
                var dict = (Dictionary<int, PressureData>)prop.GetValue(calc);
                foreach (var kv in dict.OrderBy(k => k.Key))
                {
                    var pd = kv.Value;
                    sb.Append(key).Append("|P|").Append(prop.Name).Append('|').Append(kv.Key).Append('|')
                      .Append(Fmt(pd.qh)).Append('|').Append(Fmt(pd.GCp)).Append('|').Append(Fmt(pd.GCpi)).Append('|')
                      .Append(Fmt(pd.ExternalPressure)).Append('|').Append(Fmt(pd.InternalPressure)).Append('|')
                      .Append(Fmt(pd.NetPressure)).Append('\n');
                }
            }
        }

        internal static (string text, long ms, int runs, int failures) Capture()
        {
            var sb = new StringBuilder(1 << 22);
            int runs = 0, failures = 0;
            var sw = Stopwatch.StartNew();
            foreach (var m in Matrix())
            {
                runs++;
                try
                {
                    var calc = WindLoadCalculatorFactory.Create(m.ver, m.type, m.p, m.bldg);
                    calc.Initialize();
                    Dump(sb, m.key, calc);
                }
                catch (Exception ex)
                {
                    failures++;
                    sb.Append(m.key).Append("|EXC|").Append(ex.GetType().Name).Append('\n');
                }
            }
            sw.Stop();
            return (sb.ToString(), sw.ElapsedMilliseconds, runs, failures);
        }

        [TestMethod, TestCategory("Golden")]
        public void Capture_ToFile_WhenRequested()
        {
            string path = Environment.GetEnvironmentVariable("GOLDEN_OUT");
            if (string.IsNullOrEmpty(path))
                Assert.Inconclusive("Set GOLDEN_OUT to write a golden-master capture.");

            var _ = Capture(); // warm-up (JIT) so the timing below is representative
            var (text, ms, runs, failures) = Capture();
            File.WriteAllText(path, text);
            File.WriteAllText(path + ".timing.txt",
                string.Format(Inv, "runs={0} failures={1} elapsed_ms={2} per_run_us={3:F1}\n", runs, failures, ms, 1000.0 * ms / runs));
        }
    }
}
