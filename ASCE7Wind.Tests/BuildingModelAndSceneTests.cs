using System;
using System.Collections.Generic;
using System.Linq;
using ASCE7WindLoadCalculator;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ASCE7Wind.Tests
{
    /// <summary>M3: 3D building envelope and placement of wind zones on it.</summary>
    [TestClass]
    public class BuildingModelTests
    {
        private static double Tan(double d) => Math.Tan(d * Math.PI / 180);
        private static double Cos(double d) => Math.Cos(d * Math.PI / 180);

        [TestMethod]
        public void FlatRoof_Box()
        {
            var m = new WindGeometry(Bldg.Make(60, 40, 15));
            Assert.AreEqual(5, m.Surfaces.Count);
            Assert.AreEqual(60 * 40, m.Roofs.Single().Area, 1e-9);
            Assert.AreEqual(2 * (60 + 40) * 15, m.Walls.Sum(w => w.Area), 1e-9);
        }

        [DataTestMethod]
        [DataRow(RidgeDirections.RIDGE_DIR_PARALLEL_TO_BLDGLENGTH)]
        [DataRow(RidgeDirections.RIDGE_DIR_PERP_TO_BLDGLENGTH)]
        public void GableRoof_PlanesAndGableEnds(RidgeDirections ridge)
        {
            double L = 60, B = 40, H = 15, slope = 25;
            var m = new WindGeometry(Bldg.Make(L, B, H, RoofTypes.ROOF_TYPE_GABLE, slope, ridge));
            Assert.AreEqual(2, m.Roofs.Count());
            Assert.AreEqual(L * B / Cos(slope), m.Roofs.Sum(r => r.Area), 1e-6, "sloped area = plan / cos(theta)");

            double span = ridge == RidgeDirections.RIDGE_DIR_PARALLEL_TO_BLDGLENGTH ? B : L;
            double gables = 2 * 0.5 * span * (span / 2 * Tan(slope));
            Assert.AreEqual(2 * (L + B) * H + gables, m.Walls.Sum(w => w.Area), 1e-6);
            Assert.AreEqual(H + Tan(slope) * span / 2, m.Walls.Max(w => w.Outline.Max(p => p.Z)), 1e-9);
        }

        [DataTestMethod]
        [DataRow(60.0, 40.0)]
        [DataRow(40.0, 60.0)]
        [DataRow(40.0, 40.0)]
        public void HipRoof_FourPlanesMeetAtRidge(double L, double B)
        {
            double H = 12, slope = 20;
            var m = new WindGeometry(Bldg.Make(L, B, H, RoofTypes.ROOF_TYPE_HIP, slope));
            Assert.AreEqual(4, m.Roofs.Count());
            Assert.AreEqual(L * B / Cos(slope), m.Roofs.Sum(r => r.Area), 1e-6);
            Assert.AreEqual(2 * (L + B) * H, m.Walls.Sum(w => w.Area), 1e-9, "hip roofs have no gable walls");
            double ridge = H + Tan(slope) * Math.Min(L, B) / 2;
            foreach (var r in m.Roofs)
                Assert.AreEqual(ridge, r.Outline.Max(p => p.Z), 1e-9, r.Name);
        }

        [TestMethod]
        public void RoofPlanes_AgreeWithRoofHeightFunction()
        {
            foreach (var roof in new[] { RoofTypes.ROOF_TYPE_GABLE, RoofTypes.ROOF_TYPE_HIP })
                foreach (var ridge in new[] { RidgeDirections.RIDGE_DIR_PARALLEL_TO_BLDGLENGTH, RidgeDirections.RIDGE_DIR_PERP_TO_BLDGLENGTH })
                {
                    var m = new WindGeometry(Bldg.Make(50, 30, 10, roof, 30, ridge));
                    foreach (var s in m.Roofs)
                    {
                        var c = new Vec2(s.PlanRegion.Average(p => p.X), s.PlanRegion.Average(p => p.Y));
                        Assert.AreEqual(m.RoofHeightAt(c.X, c.Y), s.LiftToRoof(c).Z, 1e-9, $"{roof} {ridge} {s.Name}");
                    }
                }
        }

        [TestMethod]
        public void Normals_PointOutward()
        {
            var m = new WindGeometry(Bldg.Make(60, 40, 15, RoofTypes.ROOF_TYPE_HIP, 20));
            var center = new Vec3(30, 20, 10);
            foreach (var s in m.Surfaces)
            {
                var p = new Vec3(s.Outline.Average(q => q.X), s.Outline.Average(q => q.Y), s.Outline.Average(q => q.Z));
                Assert.IsTrue((p - center).Dot(s.Normal) > 0, s.Name);
                // outline order agrees with the normal
                var n = (s.Outline[1] - s.Outline[0]).Cross(s.Outline[2] - s.Outline[0]);
                Assert.IsTrue(n.Dot(s.Normal) > 0, s.Name + " outline winding");
            }
        }
    }

    [TestClass]
    public class PolygonOpsTests
    {
        private static double Area(List<Vec2> tris)
        {
            double a = 0;
            for (int i = 0; i < tris.Count; i += 3)
                a += PolygonOps.SignedArea(new[] { tris[i], tris[i + 1], tris[i + 2] });
            return a;
        }

        private static List<Vec2> R(double x0, double y0, double x1, double y1) =>
            new List<Vec2> { new Vec2(x0, y0), new Vec2(x1, y0), new Vec2(x1, y1), new Vec2(x0, y1) };

        [TestMethod]
        public void Triangulate_ConcavePolygon()
        {
            var L = new List<Vec2> { new Vec2(0, 0), new Vec2(10, 0), new Vec2(10, 3), new Vec2(3, 3), new Vec2(3, 10), new Vec2(0, 10) };
            var t = PolygonOps.Triangulate(L);
            Assert.AreEqual(51, Area(t), 1e-9);
            Assert.IsTrue(Enumerable.Range(0, t.Count / 3).All(i => PolygonOps.SignedArea(new[] { t[3 * i], t[3 * i + 1], t[3 * i + 2] }) > 0), "all CCW");
        }

        /// <summary>Holes touching the outer boundary and each other, like the flat-roof edge zone.</summary>
        [TestMethod]
        public void Triangulate_HolesTouchingOuterBoundary()
        {
            var outer = R(0, 0, 60, 40);
            var holes = new List<IReadOnlyList<Vec2>>
            {
                R(0, 0, 6, 6), R(54, 0, 60, 6), R(54, 34, 60, 40), R(0, 34, 6, 40), // corner squares on the outer edge
                R(6, 6, 54, 34),                                                    // interior
            };
            double expected = 60 * 40 - 4 * 36 - 48 * 28;
            Assert.AreEqual(expected, Area(PolygonOps.Triangulate(outer, holes)), 1e-9);
        }

        [TestMethod]
        public void Triangulate_SlantedEdges()
        {
            var tri = new List<Vec2> { new Vec2(0, 0), new Vec2(8, 1), new Vec2(3, 7) };
            Assert.AreEqual(Math.Abs(PolygonOps.SignedArea(tri)), Area(PolygonOps.Triangulate(tri)), 1e-9);
        }

        [TestMethod]
        public void ClipToConvex_Square()
        {
            var clipped = PolygonOps.ClipToConvex(R(0, 0, 10, 10), R(5, -5, 20, 5));
            Assert.AreEqual(25, Math.Abs(PolygonOps.SignedArea(clipped)), 1e-9);
            Assert.AreEqual(0, PolygonOps.ClipToConvex(R(0, 0, 1, 1), R(5, 5, 6, 6)).Count);
        }

        [TestMethod]
        public void ClipSegment()
        {
            var s = PolygonOps.ClipSegmentToConvex(new Vec2(-5, 2), new Vec2(15, 2), R(0, 0, 10, 10));
            Assert.IsTrue(s.HasValue);
            Assert.AreEqual(0, s.Value.a.X, 1e-12);
            Assert.AreEqual(10, s.Value.b.X, 1e-12);
            Assert.IsNull(PolygonOps.ClipSegmentToConvex(new Vec2(-5, 20), new Vec2(15, 20), R(0, 0, 10, 10)));
        }
    }

    /// <summary>M3/M4: zones placed on the 3D envelope must cover it exactly and lie on its surfaces.</summary>
    [TestClass]
    public class WindSceneTests
    {
        private static WindScene Scene(ASCE7_Versions v, BuildingData b, WindViewMode mode, MwfrsRoofCase rc = MwfrsRoofCase.MaxSuction)
        {
            var cc = Bldg.Run(v, WindLoadCalculationTypes.COMPONENT_AND_CLADDING, b);
            var mL = Bldg.Run(v, WindLoadCalculationTypes.MWFRS, b);
            var rotated = b.Clone(); rotated.RotateBuilding();
            var mW = Bldg.Run(v, WindLoadCalculationTypes.MWFRS, rotated);
            return WindSceneBuilder.Build(b, cc, mL, mW, mode, rc);
        }

        private static IEnumerable<BuildingData> Buildings()
        {
            yield return Bldg.Make(60, 40, 15);
            yield return Bldg.Make(60, 40, 15, RoofTypes.ROOF_TYPE_GABLE, 22, RidgeDirections.RIDGE_DIR_PARALLEL_TO_BLDGLENGTH);
            yield return Bldg.Make(60, 40, 15, RoofTypes.ROOF_TYPE_GABLE, 30, RidgeDirections.RIDGE_DIR_PERP_TO_BLDGLENGTH);
            yield return Bldg.Make(60, 40, 15, RoofTypes.ROOF_TYPE_HIP, 18);
            yield return Bldg.Make(40, 70, 12, RoofTypes.ROOF_TYPE_HIP, 30);
        }

        /// <summary>Roof zone pieces lie on the roof and add up to the true sloped roof area.</summary>
        [TestMethod]
        public void RoofZones_CoverSlopedRoofExactly_AndLieOnIt()
        {
            foreach (var v in new[] { ASCE7_Versions.ASCE_VER_7_16, ASCE7_Versions.ASCE_VER_7_22 })
                foreach (var b in Buildings())
                    foreach (var mode in (WindViewMode[])Enum.GetValues(typeof(WindViewMode)))
                    {
                        var s = Scene(v, b, mode);
                        string tag = $"{v} {b.RoofType} {b.RidgeDirection} {mode}";
                        var roof = s.Zones.Where(z => z.Kind == SurfaceKind.Roof).ToList();
                        Assert.AreEqual(s.Model.Roofs.Sum(r => r.Area), roof.Sum(z => z.SurfaceArea), 1e-6, tag);
                        foreach (var p in roof.SelectMany(z => z.Triangles))
                            Assert.AreEqual(s.Model.RoofHeightAt(p.X, p.Y), p.Z, 1e-9, tag);
                    }
        }

        [TestMethod]
        public void CladdingWallZones_CoverWallsBelowEave()
        {
            var b = Bldg.Make(60, 40, 15);
            var s = Scene(ASCE7_Versions.ASCE_VER_7_16, b, WindViewMode.CladdingSuction);
            var walls = s.Zones.Where(z => z.Kind == SurfaceKind.Wall).ToList();
            Assert.AreEqual(2 * (60 + 40) * 15, walls.Sum(z => z.SurfaceArea), 1e-6);
            foreach (var face in new[] { BuildingFace.SouthWall, BuildingFace.NorthWall, BuildingFace.WestWall, BuildingFace.EastWall })
                Assert.IsTrue(walls.Any(z => z.Face == face), face.ToString());
            foreach (var p in walls.SelectMany(z => z.Triangles))
                Assert.IsTrue(p.Z >= -1e-9 && p.Z <= 15 + 1e-9 && p.X >= -1e-9 && p.X <= 60 + 1e-9 && p.Y >= -1e-9 && p.Y <= 40 + 1e-9);
        }

        [DataTestMethod]
        [DataRow(WindViewMode.MwfrsAlongLength, BuildingFace.WestWall, BuildingFace.EastWall)]
        [DataRow(WindViewMode.MwfrsAlongWidth, BuildingFace.SouthWall, BuildingFace.NorthWall)]
        public void MwfrsWalls_WindwardIsUpwind(WindViewMode mode, BuildingFace windward, BuildingFace leeward)
        {
            var s = Scene(ASCE7_Versions.ASCE_VER_7_22, Bldg.Make(60, 40, 15, RoofTypes.ROOF_TYPE_GABLE, 20, RidgeDirections.RIDGE_DIR_PARALLEL_TO_BLDGLENGTH), mode);
            Assert.IsTrue(s.Zones.Where(z => z.Face == windward).All(z => z.PressurePsf > 0), "windward wall is pushed");
            Assert.IsTrue(s.Zones.Single(z => z.Face == leeward).PressurePsf < 0, "leeward wall is pulled");
            Assert.IsNotNull(s.WindDirection);
            Assert.IsTrue(s.BaseShearKips > 0);
            // all wall area accounted for, gable triangles included
            Assert.AreEqual(s.Model.Walls.Sum(w => w.Area), s.Zones.Where(z => z.Kind == SurfaceKind.Wall).Sum(z => z.SurfaceArea), 1e-6);
        }

        [TestMethod]
        public void Cladding_SuctionAndPressureSigns()
        {
            var b = Bldg.Make(60, 40, 15, RoofTypes.ROOF_TYPE_GABLE, 22, RidgeDirections.RIDGE_DIR_PARALLEL_TO_BLDGLENGTH);
            var suction = Scene(ASCE7_Versions.ASCE_VER_7_22, b, WindViewMode.CladdingSuction);
            var pressure = Scene(ASCE7_Versions.ASCE_VER_7_22, b, WindViewMode.CladdingPressure);
            Assert.IsTrue(suction.Zones.All(z => z.HasPressure && z.PressurePsf < 0));
            Assert.IsTrue(pressure.Zones.All(z => z.HasPressure && z.PressurePsf > 0));
            Assert.IsTrue(suction.StrongestSuction.Kind == SurfaceKind.Roof, "roof corners govern uplift");
        }

        [TestMethod]
        public void OutOfLimitsBuilding_CarriesMessages()
        {
            var s = Scene(ASCE7_Versions.ASCE_VER_7_16, Bldg.Make(100, 50, 70), WindViewMode.CladdingSuction);
            Assert.IsTrue(s.Messages.Count > 0);
            Assert.IsTrue(s.Zones.All(z => !z.HasPressure));
        }

        [TestMethod]
        public void Zones_HavePlainLanguageDescriptions()
        {
            var s = Scene(ASCE7_Versions.ASCE_VER_7_16, Bldg.Make(60, 40, 15), WindViewMode.CladdingSuction);
            Assert.IsTrue(s.Zones.All(z => !string.IsNullOrWhiteSpace(z.Description) && !z.Description.StartsWith("Zone")));
        }
    }

    [TestClass]
    public class ColorScaleAndCameraTests
    {
        [TestMethod]
        public void ColorScale_NeutralAtZero_ArmsAtExtremes()
        {
            var s = new PressureColorScale(40);
            Assert.AreEqual(PressureColorScale.Neutral.ToString(), s.ColorFor(0).ToString());
            Assert.AreEqual("#9b1c1c", s.ColorFor(-40).ToString());
            Assert.AreEqual("#104281", s.ColorFor(40).ToString());
            Assert.AreEqual("#9b1c1c", s.ColorFor(-400).ToString(), "clamped");
            Assert.AreEqual(PressureColorScale.NoData.ToString(), s.ColorFor(double.NaN).ToString());
        }

        [TestMethod]
        public void ColorScale_DarkensMonotonicallyWithMagnitude()
        {
            var s = new PressureColorScale(50);
            double Luma(PressureColorScale.Rgb c) => 0.2126 * c.R + 0.7152 * c.G + 0.0722 * c.B;
            for (int sign = -1; sign <= 1; sign += 2)
            {
                double prev = double.MaxValue;
                for (double v = 0; v <= 50; v += 2.5)
                {
                    double l = Luma(s.ColorFor(sign * v));
                    Assert.IsTrue(l <= prev + 1e-9, $"lightness rises at {sign * v}");
                    prev = l;
                }
            }
        }

        [DataTestMethod]
        [DataRow(37.0, new[] { -30.0, -20.0, -10.0, 0.0, 10.0, 20.0, 30.0 })]
        [DataRow(23.0, new[] { -20.0, -10.0, 0.0, 10.0, 20.0 })]
        [DataRow(4.0, new[] { -4.0, -3.0, -2.0, -1.0, 0.0, 1.0, 2.0, 3.0, 4.0 })]
        public void ColorScale_NiceTicks(double maxAbs, double[] expected)
        {
            var ticks = new PressureColorScale(maxAbs).Ticks();
            // ticks never exceed the range and include zero
            CollectionAssert.Contains(ticks.ToList(), 0.0);
            Assert.IsTrue(ticks.All(t => Math.Abs(t) <= maxAbs + 1e-9));
            CollectionAssert.AreEqual(expected.Where(t => Math.Abs(t) <= maxAbs + 1e-9).ToList(), ticks.ToList());
        }

        [TestMethod]
        public void Camera_OrbitKeepsDistance_AndClampsPitch()
        {
            var cam = new OrbitCamera(new Vec3(30, 20, 8), -125, 28, 100);
            cam.Orbit(250, 0);
            Assert.AreEqual(100, (cam.Position - cam.Target).Length, 1e-9);
            cam.Orbit(0, 100000);
            Assert.IsTrue(cam.Pitch < Math.PI / 2, "never flips over the top");
            cam.Orbit(0, -100000);
            Assert.IsTrue(cam.Pitch > -0.1, "never far below grade");
        }

        [TestMethod]
        public void Camera_FrameFitsSphere_AndZoomClamps()
        {
            var cam = new OrbitCamera(new Vec3(0, 0, 0), -125, 28, 10);
            cam.Frame(new Vec3(30, 20, 10), 40);
            double half = OrbitCamera.FieldOfViewDeg * Math.PI / 360;
            Assert.IsTrue(Math.Sin(half) * cam.Distance >= 40, "sphere fits in the view");
            cam.Zoom(1e-6);
            Assert.AreEqual(cam.MinDistance, cam.Distance, 1e-9);
        }

        [TestMethod]
        public void Camera_PanMovesTargetInScreenPlane()
        {
            var cam = new OrbitCamera(new Vec3(0, 0, 0), -90, 0, 50);
            cam.Pan(100, 0, 500);
            Assert.AreEqual(0, cam.Target.Dot(cam.LookDirection.Normalized()), 1e-9, "no motion along the view axis");
            Assert.IsTrue(cam.Target.X < 0, "dragging right moves the scene right (target left)");
        }

        [TestMethod]
        public void Camera_TopPreset_LooksDown()
        {
            var cam = new OrbitCamera(new Vec3(0, 0, 0), 0, 0, 50);
            cam.Apply(OrbitCamera.Preset.Top);
            Assert.IsTrue(cam.LookDirection.Normalized().Z < -0.99);
        }
    }
}
