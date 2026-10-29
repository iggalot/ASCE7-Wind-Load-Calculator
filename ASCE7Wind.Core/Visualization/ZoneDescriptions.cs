namespace ASCE7WindLoadCalculator
{
    /// <summary>Plain-language names for ASCE 7 wind zones, for readers who are not engineers.</summary>
    public static class ZoneDescriptions
    {
        public static string Describe(string label, SurfaceKind kind, WindLoadCalculationTypes analysis)
        {
            string id = label != null && label.StartsWith("Zone") ? label.Substring(4) : label;

            if (analysis == WindLoadCalculationTypes.MWFRS)
            {
                switch (id)
                {
                    case "WW": return "Windward wall (wind pushes on it)";
                    case "LW": return "Leeward wall (wind pulls on it)";
                    case "SW": return "Side wall (wind pulls on it)";
                    case "WWR": return "Windward roof slope";
                    case "LWR": return "Leeward roof slope";
                    case "4": return "Roof nearest the windward edge (0 to h/2)";
                    case "3": return "Roof h/2 to h from the windward edge";
                    case "2": return "Roof h to 2h from the windward edge";
                    case "1": return "Roof more than 2h from the windward edge";
                }
                return "Zone " + id;
            }

            if (kind == SurfaceKind.Wall)
            {
                switch (id)
                {
                    case "5": return "Wall corner strip (highest wall suction)";
                    case "4": return "Wall field (middle of the wall)";
                }
                return "Wall zone " + id;
            }

            switch (id)
            {
                case "1'": return "Roof interior (lowest uplift)";
                case "1": return "Roof field";
                case "2": return "Roof edge strip";
                case "2e": return "Roof edge strip (zone 2e)";
                case "2n": return "Roof edge strip (zone 2n)";
                case "2r": return "Roof edge strip along the ridge (zone 2r)";
                case "3": return "Roof corner (highest uplift)";
                case "3e": return "Roof corner (zone 3e, highest uplift)";
                case "3r": return "Roof corner at the ridge (zone 3r)";
            }
            return "Roof zone " + id;
        }
    }
}
