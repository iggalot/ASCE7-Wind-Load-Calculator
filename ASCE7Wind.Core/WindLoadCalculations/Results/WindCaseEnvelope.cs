using System;
using System.Collections.Generic;
using System.Linq;

namespace ASCE7WindLoadCalculator
{
    /// <summary>
    /// One cell of the envelope: a piece of a surface small enough that every wind case puts one pressure on it (the overlap
    /// of the zone maps of the four winds W, E, S and N, which every quartering map is made of too), with the largest and the
    /// smallest net pressure any case puts on it and the case that does each. Max and Min are kept apart, each with its case.
    /// </summary>
    public sealed class WindEnvelopeCell
    {
        public BuildingSurface Surface { get; internal set; }
        public BuildingFace Face => Surface.Face;
        public SurfaceKind Kind => Surface.Kind;

        /// <summary>Outline in the surface's frame: plan (x, y) for a roof, (along, up) for a wall (ft).</summary>
        public IReadOnlyList<Vec2> Outline { get; internal set; }
        public double Area { get; internal set; }

        /// <summary>Most positive net pressure (psf, + toward the surface) of all the cases, and the case.</summary>
        public double MaxNetPsf { get; internal set; } = double.NegativeInfinity;
        public WindCase MaxBy { get; internal set; }
        public string MaxZone { get; internal set; }

        /// <summary>Most negative net pressure (psf, − suction) of all the cases, and the case.</summary>
        public double MinNetPsf { get; internal set; } = double.PositiveInfinity;
        public WindCase MinBy { get; internal set; }
        public string MinZone { get; internal set; }
    }

    public static class WindCaseEnvelope
    {
        /// <summary>The envelope of <paramref name="cases"/>, cell by cell (see <see cref="WindEnvelopeCell"/>).</summary>
        public static List<WindEnvelopeCell> Build(IReadOnlyList<WindCase> cases)
        {
            var cells = new List<WindEnvelopeCell>();
            if (cases == null || cases.Count == 0) return cells;

            // the common grid: every distinct zone map among the cases (cases differing only in their pressures share a
            // map) laid over each other, surface by surface, starting from the whole surface
            var maps = cases.GroupBy(MapKey).Select(grp => grp.First()).ToList();
            var surfaces = cases.SelectMany(m => m.Zones.Select(z => z.Surface)).Distinct().ToList();
            foreach (var s in surfaces)
            {
                var whole = s.Kind == SurfaceKind.Roof
                    ? s.PlanRegion.ToList()
                    : s.Outline.Select(p => new Vec2((p - s.Origin).Dot(s.U), (p - s.Origin).Dot(s.V))).ToList();
                var grid = new List<List<Vec2>> { PolygonOps.CounterClockwise(whole) };
                foreach (var m in maps)
                {
                    var zones = m.Zones.Where(q => q.Surface == s).ToList();
                    if (zones.Count == 0) continue;           // e.g. a Ch. 28 Case A wall along the wind: no zone there
                    var next = new List<List<Vec2>>();
                    foreach (var cell in grid)
                        foreach (var z in zones)
                            foreach (var convex in ConvexParts(z.Outline.ToList()))
                            {
                                var piece = PolygonOps.ClipToConvex(cell, convex);
                                if (piece.Count >= 3 && Math.Abs(PolygonOps.SignedArea(piece)) > 1e-6) next.Add(piece);
                            }
                    grid = next;
                }
                double k = s.Kind == SurfaceKind.Roof ? Math.Sqrt(1 + s.Zx * s.Zx + s.Zy * s.Zy) : 1;
                foreach (var g in grid)
                    cells.Add(new WindEnvelopeCell { Surface = s, Outline = g, Area = Math.Abs(PolygonOps.SignedArea(g)) * k });
            }

            // each case's pressure on each cell: the case's zone holding the cell's centroid
            foreach (var cell in cells)
            {
                var at = Centroid(cell.Outline);
                foreach (var c in cases)
                {
                    var z = c.Zones.FirstOrDefault(q => q.Surface == cell.Surface && Inside(q.Outline, at));
                    if (z == null) continue;
                    if (z.NetPsf > cell.MaxNetPsf) { cell.MaxNetPsf = z.NetPsf; cell.MaxBy = c; cell.MaxZone = z.Label; }
                    if (z.NetPsf < cell.MinNetPsf) { cell.MinNetPsf = z.NetPsf; cell.MinBy = c; cell.MinZone = z.Label; }
                }
            }
            return cells;
        }

        /// <summary>The zone layout of a case (surfaces and outlines, to 0.001 ft), without its pressures.</summary>
        private static string MapKey(WindCase c) =>
            string.Join("|", c.Zones.Select(z => (int)z.Face + ":" + string.Join(";", z.Outline.Select(p => $"{p.X:0.000},{p.Y:0.000}")))
                                    .OrderBy(k => k, StringComparer.Ordinal));

        private static IEnumerable<List<Vec2>> ConvexParts(List<Vec2> poly)
        {
            bool convex = true;
            for (int i = 0, n = poly.Count; i < n && convex; i++)
            {
                Vec2 a = poly[i], b = poly[(i + 1) % n], c = poly[(i + 2) % n];
                if ((b.X - a.X) * (c.Y - b.Y) - (b.Y - a.Y) * (c.X - b.X) < -1e-9) convex = false;
            }
            if (convex) { yield return PolygonOps.CounterClockwise(poly); yield break; }
            var tris = PolygonOps.Triangulate(poly);
            for (int i = 0; i + 2 < tris.Count; i += 3) yield return PolygonOps.CounterClockwise(new List<Vec2> { tris[i], tris[i + 1], tris[i + 2] });
        }

        internal static Vec2 Centroid(IReadOnlyList<Vec2> p)
        {
            double a = 0, cx = 0, cy = 0;
            for (int i = 0, n = p.Count; i < n; i++)
            {
                var u = p[i]; var v = p[(i + 1) % n];
                double k = u.X * v.Y - v.X * u.Y;
                a += k; cx += (u.X + v.X) * k; cy += (u.Y + v.Y) * k;
            }
            return Math.Abs(a) < 1e-12 ? p[0] : new Vec2(cx / (3 * a), cy / (3 * a));
        }

        /// <summary>Point in polygon (even-odd), a point on an edge counting as inside.</summary>
        internal static bool Inside(IReadOnlyList<Vec2> poly, Vec2 p)
        {
            bool inside = false;
            for (int i = 0, j = poly.Count - 1; i < poly.Count; j = i++)
            {
                Vec2 a = poly[i], b = poly[j];
                if (DistanceToSegment(p, a, b) < 1e-7) return true;
                if ((a.Y > p.Y) != (b.Y > p.Y) && p.X < (b.X - a.X) * (p.Y - a.Y) / (b.Y - a.Y) + a.X) inside = !inside;
            }
            return inside;
        }

        private static double DistanceToSegment(Vec2 p, Vec2 a, Vec2 b)
        {
            double dx = b.X - a.X, dy = b.Y - a.Y, l2 = dx * dx + dy * dy;
            double t = l2 < 1e-18 ? 0 : Math.Max(0, Math.Min(1, ((p.X - a.X) * dx + (p.Y - a.Y) * dy) / l2));
            double ex = a.X + t * dx - p.X, ey = a.Y + t * dy - p.Y;
            return Math.Sqrt(ex * ex + ey * ey);
        }
    }
}
