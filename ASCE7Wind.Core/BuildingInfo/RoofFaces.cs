using System.Collections.Generic;
using System.Linq;

namespace ASCE7WindLoadCalculator
{
    /// <summary>
    /// One face of a roof of several sections (owner 2026-11-13: hips, wings and dormers each take the wind by their own
    /// ridge), in the engine's frame: x along <see cref="BuildingData.BuildingLength"/> from the bounding box's south-west
    /// corner, y along its width, z above grade. The walls stay on the bounding box; only the roof is taken face by face.
    /// </summary>
    public sealed class RoofFacePlane
    {
        public string Id { get; set; }
        public string Name { get; set; }
        /// <summary>Its convex pieces on plan (inside the bounding box; the eaves are the overhang's, as for any roof).</summary>
        public List<List<Vec2>> Pieces { get; set; } = new List<List<Vec2>>();
        /// <summary>Its plane: z = Z0 + Zx x + Zy y (ft above grade).</summary>
        public double Z0 { get; set; }
        public double Zx { get; set; }
        public double Zy { get; set; }
        public double SlopeDeg { get; set; }
        /// <summary>Its own section's ridge runs along x (a shed's: its high edge).</summary>
        public bool RidgeAlongX { get; set; }
        /// <summary>
        /// h of its Fig. 27.3-1 values (ft): for the main section the building's (<see cref="BuildingData.MwfrsFigureHeight"/>);
        /// for a wing or dormer its own (ASCE gives none for them; owner 2026-11-13: "if not clear, use the wing's own").
        /// </summary>
        public double H { get; set; }
        /// <summary>Its section's plan size along x and along y (ft): L of its h/L (the main section: the building's).</summary>
        public double Lx { get; set; }
        public double Ly { get; set; }
        /// <summary>The section's name (e.g. "the roof", "east wing", "dormer").</summary>
        public string Section { get; set; }
        /// <summary>Its section sits on the roof (a dormer: its eave above the plates): its eaves never top a wall standing on the ground.</summary>
        public bool OnRoof { get; set; }

        /// <summary>The same face with x and y swapped (the engine's turn of the building for the other wind).</summary>
        public RoofFacePlane Swapped() => new RoofFacePlane
        {
            Id = Id, Name = Name, Section = Section, OnRoof = OnRoof, Z0 = Z0, Zx = Zy, Zy = Zx, SlopeDeg = SlopeDeg, RidgeAlongX = !RidgeAlongX,
            H = H, Lx = Ly, Ly = Lx, Pieces = Pieces.Select(p => p.Select(v => new Vec2(v.Y, v.X)).ToList()).ToList(),
        };

        /// <summary>The same face seen with x reversed (x' = L − x): the wind from the other end, as the wind along +x'.</summary>
        public RoofFacePlane Mirrored(double L) => new RoofFacePlane
        {
            Id = Id, Name = Name, Section = Section, OnRoof = OnRoof, Z0 = Z0 + Zx * L, Zx = -Zx, Zy = Zy, SlopeDeg = SlopeDeg, RidgeAlongX = RidgeAlongX,
            H = H, Lx = Lx, Ly = Ly, Pieces = Pieces.Select(p => p.Select(v => new Vec2(L - v.X, v.Y)).Reverse().ToList()).ToList(),
        };
    }

    /// <summary>
    /// The bounding box's walls under a roof of sections: each wall's top follows the faces over its edge (a wing's gable
    /// where the wing reaches the box, the main roof's eave or gable elsewhere), not one gable over the whole box. A wall
    /// standing on a lower roof is a <see cref="RoofWallPiece"/>, never on the box's edge, so nothing is counted twice.
    /// </summary>
    public static class RoofFaceProfile
    {
        /// <summary>Sampling along a wall (ft); the profile is straight between the faces' corners, so it is exact at them.</summary>
        public const double Step = 0.25;

        /// <summary>The roof's top over a plan point (ft above grade): the highest face over it, else the eave.</summary>
        public static double TopAt(BuildingData b, double x, double y) => TopAt(b, x, y, x, y, false);

        /// <summary>The faces over (<paramref name="qx"/>, <paramref name="qy"/>), their height at (x, y): a wall's edge.</summary>
        private static double TopAt(BuildingData b, double x, double y, double qx, double qy, bool groundWall)
        {
            double top = b.BuildingHeight;
            if (b.RoofFaces == null) return top;
            var q = new Vec2(qx, qy);
            foreach (var f in b.RoofFaces.Where(f => !(groundWall && f.OnRoof)))
                foreach (var p in f.Pieces)
                    if (PolygonOps.ContainsPoint(PolygonOps.CounterClockwise(p), q, 1e-9)) { top = System.Math.Max(top, f.Z0 + f.Zx * x + f.Zy * y); break; }
            return top;
        }

        /// <summary>
        /// The outline of a box wall in its (along, up) frame: along runs from (x0, y0) to (x1, y1), the plan's edge; the top
        /// sampled just inside the box (the faces end on its edge), with the faces' corners and points where it bends kept.
        /// </summary>
        public static List<Vec2> WallOutline(BuildingData b, double x0, double y0, double x1, double y1) => WallOutline(b, x0, y0, x1, y1, null);

        /// <summary>
        /// The plan's walls facing one way (<paramref name="normal"/>: ±x or ±y), projected on the plane across it: an outline in
        /// (across, up), across being y for ±x and x for ±y from 0. At each point across, the tallest of those walls there (a
        /// wing's gable set back from the main wall shows over it); none there, nothing. For the walls' force along the wind
        /// (the windward and leeward walls' projected areas, ASCE 7 27.3); with no <see cref="BuildingData.WallLines"/>, the
        /// bounding box's wall on that side.
        /// </summary>
        public static List<Vec2> Silhouette(BuildingData b, Vec2 normal)
        {
            bool alongY = System.Math.Abs(normal.X) > 0.5;     // the wall runs along y
            double L = b.BuildingLength, B = b.BuildingWidth, span = alongY ? B : L;
            if (!b.HasWallLines)
            {
                if (alongY) return normal.X < 0 ? WallOutline(b, 0, 0, 0, B) : WallOutline(b, L, 0, L, B);
                return normal.Y < 0 ? WallOutline(b, 0, 0, L, 0) : WallOutline(b, 0, B, L, B);
            }
            var walls = b.WallLines.Where(w => w.Normal.X * normal.X + w.Normal.Y * normal.Y > 0.5)
                .Select(w =>
                {
                    var o = WallOutline(b, w.A.X, w.A.Y, w.B.X, w.B.Y, w.Normal);
                    double a = alongY ? w.A.Y : w.A.X, c = alongY ? w.B.Y : w.B.X;
                    // its top, by the coordinate across: the outline after its two bottom corners, in the order of t
                    var top = o.Skip(2).Select(p => (t: a + (c > a ? 1 : -1) * p.X, z: p.Y)).OrderBy(p => p.t).ToList();
                    return (lo: System.Math.Min(a, c), hi: System.Math.Max(a, c), top);
                }).ToList();
            double At(double t)
            {
                double z = 0;
                foreach (var (lo, hi, top) in walls)
                {
                    if (t < lo - 1e-9 || t > hi + 1e-9 || top.Count == 0) continue;
                    double h = top[top.Count - 1].z;
                    for (int i = 0; i + 1 < top.Count; i++)
                        if (t <= top[i + 1].t + 1e-12)
                        {
                            double d = top[i + 1].t - top[i].t;
                            h = d < 1e-12 ? System.Math.Max(top[i].z, top[i + 1].z) : top[i].z + (top[i + 1].z - top[i].z) * (t - top[i].t) / d;
                            break;
                        }
                    z = System.Math.Max(z, h);
                }
                return z;
            }
            // the points: every wall's ends (just inside and outside them, where the height may jump) and its top's corners
            var ts = new SortedSet<double> { 0, span };
            foreach (var (lo, hi, top) in walls)
            {
                foreach (var t in new[] { lo, hi }) { ts.Add(t); if (t - 1e-6 > 0) ts.Add(t - 1e-6); if (t + 1e-6 < span) ts.Add(t + 1e-6); }
                foreach (var p in top) if (p.t > 1e-6 && p.t < span - 1e-6) ts.Add(p.t);
            }
            var pts = ts.Select(t => new Vec2(t, At(t))).ToList();   // just past a wall's end: the next wall's height, or none
            var outline = new List<Vec2> { new Vec2(0, 0), new Vec2(span, 0) };
            for (int i = pts.Count - 1; i >= 0; i--) outline.Add(pts[i]);
            return outline;
        }

        /// <summary>
        /// A wall of the plan's own outline (<see cref="BuildingData.WallLines"/>): the top read just inside it, on the side
        /// away from <paramref name="outward"/> (its outward normal; null: toward the bounding box's center).
        /// </summary>
        public static List<Vec2> WallOutline(BuildingData b, double x0, double y0, double x1, double y1, Vec2? outward)
        {
            double len = System.Math.Sqrt((x1 - x0) * (x1 - x0) + (y1 - y0) * (y1 - y0));
            double ux = (x1 - x0) / len, uy = (y1 - y0) / len;
            double cx = b.BuildingLength / 2, cy = b.BuildingWidth / 2;
            // just inside, across the wall only (along it the top must be read where it is)
            const double inset = 1e-7;
            double nx = -uy, ny = ux;
            if (outward.HasValue ? nx * outward.Value.X + ny * outward.Value.Y > 0 : nx * (cx - x0) + ny * (cy - y0) < 0) { nx = -nx; ny = -ny; }
            double At(double s) => TopAt(b, x0 + ux * s, y0 + uy * s, x0 + ux * s + nx * inset, y0 + uy * s + ny * inset, true);
            var ss = new SortedSet<double> { 0, len };
            int m = System.Math.Max(1, (int)System.Math.Ceiling(len / Step));
            for (int i = 1; i < m; i++) ss.Add(len * i / m);
            foreach (var f in b.RoofFaces ?? new List<RoofFacePlane>())
                foreach (var p in f.Pieces)
                    foreach (var v in p)
                    {
                        double s = (v.X - x0) * ux + (v.Y - y0) * uy;
                        if (s > 1e-6 && s < len - 1e-6) ss.Add(s);
                    }
            var top = ss.Select(s => new Vec2(s, At(s))).ToList();
            // drop the samples on a straight run
            var kept = new List<Vec2> { top[0] };
            for (int i = 1; i + 1 < top.Count; i++)
            {
                Vec2 a = kept[kept.Count - 1], c = top[i + 1], q = top[i];
                double cross = (c.X - a.X) * (q.Y - a.Y) - (c.Y - a.Y) * (q.X - a.X);
                if (System.Math.Abs(cross) > 1e-6 * System.Math.Max(1, (c.X - a.X))) kept.Add(q);
            }
            kept.Add(top[top.Count - 1]);
            var outline = new List<Vec2> { new Vec2(0, 0), new Vec2(len, 0) };
            for (int i = kept.Count - 1; i >= 0; i--) outline.Add(kept[i]);
            return outline;
        }
    }

    /// <summary>
    /// One straight wall of the plan's own outline (owner 2026-11-13: the walls are the building's, not its bounding box's; an
    /// L has six), in the engine's frame, standing on the ground up to the roof over it. Its outward normal on plan.
    /// </summary>
    public sealed class WallLine
    {
        public Vec2 A { get; set; }
        public Vec2 B { get; set; }
        /// <summary>Outward, a unit vector along ±x or ±y.</summary>
        public Vec2 Normal { get; set; }
        public double Length => System.Math.Sqrt((B.X - A.X) * (B.X - A.X) + (B.Y - A.Y) * (B.Y - A.Y));

        /// <summary>The way it faces in the engine's frame: "West", "East", "South" or "North".</summary>
        public string Facing => Normal.X < -0.5 ? "West" : Normal.X > 0.5 ? "East" : Normal.Y < -0.5 ? "South" : "North";

        public WallLine Swapped() => new WallLine { A = new Vec2(A.Y, A.X), B = new Vec2(B.Y, B.X), Normal = new Vec2(Normal.Y, Normal.X) };

        public WallLine Mirrored(double L) => new WallLine { A = new Vec2(L - B.X, B.Y), B = new Vec2(L - A.X, A.Y), Normal = new Vec2(-Normal.X, Normal.Y) };

        public override string ToString() => $"{Facing} wall ({A.X:0.##}, {A.Y:0.##})-({B.X:0.##}, {B.Y:0.##})";
    }

    /// <summary>
    /// A wall standing on a lower roof (a dormer's front or cheek, a taller wing's gable over the main roof): owner
    /// 2026-11-13, the front takes the windward wall's pressure, the cheeks the side walls'. Its area and centroid on its
    /// vertical plane, in the engine's frame.
    /// </summary>
    public sealed class RoofWallPiece
    {
        public string Name { get; set; }
        /// <summary>Its outward normal on plan (a unit vector along ±x or ±y).</summary>
        public Vec2 Normal { get; set; }
        /// <summary>Its area (sq ft) and centroid (plan and height).</summary>
        public double Area { get; set; }
        public Vec2 Centroid { get; set; }
        public double CentroidZ { get; set; }

        public RoofWallPiece Swapped() => new RoofWallPiece
        {
            Name = Name, Area = Area, CentroidZ = CentroidZ, Normal = new Vec2(Normal.Y, Normal.X), Centroid = new Vec2(Centroid.Y, Centroid.X),
        };

        public RoofWallPiece Mirrored(double L) => new RoofWallPiece
        {
            Name = Name, Area = Area, CentroidZ = CentroidZ, Normal = new Vec2(-Normal.X, Normal.Y), Centroid = new Vec2(L - Centroid.X, Centroid.Y),
        };
    }
}
