using System;
using System.Linq;
using ASCE7WindLoadCalculator;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ASCE7Wind.Tests
{
    /// <summary>
    /// The Fig. 27.3-1 roof bands are distances from the windward edge (0 to h/2, h/2 to h, h to 2h, beyond 2h), so the depth
    /// of the building along the wind decides which bands exist: a narrow building has only the bands that fit, the last
    /// one cut at the leeward edge. Flat roof, 10 ft along the length × 60 ft along the width, h = 15 ft:
    /// - wind from the W (depth 10 ft, h/L = 1.5, the h/L ≥ 1.0 row): 0 to 7.5 ft (Case A −1.3) and 7.5 to 10 ft (−0.7);
    ///   the h to 2h and beyond-2h bands do not exist. Case B: −0.18 on both.
    /// - wind from the S (depth 60 ft, h/L = 0.25, the h/L ≤ 0.5 row): all four bands, 0-7.5, 7.5-15, 15-30, 30-60 ft
    ///   (−0.9, −0.9, −0.5, −0.3).
    /// </summary>
    [TestClass]
    public class NarrowBuildingZoneTests
    {
        private static WindAnalysisResult Narrow() =>
            WindAnalysis.Run(ASCE7_Versions.ASCE_VER_7_16,
                new WindParameters_Base { WindSpeed = 115, ExposureCategory = WindExposureCategories.WIND_EXP_CAT_C },
                Bldg.Make(10, 60, 15));

        private static double Depth(WindCaseZone z, bool alongX) =>
            alongX ? z.Outline.Max(p => p.X) - z.Outline.Min(p => p.X) : z.Outline.Max(p => p.Y) - z.Outline.Min(p => p.Y);

        [TestMethod]
        public void OnlyTheBandsThatFit_AlongTheWind()
        {
            var a = Narrow();
            var all = WindCases.Build(a);
            var w = all.Single(c => c.Name == "Case 1, from W, roof Case A, +GCpi");
            var roofW = w.Zones.Where(z => z.Kind == SurfaceKind.Roof).ToList();
            CollectionAssert.AreEquivalent(new[] { "Zone4 (W)", "Zone3 (W)" }, roofW.Select(z => z.Label).ToArray(), "10 ft deep: 0-h/2 and h/2 to the leeward edge only");
            Assert.AreEqual(7.5 * 60, roofW.Single(z => z.Label == "Zone4 (W)").Area, 1e-9);
            Assert.AreEqual(2.5 * 60, roofW.Single(z => z.Label == "Zone3 (W)").Area, 1e-9, "cut at the leeward edge, x = 10");
            Assert.AreEqual(10 * 60, roofW.Sum(z => z.Area), 1e-9);

            // h/L = 1.5: the h/L >= 1.0 row, -1.3 then -0.7 (the same qh G multiplies both). Fig. 27.3-1 note b: the -1.3
            // is reduced with its area, 7.5 x 60 = 450 sq ft: f = 0.9 - 0.1 (450 - 250) / 750 = 0.87333, Cp = -1.13533
            double f = 0.9 - 0.1 * (450 - 250) / 750.0;
            double e4 = roofW.Single(z => z.Label == "Zone4 (W)").ExternalPsf, e3 = roofW.Single(z => z.Label == "Zone3 (W)").ExternalPsf;
            Assert.AreEqual(1.3 * f / 0.7, e4 / e3, 1e-9);
            var p4 = ((WindLoadCalculator_MWFRS)a.MwfrsAlongLength).windPressureRoof_Neg_External.Values.First(p => p.GCpUnreduced.HasValue);
            Assert.AreEqual(-1.3, p4.GCpUnreduced.Value, 1e-12, "the unreduced value is kept");
            Assert.AreEqual(-1.3 * f, p4.GCp, 1e-12);
            Assert.AreEqual(f, p4.AreaReductionFactor, 1e-12);
            Assert.AreEqual(450, p4.ReductionArea, 1e-9);
            StringAssert.Contains(p4.ReductionNote, "note b");
            Assert.AreEqual(p4.ExternalPressure / f, p4.UnreducedExternalPressure, 1e-9);
            var wB = all.Single(c => c.Name == "Case 1, from W, roof Case B, +GCpi").Zones.Where(z => z.Kind == SurfaceKind.Roof).ToList();
            Assert.AreEqual(1, wB.Select(z => z.ExternalPsf).Distinct().Count(), "Case B: -0.18 over the whole roof");
            Assert.AreEqual(e4 * 0.18 / (1.3 * f), wB[0].ExternalPsf, 1e-9);

            // from the E: the same two bands, measured from the east edge
            var e = all.Single(c => c.Name == "Case 1, from E, roof Case A, +GCpi").Zones.Where(z => z.Kind == SurfaceKind.Roof).ToList();
            Assert.AreEqual(2.5, e.Single(z => z.Label == "Zone4 (E)").Outline.Min(p => p.X), 1e-9, "0-h/2 from the east edge: x = 2.5 to 10");
            Assert.AreEqual(0, e.Single(z => z.Label == "Zone3 (E)").Outline.Min(p => p.X), 1e-9);
        }

        [TestMethod]
        public void AllFourBands_AcrossTheDeepDirection()
        {
            var all = WindCases.Build(Narrow());
            var s = all.Single(c => c.Name == "Case 1, from S, roof Case A, +GCpi").Zones.Where(z => z.Kind == SurfaceKind.Roof).ToList();
            var depth = s.ToDictionary(z => z.Label, z => Depth(z, alongX: false));
            Assert.AreEqual(7.5, depth["Zone4 (S)"], 1e-9);
            Assert.AreEqual(7.5, depth["Zone3 (S)"], 1e-9);
            Assert.AreEqual(15, depth["Zone2 (S)"], 1e-9);
            Assert.AreEqual(30, depth["Zone1 (S)"], 1e-9, "beyond 2h = 30 ft, to the leeward edge at 60 ft");
            double e1 = s.Single(z => z.Label == "Zone1 (S)").ExternalPsf;
            Assert.AreEqual(0.9 / 0.3, s.Single(z => z.Label == "Zone4 (S)").ExternalPsf / e1, 1e-9);
            Assert.AreEqual(0.5 / 0.3, s.Single(z => z.Label == "Zone2 (S)").ExternalPsf / e1, 1e-9);
        }

        /// <summary>Chapter 28 on the same building: every zone is cut to the building, so each case still covers the roof
        /// exactly once (end zones min(2a, W), zone 2 min(0.5 D, 2.5 he)).</summary>
        [TestMethod]
        public void Chapter28_ZonesCutToTheBuilding()
        {
            var a = Narrow();
            var g = new WindGeometry(a.MwfrsAlongLength.buildingData);
            foreach (var c in WindCases.BuildEnvelope(a).Where(c => !c.IsMinimumCase))   // the minimum covers the windward wall only
                foreach (var surf in g.Surfaces)
                    Assert.AreEqual(surf.Area, c.Zones.Where(z => z.Face == surf.Face).Sum(z => z.Area), 1e-6 * surf.Area, $"{c.Name}: {surf.Name}");
        }

        /// <summary>
        /// Fig. 27.3-1: the distance bands apply with the wind normal to the ridge for θ &lt; 10°, or parallel to the ridge
        /// for any θ; normal to the ridge at θ ≥ 10° the slopes take the windward / leeward roof Cp instead. Gable 60 × 40 ft,
        /// 10 ft eave, ridge across the length (so the wind from the W is normal to it).
        /// </summary>
        [DataTestMethod]
        [DataRow(5.0, false)]
        [DataRow(9.9, false)]
        [DataRow(10.0, true)]
        [DataRow(20.0, true)]
        public void DistanceBands_NormalBelow10Degrees_OrParallelAtAnyAngle(double pitch, bool slopeZones)
        {
            var b = Bldg.Make(60, 40, 10, RoofTypes.ROOF_TYPE_GABLE, pitch, RidgeDirections.RIDGE_DIR_PERP_TO_BLDGLENGTH);
            var all = WindCases.Build(WindAnalysis.Run(ASCE7_Versions.ASCE_VER_7_16,
                new WindParameters_Base { WindSpeed = 115, ExposureCategory = WindExposureCategories.WIND_EXP_CAT_C }, b));
            string[] Labels(string from) => all.Single(c => c.Name == $"Case 1, from {from}, roof Case A, +GCpi")
                .Zones.Where(z => z.Kind == SurfaceKind.Roof).Select(z => z.Label.Split(' ')[0]).Distinct().OrderBy(s => s).ToArray();

            // normal to the ridge (from W)
            if (slopeZones) CollectionAssert.AreEqual(new[] { "ZoneLWR", "ZoneWWR" }, Labels("W"), $"{pitch}°: windward / leeward slopes");
            else Assert.IsTrue(Labels("W").All(l => l == "Zone1" || l == "Zone2" || l == "Zone3" || l == "Zone4"), $"{pitch}°: distance bands");
            // parallel to the ridge (from S): distance bands at every angle
            Assert.IsTrue(Labels("S").All(l => l == "Zone1" || l == "Zone2" || l == "Zone3" || l == "Zone4"), $"{pitch}°, along the ridge: distance bands");
        }

        /// <summary>
        /// What other programs read (the Load Manager's roof wind reads the calculator's zones and its pressure tables): on the
        /// 10 ft deep building, wind along the length, only the two bands that exist, and a pressure only for a zone that exists.
        /// </summary>
        [TestMethod]
        public void PressureTables_OnlyForZonesOnTheBuilding()
        {
            var a = Narrow();
            var m = (WindLoadCalculator_MWFRS)a.MwfrsAlongLength;
            var zones = m.RoofAreaCalculator.effWindAreas;
            CollectionAssert.AreEquivalent(new[] { "Zone4", "Zone3" }, zones.Values.Select(z => z.Label_Full).ToArray());
            foreach (var table in new[] { m.windPressureRoof_Neg_External, m.windPressureRoof_Pos_External })
            {
                Assert.IsTrue(table.Keys.All(zones.ContainsKey), "no pressure for a zone that is not on the building");
                Assert.AreEqual(zones.Count, table.Count);
            }
            Assert.AreEqual(10 * 60, zones.Values.Sum(z => z.Area), 1e-9, "the zones are the roof, nothing more");
        }
    }
}
