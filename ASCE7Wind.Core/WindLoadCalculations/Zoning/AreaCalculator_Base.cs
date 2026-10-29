using System;
using System.Collections.Generic;
using System.Text;

namespace ASCE7WindLoadCalculator
{
    /// <summary>
    /// Base for all zone (effective wind area) generators. Each derived class lays out the
    /// zone polygons for one surface (roof or wall) in that surface's local 2D frame, in feet.
    /// </summary>
    public abstract class AreaCalculator_Base
    {
        public BuildingData buildingData { get; set; }

        /// <summary>True when the zoning uses the Chapter 30 edge-strip width "a".</summary>
        public virtual bool HasCritDim => false;

        /// <summary>Chapter 30 edge-strip width "a" (ft); 0 when not applicable.</summary>
        public virtual double CritDim_a => 0;

        /// <summary>Zones keyed by id. Ids are stable within a calculator and used to join to pressures.</summary>
        public Dictionary<int, EffectiveWindArea> effWindAreas { get; } = new Dictionary<int, EffectiveWindArea>();

        /// <summary>Free-text note, e.g. "gable end wall".</summary>
        public virtual string Note { get; set; } = string.Empty;

        public abstract void ComputeEffectiveWindAreas();

        /// <summary>Zones smaller than this (ft²) are treated as empty and dropped.</summary>
        public const double MinZoneArea = 1e-6;

        /// <summary>
        /// Lays out the zones, then drops empty ones (very small or narrow buildings can make a zone shrink
        /// to nothing). Returns false, without throwing, when the layout could not be completed; the zones
        /// laid out so far are kept.
        /// </summary>
        public bool Compute()
        {
            effWindAreas.Clear();
            bool ok = true;
            try
            {
                ComputeEffectiveWindAreas();
            }
            catch (ArgumentException) { ok = false; }
            catch (InvalidOperationException) { ok = false; }

            var empty = new List<int>();
            foreach (var kv in effWindAreas)
                if (kv.Value == null || kv.Value.IsDegenerate || kv.Value.Area < MinZoneArea)
                    empty.Add(kv.Key);
            foreach (var id in empty)
                effWindAreas.Remove(id);
            return ok;
        }

        /// <summary>
        /// Creates a zone if it has positive area; otherwise returns null (degenerate zones are skipped).
        /// </summary>
        public static EffectiveWindArea TryCreateZone(int id, string label, IEnumerable<Vec2> outer, IEnumerable<IEnumerable<Vec2>> holes = null)
        {
            try
            {
                var zone = new EffectiveWindArea(label, outer, holes);
                return zone.Area > 0 ? zone : null;
            }
            catch (ArgumentException)
            {
                return null; // fewer than 3 distinct vertices
            }
        }

        public string DisplayResults()
        {
            var sb = new StringBuilder();
            foreach (var kvp in effWindAreas)
                sb.Append(kvp.Value.DisplayResults()).Append('\n');
            sb.Append("\nTotal Roof Area: ").Append(TotalRoofArea());
            return sb.ToString();
        }

        public double TotalRoofArea()
        {
            double sum = 0;
            foreach (var kvp in effWindAreas)
                sum += kvp.Value.Area;
            return sum;
        }

        public static bool IsValidRectangle_BuildingLength(double L, double B, double offset) => offset > 0 && offset < L;

        public static bool IsValidRectangle_BuildingWidth(double L, double B, double offset) => offset > 0 && offset < B;

        public Bounds2 GetBoundingExtents(Dictionary<int, EffectiveWindArea> areas)
        {
            if (areas == null || areas.Count == 0)
                return Bounds2.Empty;

            double minX = double.MaxValue, maxX = double.MinValue;
            double minY = double.MaxValue, maxY = double.MinValue;

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
                return Bounds2.Empty;

            return new Bounds2(minX, minY, maxX - minX, maxY - minY);
        }
    }
}
