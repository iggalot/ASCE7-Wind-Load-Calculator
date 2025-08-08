using System;
using System.Collections.Generic;
using System.Windows;

namespace ASCE7WindLoadCalculator
{
    public abstract class AreaCalculator_Base : IEffectiveWindAreaCalculator
    {
        public abstract BuildingData buildingData { get; set; }

        public abstract bool HasCritDim { get; set; }
        public abstract double CritDim_a { get; set; }

        /// <summary>
        /// Effective wind areas for roof To be overriden by the implementating class
        /// </summary>
        public Dictionary<int, EffectiveWindArea> effWindAreas { get; set; } = new Dictionary<int, EffectiveWindArea>();

        public virtual string Note { get; set; } = String.Empty;  // a holder for a note in the calculator -- useful for recording gable vs. non gable walls

        public abstract void ComputeEffectiveWindAreas();


        /// <summary>
        /// Try to create a zone with positive area.  Otherwise return null;
        /// </summary>
        /// <param name="label"></param>
        /// <param name="outer"></param>
        /// <param name="holes"></param>
        /// <returns></returns>
        public static EffectiveWindArea TryCreateZone(int id, string label, IEnumerable<Point> outer, IEnumerable<IEnumerable<Point>> holes = null)
        {
            try
            {
                var zone = new EffectiveWindArea(label, outer, holes);
                if (zone.Area > 0)
                {
                    Console.WriteLine($"Zone {id} ({label}) created: Area = {zone.Area:F2} ft²");
                    return zone;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Zone {id} ({label}) failed: {ex.Message}");
            }

            return null;
        }

        public string DisplayResults()
        {
            string str = string.Empty;
            foreach (KeyValuePair<int, EffectiveWindArea> kvp in effWindAreas)
            {
                str += kvp.Value.DisplayResults() + "\n";
            }
            Console.WriteLine("=================================");
            str += "\nTotal Roof Area: " + TotalRoofArea();

            return str;
        }

        public double TotalRoofArea()
        {
            double sum = 0;
            foreach (KeyValuePair<int, EffectiveWindArea> kvp in effWindAreas)
            {
                sum += kvp.Value.Area;
            }
            return sum;
        }

        public static bool IsValidRectangle_BuildingLength(double L, double B, double offset)
        {
            return offset > 0 && offset < L;
        }
        public static bool IsValidRectangle_BuildingWidth(double L, double B, double offset)
        {
            return offset > 0 && offset < B;
        }

        public Rect GetBoundingExtents(Dictionary<int, EffectiveWindArea> areas)
        {
            if (areas == null || areas.Count == 0)
                return Rect.Empty;

            double minX = double.MaxValue;
            double maxX = double.MinValue;
            double minY = double.MaxValue;
            double maxY = double.MinValue;

            foreach (var area in areas)
            {
                if (area.Value?.OuterBoundary == null)
                    continue;

                foreach (var pt in area.Value.OuterBoundary)
                {
                    if (pt.X < minX) minX = pt.X;
                    if (pt.X > maxX) maxX = pt.X;
                    if (pt.Y < minY) minY = pt.Y;
                    if (pt.Y > maxY) maxY = pt.Y;
                }
            }

            if (minX == double.MaxValue || minY == double.MaxValue)
                return Rect.Empty; // no valid points found

            double width = maxX - minX;
            double height = maxY - minY;

            return new Rect(minX, minY, width, height);
        }

    }
}
