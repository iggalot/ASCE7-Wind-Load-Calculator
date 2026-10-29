namespace ASCE7WindLoadCalculator
{
    public class NormToRidge_LargeSlope_Roof_Low_ASCE7_16 : Chapter27and30_GCpCurveBase
    {
        public override string ChartTitle { get; set; } = "ASCE 7-16 Figure 27.3-1"; 
        public override string ChartCriteria { get; set; } = "Normal to Ridge for slope >= 10deg  -- h/L <= 0.25;";

        public NormToRidge_LargeSlope_Roof_Low_ASCE7_16(double slope)
        {
            // Positive ExternalPressure Roof Zones
            RoofCurves_Pos["ZoneWWR"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (10,  -0.18),
                (15, 0.0),
                (20, 0.2),
                (25, 0.3),
                (30, 0.3),
                (35, 0.4),
                (45, 0.4),
                (60, 0.01*slope),
                (80, 0.8)
            });
            // Negative ExternalPressure Roof Zones
            RoofCurves_Neg["ZoneWWR"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (10, -0.7),
                (15, -0.5),
                (20, -0.3),
                (25, -0.2),
                (30, -0.2),
                (35, 0.0),
                (45, 0.0),
                (60, 0.0),
                (80, 0.0)
            });

            RoofCurves_Neg["ZoneLWR"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (10, -0.3),
                (15, -0.5),
                (20, -0.6),
                (80, -0.6)
            });
        }
    }
}
