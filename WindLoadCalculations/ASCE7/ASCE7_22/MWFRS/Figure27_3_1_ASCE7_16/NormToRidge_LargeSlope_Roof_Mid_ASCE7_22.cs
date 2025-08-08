namespace ASCE7WindLoadCalculator
{
    /// <summary>
    /// Contains the values of the Cp curves for ASCE 7-22 Figure 27.3-1
    /// -- h/L = 0.50
    /// </summary>
    public class NormToRidge_LargeSlope_Roof_Mid_ASCE7_22 : Chapter27and30_GCpCurveBase
    {
        public override string ChartTitle { get; set; } = "ASCE 7-16 Figure 27.3-1";
        public override string ChartCriteria { get; set; } = "Normal to Ridge for slope >= 10deg  -- h/L = 0.5;";

        public NormToRidge_LargeSlope_Roof_Mid_ASCE7_22(double slope)
        {
            // Positive ExternalPressure Roof Zones
            RoofCurves_Pos["ZoneWWR"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (10,  -0.18),
                (15, -0.18),
                (20, 0.0),
                (25, 0.2),
                (30, 0.2),
                (35, 0.3),
                (45, 0.4),
                (60, 0.6),
                (80, 0.8)
            });
            // Negative ExternalPressure Roof Zones
            RoofCurves_Neg["ZoneWWR"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (10, -0.9),
                (15, -0.7),
                (20, -0.4),
                (25, -0.3),
                (30, -0.2),
                (35, -0.2),
                (45, 0.0),
                (60, 0.0),
                (80, 0.0)
            });

            RoofCurves_Neg["ZoneLWR"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (10, -0.5),
                (15, -0.5),
                (20, -0.6),
                (80, -0.6)
            });
        }
    }
}
