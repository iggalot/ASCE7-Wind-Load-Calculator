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
            // its own windward edge: Zone4 (0 to h/2) starts at y = 30, the wing's south wall, not the building's
            var z4 = wingBands.Where(z => z.Label.StartsWith("Zone4")).ToList();
            Assert.IsTrue(z4.Count > 0);
            Assert.AreEqual(30, z4.SelectMany(z => z.Outline).Min(p => p.Y), 1e-6);

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
            // slopes trade places, the main roof pushes nothing along x, so the force is the same and the torsion reversed
            Assert.AreEqual(60, Roof(east, "main").Where(z => z.Label.StartsWith("Zone4")).SelectMany(z => z.Outline).Max(p => p.X), 1e-6);
            Assert.AreEqual(0, Roof(west, "main").Where(z => z.Label.StartsWith("Zone4")).SelectMany(z => z.Outline).Min(p => p.X), 1e-6);
            Assert.AreEqual(west.Vx, -east.Vx, 1e-6);
            Assert.AreEqual(west.Mt, -east.Mt, 1e-6);
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
        /// (y / 2) up to 5, where it is above: ∫ from y = 4 to 10 of (5 − y / 2) dy = 9 sq ft. Its back (y = 12): its rake above
        /// the roof's 6, 1.5 − |x − 20| / 2: ½ × 6 × 1.5 = 4.5 sq ft. From the south the front takes q(z) G (0.8) Kd, the back
        /// q(h) G Cp(leeward) Kd: Vy grows by their F = −p A n.
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
            Assert.AreEqual(4.5, walls.Single(x => x.Name.StartsWith("dormer, north")).Area, 1e-6);
            Assert.AreEqual(4, walls.Count, "the main roof's walls stand on the ground: the bounding box's");

            var a = Run(d);
            var bare = d.Clone(); bare.RoofWalls = new List<RoofWallPiece>();
            var withWalls = WindCases.Build(a).First(c => c.FigureCase == 1 && c.From == "S");
            var without = WindCases.Build(Run(bare)).First(c => c.Name == withWalls.Name);
            var my = (WindLoadCalculator_MWFRS)a.MwfrsAlongWidth;
            double G = my.Parameters.GustFactor, kd = my.PressureKd, qh = my.CalculateDynamicWindPressure(my.buildingData.MwfrsFigureHeight);
            double qz = my.CalculateDynamicWindPressure(Math.Max(15, front.CentroidZ));
            double back = walls.Single(x => x.Name.StartsWith("dormer, north")).Area;
            double expected = (qz * G * 0.8 * kd * front.Area - qh * G * my.Cp_LW * kd * back) / 1000.0;
            Assert.AreEqual(without.Vy + expected, withWalls.Vy, 1e-9);
            Assert.AreEqual(without.Mt, withWalls.Mt, 1e-9, "the dormer on the plan's center line (x = 20): no torsion of its own");
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
