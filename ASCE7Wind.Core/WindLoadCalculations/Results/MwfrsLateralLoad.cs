using System;
using System.Collections.Generic;

namespace ASCE7WindLoadCalculator
{
    /// <summary>
    /// Lateral (horizontal) wind load on the building from MWFRS wall pressures, for the shear-wall design.
    /// </summary>
    public static class MwfrsLateralLoad
    {
        /// <summary>Height of the strips used to integrate the windward-wall pressure over its height (ft).</summary>
        public const double StripHeight = 0.25;

        /// <summary>
        /// Base shear (kips) in the wind direction from windward and leeward wall pressures:
        /// V = integral of p_WW(z) over the windward wall + |p_LW| x leeward wall area, external pressures only.
        /// The windward wall uses qz at each height (27.3); the leeward wall uses qh. Internal pressure acts
        /// equally and oppositely on the windward and leeward walls of a closed building, so it cancels.
        /// Not yet included (roadmap M6): horizontal roof components, the 16 psf minimum (27.1.5) and the
        /// torsional load cases of Fig. 27.3-8.
        /// </summary>
        public static double WallBaseShearKips(WindLoadCalculator_Base calculator)
        {
            if (!(calculator is WindLoadCalculator_MWFRS mwfrs))
                throw new ArgumentException("Base shear needs an MWFRS calculator.", nameof(calculator));

            var walls = calculator.WallAreaCalculator_BldgLength;
            if (walls == null) return 0;

            double pounds = 0;
            foreach (var zone in walls.effWindAreas)
            {
                switch (zone.Value.Label_Full)
                {
                    case "ZoneWW":
                        pounds += IntegrateOverHeight(zone.Value.OuterBoundary, mwfrs.WindwardWallExternalPressureAt);
                        break;
                    case "ZoneLW":
                        if (calculator.windPressureWall_Pos_External_MWFRS.TryGetValue(zone.Key, out var p))
                            pounds -= p.ExternalPressure * zone.Value.Area; // suction away from the building: same direction
                        break;
                }
            }
            return pounds / 1000.0;
        }

        /// <summary>
        /// The part of <see cref="WallBaseShearKips"/> from the wall area between heights <paramref name="bottom"/> and
        /// <paramref name="top"/> (ft; use double.PositiveInfinity for the top of the wall, gable included): a horizontal strip
        /// of the walls, for the wind on one floor diaphragm. The windward wall is integrated in the same 0.25 ft strips as
        /// the base shear, each strip going to the band holding its mid-height, so the bands add up exactly to the base shear.
        /// </summary>
        public static double WallShearKipsBetween(WindLoadCalculator_Base calculator, double bottom, double top)
        {
            if (!(calculator is WindLoadCalculator_MWFRS mwfrs))
                throw new ArgumentException("Base shear needs an MWFRS calculator.", nameof(calculator));
            var walls = calculator.WallAreaCalculator_BldgLength;
            if (walls == null) return 0;

            bool In(double z) => z >= bottom && z < top;
            double pounds = 0;
            foreach (var zone in walls.effWindAreas)
            {
                switch (zone.Value.Label_Full)
                {
                    case "ZoneWW":
                        pounds += IntegrateOverHeight(zone.Value.OuterBoundary, z => In(z) ? mwfrs.WindwardWallExternalPressureAt(z) : 0);
                        break;
                    case "ZoneLW":
                        if (calculator.windPressureWall_Pos_External_MWFRS.TryGetValue(zone.Key, out var p))
                            pounds -= p.ExternalPressure * AreaBetween(zone.Value.OuterBoundary, bottom, top);
                        break;
                }
            }
            return pounds / 1000.0;
        }

        /// <summary>Area of a wall polygon (along, up frame) between two heights.</summary>
        public static double AreaBetween(IReadOnlyList<Vec2> wall, double bottom, double top)
        {
            double left = double.MaxValue, right = double.MinValue, low = double.MaxValue, high = double.MinValue;
            foreach (var p in wall) { left = Math.Min(left, p.X); right = Math.Max(right, p.X); low = Math.Min(low, p.Y); high = Math.Max(high, p.Y); }
            double b = Math.Max(bottom, low - 1), t = Math.Min(top, high + 1);
            if (t <= b) return 0;
            var band = new List<Vec2> { new Vec2(left - 1, b), new Vec2(right + 1, b), new Vec2(right + 1, t), new Vec2(left - 1, t) };
            var piece = PolygonOps.ClipToConvex(PolygonOps.CounterClockwise(wall), band);
            return piece.Count < 3 ? 0 : Math.Abs(PolygonOps.SignedArea(piece));
        }

        /// <summary>Integral of p(z) dA over a convex wall polygon in its (along, up) frame.</summary>
        public static double IntegrateOverHeight(IReadOnlyList<Vec2> wall, Func<double, double> pressureAtZ)
        {
            double top = double.MinValue, bottom = double.MaxValue, left = double.MaxValue, right = double.MinValue;
            foreach (var p in wall)
            {
                top = Math.Max(top, p.Y); bottom = Math.Min(bottom, p.Y);
                left = Math.Min(left, p.X); right = Math.Max(right, p.X);
            }

            double sum = 0;
            for (double z0 = bottom; z0 < top - 1e-9; z0 += StripHeight)
            {
                double z1 = Math.Min(top, z0 + StripHeight);
                var strip = new List<Vec2> { new Vec2(left - 1, z0), new Vec2(right + 1, z0), new Vec2(right + 1, z1), new Vec2(left - 1, z1) };
                var piece = PolygonOps.ClipToConvex(PolygonOps.CounterClockwise(wall), strip);
                if (piece.Count < 3) continue;
                sum += Math.Abs(PolygonOps.SignedArea(piece)) * pressureAtZ(0.5 * (z0 + z1));
            }
            return sum;
        }
    }
}
