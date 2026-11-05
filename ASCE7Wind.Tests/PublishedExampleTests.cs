using System;
using System.Collections.Generic;
using System.Linq;
using ASCE7WindLoadCalculator;
using ASCE7WindLoadCalculator.ModelAdapter;
using BuildingModel.Geometry;
using BuildingModel.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ASCE7Wind.Tests
{
    /// <summary>
    /// Published worked examples, independent of this program's author: the expected values are the examples' own printed
    /// numbers, never worked out here. Each test builds its own building model of the example, runs it through the whole
    /// chain (model → WindAdapter → WindAnalysis → lateral loads) and compares. The examples, their figures and what was
    /// chosen from them are in audit/examples (one folder each). Where an example rounds or departs from the standard, the
    /// test says so in its message rather than changing the published number.
    /// </summary>
    [TestClass]
    public class PublishedExampleTests
    {
        // ---------------------------------------------------------------- shared

        private static Building Model(double x, double y, (string name, double z)[] levels, Roof roof)
        {
            var b = Building.New();
            foreach (var (name, z) in levels) b.AddDiaphragm(b.AddLevel(name, z), new Rect2(0, 0, x, y));
            b.Roof = roof;
            return b;
        }

        private static (WindModelInput input, WindAnalysisResult analysis) Run(Building b, double V, WindOptions options = null,
            LateralProcedure procedure = LateralProcedure.Directional)
        {
            var input = WindAdapter.From(b, options);
            var p = new WindParameters_Base
            {
                WindSpeed = V, ExposureCategory = WindExposureCategories.WIND_EXP_CAT_C, Kzt = 1.0, LateralProcedure = procedure,
            };
            return (input, WindAnalysis.Run(ASCE7_Versions.ASCE_VER_7_16, p, input.Building));
        }

        private static double Deg(double rise, double run) => Math.Atan(rise / run) * 180 / Math.PI;

        private static IEnumerable<(EffectiveWindArea zone, PressureData p)> Roof(WindLoadCalculator_Base c, bool mostNegative)
        {
            var set = mostNegative ? c.windPressureRoof_Neg_External : c.windPressureRoof_Pos_External;
            foreach (var kv in c.RoofAreaCalculator.effWindAreas)
                if (set.TryGetValue(kv.Key, out var p)) yield return (kv.Value, p);
        }

        private static double RoofCp(WindLoadCalculator_Base c, string zone, bool mostNegative) =>
            Roof(c, mostNegative).First(z => z.zone.Label_Full == zone).p.GCp;

        private static double WallCp(WindLoadCalculator_Base c, string zone) =>
            c.windPressureWall_Pos_External_MWFRS[c.WallAreaCalculator_BldgLength.effWindAreas.First(kv => kv.Value.Label_Full == zone).Key].GCp;

        // ================================================================ Example 1
        // T. Bartlett Quimby, "A Beginner's Guide to ASCE 7-05", Example Problem 7.1, Two Story Building, Method 2.
        // ASCE 7-05 Method 2 is today's Chapter 27 directional procedure: the same Fig. 27.3-1 Cp, G = 0.85, GCpi = 0.18, and
        // with I = 1.0 its qz = 0.00256 Kz Kzt Kd V² I is ASCE 7-16's qz (Kd inside q, Ke = 1). Run as ASCE 7-16.
        // Building: 90 ft E-W × 50 ft N-S, two 11 ft stories, 3:12 gable spanning the 50 ft (ridge E-W), V = 120 mph, Exp C.

        private static Building Quimby() => Model(90, 50, new[] { ("2nd floor", 11.0), ("Roof", 22.0) },
            new Roof { Shape = RoofShape.Gable, Pitch = Deg(3, 12), Ridge = RidgeAxis.WestEast, Overhang = 0, Parapet = 0 });

        [TestMethod]
        public void Ex1_Quimby_VelocityPressures()
        {
            var (input, a) = Run(Quimby(), 120);
            var c = a.MwfrsAlongWidth;
            Assert.AreEqual(25.1, input.Building.MeanRoofHeight, 0.05, "h = 2·11 + (3/12)·25/2 = 25.1 ft");
            Assert.AreEqual(29.7, c.CalculateDynamicWindPressure(25.125), 0.05, "qh, roof (Kz 0.946)");
            Assert.AreEqual(27.1, c.CalculateDynamicWindPressure(16.5), 0.05, "qz, 2nd floor at 16.5 ft (Kz 0.866)");
            Assert.AreEqual(26.6, c.CalculateDynamicWindPressure(5.5), 0.05, "qz, 1st floor at 5.5 ft (Kz 0.849, 15 ft)");
            Assert.AreEqual(0.18, c.GetGCpi(), 1e-9, "GCpi, enclosed");
            Assert.AreEqual(5.34, c.CalculateDynamicWindPressure(25.125) * c.GetGCpi(), 0.02, "qh GCpi");
        }

        /// <summary>N/S wind: normal to the ridge, L/B = 50/90, h/L = 0.50, roof 14.0°.</summary>
        [TestMethod]
        public void Ex1_Quimby_NS_Coefficients()
        {
            var c = Run(Quimby(), 120).analysis.MwfrsAlongWidth;
            Assert.AreEqual(0.8, WallCp(c, "ZoneWW"), 1e-9, "windward wall");
            Assert.AreEqual(-0.5, WallCp(c, "ZoneLW"), 1e-9, "leeward wall, L/B 0.556");
            Assert.AreEqual(-0.7, WallCp(c, "ZoneSW"), 1e-9, "side walls");
            Assert.AreEqual(-0.5, RoofCp(c, "ZoneLWR", true), 0.005, "leeward roof");
            Assert.AreEqual(-0.18, RoofCp(c, "ZoneWWR", false), 0.005, "windward roof, the other value");
            Assert.AreEqual(-0.70, RoofCp(c, "ZoneWWR", true), 0.005,
                "windward roof: the example takes the 15° value (\"close to 15 degrees and probably not worth interpolating\"); " +
                "the roof is 14.04°, so Fig. 27.3-1 interpolated gives -0.74");
        }

        /// <summary>E/W wind: parallel to the ridge, L/B = 90/50 = 1.8, distance zones from the windward edge.</summary>
        [TestMethod]
        public void Ex1_Quimby_EW_Coefficients()
        {
            var c = Run(Quimby(), 120).analysis.MwfrsAlongLength;
            Assert.AreEqual(-0.34, WallCp(c, "ZoneLW"), 0.005, "leeward wall, L/B 1.8 interpolated");
            Assert.AreEqual(-0.9, RoofCp(c, "Zone4", true), 0.005, "P11, 0 to h/2");
            Assert.AreEqual(-0.9, RoofCp(c, "Zone3", true), 0.005, "P11, h/2 to h");
            Assert.AreEqual(-0.5, RoofCp(c, "Zone2", true), 0.005, "P12, h to 2h");
            Assert.AreEqual(-0.3, RoofCp(c, "Zone1", true), 0.005, "P13, beyond 2h");
            foreach (var z in new[] { "Zone4", "Zone3", "Zone2", "Zone1" })
                Assert.AreEqual(-0.18, RoofCp(c, z, false), 0.005, z + ", the other value");
            // the bands' plan areas: P11 0 to 25.1 ft, P12 25.1 to 50.2 ft, P13 the rest, each × 50 ft wide
            double Plan(string z) => Roof(c, true).Where(r => r.zone.Label_Full == z).Sum(r => r.zone.Area);
            Assert.AreEqual(25.125 * 50, Plan("Zone4") + Plan("Zone3"), 1.0, "P11 plan area");
            Assert.AreEqual(25.125 * 50, Plan("Zone2"), 1.0, "P12 plan area");
        }

        /// <summary>
        /// Net horizontal force along the wind (k), the example's sums: N/S 58.29 (windward roof -0.7) and 65.66 (-0.18), E/W
        /// 34.01. Internal pressure cancels. The program reports the governing roof value (the larger total).
        /// </summary>
        [TestMethod]
        public void Ex1_Quimby_HorizontalForces()
        {
            var lat = WindLateralLoads.Compute(Run(Quimby(), 120).analysis);
            Assert.AreEqual(65.66, lat.AlongWidth.TotalKips, 0.01 * 65.66, "N/S wind, governing roof value (Case II, -0.18)");
            Assert.AreEqual(34.01, lat.AlongLength.TotalKips, 0.01 * 34.01, "E/W wind");
        }

        /// <summary>
        /// Uplift on the roof (k), the example's vertical sums for N/S wind with + internal pressure: Case I 92.06 (windward
        /// roof -0.7), Case II 62.58 (-0.18). Here: each roof zone's net pressure (external − qh GCpi) × its plan area.
        /// </summary>
        [TestMethod]
        public void Ex1_Quimby_NS_RoofUplift()
        {
            var c = Run(Quimby(), 120).analysis.MwfrsAlongWidth;
            double Up(bool neg) => -Roof(c, neg).Sum(r => (r.p.ExternalPressure - r.p.InternalPressure) * r.zone.Area) / 1000;
            Assert.AreEqual(62.58, Up(false), 0.01 * 62.58, "Case II: windward roof -0.18");
            Assert.AreEqual(92.06, Up(true), 0.01 * 92.06, "Case I: windward roof -0.7 (the example's 15° value; -0.74 at 14.04°)");
        }

        /// <summary>
        /// Uplift for E/W wind (k), parallel to the ridge, + internal pressure: Case I 83.34 (bands -0.9 / -0.5 / -0.3),
        /// Case II 44.43 (all -0.18).
        /// </summary>
        [TestMethod]
        public void Ex1_Quimby_EW_RoofUplift()
        {
            var c = Run(Quimby(), 120).analysis.MwfrsAlongLength;
            double Up(bool neg) => -Roof(c, neg).Sum(r => (r.p.ExternalPressure - r.p.InternalPressure) * r.zone.Area) / 1000;
            Assert.AreEqual(83.34, Up(true), 0.01 * 83.34, "Case I");
            Assert.AreEqual(44.43, Up(false), 0.01 * 44.43, "Case II");
        }

        // ================================================================ Example 2
        // SkyCiv, "ASCE 7-10 Wind Load Calculation Example": plant building, Cordova TN, 64 ft × 104 ft, eave 30 ft, apex 36 ft,
        // roof 3:16 (10.62°), partially enclosed (GCpi ±0.55), V = 120 mph, Exp C, Kd 0.85, G 0.85, three stories (10, 20, 30 ft).
        // The 6 ft rise over a 32 ft half span puts the ridge along the 104 ft side; "wind along L" (the 64 ft) is normal to it.
        // ASCE 7-10 Chapter 27 is ASCE 7-16's (same q = 0.00256 Kz Kzt Kd V², Ke = 1). Run as ASCE 7-16.

        private static Building SkyCiv() => Model(64, 104, new[] { ("Level 2", 10.0), ("Level 3", 20.0), ("Roof", 30.0) },
            new Roof { Shape = RoofShape.Gable, Pitch = Deg(3, 16), Ridge = RidgeAxis.SouthNorth, Overhang = 0, Parapet = 0 });

        private static readonly WindOptions Partial = new WindOptions { Enclosure = BuildingEnclosures.BLDG_PARTIALLY_ENCLOSED };

        [TestMethod]
        public void Ex2_SkyCiv_VelocityPressures()
        {
            var (input, a) = Run(SkyCiv(), 120, Partial);
            var c = a.MwfrsAlongLength;
            Assert.AreEqual(33.0, input.Building.MeanRoofHeight, 0.01, "h, average of eave and apex");
            Assert.AreEqual(26.63, c.CalculateDynamicWindPressure(10), 0.05, "qz at 10 ft (Kz 0.85)");
            Assert.AreEqual(28.20, c.CalculateDynamicWindPressure(20), 0.10, "qz at 20 ft (Kz 0.90)");
            Assert.AreEqual(30.71, c.CalculateDynamicWindPressure(30), 0.10, "qz at 30 ft (Kz 0.98)");
            Assert.AreEqual(31.33, c.CalculateDynamicWindPressure(33), 0.10, "qh at 33 ft (Kz 1.00)");
            Assert.AreEqual(0.55, c.GetGCpi(), 1e-9, "GCpi, partially enclosed");
        }

        /// <summary>Wind along L (64 ft), normal to the ridge: h/L 0.516, θ 10.62°, the example's Table 7.</summary>
        [TestMethod]
        public void Ex2_SkyCiv_AlongL_RoofCoefficients()
        {
            var c = Run(SkyCiv(), 120, Partial).analysis.MwfrsAlongLength;
            // The example does not reduce: -0.89 is the unreduced value. h/L = 33 / 64 = 0.5156 reaches 3.1% into the
            // h/L >= 1.0 row, whose -1.3 (at 10°) Fig. 27.3-1 note b reduces with area: the windward roof is 32 x 104 / cos 10.62°
            // = 3386 sq ft, f = 0.8. At θ = 10.62°: h/L 0.5 row -0.9 + 0.1239 x 0.2 = -0.8752; h/L 1.0 row -1.3 + 0.1239 x 0.3
            // = -1.2628 unreduced, -1.04 + 0.1239 x 0.04 = -1.0350 reduced. Cp = -0.8752 + 0.03125 (-1.2628 + 0.8752) = -0.8873
            // unreduced; -0.8752 + 0.03125 (-1.0350 + 0.8752) = -0.8802 reduced.
            var wwr = Roof(c, true).First(z => z.zone.Label_Full == "ZoneWWR").p;
            Assert.AreEqual(-0.89, wwr.GCpUnreduced.Value, 0.006, "windward roof, unreduced (the example)");
            double t = (Math.Atan(3.0 / 16) * 180 / Math.PI - 10) / 5, w = (33.0 / 64 - 0.5) / 0.5;
            double mid = -0.9 + t * 0.2;
            Assert.AreEqual(mid + w * (-1.3 + t * 0.3 - mid), wwr.GCpUnreduced.Value, 1e-9, "unreduced, by hand");
            Assert.AreEqual(mid + w * (-1.04 + t * 0.04 - mid), wwr.GCp, 1e-9, "reduced, by hand (note b, f = 0.8)");
            Assert.AreEqual(0.8, wwr.AreaReductionFactor, 1e-12);
            Assert.AreEqual(-0.18, RoofCp(c, "ZoneWWR", false), 0.006, "windward roof, the other value");
            Assert.AreEqual(-0.51, RoofCp(c, "ZoneLWR", true), 0.006, "leeward roof");
        }

        /// <summary>Wind along B (104 ft), parallel to the ridge: h/B 0.317, the example's Table 8.</summary>
        [TestMethod]
        public void Ex2_SkyCiv_AlongB_RoofCoefficients()
        {
            var c = Run(SkyCiv(), 120, Partial).analysis.MwfrsAlongWidth;
            Assert.AreEqual(-0.9, RoofCp(c, "Zone4", true), 0.005, "0 to h/2");
            Assert.AreEqual(-0.9, RoofCp(c, "Zone3", true), 0.005, "h/2 to h");
            Assert.AreEqual(-0.5, RoofCp(c, "Zone2", true), 0.005, "h to 2h");
            Assert.AreEqual(-0.3, RoofCp(c, "Zone1", true), 0.005, "beyond 2h");
            Assert.AreEqual(-0.18, RoofCp(c, "Zone1", false), 0.005, "the other value");
        }

        /// <summary>
        /// Design pressures (psf), the example's Tables 9 and 10, wind along L: p = q G Cp − qh (±GCpi), windward wall with qz,
        /// the rest with qh.
        /// </summary>
        [TestMethod]
        public void Ex2_SkyCiv_AlongL_DesignPressures()
        {
            var c = (WindLoadCalculator_MWFRS)Run(SkyCiv(), 120, Partial).analysis.MwfrsAlongLength;
            double qhi = c.CalculateDynamicWindPressure(33) * c.GetGCpi();
            double Wall(string z) => c.windPressureWall_Pos_External_MWFRS[c.WallAreaCalculator_BldgLength.effWindAreas.First(kv => kv.Value.Label_Full == z).Key].ExternalPressure;
            double RoofP(string z, bool neg) => Roof(c, neg).First(r => r.zone.Label_Full == z).p.ExternalPressure;
            // every row checked; all misses reported together (row: published, computed, tolerance)
            var rows = new (double published, double computed, double tol, string what)[]
            {
                (0.88, c.WindwardWallExternalPressureAt(10) - qhi, 0.05, "windward wall at 10 ft, +GCpi"),
                (35.35, c.WindwardWallExternalPressureAt(10) + qhi, 0.05, "windward wall at 10 ft, -GCpi"),
                (4.07, c.WindwardWallExternalPressureAt(33) - qhi, 0.10, "windward wall at 33 ft, +GCpi"),
                (38.54, c.WindwardWallNetPressureAt(33), 0.10, "windward wall at 33 ft, -GCpi"),
                (-30.55, Wall("ZoneLW") - qhi, 0.05, "leeward wall, +GCpi"),
                (3.92, Wall("ZoneLW") + qhi, 0.05, "leeward wall, -GCpi"),
                (-35.88, Wall("ZoneSW") - qhi, 0.05, "side wall, +GCpi"),
                (-1.41, Wall("ZoneSW") + qhi, 0.05, "side wall, -GCpi"),
                (-40.87, RoofP("ZoneWWR", true) - qhi, 0.10, "windward roof, +GCpi"),
                (-6.41, RoofP("ZoneWWR", true) + qhi, 0.10, "windward roof, -GCpi"),
                (-22.03, RoofP("ZoneWWR", false) - qhi, 0.10, "windward roof (Cp -0.18), +GCpi"),
                (12.44, RoofP("ZoneWWR", false) + qhi, 0.10, "windward roof (Cp -0.18), -GCpi"),
                (-30.71, RoofP("ZoneLWR", true) - qhi, 0.15, "leeward roof, +GCpi"),
                (3.76, RoofP("ZoneLWR", true) + qhi, 0.15, "leeward roof, -GCpi"),
            };
            var misses = rows.Where(r => Math.Abs(r.published - r.computed) > r.tol)
                .Select(r => $"{r.what}: published {r.published:0.00}, computed {r.computed:0.00} (tol {r.tol})").ToList();
            Assert.AreEqual(0, misses.Count, "the example rounds Kz to 2 places (Kh 1.00, qh 31.33); this program uses the power law " +
                $"(qh {c.CalculateDynamicWindPressure(33):0.00}).\n  " + string.Join("\n  ", misses));
        }

        // ================================================================ Example 3
        // Medeek Design wind load report (online calculator, issued by an engineering firm), ASCE 7-10, MWFRS envelope procedure
        // (Ch. 28 Part 1, Fig. 28.4-1 = ASCE 7-16 Fig. 28.3-1): gable, 36 ft wide × 48 ft long, eave 12 ft, 4/12 (18.43°),
        // 2 ft overhangs, enclosed, V = 115 mph, Exp C, Kzt 1, Kd .85; h = 15 ft, qh = 24.43 psf. Run as ASCE 7-16, Ch. 28.
        // Transverse (Load Case A) is the wind across the ridge, along the 36 ft; longitudinal (Case B) along the ridge.

        private static Building Medeek() => Model(48, 36, new[] { ("Roof", 12.0) },
            new Roof { Shape = RoofShape.Gable, Pitch = Deg(4, 12), Ridge = RidgeAxis.WestEast, Overhang = 2, Parapet = 0 });

        private static WindLateralResult MedeekLateral() =>
            WindLateralLoads.Compute(Run(Medeek(), 115, null, LateralProcedure.Envelope).analysis, false, LateralProcedure.Envelope);

        private static double Gcpf(EnvelopeDirection d, string zone) => d.Zones.First(z => z.Zone == zone && !z.IsParapet).GCpf.Value;

        [TestMethod]
        public void Ex3_Medeek_Parameters()
        {
            var lat = MedeekLateral();
            Assert.IsNull(lat.ProcedureNote, "Chapter 28 applies: " + lat.ProcedureNote);
            var t = lat.EnvelopeAlongWidth;
            Assert.AreEqual(15.0, Run(Medeek(), 115).input.Building.MeanRoofHeight, 0.01, "h");
            Assert.AreEqual(24.43, t.Qh * t.Kd, 0.05, "qh (Kh 0.85)");
            Assert.AreEqual(18.43, t.Theta, 0.01, "roof angle");
            Assert.AreEqual(3.00, t.A, 0.005, "end zone width a, as printed");
        }

        /// <summary>Load Case A, transverse: GCpf interpolated at 18.43° (the report's table).</summary>
        [TestMethod]
        public void Ex3_Medeek_CaseA_Coefficients()
        {
            var t = MedeekLateral().EnvelopeAlongWidth;
            Assert.AreEqual("A", t.LoadCase);
            var expected = new Dictionary<string, double>
            {
                ["1"] = 0.52, ["2"] = -0.69, ["3"] = -0.47, ["4"] = -0.42, ["1E"] = 0.78, ["2E"] = -1.07, ["3E"] = -0.67, ["4E"] = -0.62,
            };
            foreach (var kv in expected) Assert.AreEqual(kv.Value, Gcpf(t, kv.Key), 0.006, "zone " + kv.Key);
            // design pressure with +GCpi: qh (GCpf − 0.18)
            var printed = new Dictionary<string, double>
            {
                ["1"] = 8.22, ["2"] = -21.25, ["3"] = -15.84, ["4"] = -14.54, ["1E"] = 14.66, ["2E"] = -30.54, ["3E"] = -20.85, ["4E"] = -19.50,
            };
            double q = t.Qh * t.Kd;
            foreach (var kv in printed) Assert.AreEqual(kv.Value, q * (Gcpf(t, kv.Key) - 0.18), 0.15, "zone " + kv.Key + " with +GCpi");
        }

        /// <summary>Load Case B, longitudinal: GCpf (the report's table).</summary>
        [TestMethod]
        public void Ex3_Medeek_CaseB_Coefficients()
        {
            var l = MedeekLateral().EnvelopeAlongLength;
            Assert.AreEqual("B", l.LoadCase);
            var expected = new Dictionary<string, double>
            {
                ["1"] = -0.45, ["2"] = -0.69, ["3"] = -0.37, ["4"] = -0.45, ["5"] = 0.40, ["6"] = -0.29,
                ["1E"] = -0.48, ["2E"] = -1.07, ["3E"] = -0.53, ["4E"] = -0.48, ["5E"] = 0.61, ["6E"] = -0.43,
            };
            foreach (var kv in expected) Assert.AreEqual(kv.Value, Gcpf(l, kv.Key), 0.006, "zone " + kv.Key);
        }

        /// <summary>Torsional cases: the T zones at 25% of the full pressure (1T 2.05 = 0.25 × 8.22 psf ...).</summary>
        [TestMethod]
        public void Ex3_Medeek_TorsionalZones()
        {
            var lat = MedeekLateral();
            foreach (var z in new[] { "1", "2", "3", "4" })
                Assert.IsTrue(lat.EnvelopeAlongWidth.Zones.Any(r => r.Zone == z && Math.Abs(r.TorsionalFactor - 0.25) < 1e-9), z + "T in Case A");
            foreach (var z in new[] { "5", "6" })
                Assert.IsTrue(lat.EnvelopeAlongLength.Zones.Any(r => r.Zone == z && Math.Abs(r.TorsionalFactor - 0.25) < 1e-9), z + "T in Case B");
        }

        /// <summary>
        /// The roof's horizontal force, transverse (Case A), no overhangs: the report's -1027 lb ASD = -1.712 k at strength.
        /// It depends on the end zone width: by hand, qh × 6 ft rise × [(-0.69 + 0.47)(48 - 2a) + (-1.07 + 0.67)(2a)] =
        /// -1.706 k with the report's a = 3.0, -1.738 k with a = 3.6 (0.1 × 36 ft, the standard's).
        /// </summary>
        [TestMethod]
        public void Ex3_Medeek_CaseA_RoofHorizontal()
        {
            var t = MedeekLateral().EnvelopeAlongWidth;
            double roof = t.Zones.Where(z => z.IsRoof).Sum(z => z.Fx);
            // By hand, 18.43°: q nx = 24.43 x sin 18.43° / 1000 = 0.007725 k/sq ft per unit GCpf; each slope 18 x 48 / cos
            // = 910.7 sq ft, the 2a end strip 2a x 18 / cos. Fx = 0.007725 [-(1.07 A_E + 0.69 A_I) + (0.673 A_E + 0.4685 A_I)].
            // The report prints -1.712 with its a = 3.0 ft (A_E 113.84, A_I 796.90); with a = 3.6 ft (0.1 x 36 &lt; 0.4 x 15,
            // Fig. 28.3-1 note 9: A_E 136.61, A_I 774.13) it is -1.743.
            double cos = Math.Cos(Math.Atan(4.0 / 12)), qn = t.Qh * t.Kd * Math.Sin(Math.Atan(4.0 / 12)) / 1000;
            double g3E = -t.Zones.First(z => z.Zone == "3E" && z.IsRoof).GCpf.Value, g3 = -t.Zones.First(z => z.Zone == "3" && z.IsRoof).GCpf.Value;
            double Fx(double a) { double ae = 2 * a * 18 / cos, ai = 18 * 48 / cos - ae; return qn * (-(1.07 * ae + 0.69 * ai) + g3E * ae + g3 * ai); }
            Assert.AreEqual(-1.712, Fx(3.0), 0.002, "the report's value, with its a = 3.0");
            Assert.AreEqual(Fx(t.A), roof, 1e-9, $"roof horizontal (k), a = {t.A}");
        }

        /// <summary>
        /// The report's base shears are ASD (× 0.6) diaphragm reactions with the bottom half of the walls left out. Back to
        /// strength level, whole walls: Case A walls 4180 / 0.6 × 2 = 13.93 k, the roof's negative horizontal set aside
        /// ("Roof Pressure = 0" governs, note d); Case B walls 2277 / 0.6 × 2 + gable ends 1100 / 0.6 = 9.42 k.
        /// </summary>
        [TestMethod]
        public void Ex3_Medeek_BaseShear()
        {
            var lat = MedeekLateral();
            var t = lat.EnvelopeAlongWidth;
            Assert.IsTrue(t.RoofNeglected, "transverse: the roof's horizontal force is negative, so it is set aside");
            Assert.AreEqual(13.93, t.BasicKips, 0.02 * 13.93, "Case A, walls only");
            Assert.AreEqual(9.42, lat.EnvelopeAlongLength.BasicKips, 0.03 * 9.42, "Case B, walls and gable ends");
        }
    }
}
