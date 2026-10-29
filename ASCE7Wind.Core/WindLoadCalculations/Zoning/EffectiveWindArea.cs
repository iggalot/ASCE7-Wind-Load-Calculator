using System;
using System.Collections.Generic;
using System.Linq;

namespace ASCE7WindLoadCalculator
{
    /// <summary>
    /// One wind zone: a labeled polygon (optionally with holes) in a surface's local 2D frame, in feet.
    /// Outer boundary is stored counter-clockwise and holes clockwise. Area and centroid are computed once
    /// on first use and cached (removing duplicate vertices does not change either).
    /// </summary>
    public class EffectiveWindArea
    {
        public List<Vec2> OuterBoundary { get; private set; }
        public List<List<Vec2>> Holes { get; private set; }

        /// <summary>Long zone name that always starts with "Zone", e.g. "Zone2e".</summary>
        public string Label_Full { get; set; }

        /// <summary>Zone identifier without the "Zone" prefix, e.g. "2e".</summary>
        public string Label_Short => Label_Full.Substring(4);

        private double? area;
        private Vec2? centroid;

        public EffectiveWindArea(string label, IEnumerable<Vec2> outer, IEnumerable<IEnumerable<Vec2>> holes = null)
        {
            if (outer == null)
                throw new ArgumentNullException(nameof(outer));

            Label_Full = label;
            OuterBoundary = outer.ToList();
            ValidateAndFixWinding(OuterBoundary, shouldBeCCW: true);

            Holes = new List<List<Vec2>>();
            if (holes != null)
            {
                foreach (var hole in holes)
                {
                    var holeList = hole.ToList();
                    ValidateAndFixWinding(holeList, shouldBeCCW: false);
                    Holes.Add(holeList);
                }
            }
        }

        /// <summary>Net area (ft²): outer area minus hole areas.</summary>
        public double Area
        {
            get
            {
                if (area == null)
                {
                    double total = ComputeArea(OuterBoundary);
                    foreach (var hole in Holes)
                        total -= ComputeArea(hole);
                    area = total;
                }
                return area.Value;
            }
        }

        /// <summary>Area-weighted centroid of the net shape.</summary>
        public Vec2 Centroid
        {
            get
            {
                if (centroid == null)
                {
                    var (cxOuter, cyOuter, areaOuter) = ComputeWeightedCentroid(OuterBoundary);
                    double cxTotal = cxOuter * areaOuter;
                    double cyTotal = cyOuter * areaOuter;
                    double netArea = areaOuter;

                    foreach (var hole in Holes)
                    {
                        var (cxHole, cyHole, areaHole) = ComputeWeightedCentroid(hole);
                        cxTotal -= cxHole * areaHole;
                        cyTotal -= cyHole * areaHole;
                        netArea -= areaHole;
                    }

                    if (netArea <= 0)
                        throw new InvalidOperationException("Resulting polygon area is non-positive." + Label_Full);

                    centroid = new Vec2(cxTotal / netArea, cyTotal / netArea);
                }
                return centroid.Value;
            }
        }

        private static void ValidateAndFixWinding(List<Vec2> polygon, bool shouldBeCCW)
        {
            if (polygon.Count < 3)
                throw new ArgumentException("Polygon must have at least 3 points.");

            // At least 3 vertices distinct to 1e-6 ft.
            var distinct = new HashSet<(long, long)>();
            foreach (var p in polygon)
            {
                distinct.Add(((long)Math.Round(p.X * 1e6), (long)Math.Round(p.Y * 1e6)));
                if (distinct.Count >= 3) break;
            }
            if (distinct.Count < 3)
                throw new ArgumentException("Polygon must have at least 3 distinct points.");

            if (IsCounterClockwise(polygon) != shouldBeCCW)
                polygon.Reverse();
        }

        private static bool IsCounterClockwise(List<Vec2> polygon)
        {
            double sum = 0;
            int n = polygon.Count;
            for (int i = 0; i < n; i++)
            {
                Vec2 a = polygon[i];
                Vec2 b = polygon[(i + 1) % n];
                sum += (b.X - a.X) * (b.Y + a.Y);
            }
            return sum < 0;
        }

        private static double ComputeArea(List<Vec2> polygon)
        {
            double sum = 0;
            int n = polygon.Count;
            for (int i = 0; i < n; i++)
            {
                Vec2 a = polygon[i];
                Vec2 b = polygon[(i + 1) % n];
                sum += (a.X * b.Y - b.X * a.Y);
            }
            return Math.Abs(sum) * 0.5;
        }

        private static (double cx, double cy, double area) ComputeWeightedCentroid(List<Vec2> polygon)
        {
            double area = 0, cx = 0, cy = 0;
            int n = polygon.Count;
            for (int i = 0; i < n; i++)
            {
                Vec2 a = polygon[i];
                Vec2 b = polygon[(i + 1) % n];
                double cross = a.X * b.Y - b.X * a.Y;
                area += cross;
                cx += (a.X + b.X) * cross;
                cy += (a.Y + b.Y) * cross;
            }
            area *= 0.5;
            double factor = 1.0 / (6.0 * area);
            return (cx * factor, cy * factor, Math.Abs(area));
        }

        public string DisplayResults()
        {
            var c = Centroid;
            return $"{Label_Full}: Area = {Area:F2} ft², Centroid = ({c.X:F2}, {c.Y:F2})";
        }

        /// <summary>Removes repeated consecutive vertices (and a closing vertex equal to the first).</summary>
        public static void RemoveConsecutiveDuplicates(List<Vec2> points)
        {
            if (points == null || points.Count < 2)
                return;

            int i = 1;
            while (i < points.Count)
            {
                if (points[i].Equals(points[i - 1]))
                    points.RemoveAt(i);
                else
                    i++;
            }

            if (points.Count > 1 && points[0].Equals(points[points.Count - 1]))
                points.RemoveAt(points.Count - 1);
        }
    }
}
