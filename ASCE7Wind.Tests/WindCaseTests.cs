using System;
using System.Linq;
using ASCE7WindLoadCalculator;
using ASCE7WindLoadCalculator.ModelAdapter;
using BuildingModel.Geometry;
using BuildingModel.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ASCE7Wind.Tests
{
    /// <summary>
    /// The wind cases, each with its own zone pressures, against Quimby's Example 7.1 (audit/examples/01): 90 ft E-W × 50 ft
    /// N-S, two 11 ft stories, 3:12 gable with the ridge E-W, V 120, Exp. C; qh = 29.7 psf, qh G = 25.2 psf, qh GCpi = 5.34
    /// psf. Its Cases I-IV are one wind with one roof value and one sign of GCpi each, which is what a case is here.
    /// </summary>
    [TestClass]
    public class WindCaseTests
    {
        private static WindAnalysisResult Quimby()
        {
            var b = Building.New();
            b.AddDiaphragm(b.AddLevel("2nd floor", 11), new Rect2(0, 0, 90, 50));
            b.AddDiaphragm(b.AddLevel("Roof", 22), new Rect2(0, 0, 90, 50));
            b.Roof = new Roof { Shape = RoofShape.Gable, Pitch = Math.Atan(0.25) * 180 / Math.PI, Ridge = RidgeAxis.WestEast, Overhang = 0, Parapet = 0 };
            var p = new WindParameters_Base { WindSpeed = 120, ExposureCategory = WindExposureCategories.WIND_EXP_CAT_C, Kzt = 1.0 };
            return WindAnalysis.Run(ASCE7_Versions.ASCE_VER_7_16, p, WindAdapter.From(b).Building);
        }

        private static WindCase One(System.Collections.Generic.List<WindCase> all, string name) => all.Single(c => c.Name == name);

        [TestMethod]
        public void Count_EveryWind_RoofValue_GCpi_AndTorsion()
        {
            var a = Quimby();
            var all = WindCases.Build(a);
            Assert.AreEqual(16, all.Count(c => c.FigureCase == 1));    // 4 winds × 2 roof values × 2 GCpi
            Assert.AreEqual(32, all.Count(c => c.FigureCase == 2));    // × 2 torsion
            Assert.AreEqual(32, all.Count(c => c.FigureCase == 3));    // 4 quartering × 4 roof pairs × 2 GCpi
            Assert.AreEqual(128, all.Count(c => c.FigureCase == 4));   // × 4 torsion
            Assert.AreEqual(all.Count, all.Select(c => c.Name).Distinct().Count(), "names are unique");
            Assert.AreEqual(4, all.Count(c => c.FigureCase == WindLateralLoads.MinimumCaseNumber));   // 27.1.5, one per wind (C27.1.5)
            Assert.AreEqual(48 + 4, WindCases.Build(a, onlyCases1And3: true).Count);
        }

        /// <summary>E/W wind, parallel to the ridge: Quimby's vertical sums, Cases I to IV (k, + up).</summary>
        [DataTestMethod]
        [DataRow("W")]
        [DataRow("E")]
        public void Case1_AlongTheRidge_UpliftMatchesQuimby(string from)
        {
            var all = WindCases.Build(Quimby());
            Assert.AreEqual(83.34, One(all, $"Case 1, from {from}, roof Case A, +GCpi").UpliftKips, 0.01 * 83.34, "Case I");
            Assert.AreEqual(44.43, One(all, $"Case 1, from {from}, roof Case B, +GCpi").UpliftKips, 0.01 * 44.43, "Case II");
            Assert.AreEqual(35.31, One(all, $"Case 1, from {from}, roof Case A, −GCpi").UpliftKips, 0.01 * 35.31, "Case III");
            Assert.AreEqual(-3.60, One(all, $"Case 1, from {from}, roof Case B, −GCpi").UpliftKips, 0.4, "Case IV");
            var c = One(all, $"Case 1, from {from}, roof Case A, +GCpi");
            Assert.AreEqual(from == "W" ? 34.01 : -34.01, c.Vx, 0.34, "horizontal, both roof values alike");
            Assert.AreEqual(0, c.Vy, 1e-9);
        }

        /// <summary>
        /// N/S wind, normal to the ridge: Quimby's Cases II and IV (roof Case B, -0.18). Case I/III use the windward
        /// roof's -0.74 at 14.04° (Quimby took -0.70): 92.06 + 0.04 × 25.2 psf × 2263 ft² of plan = 94.37 k.
        /// </summary>
        [DataTestMethod]
        [DataRow("S")]
        [DataRow("N")]
        public void Case1_AcrossTheRidge_UpliftMatchesQuimby(string from)
        {
            var all = WindCases.Build(Quimby());
            Assert.AreEqual(62.58, One(all, $"Case 1, from {from}, roof Case B, +GCpi").UpliftKips, 0.01 * 62.58, "Case II");
            Assert.AreEqual(14.54, One(all, $"Case 1, from {from}, roof Case B, −GCpi").UpliftKips, 0.01 * 14.54 + 0.2, "Case IV");
            Assert.AreEqual(94.37, One(all, $"Case 1, from {from}, roof Case A, +GCpi").UpliftKips, 0.01 * 94.37, "Case I at -0.74");
            double s = from == "S" ? 1 : -1;
            // Quimby Cases I/III sum the walls and a roof force against the wind: 58.29 k. Fig. 27.3-1 note 7 (7-16; 7-22 alike)
            // keeps the shear at no less than the walls alone, so the roof is set aside (the zones stay, for uplift)
            var a1 = One(all, $"Case 1, from {from}, roof Case A, +GCpi");
            Assert.IsTrue(a1.RoofNeglected, "roof force against the wind");
            Assert.AreEqual(s * 58.29, a1.Vy + RoofFy(a1), 0.01 * 58.29, "Quimby Cases I/III: walls + roof");
            Assert.IsTrue(s * RoofFy(a1) < 0 && Math.Abs(a1.Vy) > 58.29, "the walls alone are more");
            Assert.AreEqual(s * 65.66, One(all, $"Case 1, from {from}, roof Case B, +GCpi").Vy, 0.01 * 65.66, "Quimby Cases II/IV");
        }

        /// <summary>Zone by zone, Case 1 from the west, roof Case A, +GCpi: Quimby's net pressures (psf).</summary>
        [TestMethod]
        public void Case1_ZonePressures_MatchQuimby()
        {
            var c = One(WindCases.Build(Quimby()), "Case 1, from W, roof Case A, +GCpi");
            Assert.IsTrue(c.Zones.All(z => Math.Abs(z.InternalPsf - 5.34) < 0.02), "qh GCpi = +5.34 on every zone");
            double Net(string label, BuildingFace? face = null) =>
                c.Zones.First(z => z.Label.StartsWith(label) && (face == null || z.Face == face)).NetPsf;
            Assert.AreEqual(-28.0, Net("Zone4 (W)"), 0.1, "P11, 0 to h/2: -22.7 - 5.34");
            Assert.AreEqual(-17.9, Net("Zone2 (W)"), 0.1, "P12, h to 2h");
            Assert.AreEqual(-12.9, Net("Zone1 (W)"), 0.1, "P13, beyond 2h");
            Assert.AreEqual(-23.0, Net("ZoneSW (W)", BuildingFace.SouthWall), 0.1, "P15, side walls");
            Assert.AreEqual(-13.9, Net("ZoneLW (W)", BuildingFace.EastWall), 0.1, "P14, leeward wall");
            Assert.AreEqual(12.8, Net("ZoneWW (W), 0-15", BuildingFace.WestWall), 0.1, "P8: windward wall up to 15 ft (Kz at 15 ft)");
        }

        /// <summary>
        /// A quartering wind resolved area by area. Case 3 from the SW, roof Case A for both winds, +GCpi: the roof near the
        /// west eave on the south (windward) slope is in the west wind's Zone4 (Cp -0.9) and the south wind's windward roof
        /// (Cp -0.740): 0.75 × 25.25 × (-0.9) + 0.75 × 25.25 × (-0.740) - 5.34 = -36.4 psf.
        /// </summary>
        [TestMethod]
        public void Case3_Quartering_ResolvedAreaByArea()
        {
            var all = WindCases.Build(Quimby());
            var c = One(all, "Case 3, from SW, roof W Case A / S Case A, +GCpi");
            var z = c.Zones.First(q => q.Label == "Zone4 (W) + ZoneWWR (S)");
            Assert.AreEqual(-36.4, z.NetPsf, 0.2);
            Assert.AreEqual(BuildingFace.RoofSouth, z.Face);

            // the pieces cover the roof once: the same roof area as a single wind
            double Roof(WindCase k) => k.Zones.Where(q => q.Kind == SurfaceKind.Roof).Sum(q => q.Area);
            Assert.AreEqual(Roof(One(all, "Case 1, from W, roof Case A, +GCpi")), Roof(c), 1e-6);
            Assert.AreEqual(4500 / Math.Cos(Math.Atan(0.25)), Roof(c), 0.5, "90 × 50 plan on a 3:12 slope");

            // uplift: 0.75 × each wind's external uplift + the internal 5.34 psf × 4500 ft² (24.0 k) once:
            // 0.75 × (83.34 - 24.03) + 0.75 × (94.37 - 24.03) + 24.03 = 121.3 k
            Assert.AreEqual(121.3, c.UpliftKips, 1.2);
            Assert.AreEqual(0.75 * 34.01, c.Vx, 0.3);
            Assert.AreEqual(0.75 * One(all, "Case 1, from S, roof Case A, +GCpi").Vy, c.Vy, 1e-9, "75% of the S wind, its roof set aside (Fig. 27.3-1 note 7)");
            Assert.AreEqual(0, c.Mt, 1e-9);
        }

        /// <summary>
        /// Nothing counted twice, nothing left out: in every one of the 208 cases each wall and roof surface is covered by its
        /// pieces exactly once (the pieces' areas add up to the surface's own area), and the internal pressure is on each
        /// piece once with the case's single sign.
        /// </summary>
        [TestMethod]
        public void EveryCase_CoversEachSurfaceOnce()
        {
            var a = Quimby();
            var g = new WindGeometry(a.MwfrsAlongLength.buildingData);
            double qi = 5.34;
            foreach (var c in WindCases.Build(a).Where(c => !c.IsMinimumCase))   // the minimum covers the windward wall only
            {
                foreach (var s in g.Surfaces)
                {
                    double covered = c.Zones.Where(z => z.Face == s.Face).Sum(z => z.Area);
                    Assert.AreEqual(s.Area, covered, 1e-6 * Math.Max(1, s.Area), $"{c.Name}: {s.Name} covered {covered:0.###} of {s.Area:0.###} ft²");
                }
                Assert.IsTrue(c.Zones.All(z => Math.Abs(z.InternalPsf - c.InternalSign * qi) < 0.02), c.Name + ": one internal pressure, the case's sign");
            }
        }

        /// <summary>The mirrored winds land on the right side: from the N the windward roof is the north slope, from the E the
        /// 0 to h/2 band is at the east end, and each windward wall is the wall the wind comes from.</summary>
        [TestMethod]
        public void MirroredWinds_OnTheRightFaces()
        {
            var all = WindCases.Build(Quimby());
            var n = One(all, "Case 1, from N, roof Case A, +GCpi");
            Assert.IsTrue(n.Zones.Where(z => z.Label.StartsWith("ZoneWWR")).All(z => z.Face == BuildingFace.RoofNorth));
            Assert.IsTrue(n.Zones.Where(z => z.Label.StartsWith("ZoneLWR")).All(z => z.Face == BuildingFace.RoofSouth));
            Assert.IsTrue(n.Zones.Where(z => z.Label.StartsWith("ZoneWW (")).All(z => z.Face == BuildingFace.NorthWall));
            var e = One(all, "Case 1, from E, roof Case A, +GCpi");
            Assert.IsTrue(e.Zones.Where(z => z.Label.StartsWith("Zone4")).All(z => z.Centroid.X > 90 - 25.125 / 2 - 1e-6));
            Assert.IsTrue(e.Zones.Where(z => z.Label.StartsWith("ZoneWW (")).All(z => z.Face == BuildingFace.EastWall));
            // windward wall pushes toward -x (inward on the east wall): its force points west
            Assert.IsTrue(e.Zones.Where(z => z.Label.StartsWith("ZoneWW (")).All(z => z.ForceKips.X < 0));
        }

        /// <summary>Fig. 27.3-8 torsion on the design force of the case's own wind and roof value: e = 0.15 B.</summary>
        [TestMethod]
        public void Cases2And4_Torsion()
        {
            var all = WindCases.Build(Quimby());
            var c1 = One(all, "Case 1, from S, roof Case B, +GCpi");
            var c2 = One(all, "Case 2, from S, roof Case B, +GCpi, torsion +");
            Assert.AreEqual(0.75 * c1.Vy, c2.Vy, 1e-9);
            Assert.AreEqual(0.75 * c1.Vy * 0.15 * 90, c2.Mt, 1e-9, "wind along y: e = 0.15 × 90 ft");
            Assert.AreEqual(-c2.Mt, One(all, "Case 2, from S, roof Case B, +GCpi, torsion −").Mt, 1e-9);
            Assert.IsTrue(c2.Zones.Zip(c1.Zones, (a, b) => Math.Abs(a.ExternalPsf - 0.75 * b.ExternalPsf) < 1e-9).All(x => x), "every zone at 75%");

            double vx = One(all, "Case 1, from W, roof Case B, +GCpi").Vx, vy = c1.Vy;
            var c4 = One(all, "Case 4, from SW, roof W Case B / S Case B, +GCpi, torsion +−");
            Assert.AreEqual(0.563 * (vx * 0.15 * 50 - vy * 0.15 * 90), c4.Mt, 1e-9);
        }

        /// <summary>
        /// 27.1.5 minimum, a separate case per wind (C27.1.5): 16 psf on the windward wall's whole elevation, 8 psf on the roof
        /// projected normal to the wind, no internal pressure, no torsion. Quimby: from W (along the ridge) the gable end
        /// 50 × 22 + ½ × 50 × 6.25 = 1256.25 sq ft, no roof seen: 20.10 k. From S: wall 90 × 22 = 1980, roof 90 × 6.25 = 562.5:
        /// 31.68 + 4.50 = 36.18 k.
        /// </summary>
        [TestMethod]
        public void MinimumCase_PerWind_Separate()
        {
            var all = WindCases.Build(Quimby());
            var min = all.Where(c => c.IsMinimumCase).ToList();
            Assert.AreEqual(4, min.Count);
            var w = min.Single(c => c.From == "W");
            Assert.AreEqual(16 * 1256.25 / 1000, w.Vx, 1e-6);
            Assert.AreEqual(0, w.Vy, 1e-12);
            Assert.AreEqual(0, w.Mt, 1e-12);
            Assert.AreEqual(0, w.InternalSign);
            var wz = w.Zones.Single();
            Assert.AreEqual(BuildingFace.WestWall, wz.Face);
            Assert.AreEqual(16.0, wz.ExternalPsf, 1e-12);
            Assert.AreEqual(0.0, wz.InternalPsf, 1e-12);
            Assert.AreEqual(1256.25, wz.Area, 1e-6);
            Assert.AreEqual(w.Vx, wz.ForceKips.X, 1e-6, "16 psf on the wall is the whole force from W (no roof seen)");
            var s = min.Single(c => c.From == "S");
            Assert.AreEqual((16 * 1980 + 8 * 562.5) / 1000, s.Vy, 1e-6);
            Assert.AreEqual(-s.Vy, min.Single(c => c.From == "N").Vy, 1e-12);
            // the other cases are not raised: Case 1 keeps the calculated force
            var my = Quimby().MwfrsAlongWidth;
            var lat = WindLateralLoads.DirectionalForce(my, "along width");
            Assert.AreEqual(lat.WallsKips + lat.ParapetKips + WindLateralLoads.RoofHorizontalKips(my, new WindGeometry(my.buildingData), my.windPressureRoof_Pos_External),
                One(all, "Case 1, from S, roof Case B, +GCpi").Vy, 1e-9);
        }

        private static WindAnalysisResult QuimbyOn(ASCE7_Versions v)
        {
            var b = Building.New();
            b.AddDiaphragm(b.AddLevel("2nd floor", 11), new Rect2(0, 0, 90, 50));
            b.AddDiaphragm(b.AddLevel("Roof", 22), new Rect2(0, 0, 90, 50));
            b.Roof = new Roof { Shape = RoofShape.Gable, Pitch = Math.Atan(0.25) * 180 / Math.PI, Ridge = RidgeAxis.WestEast, Overhang = 0, Parapet = 0 };
            var p = new WindParameters_Base { WindSpeed = 120, ExposureCategory = WindExposureCategories.WIND_EXP_CAT_C, Kzt = 1.0 };
            return WindAnalysis.Run(v, p, WindAdapter.From(b).Building);
        }

        private static bool Inside(System.Collections.Generic.IReadOnlyList<Vec2> poly, Vec2 p)
        {
            bool inside = false;
            for (int i = 0, j = poly.Count - 1; i < poly.Count; j = i++)
                if ((poly[i].Y > p.Y) != (poly[j].Y > p.Y) &&
                    p.X < (poly[j].X - poly[i].X) * (p.Y - poly[i].Y) / (poly[j].Y - poly[i].Y) + poly[i].X)
                    inside = !inside;
            return inside;
        }

        private static Vec2 Mid(System.Collections.Generic.IReadOnlyList<Vec2> p) => new Vec2(p.Average(q => q.X), p.Average(q => q.Y));

        /// <summary>External pressure of the case's zone on the same surface holding the point.</summary>
        private static double ExtAt(WindCase c, WindCaseZone like, Vec2 at) =>
            c.Zones.Single(z => z.Surface == like.Surface && Inside(z.Outline, at)).ExternalPsf;

        private static double RoofFx(WindCase c) =>
            c.Zones.Where(z => z.Kind == SurfaceKind.Roof).Sum(z => -z.ExternalPsf * z.Area * z.Normal.X / 1000);
        private static double RoofFy(WindCase c) =>
            c.Zones.Where(z => z.Kind == SurfaceKind.Roof).Sum(z => -z.ExternalPsf * z.Area * z.Normal.Y / 1000);

        /// <summary>
        /// ASCE 7-22 Fig. 27.3-8 note 2: in Case 3 each roof area takes 100% of the larger of the Case 1 roof pressures of its two
        /// winds, in Case 4 100% of the larger of the Case 2 values (0.75 × Case 1); the walls stay at 75% / 56.3%. The forces are
        /// the walls at that factor plus the roof zones as resolved. ASCE 7-16: every surface at 75% / 56.3% of each wind.
        /// </summary>
        [TestMethod]
        public void Cases3And4_Roof_7_22_TakesTheLarger_7_16_SeventyFivePercentOfEach()
        {
            foreach (var v in new[] { ASCE7_Versions.ASCE_VER_7_22, ASCE7_Versions.ASCE_VER_7_16 })
            {
                bool larger = v == ASCE7_Versions.ASCE_VER_7_22;
                var all = WindCases.Build(QuimbyOn(v));
                var w = One(all, "Case 1, from W, roof Case A, +GCpi");
                var s = One(all, "Case 1, from S, roof Case A, +GCpi");
                var c3 = One(all, "Case 3, from SW, roof W Case A / S Case A, +GCpi");
                var c4 = One(all, "Case 4, from SW, roof W Case A / S Case A, +GCpi, torsion ++");
                foreach (var (c, f, roofF) in new[] { (c3, 0.75, 1.0), (c4, 0.563, 0.75) })
                {
                    foreach (var z in c.Zones)
                    {
                        var at = Mid(z.Outline);
                        double a = ExtAt(w, z, at), b = ExtAt(s, z, at);
                        double expected = z.Kind == SurfaceKind.Roof && larger
                            ? roofF * (Math.Abs(a) >= Math.Abs(b) ? a : b)
                            : f * a + f * b;
                        Assert.AreEqual(expected, z.ExternalPsf, 1e-9, $"{v} {c.Name}: {z.Label}");
                    }
                    if (larger)
                    {
                        // walls and parapets at the factor, roof as resolved; a roof force against its wind left out (Fig. 27.3-1 note 7)
                        double wallsX = w.Vx - (w.RoofNeglected ? 0 : RoofFx(w)), wallsY = s.Vy - (s.RoofNeglected ? 0 : RoofFy(s));
                        Assert.AreEqual(f * wallsX + Math.Max(0, RoofFx(c)), c.Vx, 1e-6, $"{v} {c.Name} Vx");
                        Assert.AreEqual(f * wallsY + Math.Max(0, RoofFy(c)), c.Vy, 1e-6, $"{v} {c.Name} Vy");
                    }
                    else
                    {
                        Assert.AreEqual(f * w.Vx, c.Vx, 1e-9, $"{v} {c.Name} Vx");
                        Assert.AreEqual(f * s.Vy, c.Vy, 1e-9, $"{v} {c.Name} Vy");
                    }
                }
            }
        }
    }
}
