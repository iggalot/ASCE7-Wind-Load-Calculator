using System;
using System.Collections.Generic;
using System.Linq;

namespace ASCE7WindLoadCalculator
{
    /// <summary>What the 3D view shows.</summary>
    public enum WindViewMode
    {
        /// <summary>Components &amp; cladding, outward (suction) pressures - governs uplift and wall pull-off.</summary>
        CladdingSuction,
        /// <summary>Components &amp; cladding, inward pressures.</summary>
        CladdingPressure,
        /// <summary>Whole-building (MWFRS) pressures, wind blowing along the BuildingLength (from the west).</summary>
        MwfrsAlongLength,
        /// <summary>Whole-building (MWFRS) pressures, wind blowing along the BuildingWidth (from the south).</summary>
        MwfrsAlongWidth,
    }

    /// <summary>The two MWFRS roof coefficient cases of Fig. 27.3-1.</summary>
    public enum MwfrsRoofCase
    {
        /// <summary>Larger roof suction (the negative Cp values).</summary>
        MaxSuction,
        /// <summary>Alternate case (the positive / less negative Cp values).</summary>
        Alternate,
    }

    /// <summary>One wind zone placed on the 3D building.</summary>
    public sealed class SceneZone
    {
        public int ZoneId { get; internal set; }
        public string Label { get; internal set; }
        public string Description { get; internal set; }
        public BuildingFace Face { get; internal set; }
        public SurfaceKind Kind { get; internal set; }
        public string SurfaceName { get; internal set; }

        /// <summary>Net design pressure (psf); negative = suction away from the surface. NaN when no coefficient applies.</summary>
        public double PressurePsf { get; internal set; }

        /// <summary>Effective wind area used for the coefficient (ft²; roofs in plan).</summary>
        public double AreaFt2 { get; internal set; }

        /// <summary>Triangles in model coordinates, three vertices each.</summary>
        public List<Vec3> Triangles { get; } = new List<Vec3>();

        /// <summary>Zone outline segments in model coordinates.</summary>
        public List<(Vec3 A, Vec3 B)> Edges { get; } = new List<(Vec3, Vec3)>();

        /// <summary>Outward normal of the surface the zone lies on.</summary>
        public Vec3 Normal { get; internal set; }

        /// <summary>Area-weighted center of the zone's 3D triangles.</summary>
        public Vec3 Center
        {
            get
            {
                double a = 0; var c = new Vec3(0, 0, 0);
                for (int i = 0; i + 2 < Triangles.Count; i += 3)
                {
                    double ta = Vec3.TriangleArea(Triangles[i], Triangles[i + 1], Triangles[i + 2]);
                    c = c + (Triangles[i] + Triangles[i + 1] + Triangles[i + 2]) * (ta / 3.0);
                    a += ta;
                }
                return a > 0 ? c * (1.0 / a) : c;
            }
        }

        /// <summary>True (sloped) area of the zone as drawn (ft²).</summary>
        public double SurfaceArea
        {
            get
            {
                double a = 0;
                for (int i = 0; i + 2 < Triangles.Count; i += 3)
                    a += Vec3.TriangleArea(Triangles[i], Triangles[i + 1], Triangles[i + 2]);
                return a;
            }
        }

        public bool HasPressure => !double.IsNaN(PressurePsf);

        /// <summary>
        /// Pressure that varies over the zone (the windward wall's qz-based taper), or null when the zone has
        /// one value. Used to draw tapered pressure blocks.
        /// </summary>
        public Func<Vec3, double> PressureAt { get; internal set; }

        /// <summary>Net pressure (psf) at a point of the zone.</summary>
        public double PressureAtPoint(Vec3 p) => PressureAt != null ? PressureAt(p) : PressurePsf;
    }

    /// <summary>Everything the 3D view needs, computed without any UI types.</summary>
    public sealed class WindScene
    {
        public WindGeometry Model { get; internal set; }
        public WindViewMode Mode { get; internal set; }
        public MwfrsRoofCase RoofCase { get; internal set; }
        public string Title { get; internal set; }
        public string Subtitle { get; internal set; }
        public List<SceneZone> Zones { get; } = new List<SceneZone>();
        public List<string> Messages { get; } = new List<string>();

        /// <summary>Unit vector the wind blows toward (MWFRS views); null for cladding (any direction).</summary>
        public Vec3? WindDirection { get; internal set; }

        /// <summary>Design lateral force in the wind direction (kips), MWFRS views only.</summary>
        public double? BaseShearKips => LateralForce?.DesignKips;

        /// <summary>Lateral force in the wind direction (walls + roof, 27.1.5 minimum), MWFRS views only.</summary>
        public DirectionalWindForce LateralForce { get; internal set; }

        public double MinPsf => Zones.Where(z => z.HasPressure).Select(z => z.PressurePsf).DefaultIfEmpty(0).Min();
        public double MaxPsf => Zones.Where(z => z.HasPressure).Select(z => z.PressurePsf).DefaultIfEmpty(0).Max();

        /// <summary>Largest magnitude, used to scale colors symmetrically about zero.</summary>
        public double MaxAbsPsf => Math.Max(Math.Abs(MinPsf), Math.Abs(MaxPsf));

        public SceneZone StrongestSuction => Zones.Where(z => z.HasPressure && z.PressurePsf < 0).OrderBy(z => z.PressurePsf).FirstOrDefault();
        public SceneZone StrongestPressure => Zones.Where(z => z.HasPressure && z.PressurePsf > 0).OrderByDescending(z => z.PressurePsf).FirstOrDefault();
    }

    /// <summary>
    /// Places calculated wind zones onto the 3D building. Roof zones (laid out in plan) are triangulated,
    /// clipped to each roof plane and lifted onto it, so zones that cross a ridge or hip fold correctly.
    /// Wall zones (laid out in each wall's local frame) are mapped onto the wall faces.
    /// </summary>
    public static class WindSceneBuilder
    {
        public static WindScene Build(
            BuildingData building,
            WindLoadCalculator_Base cladding,
            WindLoadCalculator_Base mwfrsAlongLength,
            WindLoadCalculator_Base mwfrsAlongWidth,
            WindViewMode mode,
            MwfrsRoofCase roofCase = MwfrsRoofCase.MaxSuction)
        {
            if (building == null) throw new ArgumentNullException(nameof(building));
            var model = new WindGeometry(building);
            var scene = new WindScene { Model = model, Mode = mode, RoofCase = roofCase };

            switch (mode)
            {
                case WindViewMode.CladdingSuction:
                case WindViewMode.CladdingPressure:
                    BuildCladding(scene, cladding, suction: mode == WindViewMode.CladdingSuction);
                    break;
                case WindViewMode.MwfrsAlongLength:
                    BuildMwfrs(scene, mwfrsAlongLength, alongWidth: false, roofCase);
                    break;
                case WindViewMode.MwfrsAlongWidth:
                    BuildMwfrs(scene, mwfrsAlongWidth, alongWidth: true, roofCase);
                    break;
            }
            return scene;
        }

        // ---- Components & cladding --------------------------------------------------------------

        private static void BuildCladding(WindScene scene, WindLoadCalculator_Base calc, bool suction)
        {
            var edition = calc?.Edition?.Name ?? "ASCE 7";
            scene.Title = suction ? "Cladding suction (pull-off)" : "Cladding pressure (push-in)";
            scene.Subtitle = $"{edition} components & cladding (Chapter 30) - worst case for any wind direction";
            if (calc == null) { scene.Messages.Add("No cladding results yet - enter the wind inputs."); return; }
            scene.Messages.AddRange(calc.ValidationMessages);

            var roofP = suction ? calc.windPressureRoof_Neg_External : calc.windPressureRoof_Pos_External;
            var lenP = suction ? calc.windPressureBuildingLengthWall_Neg_External : calc.windPressureBuildingLengthWall_Pos_External;
            var widP = suction ? calc.windPressureBuildingWidthWall_Neg_External : calc.windPressureBuildingWidthWall_Pos_External;

            AddRoofZones(scene, calc.RoofAreaCalculator, roofP, swapXY: false, WindLoadCalculationTypes.COMPONENT_AND_CLADDING);
            AddWallZones(scene, calc.WallAreaCalculator_BldgLength, lenP, new[] { BuildingFace.SouthWall, BuildingFace.NorthWall }, null, WindLoadCalculationTypes.COMPONENT_AND_CLADDING);
            AddWallZones(scene, calc.WallAreaCalculator_BldgWidth, widP, new[] { BuildingFace.WestWall, BuildingFace.EastWall }, null, WindLoadCalculationTypes.COMPONENT_AND_CLADDING);
        }

        // ---- MWFRS --------------------------------------------------------------------------------

        private static void BuildMwfrs(WindScene scene, WindLoadCalculator_Base calc, bool alongWidth, MwfrsRoofCase roofCase)
        {
            var edition = calc?.Edition?.Name ?? "ASCE 7";
            scene.Title = alongWidth ? "Whole-building wind, from the south" : "Whole-building wind, from the west";
            scene.Subtitle = $"{edition} main wind force resisting system (Chapter 27) - " +
                             (roofCase == MwfrsRoofCase.MaxSuction ? "roof Cp giving larger uplift" : "other required roof Cp");
            scene.WindDirection = alongWidth ? Vec3.UnitY : Vec3.UnitX;
            if (calc == null) { scene.Messages.Add("No whole-building results yet - enter the wind inputs."); return; }
            scene.Messages.AddRange(calc.ValidationMessages);

            var roofP = roofCase == MwfrsRoofCase.MaxSuction ? calc.windPressureRoof_Neg_External : calc.windPressureRoof_Pos_External;
            AddRoofZones(scene, calc.RoofAreaCalculator, roofP, swapXY: alongWidth, WindLoadCalculationTypes.MWFRS);

            // The width-direction calculator analyses the building turned 90 degrees, so its windward wall
            // is the south wall and its side walls are the west/east walls.
            var walls = calc.WallAreaCalculator_BldgLength;
            var faces = alongWidth
                ? new Dictionary<string, BuildingFace[]>
                {
                    ["ZoneWW"] = new[] { BuildingFace.SouthWall }, ["ZoneLW"] = new[] { BuildingFace.NorthWall },
                    ["ZoneSW"] = new[] { BuildingFace.WestWall, BuildingFace.EastWall },
                }
                : new Dictionary<string, BuildingFace[]>
                {
                    ["ZoneWW"] = new[] { BuildingFace.WestWall }, ["ZoneLW"] = new[] { BuildingFace.EastWall },
                    ["ZoneSW"] = new[] { BuildingFace.SouthWall, BuildingFace.NorthWall },
                };
            AddWallZones(scene, walls, calc.windPressureWall_Pos_External_MWFRS, null, faces, WindLoadCalculationTypes.MWFRS,
                calc as WindLoadCalculator_MWFRS);

            if (calc is WindLoadCalculator_MWFRS)
                scene.LateralForce = WindLateralLoads.DirectionalForce(calc, alongWidth ? "along width" : "along length");
        }

        // ---- Mapping ------------------------------------------------------------------------------

        private static void AddRoofZones(WindScene scene, AreaCalculator_Base zones, Dictionary<int, PressureData> pressures,
            bool swapXY, WindLoadCalculationTypes analysis)
        {
            if (zones == null) return;
            var roofs = scene.Model.Roofs.ToList();
            Vec2 Map(Vec2 p) => swapXY ? new Vec2(p.Y, p.X) : p;

            foreach (var kv in zones.effWindAreas)
            {
                var area = kv.Value;
                var outer = area.OuterBoundary.Select(Map).ToList();
                var holes = area.Holes.Select(h => (IReadOnlyList<Vec2>)h.Select(Map).ToList()).ToList();
                var tris = PolygonOps.Triangulate(outer, holes);

                // A zone may span several roof planes; emit one SceneZone per plane it touches so each
                // piece knows its own surface (for hover text and lighting).
                foreach (var roof in roofs)
                {
                    var sz = NewZone(kv.Key, area, pressures, roof, analysis);
                    for (int i = 0; i + 2 < tris.Count; i += 3)
                    {
                        var piece = PolygonOps.ClipToConvex(new[] { tris[i], tris[i + 1], tris[i + 2] }, roof.PlanRegion);
                        for (int k = 1; k + 1 < piece.Count; k++)
                        {
                            var a = roof.LiftToRoof(piece[0]); var b = roof.LiftToRoof(piece[k]); var c = roof.LiftToRoof(piece[k + 1]);
                            if (Vec3.TriangleArea(a, b, c) > 1e-9) { sz.Triangles.Add(a); sz.Triangles.Add(b); sz.Triangles.Add(c); }
                        }
                    }
                    if (sz.Triangles.Count == 0) continue;

                    foreach (var ring in new[] { (IReadOnlyList<Vec2>)outer }.Concat(holes))
                        for (int i = 0, n = ring.Count; i < n; i++)
                        {
                            var seg = PolygonOps.ClipSegmentToConvex(ring[i], ring[(i + 1) % n], roof.PlanRegion);
                            if (seg.HasValue) sz.Edges.Add((roof.LiftToRoof(seg.Value.a), roof.LiftToRoof(seg.Value.b)));
                        }
                    scene.Zones.Add(sz);
                }
            }
        }

        private static void AddWallZones(WindScene scene, AreaCalculator_Base zones, Dictionary<int, PressureData> pressures,
            BuildingFace[] allFaces, Dictionary<string, BuildingFace[]> facesByLabel, WindLoadCalculationTypes analysis,
            WindLoadCalculator_MWFRS taper = null)
        {
            if (zones == null) return;
            foreach (var kv in zones.effWindAreas)
            {
                var area = kv.Value;
                var faces = facesByLabel != null
                    ? (facesByLabel.TryGetValue(area.Label_Full, out var f) ? f : new BuildingFace[0])
                    : allFaces;
                if (taper != null && area.Label_Full == "ZoneWW")
                {
                    foreach (var face in faces)
                        AddWindwardWallBands(scene, kv.Key, area, scene.Model.Surface(face), taper);
                    continue;
                }

                var tris = PolygonOps.Triangulate(area.OuterBoundary, area.Holes.Cast<IReadOnlyList<Vec2>>());

                foreach (var face in faces)
                {
                    var wall = scene.Model.Surface(face);
                    if (wall == null) continue;
                    var sz = NewZone(kv.Key, area, pressures, wall, analysis);
                    foreach (var p in tris) sz.Triangles.Add(wall.FromWallFrame(p));
                    foreach (var ring in new[] { (IReadOnlyList<Vec2>)area.OuterBoundary }.Concat(area.Holes))
                        for (int i = 0, n = ring.Count; i < n; i++)
                            sz.Edges.Add((wall.FromWallFrame(ring[i]), wall.FromWallFrame(ring[(i + 1) % n])));
                    scene.Zones.Add(sz);
                }
            }
        }

        /// <summary>Height of the windward-wall color bands above the 15 ft line (ft).</summary>
        public const double WindwardBandHeight = 5.0;

        /// <summary>
        /// The windward wall pressure grows with height (qz). It is drawn as horizontal bands, each colored
        /// with the pressure at its top (the value that governs the band), with a 15 ft band at the bottom
        /// where Kz is constant. Each band also carries the exact pressure-at-height for tapered blocks.
        /// </summary>
        private static void AddWindwardWallBands(WindScene scene, int id, EffectiveWindArea area, BuildingSurface wall,
            WindLoadCalculator_MWFRS calc)
        {
            if (wall == null) return;
            var outline = PolygonOps.CounterClockwise(area.OuterBoundary);
            double top = outline.Max(p => p.Y), left = outline.Min(p => p.X) - 1, right = outline.Max(p => p.X) + 1;

            var levels = new List<double> { 0 };
            if (top > 15) levels.Add(15);
            for (double z = 15 + WindwardBandHeight; z < top - 1e-6; z += WindwardBandHeight) levels.Add(z);
            levels.Add(top);

            Func<Vec3, double> taper = p => calc.WindwardWallNetPressureAt(p.Z);
            for (int b = 0; b + 1 < levels.Count; b++)
            {
                double z0 = levels[b], z1 = levels[b + 1];
                if (z1 - z0 < 1e-6) continue;
                var band = new List<Vec2> { new Vec2(left, z0), new Vec2(right, z0), new Vec2(right, z1), new Vec2(left, z1) };
                var piece = PolygonOps.ClipToConvex(outline, band);
                if (piece.Count < 3 || Math.Abs(PolygonOps.SignedArea(piece)) < AreaCalculator_Base.MinZoneArea) continue;

                var sz = new SceneZone
                {
                    ZoneId = id,
                    Label = area.Label_Full,
                    Description = $"Windward wall, {z0:0.#}-{z1:0.#} ft (pressure grows with height)",
                    Face = wall.Face,
                    Kind = SurfaceKind.Wall,
                    SurfaceName = wall.Name,
                    AreaFt2 = Math.Abs(PolygonOps.SignedArea(piece)),
                    Normal = wall.Normal,
                    PressurePsf = calc.WindwardWallNetPressureAt(z1),
                    PressureAt = taper,
                };
                foreach (var p in PolygonOps.Triangulate(piece)) sz.Triangles.Add(wall.FromWallFrame(p));
                for (int i = 0, n = piece.Count; i < n; i++)
                    sz.Edges.Add((wall.FromWallFrame(piece[i]), wall.FromWallFrame(piece[(i + 1) % n])));
                scene.Zones.Add(sz);
            }
        }

        private static SceneZone NewZone(int id, EffectiveWindArea area, Dictionary<int, PressureData> pressures,
            BuildingSurface surface, WindLoadCalculationTypes analysis)
        {
            return new SceneZone
            {
                ZoneId = id,
                Label = area.Label_Full,
                Description = ZoneDescriptions.Describe(area.Label_Full, surface.Kind, analysis),
                Face = surface.Face,
                Kind = surface.Kind,
                SurfaceName = surface.Name,
                AreaFt2 = area.Area,
                Normal = surface.Normal,
                PressurePsf = pressures != null && pressures.TryGetValue(id, out var p) ? p.NetPressure : double.NaN,
            };
        }
    }
}
