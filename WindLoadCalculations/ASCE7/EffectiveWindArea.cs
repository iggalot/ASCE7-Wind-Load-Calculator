using System;
using System.Collections.Generic;
using System.Windows;
using System.Linq;

namespace ASCE7WindLoadCalculator
{
    public class EffectiveWindArea
    {
        public List<Point> OuterBoundary { get; private set; }
        public List<List<Point>> Holes { get; private set; }

        /// <summary>
        /// The long form of the zone name, e.g. "Zone2e" that always starts with "Zone"
        /// </summary>
        public string Label_Full { get; set; }
        
        /// <summary>
        /// Returns only the short numeric identifier of the label "Zone2e" becomes "2e"
        /// </summary>
        public string Label_Short { get => Label_Full.Substring(4); }

        public EffectiveWindArea(string label, IEnumerable<Point> outer, IEnumerable<IEnumerable<Point>> holes = null)
        {
            if (outer == null)
                throw new ArgumentNullException(nameof(outer));

            Label_Full = label;
            OuterBoundary = outer.ToList();
            ValidateAndFixWinding(OuterBoundary, shouldBeCCW: true);

            Holes = new List<List<Point>>();
            if (holes != null)
            {
                foreach (var hole in holes)
                {
                    var holeList = hole.ToList();
                    ValidateAndFixWinding(holeList, shouldBeCCW: false); // holes must be CW
                    Holes.Add(holeList);
                }
            }
        }

        public double Area
        {
            get
            {
                double totalArea = ComputeArea(OuterBoundary);
                foreach (var hole in Holes)
                {
                    totalArea -= ComputeArea(hole);
                }
                return totalArea;
            }
        }

        public Point Centroid
        {
            get
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

                return new Point(cxTotal / netArea, cyTotal / netArea);
            }
        }

        private static void ValidateAndFixWinding(List<Point> polygon, bool shouldBeCCW)
        {
            if (polygon.Count < 3)
                throw new ArgumentException("Polygon must have at least 3 points.");

            if (polygon.Select(p => $"{p.X:F6},{p.Y:F6}").Distinct().Count() < 3)
                throw new ArgumentException("Polygon must have at least 3 distinct points.");

            bool isCCW = IsCounterClockwise(polygon);
            if (isCCW != shouldBeCCW)
                polygon.Reverse();
        }

        private static bool IsCounterClockwise(List<Point> polygon)
        {
            double sum = 0;
            int n = polygon.Count;

            for (int i = 0; i < n; i++)
            {
                Point a = polygon[i];
                Point b = polygon[(i + 1) % n];
                sum += (b.X - a.X) * (b.Y + a.Y);
            }

            return sum < 0;
        }

        private static double ComputeArea(List<Point> polygon)
        {
            double area = 0;
            int n = polygon.Count;

            for (int i = 0; i < n; i++)
            {
                Point a = polygon[i];
                Point b = polygon[(i + 1) % n];
                area += (a.X * b.Y - b.X * a.Y);
            }

            return Math.Abs(area) * 0.5;
        }

        private static (double cx, double cy, double area) ComputeWeightedCentroid(List<Point> polygon)
        {
            double area = 0, cx = 0, cy = 0;
            int n = polygon.Count;

            for (int i = 0; i < n; i++)
            {
                Point a = polygon[i];
                Point b = polygon[(i + 1) % n];

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
            return $"{Label_Full}: Area = {Area:F2} ft², Centroid = ({Centroid.X:F2}, {Centroid.Y:F2})";
        }

        public static void RemoveConsecutiveDuplicates(List<Point> points)
        {
            if (points == null || points.Count < 2)
                return;

            int i = 1;
            while (i < points.Count)
            {
                if (points[i].Equals(points[i - 1]))
                {
                    points.RemoveAt(i); // Don't increment i; the next item shifts into position i
                }
                else
                {
                    i++;
                }
            }


            // Check if first and last points are equal after cleaning
            if (points.Count > 1 && points[0].Equals(points[points.Count - 1]))
            {
                points.RemoveAt(points.Count - 1); // Remove the last duplicate point
            }
        }
    }
}
