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
    /// A roof of several sections taken face by face (owner 2026-11-13): each face by its own section's ridge for each wind,
    /// the walls on the bounding box, the off-center roof's torsion in Case 1, and the walls standing on the roof (dormers).
    /// </summary>
    [TestClass]
    public class RoofFaceWindTests
    {
        private const double Pitch = 26.565051177077990;   // 6:12

        private static Building Box(double l, double w)
        {
            var b = Building.New();
            b.AddDiaphragm(b.AddLevel("Roof", 10), new Rect2(0, 0, l, w));
            return b;
        }

        private static WindAnalysisResult Run(BuildingData d) =>
            WindAnalysis.Run(ASCE7_Versions.ASCE_VER_7_22, new WindParameters_Base { WindSpeed = 115, ExposureCategory = WindExposureCategories.WIND_EXP_CAT_C }, d);

        /// <summary>
        /// One section over the whole plan, a gable W-E: face by face it is the plain gable, every case's forces and torsion
        /// the same (the main section takes the building's h and L; its faces' windward edges are the building's).
        /// </summary>
        [TestMethod]
        public void OneSection_IsThePlainRoof()
        {
            var b = Box(60, 40);
            b.Roof = new Roof { Shape = RoofShape.Gable, Pitch = Pitch, Ridge = RidgeAxis.WestEast };
            var plain = WindCases.Build(Run(WindAdapter.From(b).Building));
            b.Roof.Sections = new List<RoofSection> { new RoofSection { Name = "main", Walls = new Rect2(0, 0, 60, 40), Shape = RoofShape.Gable, Ridge = RidgeAxis.WestEast } };
            var d = WindAdapter.From(b).Building;
            Assert.IsTrue(d.HasRoofFaces);
            Assert.AreEqual(0, d.RoofWalls.Count);
            var faces = WindCases.Build(Run(d));
            Assert.AreEqual(plain.Count, faces.Count);
            for (int i = 0; i < plain.Count; i++)
            {
                Assert.AreEqual(plain[i].Name, faces[i].Name);
                Assert.AreEqual(plain[i].Vx, faces[i].Vx, 1e-6, plain[i].Name);
                Assert.AreEqual(plain[i].Vy, faces[i].Vy, 1e-6, plain[i].Name);
                Assert.AreEqual(plain[i].Mt, faces[i].Mt, 1e-6, plain[i].Name);
            }
        }

        /// <summary>
        /// A hip 60 × 30 (ridge W-E), plain and as one section, the same (owner 2026-11-13: the hip end facing a wind along the
        /// ridge takes the distance bands, the ASCE 7 subcommittee's reading, in both): from the west no windward or leeward
        /// roof at all; from the south the long slopes are.
        /// </summary>
        [TestMethod]
        public void Hip_TheEndsAlongTheRidge_TakeTheBands_PlainOrSections()
        {
            var b = Box(60, 30);
            b.Roof = new Roof { Shape = RoofShape.Hip, Pitch = Pitch, Ridge = RidgeAxis.WestEast };
            var plain = WindCases.Build(Run(WindAdapter.From(b).Building));
            b.Roof.Sections = new List<RoofSection> { new RoofSection { Name = "main", Walls = new Rect2(0, 0, 60, 30), Shape = RoofShape.Hip, Ridge = RidgeAxis.WestEast } };
            var faces = WindCases.Build(Run(WindAdapter.From(b).Building));
            foreach (var cases in new[] { plain, faces })
            {
                var west = cases.First(c => c.FigureCase == 1 && c.From == "W");
                Assert.IsFalse(west.Zones.Any(z => z.Kind == SurfaceKind.Roof && (z.Label.StartsWith("ZoneWWR") || z.Label.StartsWith("ZoneLWR"))), "along the ridge: bands only, the ends too");
                var south = cases.First(c => c.FigureCase == 1 && c.From == "S");
                Assert.IsTrue(south.Zones.Any(z => z.Kind == SurfaceKind.Roof && z.Label.StartsWith("ZoneWWR")));
            }
            var w1 = plain.First(c => c.FigureCase == 1 && c.From == "W");
            var w2 = faces.First(c => c.Name == w1.Name);
            Assert.AreEqual(w1.Vx, w2.Vx, 1e-6, "plain and one section alike");
        }

        /// <summary>
        /// An L: the main gable 60 × 30 ft W-E and a wing (40..60, 30..50) with its ridge S-N, both 6:12. From the west the
        /// main roof is parallel to its ridge (the distance bands) and the wing normal to its own (windward and leeward roof);
        /// from the south the other way round, the wing's bands from its own windward edge (y = 30). The wing sits off the
        /// plan's center, so Case 1 has a torsion: the roof pieces' Σ (c − o) × F, F = −p A n; from the east it is not the
        /// west's mirrored.
        /// </summary>
        [TestMethod]
        public void LPlan_EachFaceByItsOwnRidge_AndTheWingsTorsion()
        {
            var b = Box(60, 30);
            b.AddDiaphragm(b.Levels.Single(l => l.Name == "Roof"), new Rect2(40, 30, 60, 50));
            b.Roof = new Roof
            {
                Shape = RoofShape.Gable, Pitch = Pitch, Ridge = RidgeAxis.WestEast,
                Sections = new List<RoofSection>
                {
                    new RoofSection { Name = "main", Walls = new Rect2(0, 0, 60, 30), Shape = RoofShape.Gable, Ridge = RidgeAxis.WestEast },
                    new RoofSection { Name = "wing", Walls = new Rect2(40, 30, 60, 50), Shape = RoofShape.Gable, Ridge = RidgeAxis.SouthNorth },
                },
            };
            var w = WindAdapter.From(b);
            var d = w.Building;
            Assert.AreEqual(60, d.BuildingLength, 1e-9);
            Assert.AreEqual(50, d.BuildingWidth, 1e-9);
            // h (26.2): the eave 10 and the highest point, the main ridge 10 + 7.5; not a gable over the 50 ft width (10 + 12.5)
            Assert.AreEqual(17.5, d.RidgeHeight, 1e-9);
            Assert.AreEqual(13.75, d.MeanRoofHeight, 1e-9);
            Assert.IsTrue(w.Notes.Any(n => n.Contains("face by face")));
            Assert.IsTrue(w.Notes.Any(n => n.Contains("not regular-shaped") && n.Contains("wing")), "the wing projects past the main roof: flagged (26.2, 27.1.3)");
            var cases = WindCases.Build(Run(d));
            WindCase C1(string from) => cases.First(c => c.FigureCase == 1 && c.From == from);
            IEnumerable<WindCaseZone> Roof(WindCase c, string section) => c.Zones.Where(z => z.Kind == SurfaceKind.Roof && z.Surface.RoofFace.Section == section);

            var west = C1("W");
            Assert.IsTrue(Roof(west, "main").All(z => z.Label.StartsWith("Zone1") || z.Label.StartsWith("Zone2") || z.Label.StartsWith("Zone3") || z.Label.StartsWith("Zone4")), "main: along its ridge");
            Assert.IsTrue(Roof(west, "wing").All(z => z.Label.StartsWith("ZoneWWR") || z.Label.StartsWith("ZoneLWR")), "wing: across its ridge");
            var south = C1("S");
            Assert.IsTrue(Roof(south, "main").All(z => z.Label.StartsWith("ZoneWWR") || z.Label.StartsWith("ZoneLWR")));
            var wingBands = Roof(south, "wing").ToList();
            Assert.IsTrue(wingBands.All(z => !z.Label.StartsWith("ZoneWW") && !z.Label.StartsWith("ZoneLW")));
            // its own windward edge: the wing's ridge runs back over the main roof until it meets it (owner 2026-11-13: two
            // roofs merged, no gable wall), (30 − y) / 2 = 5 at y = 20, its slopes with it: Zone4 (0 to h/2) starts there
            var z4 = wingBands.Where(z => z.Label.StartsWith("Zone4")).ToList();
            Assert.IsTrue(z4.Count > 0);
            Assert.AreEqual(20, z4.SelectMany(z => z.Outline).Min(p => p.Y), 1e-6);
            Assert.IsFalse(d.RoofWalls.Any(x => x.Name.StartsWith("wing, south")), "no gable wall where the wing meets the main roof");

            // the torsion of Case 1 from the west: the roof's pieces about the plan's center (30, 25), external pressures
            double M(WindCase c) => c.Zones.Where(z => z.Kind == SurfaceKind.Roof).Sum(z =>
            {
                var f = z.Normal * (-z.ExternalPsf * z.Area / 1000.0);
                return (z.Centroid.X - 30) * f.Y - (z.Centroid.Y - 25) * f.X;
            });
            Assert.AreEqual(M(west), west.Mt, 1e-6);
            Assert.IsTrue(Math.Abs(west.Mt) > 0.01, $"{west.Mt}");
            var east = C1("E");
            Assert.AreEqual(M(east), east.Mt, 1e-6);
            // from the east the building is seen from there: the main roof's 0 to h/2 band at its east end; the wing's two
            // slopes trade places and the main roof pushes nothing along x, so the force is the same. The torsion is not the
            // west's reversed: the wing notches the main roof's north slope at its east end (the valleys), so that slope's push
            // along y differs with the bands taken from the other end
            Assert.AreEqual(60, Roof(east, "main").Where(z => z.Label.StartsWith("Zone4")).SelectMany(z => z.Outline).Max(p => p.X), 1e-6);
            Assert.AreEqual(0, Roof(west, "main").Where(z => z.Label.StartsWith("Zone4")).SelectMany(z => z.Outline).Min(p => p.X), 1e-6);
            Assert.AreEqual(west.Vx, -east.Vx, 1e-6);
            Assert.IsTrue(Math.Abs(west.Mt + east.Mt) > 0.01, "the notched north slope's bands differ from each end");
            // the box's walls follow the faces over their edges, not one gable over the 50 ft width (that was 500 + ½ 50 ×
            // 12.5 = 812.5): west and east the main gable (y 0..30, ridge 10 + 7.5) and the eave past it, 500 + ½ 30 × 7.5 =
            // 612.5; south the main eave, 600; north the wing's gable (x 40..60, 10 + 5), 600 + ½ 20 × 5 = 650
            double Wall(double x0, double y0, double x1, double y1) => Math.Abs(PolygonOps.SignedArea(RoofFaceProfile.WallOutline(d, x0, y0, x1, y1)));
            Assert.AreEqual(612.5, Wall(0, 0, 0, 50), 1e-6);
            Assert.AreEqual(612.5, Wall(60, 0, 60, 50), 1e-6);
            Assert.AreEqual(600, Wall(0, 0, 60, 0), 1e-6);
            Assert.AreEqual(650, Wall(0, 50, 60, 50), 1e-6);
            Assert.AreEqual(612.5, west.Zones.Where(z => z.Kind == SurfaceKind.Wall && z.Label.StartsWith("ZoneWW")).Sum(z => z.Area), 1e-6, "the windward wall's bands");
            Assert.AreEqual(650, south.Zones.Where(z => z.Kind == SurfaceKind.Wall && z.Label.StartsWith("ZoneLW")).Sum(z => z.Area), 1e-6, "from the south, the leeward north wall");
            // the Results page's force table is the wind cases themselves, the roof's torsion in it
            var table = WindLateralLoads.Compute(Run(d)).LoadCases;
            Assert.AreEqual(cases.Count, table.Count);
            var t1 = table.Single(c => c.Name == west.Name);
            Assert.AreEqual(west.Vx, t1.Vx, 1e-9);
            Assert.AreEqual(west.Mt, t1.Mt, 1e-9);
            // Case 2 adds 0.75 of it to the Fig. 27.3-8 eccentricity
            var c2 = cases.First(c => c.FigureCase == 2 && c.From == "W" && c.RoofAlongLength == west.RoofAlongLength && c.InternalSign == west.InternalSign && c.Name.EndsWith("torsion +"));
            Assert.AreEqual(0.75 * west.Vx * 0.15 * 50 + 0.75 * west.Mt, c2.Mt, 1e-6);
        }

        /// <summary>
        /// A dormer on the south slope of a 40 × 30 gable (W-E, 6:12, tan θ = 1/2): its walls (15..25, 4..12), a gable S-N at
        /// 6:12, its eave 5 ft above the roof level. Its front (y = 4) from the main roof (z = y / 2 = 2) up to its own eave
        /// (5) and rake (5 + (5 − |x − 20|) / 2): 10 × 3 + ½ × 10 × 2.5 = 42.5 sq ft. Each cheek (x = 15, 25) from the roof
        /// (y / 2) up to 5, where it is above: ∫ from y = 4 to 10 of (5 − y / 2) dy = 9 sq ft. No back wall: its ridge (5 + 2.5)
        /// runs back into the main roof, which rises to it at its ridge (y = 15; owner 2026-11-13, two roofs merged). From the
        /// south the front takes q(z) G (0.8) Kd: Vy grows by its F = −p A n.
        /// </summary>
        [TestMethod]
        public void Dormer_ItsWallsOnTheRoof_ByHand()
        {
            var b = Box(40, 30);
            b.Roof = new Roof
            {
                Shape = RoofShape.Gable, Pitch = Pitch, Ridge = RidgeAxis.WestEast,
                Sections = new List<RoofSection>
                {
                    new RoofSection { Name = "main", Walls = new Rect2(0, 0, 40, 30), Shape = RoofShape.Gable, Ridge = RidgeAxis.WestEast },
                    new RoofSection { Name = "dormer", Walls = new Rect2(15, 4, 25, 12), Shape = RoofShape.Gable, Ridge = RidgeAxis.SouthNorth, EaveRise = 5 },
                },
            };
            var d = WindAdapter.From(b).Building;
            var walls = d.RoofWalls;
            var front = walls.Single(x => x.Name.StartsWith("dormer, south"));
            Assert.AreEqual(42.5, front.Area, 1e-6);
            Assert.AreEqual(-1, front.Normal.Y, 1e-12);
            Assert.AreEqual(20, front.Centroid.X, 1e-6);
            Assert.AreEqual(9, walls.Single(x => x.Name.StartsWith("dormer, west")).Area, 1e-6);
            Assert.AreEqual(9, walls.Single(x => x.Name.StartsWith("dormer, east")).Area, 1e-6);
            Assert.IsFalse(walls.Any(x => x.Name.StartsWith("dormer, north")), "its ridge runs back into the roof: no back wall");
            Assert.AreEqual(3, walls.Count, "the main roof's walls stand on the ground: the bounding box's");

            var a = Run(d);
            var bare = d.Clone(); bare.RoofWalls = new List<RoofWallPiece>();
            var withWalls = WindCases.Build(a).First(c => c.FigureCase == 1 && c.From == "S");
            var without = WindCases.Build(Run(bare)).First(c => c.Name == withWalls.Name);
            var my = (WindLoadCalculator_MWFRS)a.MwfrsAlongWidth;
            double G = my.Parameters.GustFactor, kd = my.PressureKd, qh = my.CalculateDynamicWindPressure(my.buildingData.MwfrsFigureHeight);
            double qz = my.CalculateDynamicWindPressure(Math.Max(15, front.CentroidZ));
            double expected = qz * G * 0.8 * kd * front.Area / 1000.0;
            Assert.AreEqual(without.Vy + expected, withWalls.Vy, 1e-9);
            Assert.AreEqual(without.Mt, withWalls.Mt, 1e-9, "the dormer on the plan's center line (x = 20): no torsion of its own");
        }
        /// <summary>
        /// Tutorial 8's L-shaped house (owner 2026-11-13, "the wind loads are acting on the bounding box of the building, not
        /// on the individual walls"): a hip over the main block 56 × 28, a gable wing 24 × 24 north of its east end, a dormer
        /// (12..20, 1..8) on the south slope, 6:12, 1.5 ft eaves.
        /// </summary>
        internal static Building LHouse()
        {
            var b = Building.New();
            var top = b.AddLevel("Level 2", 9);
            b.AddDiaphragm(top, new Rect2(0, 0, 56, 28));
            b.AddDiaphragm(top, new Rect2(32, 28, 56, 52)).Offset = -1;   // as the tutorial's: the wing's floor a foot down
            b.Roof = new Roof
            {
                Shape = RoofShape.Hip, Pitch = Pitch, Ridge = RidgeAxis.WestEast, Overhang = 1.5,
                Sections = new List<RoofSection>
                {
                    new RoofSection { Name = "main", Walls = new Rect2(0, 0, 56, 28), Shape = RoofShape.Hip, Ridge = RidgeAxis.WestEast },
                    new RoofSection { Name = "wing", Walls = new Rect2(32, 28, 56, 52), Shape = RoofShape.Gable, Ridge = RidgeAxis.SouthNorth },
                    new RoofSection { Name = "dormer", Walls = new Rect2(12, 1, 20, 8), Shape = RoofShape.Gable, Ridge = RidgeAxis.SouthNorth, EaveRise = 3 },
                },
            };
            return b;
        }

        /// <summary>
        /// The L's walls are its own six, not the bounding box's four: none around the empty corner (x 0..32, y 28..52). From
        /// the west the windward walls are x = 0 (y 0..28) and the wing's x = 32 (y 28..52), each its own outline, 9 ft at the
        /// eave (the hip's end and the wing's eave side): 28 × 9 + 24 × 9. The south wall stops at the main roof's eave (9 ft)
        /// under the dormer too: the dormer's eave reaching past it is over the roof, not on the wall.
        /// </summary>
        [TestMethod]
        public void LHouse_TheWallsAreItsOwn()
        {
            var d = WindAdapter.From(LHouse()).Building;
            Assert.AreEqual(6, d.WallLines.Count, string.Join("; ", d.WallLines));
            var g = new WindGeometry(d);
            Assert.AreEqual(6, g.Walls.Count());
            foreach (var w in g.Walls)
                foreach (var p in w.Outline)
                    Assert.IsFalse(p.X < 32 - 1e-6 && p.Y > 28 + 1e-6, $"{w.Name}: nothing in the empty corner ({p.X}, {p.Y})");
            var south = g.WallsFacing(BuildingFace.SouthWall).Single();
            Assert.AreEqual(9, south.Outline.Max(p => p.Z), 1e-6, "the dormer's eave is not the south wall's top");
            Assert.AreEqual(56 * 9, south.Area, 1e-6);

            var west = WindCases.Build(Run(d)).First(c => c.FigureCase == 1 && c.From == "W");
            var ww = west.Zones.Where(z => z.Kind == SurfaceKind.Wall && z.Label.StartsWith("ZoneWW")).ToList();
            Assert.AreEqual(28 * 9 + 24 * 9, ww.Sum(z => z.Area), 1e-6);
            Assert.AreEqual(2, ww.Select(z => z.Surface).Distinct().Count(), "both walls facing west");
            Assert.IsTrue(ww.Any(z => z.Surface.Line.A.X == 32), "the wing's west wall, set back");
        }

        /// <summary>Every piece of the L's roof takes a zone in every wind (none left without a pressure).</summary>
        [TestMethod]
        public void LHouse_EveryRoofPieceTakesTheWind() => EveryRoofPieceTakesTheWind(LHouse());

        /// <summary>
        /// The result made from the three calculations alone (as the 3D view and SWC make it) works out the winds from the east
        /// and north itself: its cases are the full run's, not the west's and south's mirrored.
        /// </summary>
        [TestMethod]
        public void LHouse_ThreeCalculations_TheFullRunsCases()
        {
            var d = WindAdapter.From(LHouse()).Building;
            var full = Run(d);
            var three = new WindAnalysisResult(full.Cladding, full.MwfrsAlongLength, full.MwfrsAlongWidth);
            Assert.IsNotNull(three.MwfrsFromEast);
            Assert.IsNotNull(three.MwfrsFromNorth);
            var a = WindCases.Build(full);
            var b = WindCases.Build(three);
            Assert.AreEqual(a.Count, b.Count);
            for (int i = 0; i < a.Count; i++)
            {
                Assert.AreEqual(a[i].Vx, b[i].Vx, 1e-9, a[i].Name);
                Assert.AreEqual(a[i].Vy, b[i].Vy, 1e-9, a[i].Name);
                Assert.AreEqual(a[i].Mt, b[i].Mt, 1e-9, a[i].Name);
                Assert.AreEqual(a[i].Zones.Count, b[i].Zones.Count, a[i].Name);
            }
        }

        /// <summary>The same on a saved project named by LHOUSE_FILE (a look at a real file; skipped without it).</summary>
        [TestMethod]
        public void LHouseFile_EveryRoofPieceTakesTheWind()
        {
            var path = Environment.GetEnvironmentVariable("LHOUSE_FILE");
            if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path)) Assert.Inconclusive("LHOUSE_FILE not set");
            EveryRoofPieceTakesTheWind(BuildingModel.Serialization.ModelFile.Load(path).Building);
        }

        private static void EveryRoofPieceTakesTheWind(Building b)
        {
            var d = WindAdapter.From(b).Building;
            var g = new WindGeometry(d);
            var faces = g.Roofs.GroupBy(r => r.RoofFace.Id).ToList();
            foreach (var c in WindCases.Build(Run(d)).Where(c => c.Zones.Any(z => z.Kind == SurfaceKind.Roof)))
                foreach (var f in faces)
                {
                    double plan = f.Sum(r => Math.Abs(PolygonOps.SignedArea(r.PlanRegion.ToList())));
                    double covered = c.Zones.Where(z => z.Kind == SurfaceKind.Roof && z.Surface.RoofFace?.Id == f.Key)
                        .Sum(z => Math.Abs(PolygonOps.SignedArea(z.Outline.Select(p => new Vec2(p.X, p.Y)).ToList())));
                    Assert.AreEqual(plan, covered, 0.01 * plan + 0.01, $"{c.Name}: {f.First().Name}");
                }
            // and the envelope of them all (the Results page's picture): every roof piece in cells, none left out
            var cases = WindCases.Build(Run(d));
            var cells = WindCaseEnvelope.Build(cases);
            foreach (var s in cases.SelectMany(c => c.Zones).Where(z => z.Kind == SurfaceKind.Roof).Select(z => z.Surface).Distinct())
            {
                double plan = Math.Abs(PolygonOps.SignedArea(s.PlanRegion.ToList()));
                double inCells = cells.Where(x => x.Surface == s).Sum(x => Math.Abs(PolygonOps.SignedArea(x.Outline.ToList())));
                Assert.AreEqual(plan, inCells, 0.01 * plan + 0.01, $"{s.Name}: the envelope's cells");
            }
            // each cell drawn whole: its triangles (as the picture draws it) add up to it
            foreach (var x in cells)
            {
                double a = Math.Abs(PolygonOps.SignedArea(x.Outline.ToList()));
                var tris = PolygonOps.Triangulate(x.Outline.ToList());
                double t = 0;
                for (int i = 0; i + 2 < tris.Count; i += 3) t += Math.Abs(PolygonOps.SignedArea(new List<Vec2> { tris[i], tris[i + 1], tris[i + 2] }));
                Assert.AreEqual(a, t, 0.01 * a + 0.01, $"{x.Surface.Name}: a cell of {x.Outline.Count} corners drawn short");
            }
            var unloaded = cells.Where(x => x.Kind == SurfaceKind.Roof && (x.MinBy == null || x.MaxBy == null)).ToList();
            Assert.AreEqual(0, unloaded.Count, string.Join("; ", unloaded.Take(8).Select(x => $"{x.Surface.Name} at ({WindCaseEnvelope.Centroid(x.Outline).X:0.#}, {WindCaseEnvelope.Centroid(x.Outline).Y:0.#}) min {x.MinBy?.Name} max {x.MaxBy?.Name}")));
        }

        /// <summary>Chapter 28 chosen on a roof of several sections: not applied (C28.2), Chapter 27 face by face, said why.</summary>
        [TestMethod]
        public void Chapter28_OnARoofOfSections_FallsBackToChapter27FaceByFace()
        {
            var b = Box(40, 30);
            b.Roof = new Roof
            {
                Shape = RoofShape.Gable, Pitch = Pitch, Ridge = RidgeAxis.WestEast,
                Sections = new List<RoofSection>
                {
                    new RoofSection { Name = "main", Walls = new Rect2(0, 0, 40, 30), Shape = RoofShape.Gable, Ridge = RidgeAxis.WestEast },
                    new RoofSection { Name = "dormer", Walls = new Rect2(15, 4, 25, 12), Shape = RoofShape.Gable, Ridge = RidgeAxis.SouthNorth, EaveRise = 5 },
                },
            };
            var d = WindAdapter.From(b).Building;
            Assert.IsTrue(EnvelopeLateralLoads.Applicability(d).Any(x => x.Contains("several sections") && x.Contains("28.1.3")));
            Assert.IsFalse(WindAdapter.From(b).Notes.Any(n => n.Contains("not regular-shaped")), "a dormer on the roof is no projection of the plan");
            var a = WindAnalysis.Run(ASCE7_Versions.ASCE_VER_7_22, new WindParameters_Base
            {
                WindSpeed = 115, ExposureCategory = WindExposureCategories.WIND_EXP_CAT_C, LateralProcedure = LateralProcedure.Envelope,
            }, d);
            Assert.AreEqual(LateralProcedure.Directional, WindCases.ProcedureOf(a));
            Assert.AreEqual(WindCases.Build(a).Count, WindCases.For(a).Count);
        }

    }
}
