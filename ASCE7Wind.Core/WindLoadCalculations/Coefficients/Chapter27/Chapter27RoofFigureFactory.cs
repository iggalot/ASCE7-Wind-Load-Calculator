namespace ASCE7WindLoadCalculator
{
    /// <summary>
    /// Selects the Fig. 27.3-1 roof Cp table for the building (identical in ASCE 7-16 and 7-22).
    /// Table rows are given at h/L = 0.25, 0.5 and 1.0 (normal to ridge, theta >= 10 deg) and at
    /// h/L = 0.5 and 1.0 (parallel to ridge, and normal to ridge with theta &lt; 10 deg); Cp is
    /// interpolated linearly in h/L between rows.
    /// </summary>
    public static class Chapter27RoofFigureFactory
    {
        public static Chapter27and30_GCpCurveBase CreateRoofFigure(BuildingData buildingData)
        {
            double slope = buildingData.RoofPitch;
            double hOverL = buildingData.h_Over_L;

            if (buildingData.RoofType == RoofTypes.ROOF_TYPE_HIP)
                return HipRoof(hOverL, slope);

            bool normalToRidge = buildingData.RidgeDirection == RidgeDirections.RIDGE_DIR_PERP_TO_BLDGLENGTH;

            if (normalToRidge && slope >= 10)
                return NormalToRidge(hOverL, slope);

            return ParallelToRidge(hOverL, slope);
        }

        /// <summary>
        /// Hip roofs present both kinds of surface to the wind: planes running along the wind (distance
        /// zones 1-4 of the parallel-to-ridge table) and the hipped planes facing it (windward/leeward roof,
        /// normal-to-ridge table at the roof slope). Fig. 27.3-1 has no separate hip table, so both sets are
        /// combined. Below 10 deg the code uses the distance table for every roof plane; the hipped faces then
        /// take the windward-most (Zone 4) values, which is conservative.
        /// </summary>
        private static Chapter27and30_GCpCurveBase HipRoof(double hOverL, double slope)
        {
            var along = ParallelToRidge(hOverL, slope);
            var combined = new CombinedCpFigure("Figure 27.3-1 (hip roof)", along);

            if (slope >= 10)
            {
                var facing = NormalToRidge(hOverL, slope);
                foreach (var key in new[] { "ZoneWWR", "ZoneLWR" })
                {
                    combined.RoofCurves_Pos[key] = facing.RoofCurves_Pos[key];
                    combined.RoofCurves_Neg[key] = facing.RoofCurves_Neg[key];
                }
            }
            else
            {
                foreach (var key in new[] { "ZoneWWR", "ZoneLWR" })
                {
                    combined.RoofCurves_Pos[key] = along.RoofCurves_Pos["Zone4"];
                    combined.RoofCurves_Neg[key] = along.RoofCurves_Neg["Zone4"];
                }
            }
            return combined;
        }

        private sealed class CombinedCpFigure : Chapter27and30_GCpCurveBase
        {
            public override string ChartTitle { get; set; }
            public override string ChartCriteria { get; set; }

            public CombinedCpFigure(string title, Chapter27and30_GCpCurveBase source)
            {
                ChartTitle = title;
                ChartCriteria = source.ChartCriteria;
                foreach (var kv in source.RoofCurves_Pos) RoofCurves_Pos[kv.Key] = kv.Value;
                foreach (var kv in source.RoofCurves_Neg) RoofCurves_Neg[kv.Key] = kv.Value;
            }
        }

        /// <summary>Wind parallel to ridge (or flat / low-slope roofs): distance-based zones from the windward edge.</summary>
        private static Chapter27and30_GCpCurveBase ParallelToRidge(double hOverL, double slope)
        {
            if (hOverL <= 0.5)
                return new ParallelToRidge_LowSlope_Roof_Low();
            if (hOverL >= 1.0)
                return new ParallelToRidge_LowSlope_Roof_High();
            return new InterpolatedCpCurve(hOverL, slope, 0.5, 1.0,
                new ParallelToRidge_LowSlope_Roof_Low(), new ParallelToRidge_LowSlope_Roof_High());
        }

        /// <summary>Wind normal to ridge, theta >= 10 deg: windward and leeward roof faces, Cp vs slope.</summary>
        private static Chapter27and30_GCpCurveBase NormalToRidge(double hOverL, double slope)
        {
            if (hOverL <= 0.25)
                return new NormToRidge_LargeSlope_Roof_Low(slope);
            if (hOverL < 0.5)
                return new InterpolatedCpCurve(hOverL, slope, 0.25, 0.5,
                    new NormToRidge_LargeSlope_Roof_Low(slope), new NormToRidge_LargeSlope_Roof_Mid(slope));
            if (hOverL < 1.0)
                return new InterpolatedCpCurve(hOverL, slope, 0.5, 1.0,
                    new NormToRidge_LargeSlope_Roof_Mid(slope), new NormToRidge_LargeSlope_Roof_High(slope));
            return new NormToRidge_LargeSlope_Roof_High(slope);
        }
    }
}
