namespace ASCE7WindLoadCalculator
{
    /// <summary>
    /// Contains the values of the Cp curves for ASCE 7-22 Figure 27.3-1
    /// -- h/L >= 1.0
    /// </summary>
    public class NormToRidge_LargeSlope_Roof_High : Chapter27and30_GCpCurveBase
    {
        public override string ChartTitle { get; set; } = "ASCE 7-16 Figure 27.3-1";
        public override string ChartCriteria { get; set; } = "Normal to Ridge for slope >= 10deg  -- h/L = 1.0";
        /// <param name="reductionFactor">Fig. 27.3-1 note b factor on the −1.3 value (1.0 = unreduced).</param>
        public NormToRidge_LargeSlope_Roof_High(double slope, double reductionFactor = 1.0)
        {
            // Positive ExternalPressure Roof Zones
            RoofCurves_Pos["ZoneWWR"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (10, -0.18),
                (15, -0.18),
                (20, -0.18),
                (25, 0.0),
                (30, 0.2),
                (35, 0.2),
                (45, 0.3),
                (60, 0.6),
                (80, 0.8)
            });
            // Negative ExternalPressure Roof Zones
            RoofCurves_Neg["ZoneWWR"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (10, -1.3 * reductionFactor),   // Fig. 27.3-1 note b: reduced with the area it applies to (Chapter27RoofFigureFactory.AreaReductionFactor)
                (15, -1.0),
                (20, -0.7),
                (25, -0.5),
                (30, -0.3),
                (35, -0.2),
                (45, 0.0),
                (60, 0.0),
                (80, 0.0)
            });

            RoofCurves_Neg["ZoneLWR"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (10, -0.7),
                (15, -0.6),
                (20, -0.6),
                (80, -0.6)
            });

            // Fig. 27.3-1 gives one leeward-roof value; it applies to both load cases.
            RoofCurves_Pos["ZoneLWR"] = RoofCurves_Neg["ZoneLWR"];
        }

    }
}
