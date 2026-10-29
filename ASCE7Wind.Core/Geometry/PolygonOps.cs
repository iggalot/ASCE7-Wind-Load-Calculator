using System;
using System.Collections.Generic;
using System.Linq;

namespace ASCE7WindLoadCalculator
{
    /// <summary>2D polygon utilities used to turn zone outlines into renderable triangles.</summary>
    public static class PolygonOps
    {
        private const double Eps = 1e-9;

        /// <summary>Signed area: positive for counter-clockwise vertex order.</summary>
        public static double SignedArea(IReadOnlyList<Vec2> p)
        {
            double s = 0;
            for (int i = 0, n = p.Count; i < n; i++)
            {
                var a = p[i]; var b = p[(i + 1) % n];
                s += a.X * b.Y - b.X * a.Y;
            }
            return 0.5 * s;
        }

        /// <summary>
        /// Triangulates a polygon with optional holes using horizontal slab decomposition and the
        /// even-odd rule. Robust for holes that touch each other or the outer boundary (as zone layouts
        /// do), and for any simple polygon. Returns triangles as consecutive vertex triples, all CCW.
        /// </summary>
        public static List<Vec2> Triangulate(IReadOnlyList<Vec2> outer, IEnumerable<IReadOnlyList<Vec2>> holes = null)
        {
            var rings = new List<IReadOnlyList<Vec2>> { outer };
            if (holes != null) rings.AddRange(holes);

            // Non-horizontal edges of every ring.
            var edges = new List<(Vec2 a, Vec2 b)>();
            var ys = new List<double>();
            foreach (var ring in rings)
            {
                for (int i = 0, n = ring.Count; i < n; i++)
                {
                    var a = ring[i]; var b = ring[(i + 1) % n];
                    ys.Add(a.Y);
                    if (Math.Abs(a.Y - b.Y) > Eps)
                        edges.Add(a.Y < b.Y ? (a, b) : (b, a)); // a = lower end
                }
            }

            var levels = ys.Distinct().OrderBy(y => y).ToList();
            var tris = new List<Vec2>();
            var crossing = new List<(double xMid, double x0, double x1)>();

            for (int s = 0; s + 1 < levels.Count; s++)
            {
                double y0 = levels[s], y1 = levels[s + 1];
                if (y1 - y0 <= Eps) continue;
                double ym = 0.5 * (y0 + y1);

                crossing.Clear();
                foreach (var (a, b) in edges)
                {
                    if (a.Y <= ym && b.Y >= ym)
                        crossing.Add((XAt(a, b, ym), XAt(a, b, y0), XAt(a, b, y1)));
                }
                crossing.Sort((p, q) => p.xMid.CompareTo(q.xMid));

                for (int k = 0; k + 1 < crossing.Count; k += 2)
                {
                    var left = crossing[k]; var right = crossing[k + 1];
                    var p0 = new Vec2(left.x0, y0);
                    var p1 = new Vec2(right.x0, y0);
                    var p2 = new Vec2(right.x1, y1);
                    var p3 = new Vec2(left.x1, y1);
                    AddTriangle(tris, p0, p1, p2);
                    AddTriangle(tris, p0, p2, p3);
                }
            }
            return tris;
        }

        private static double XAt(Vec2 a, Vec2 b, double y) => a.X + (y - a.Y) * (b.X - a.X) / (b.Y - a.Y);

        private static void AddTriangle(List<Vec2> tris, Vec2 a, Vec2 b, Vec2 c)
        {
            double cross = (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);
            if (Math.Abs(cross) <= 1e-12) return; // degenerate (zero-width slab piece)
            if (cross > 0) { tris.Add(a); tris.Add(b); tris.Add(c); }
            else { tris.Add(a); tris.Add(c); tris.Add(b); }
        }

        /// <summary>Returns the CCW version of a polygon.</summary>
        public static List<Vec2> CounterClockwise(IReadOnlyList<Vec2> p)
        {
            var list = p.ToList();
            if (SignedArea(list) < 0) list.Reverse();
            return list;
        }

        /// <summary>
        /// Sutherland-Hodgman: clips a polygon against a convex CCW clip polygon. Returns the (possibly
        /// empty) intersection.
        /// </summary>
        public static List<Vec2> ClipToConvex(IReadOnlyList<Vec2> subject, IReadOnlyList<Vec2> convexCcw)
        {
            var output = subject.ToList();
            for (int i = 0, n = convexCcw.Count; i < n && output.Count > 0; i++)
            {
                var a = convexCcw[i]; var b = convexCcw[(i + 1) % n];
                var input = output;
                output = new List<Vec2>(input.Count + 2);
                for (int j = 0, m = input.Count; j < m; j++)
                {
                    var cur = input[j]; var prev = input[(j + m - 1) % m];
                    double dc = Side(a, b, cur), dp = Side(a, b, prev);
                    bool cIn = dc >= -Eps, pIn = dp >= -Eps;
                    if (cIn)
                    {
                        if (!pIn) output.Add(Intersect(prev, cur, dp, dc));
                        output.Add(cur);
                    }
                    else if (pIn)
                    {
                        output.Add(Intersect(prev, cur, dp, dc));
                    }
                }
            }
            return output;
        }

        /// <summary>Cyrus-Beck: the part of segment pq inside a convex CCW polygon, or null.</summary>
        public static (Vec2 a, Vec2 b)? ClipSegmentToConvex(Vec2 p, Vec2 q, IReadOnlyList<Vec2> convexCcw)
        {
            double t0 = 0, t1 = 1;
            var d = q - p;
            for (int i = 0, n = convexCcw.Count; i < n; i++)
            {
                var a = convexCcw[i]; var b = convexCcw[(i + 1) % n];
                double num = Side(a, b, p);           // >= 0 inside
                double den = Side(a, b, p + d) - num; // change along the segment
                if (Math.Abs(den) < 1e-15)
                {
                    if (num < -Eps) return null;      // parallel and outside
                    continue;
                }
                double t = -num / den;
                if (den > 0) t0 = Math.Max(t0, t); else t1 = Math.Min(t1, t);
                if (t0 > t1 + 1e-12) return null;
            }
            if (t1 - t0 <= 1e-12) return null;
            return (p + d * t0, p + d * t1);
        }

        private static double Side(Vec2 a, Vec2 b, Vec2 p) => (b.X - a.X) * (p.Y - a.Y) - (b.Y - a.Y) * (p.X - a.X);

        private static Vec2 Intersect(Vec2 p, Vec2 q, double dp, double dq)
        {
            double t = dp / (dp - dq);
            return p + (q - p) * t;
        }
    }
}
