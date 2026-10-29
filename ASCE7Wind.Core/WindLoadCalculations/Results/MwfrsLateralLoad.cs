using System;

namespace ASCE7WindLoadCalculator
{
    /// <summary>
    /// Lateral (horizontal) wind load on the building from MWFRS wall pressures, for the shear-wall design.
    /// </summary>
    public static class MwfrsLateralLoad
    {
        /// <summary>
        /// Base shear (kips) in the wind direction from windward and leeward wall pressures:
        /// V = sum(p_WW A) - sum(p_LW A), using external pressures only.
        /// Internal pressure acts equally and oppositely on the windward and leeward walls of a closed
        /// building, so it cancels and must not be added.
        /// Not yet included (see roadmap M6): horizontal roof components, the 16 psf minimum (27.1.5)
        /// and the torsional load cases of Fig. 27.3-8.
        /// </summary>
        public static double WallBaseShearKips(WindLoadCalculator_Base calculator)
        {
            if (!(calculator is WindLoadCalculator_MWFRS))
                throw new ArgumentException("Base shear needs an MWFRS calculator.", nameof(calculator));

            var walls = calculator.WallAreaCalculator_BldgLength;
            if (walls == null) return 0;

            double pounds = 0;
            foreach (var zone in walls.effWindAreas)
            {
                if (!calculator.windPressureWall_Pos_External_MWFRS.TryGetValue(zone.Key, out var p))
                    continue;

                switch (zone.Value.Label_Full)
                {
                    case "ZoneWW": pounds += p.ExternalPressure * zone.Value.Area; break; // pressure toward the building
                    case "ZoneLW": pounds -= p.ExternalPressure * zone.Value.Area; break; // suction away from it: same direction
                }
            }
            return pounds / 1000.0;
        }
    }
}
