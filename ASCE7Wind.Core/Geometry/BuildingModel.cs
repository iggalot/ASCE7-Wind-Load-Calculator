using System;
using System.Collections.Generic;
using System.Linq;

namespace ASCE7WindLoadCalculator
{
    public enum SurfaceKind { Wall, Roof }

    /// <summary>Which face of the rectangular building a surface belongs to.</summary>
    public enum BuildingFace
    {
        /// <summary>Wall at y = 0, spanning the BuildingLength.</summary>
        SouthWall,
        /// <summary>Wall at y = B, spanning the BuildingLength.</summary>
        NorthWall,
        /// <summary>Wall at x = 0, spanning the BuildingWidth.</summary>
        WestWall,
        /// <summary>Wall at x = L, spanning the BuildingWidth.</summary>
        EastWall,
        RoofFlat,
        RoofSouth,
        RoofNorth,
        RoofWest,
        RoofEast,
    }

    /// <summary>
    /// One planar surface of the building envelope.
    /// Walls: local frame (u along the wall from <see cref="Origin"/>, v up), point = Origin + u*U + v*V.
    /// Roofs: a convex region in plan (x, y) and a plane z = Z0 + Zx*x + Zy*y over it.
    /// </summary>
    public sealed class BuildingSurface
    {
        public BuildingFace Face { get; internal set; }
        public SurfaceKind Kind { get; internal set; }
        public string Name { get; internal set; }

        /// <summary>Outline in 3D model coordinates (ft), counter-clockwise seen from outside.</summary>
        public IReadOnlyList<Vec3> Outline { get; internal set; }

        // Wall frame
        public Vec3 Origin { get; internal set; }
        public Vec3 U { get; internal set; }
        public Vec3 V { get; internal set; }

        // Roof plane over a plan region
        public IReadOnlyList<Vec2> PlanRegion { get; internal set; }
        public double Z0 { get; internal set; }
        public double Zx { get; internal set; }
        public double Zy { get; internal set; }

        /// <summary>Outward unit normal.</summary>
        public Vec3 Normal { get; internal set; }

        /// <summary>Wall local (u, v) to 3D.</summary>
        public Vec3 FromWallFrame(Vec2 p) => Origin + U * p.X + V * p.Y;

        /// <summary>Roof plan point (x, y) lifted onto this roof plane.</summary>
        public Vec3 LiftToRoof(Vec2 p) => new Vec3(p.X, p.Y, Z0 + Zx * p.X + Zy * p.Y);

        /// <summary>True surface area (ft²).</summary>
        public double Area
        {
            get
            {
                double a = 0;
                for (int i = 1; i + 1 < Outline.Count; i++)
                    a += Vec3.TriangleArea(Outline[0], Outline[i], Outline[i + 1]);
                return a;
            }
        }
    }

    /// <summary>
    /// The building envelope as 3D planar surfaces, derived only from <see cref="BuildingData"/> so that
    /// wind zoning, drawings and the 3D view all share one geometry. Plan origin at the south-west
    /// eave corner: x along BuildingLength, y along BuildingWidth, z up from grade.
    /// </summary>
    public sealed class BuildingModel
    {
        public BuildingData Building { get; }
        public IReadOnlyList<BuildingSurface> Surfaces { get; }

        public double L => Building.BuildingLength;
        public double B => Building.BuildingWidth;
        public double EaveHeight => Building.BuildingHeight;
        public double RidgeHeight => Building.RidgeHeight;

        public IEnumerable<BuildingSurface> Walls => Surfaces.Where(s => s.Kind == SurfaceKind.Wall);
        public IEnumerable<BuildingSurface> Roofs => Surfaces.Where(s => s.Kind == SurfaceKind.Roof);

        public BuildingSurface Surface(BuildingFace face) => Surfaces.FirstOrDefault(s => s.Face == face);

        public BuildingModel(BuildingData building)
        {
            Building = building ?? throw new ArgumentNullException(nameof(building));
            if (L <= 0 || B <= 0 || EaveHeight <= 0)
                throw new ArgumentException("Building length, width and eave height must be positive.");

            var list = new List<BuildingSurface>();
            BuildWalls(list);
            BuildRoof(list);
            Surfaces = list;
        }

        /// <summary>Roof height at a plan point (ft): the highest roof plane covering it.</summary>
        public double RoofHeightAt(double x, double y)
        {
            double tan = Math.Tan(Building.RoofPitch * Math.PI / 180.0);
            double h = EaveHeight;
            switch (Building.RoofType)
            {
                case RoofTypes.ROOF_TYPE_FLAT:
                    return h;
                case RoofTypes.ROOF_TYPE_GABLE:
                    return Building.RidgeDirection == RidgeDirections.RIDGE_DIR_PERP_TO_BLDGLENGTH
                        ? h + tan * Math.Min(x, L - x)
                        : h + tan * Math.Min(y, B - y);
                case RoofTypes.ROOF_TYPE_HIP:
                    return h + tan * Math.Min(Math.Min(x, L - x), Math.Min(y, B - y));
                default:
                    return h;
            }
        }

        // ---------------------------------------------------------------------------------------

        private bool GableEndsOnLengthWalls =>
            Building.RoofType == RoofTypes.ROOF_TYPE_GABLE && Building.RidgeDirection == RidgeDirections.RIDGE_DIR_PERP_TO_BLDGLENGTH;

        private bool GableEndsOnWidthWalls =>
            Building.RoofType == RoofTypes.ROOF_TYPE_GABLE && Building.RidgeDirection == RidgeDirections.RIDGE_DIR_PARALLEL_TO_BLDGLENGTH;

        private void BuildWalls(List<BuildingSurface> list)
        {
            double h = EaveHeight, ridge = RidgeHeight;

            list.Add(Wall(BuildingFace.SouthWall, "South wall", new Vec3(0, 0, 0), Vec3.UnitX, L, -Vec3.UnitY, GableEndsOnLengthWalls, h, ridge));
            list.Add(Wall(BuildingFace.NorthWall, "North wall", new Vec3(0, B, 0), Vec3.UnitX, L, Vec3.UnitY, GableEndsOnLengthWalls, h, ridge));
            list.Add(Wall(BuildingFace.WestWall, "West wall", new Vec3(0, 0, 0), Vec3.UnitY, B, -Vec3.UnitX, GableEndsOnWidthWalls, h, ridge));
            list.Add(Wall(BuildingFace.EastWall, "East wall", new Vec3(L, 0, 0), Vec3.UnitY, B, Vec3.UnitX, GableEndsOnWidthWalls, h, ridge));
        }

        private static BuildingSurface Wall(BuildingFace face, string name, Vec3 origin, Vec3 u, double length, Vec3 normal,
            bool gable, double eave, double ridge)
        {
            var s = new BuildingSurface { Face = face, Kind = SurfaceKind.Wall, Name = name, Origin = origin, U = u, V = Vec3.UnitZ, Normal = normal };
            var local = new List<Vec2> { new Vec2(0, 0), new Vec2(length, 0), new Vec2(length, eave) };
            if (gable) local.Add(new Vec2(length / 2, ridge));
            local.Add(new Vec2(0, eave));
            var pts = local.Select(s.FromWallFrame).ToList();
            // Outline CCW seen from outside: reverse when the frame's u x v points inward.
            if (u.Cross(Vec3.UnitZ).Dot(normal) < 0) pts.Reverse();
            s.Outline = pts;
            return s;
        }

        private void BuildRoof(List<BuildingSurface> list)
        {
            double h = EaveHeight;
            double tan = Math.Tan(Building.RoofPitch * Math.PI / 180.0);

            switch (Building.RoofType)
            {
                case RoofTypes.ROOF_TYPE_FLAT:
                    list.Add(Roof(BuildingFace.RoofFlat, "Roof", Rect(0, 0, L, B), h, 0, 0));
                    break;

                case RoofTypes.ROOF_TYPE_GABLE when Building.RidgeDirection == RidgeDirections.RIDGE_DIR_PERP_TO_BLDGLENGTH:
                    // Ridge along y at x = L/2.
                    list.Add(Roof(BuildingFace.RoofWest, "West roof slope", Rect(0, 0, L / 2, B), h, tan, 0));
                    list.Add(Roof(BuildingFace.RoofEast, "East roof slope", Rect(L / 2, 0, L, B), h + tan * L, -tan, 0));
                    break;

                case RoofTypes.ROOF_TYPE_GABLE:
                    // Ridge along x at y = B/2.
                    list.Add(Roof(BuildingFace.RoofSouth, "South roof slope", Rect(0, 0, L, B / 2), h, 0, tan));
                    list.Add(Roof(BuildingFace.RoofNorth, "North roof slope", Rect(0, B / 2, L, B), h + tan * B, 0, -tan));
                    break;

                case RoofTypes.ROOF_TYPE_HIP:
                    double r = Math.Min(L, B) / 2; // horizontal run from eave to ridge
                    var c1 = new Vec2(r, r); var c2 = new Vec2(L - r, r);
                    var c3 = new Vec2(L - r, B - r); var c4 = new Vec2(r, B - r);
                    list.Add(Roof(BuildingFace.RoofSouth, "South roof slope", Poly(new Vec2(0, 0), new Vec2(L, 0), c2, c1), h, 0, tan));
                    list.Add(Roof(BuildingFace.RoofEast, "East roof slope", Poly(new Vec2(L, 0), new Vec2(L, B), c3, c2), h + tan * L, -tan, 0));
                    list.Add(Roof(BuildingFace.RoofNorth, "North roof slope", Poly(new Vec2(L, B), new Vec2(0, B), c4, c3), h + tan * B, 0, -tan));
                    list.Add(Roof(BuildingFace.RoofWest, "West roof slope", Poly(new Vec2(0, B), new Vec2(0, 0), c1, c4), h, tan, 0));
                    break;
            }
        }

        private static List<Vec2> Rect(double x0, double y0, double x1, double y1) =>
            new List<Vec2> { new Vec2(x0, y0), new Vec2(x1, y0), new Vec2(x1, y1), new Vec2(x0, y1) };

        /// <summary>Convex region in CCW order with repeated vertices removed (a square hip has triangular faces).</summary>
        private static List<Vec2> Poly(params Vec2[] pts)
        {
            var list = new List<Vec2>();
            foreach (var p in pts)
                if (list.Count == 0 || (list[list.Count - 1] - p).X * (list[list.Count - 1] - p).X + (list[list.Count - 1] - p).Y * (list[list.Count - 1] - p).Y > 1e-18)
                    list.Add(p);
            if (list.Count > 1 && list[0] == list[list.Count - 1]) list.RemoveAt(list.Count - 1);
            return PolygonOps.CounterClockwise(list);
        }

        private static BuildingSurface Roof(BuildingFace face, string name, List<Vec2> region, double z0, double zx, double zy)
        {
            var s = new BuildingSurface
            {
                Face = face, Kind = SurfaceKind.Roof, Name = name,
                PlanRegion = PolygonOps.CounterClockwise(region),
                Z0 = z0, Zx = zx, Zy = zy,
                Normal = new Vec3(-zx, -zy, 1).Normalized(),
            };
            s.Outline = s.PlanRegion.Select(s.LiftToRoof).ToList();
            return s;
        }
    }
}
