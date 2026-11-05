using System;
using System.Collections.Generic;
using System.Linq;
using ASCE7WindLoadCalculator;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ASCE7Wind.Tests
{
    /// <summary>
    /// The Chapter 28 wind cases (7-22 Load Cases 1 to 4 at each reference corner, each sign of GCpi), each with its own
    /// zones, against the hand calculations of <see cref="EnvelopeTests"/>: p = qh Kd [(GCpf) − (GCpi)].
    /// </summary>
    [TestClass]
    public class EnvelopeWindCaseTests
    {
        private static WindAnalysisResult Analysis(BuildingData b, ASCE7_Versions v = ASCE7_Versions.ASCE_VER_7_22) =>
            WindAnalysis.Run(v, new WindParameters_Base { WindSpeed = 115, ExposureCategory = WindExposureCategories.WIND_EXP_CAT_C }, b);

        private static IEnumerable<BuildingData> Buildings()
        {
            yield return Bldg.Make(60, 40, 15);
            yield return Bldg.Make(60, 40, 10, RoofTypes.ROOF_TYPE_GABLE, 30, RidgeDirections.RIDGE_DIR_PARALLEL_TO_BLDGLENGTH);
            yield return Bldg.Make(60, 40, 10, RoofTypes.ROOF_TYPE_GABLE, 30, RidgeDirections.RIDGE_DIR_PERP_TO_BLDGLENGTH);
        }

        [TestMethod]
        public void Count_TwoAxes_FourCorners_BasicAndTorsional_EachGCpi()
        {
            var a = Analysis(Bldg.Make(60, 40, 15));
            var all = WindCases.BuildEnvelope(a);
            // a flat roof: Case A (0-5° row) and Case B each way, as a sloped roof uses both tables
            // and the minimum (28.3.6 in 7-22) as a separate case from each side of each axis (C28.3.6)
            Assert.AreEqual(2 * 2 * 4 * 2 * 2 + 4, all.Count);
            Assert.AreEqual(4, all.Count(c => c.IsMinimumCase && c.FigureCase == WindLateralLoads.MinimumCaseNumber && c.InternalSign == 0));
            Assert.AreEqual((all.Count - 4) / 2, all.Count(c => c.CoefficientCase == "B"));
            Assert.AreEqual(all.Count, all.Select(c => c.Name).Distinct().Count(), "names are unique");
            Assert.IsTrue(all.All(c => c.Procedure == LateralProcedure.Envelope));
            CollectionAssert.AreEquivalent(new[] { "SW", "SE", "NW", "NE" }, all.Where(c => !c.IsMinimumCase).Select(c => c.ReferenceCorner).Distinct().ToArray());
            Assert.AreEqual(32 + 4, WindCases.BuildEnvelope(a, torsionalExempt: true).Count);
            var gab = WindCases.BuildEnvelope(Analysis(Bldg.Make(60, 40, 10, RoofTypes.ROOF_TYPE_GABLE, 30, RidgeDirections.RIDGE_DIR_PARALLEL_TO_BLDGLENGTH)));
            Assert.AreEqual(2 * 4 * 2 * 2 + 4, gab.Count, "a gable: one set each way, by the ridge; and the four minimum cases");

            // the gable, ridge along the length: wind across the ridge (from S, N) is LC1 / LC3 (Case A), along it LC2 / LC4 (Case B)
            var g = WindCases.BuildEnvelope(Analysis(Bldg.Make(60, 40, 10, RoofTypes.ROOF_TYPE_GABLE, 30, RidgeDirections.RIDGE_DIR_PARALLEL_TO_BLDGLENGTH)));
            Assert.IsTrue(g.Where(c => !c.IsMinimumCase && (c.From == "S" || c.From == "N")).All(c => c.CoefficientCase == "A" && (c.FigureCase == 1 || c.FigureCase == 3)));
            Assert.IsTrue(g.Where(c => !c.IsMinimumCase && (c.From == "W" || c.From == "E")).All(c => c.CoefficientCase == "B" && (c.FigureCase == 2 || c.FigureCase == 4)));
        }

        /// <summary>
        /// Flat 60 × 40 × 15 ft, wind from the W, a = 4 ft, end zone 2a = 8 ft. Reference corner SW: zone 1E on the west wall
        /// from y = 0 to 8, net 0.61 q − qh Kd (+0.18). Reference corner NW: the same strip at y = 32 to 40. Torsional case:
        /// the far half of zone 1 (y = 20 to 40 from SW) at 25 %: 0.25 × 0.40 q − 0.18 qh Kd.
        /// </summary>
        [TestMethod]
        public void FlatRoof_EndZoneAtTheReferenceCorner_AndTheTZones()
        {
            var a = Analysis(Bldg.Make(60, 40, 15));
            var e = EnvelopeLateralLoads.Direction((WindLoadCalculator_MWFRS)a.MwfrsAlongLength, "along length");
            double q = e.Qh * e.Kd, qi = 0.18 * q;
            var all = WindCases.BuildEnvelope(a);
            WindCase One(string name) => all.Single(c => c.Name == name);

            var sw = One("Ch 28 LC1 (Case A), from W, ref. corner SW, +GCpi");
            var z1e = sw.Zones.Single(z => z.Label == "Zone1E");
            Assert.AreEqual(BuildingFace.WestWall, z1e.Face);
            Assert.AreEqual(0.61 * q - qi, z1e.NetPsf, 1e-9);
            Assert.AreEqual(8 * 15, z1e.Area, 1e-9);
            Assert.AreEqual(4, z1e.Centroid.Y, 1e-9);

            var nw = One("Ch 28 LC1 (Case A), from W, ref. corner NW, −GCpi");
            var n1e = nw.Zones.Single(z => z.Label == "Zone1E");
            Assert.AreEqual(BuildingFace.WestWall, n1e.Face);
            Assert.AreEqual(36, n1e.Centroid.Y, 1e-9);
            Assert.AreEqual(0.61 * q + qi, n1e.NetPsf, 1e-9);

            // from the E, reference corner NE: zone 1E on the east wall at the north end; leeward 4E on the west wall there
            var ne = One("Ch 28 LC1 (Case A), from E, ref. corner NE, +GCpi");
            Assert.AreEqual(BuildingFace.EastWall, ne.Zones.Single(z => z.Label == "Zone1E").Face);
            Assert.AreEqual(36, ne.Zones.Single(z => z.Label == "Zone4E").Centroid.Y, 1e-9);
            Assert.AreEqual(BuildingFace.WestWall, ne.Zones.Single(z => z.Label == "Zone4E").Face);

            var t = One("Ch 28 LC3 (Case A, torsional), from W, ref. corner SW, +GCpi");
            var z1t = t.Zones.Single(z => z.Label == "Zone1T" && z.Face == BuildingFace.WestWall);
            Assert.AreEqual(0.25 * 0.40 * q - qi, z1t.NetPsf, 1e-9);
            Assert.AreEqual(20 * 15, z1t.Area, 1e-9);
            Assert.AreEqual(30, z1t.Centroid.Y, 1e-9);
            // the near half keeps the full value
            Assert.AreEqual(0.40 * q - qi, t.Zones.Single(z => z.Label == "Zone1" && z.Face == BuildingFace.WestWall).NetPsf, 1e-9);
        }

        /// <summary>
        /// The zones are the case: their horizontal forces add up to the case's Vx and Vy, and their moment about the plan
        /// center to Mt, at every corner of both axes (no parapet; the roof is in the shear here). Internal pressure cancels.
        /// </summary>
        [TestMethod]
        public void ZoneForces_AddUpToVxVyMt_AtEveryCorner()
        {
            foreach (var b in Buildings())
            {
                var all = WindCases.BuildEnvelope(Analysis(b));
                double cx = b.BuildingLength / 2, cy = b.BuildingWidth / 2;
                foreach (var c in all.Where(c => !c.IsMinimumCase))   // the minimum's 8 psf roof part is in Vx, not drawn
                {
                    Assert.IsFalse(c.RoofNeglected, c.Name);
                    double fx = c.Zones.Sum(z => z.ForceKips.X), fy = c.Zones.Sum(z => z.ForceKips.Y);
                    double mz = c.Zones.Sum(z => (z.Centroid.X - cx) * z.ForceKips.Y - (z.Centroid.Y - cy) * z.ForceKips.X);
                    string tag = $"{b.RoofType} {b.RidgeDirection}: {c.Name}";
                    Assert.AreEqual(c.Vx, fx, 1e-6, tag + " Vx");
                    Assert.AreEqual(c.Vy, fy, 1e-6, tag + " Vy");
                    Assert.AreEqual(c.Mt, mz, 1e-6, tag + " Mt");
                }
            }
        }

        /// <summary>Each case covers each surface once, every wall and roof plane. In Case A the walls parallel to the wind
        /// take GCpf = 0 (Fig. 28.3-1): their zone is there, with the internal pressure alone.</summary>
        [TestMethod]
        public void EveryCase_CoversEachSurfaceOnce()
        {
            foreach (var b in Buildings())
            {
                var a = Analysis(b);
                var g = new WindGeometry(a.MwfrsAlongLength.buildingData);
                foreach (var c in WindCases.BuildEnvelope(a).Where(c => !c.IsMinimumCase))   // the minimum covers the windward wall only
                    foreach (var s in g.Surfaces)
                    {
                        double covered = c.Zones.Where(z => z.Face == s.Face).Sum(z => z.Area);
                        Assert.AreEqual(s.Area, covered, 1e-6 * Math.Max(1, s.Area), $"{b.RoofType}: {c.Name}: {s.Name}");
                    }
            }
        }

        /// <summary>
        /// Flat 60 × 40 × 15 ft, Case A from the W: the south and north walls (parallel to the wind) take GCpf = 0, so their net
        /// pressure is the internal pressure alone, −0.18 qh Kd with +GCpi and +0.18 qh Kd with −GCpi, over the whole wall
        /// (60 × 15 ft). Equal and opposite on the two walls, they add nothing to Vy or Mt.
        /// </summary>
        [TestMethod]
        public void CaseA_WallsParallelToTheWind_GCpfZero_InternalPressureOnly()
        {
            var a = Analysis(Bldg.Make(60, 40, 15));
            var e = EnvelopeLateralLoads.Direction((WindLoadCalculator_MWFRS)a.MwfrsAlongLength, "along length");
            double q = e.Qh * e.Kd;
            var all = WindCases.BuildEnvelope(a);
            foreach (var (name, net) in new[] { ("+GCpi", -0.18 * q), ("−GCpi", 0.18 * q) })
            {
                var c = all.Single(k => k.Name == "Ch 28 LC1 (Case A), from W, ref. corner SW, " + name);
                foreach (var face in new[] { BuildingFace.SouthWall, BuildingFace.NorthWall })
                {
                    var z = c.Zones.Single(k => k.Face == face);
                    Assert.AreEqual(WindCases.ZeroSideWall, z.Label);
                    Assert.AreEqual(0, z.ExternalPsf, 1e-12);
                    Assert.AreEqual(net, z.NetPsf, 1e-9, name);
                    Assert.AreEqual(60 * 15, z.Area, 1e-9);
                }
            }
        }

        [TestMethod]
        public void Envelope_OfTheChapter28Cases_CoversEverySurface()
        {
            foreach (var b in Buildings())
            {
                var a = Analysis(b);
                var all = WindCases.BuildEnvelope(a);
                var cells = WindCaseEnvelope.Build(all);
                var g = new WindGeometry(a.MwfrsAlongLength.buildingData);
                foreach (var s in g.Surfaces)
                    Assert.AreEqual(s.Area, cells.Where(c => c.Face == s.Face).Sum(c => c.Area), 1e-6 * s.Area, s.Name);
                Assert.IsTrue(cells.All(c => c.MaxBy != null && c.MinBy != null && c.MaxNetPsf >= c.MinNetPsf));
                // the end zone at a corner: the largest push there is 1E with −GCpi (0.61 q + 0.18 q) for a flat roof
                if (b.RoofType == RoofTypes.ROOF_TYPE_FLAT)
                {
                    var e = EnvelopeLateralLoads.Direction((WindLoadCalculator_MWFRS)a.MwfrsAlongLength, "along length");
                    double q = e.Qh * e.Kd;
                    var corner = cells.Single(c => c.Face == BuildingFace.WestWall && WindCaseEnvelope.Inside(c.Outline, new Vec2(1, 1))
                                                   && !c.Outline.Any(p => Math.Abs(p.X - 1) < 1e-9 && Math.Abs(p.Y - 1) < 1e-9));
                    Assert.AreEqual(0.61 * q + 0.18 * q, corner.MaxNetPsf, 1e-9);
                    CollectionAssert.Contains(new[] { "Zone1E", "Zone5E" }, corner.MaxZone, "Case A 1E or Case B 5E, both 0.61");
                }
            }
        }
    }
}
