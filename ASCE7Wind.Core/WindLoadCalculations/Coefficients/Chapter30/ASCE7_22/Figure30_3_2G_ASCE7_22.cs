namespace ASCE7WindLoadCalculator
{
    /// <summary>
    /// ASCE 7-22 Figure 30.3-2G
    /// Hip Roofs
    /// h <= 60ft
    /// slope = 45 deg
    /// </summary>
    public class Figure30_3_2G_ASCE7_22 : Chapter27and30_GCpCurveBase
    {
        public override string ChartTitle { get; set; } = "ASCE 7-22 Figure 30.3-2G - Hip Roofs";
        public override string ChartCriteria { get; set; } = "h <= 60ft, slope = 45deg";
        public Figure30_3_2G_ASCE7_22()
        {
            // Roof Negative ExternalPressure Zones
            RoofCurves_Neg["Zone3"] = ExternalGCpCurve.ForArea(new (double X, double Y)[]
            {
                // Fig. 30.3-2G (7-22 p. 325), printed breakpoints, straight on log area
                (1.0, -2.4),
                (10, -2.4),
                (100, -1.0),
                (1000, -1.0)
            });
            RoofCurves_Neg["Zone2"] = ExternalGCpCurve.ForArea(new (double X, double Y)[]
            {
                // Fig. 30.3-2G (7-22 p. 325), printed breakpoints, straight on log area
                (1.0, -1.8),
                (10, -1.8),
                (100, -0.8),
                (1000, -0.8)
            });
            RoofCurves_Neg["Zone1"] = ExternalGCpCurve.ForArea(new (double X, double Y)[]
            {
                // Fig. 30.3-2G (7-22 p. 325), printed breakpoints, straight on log area
                (1.0, -1.5),
                (10, -1.5),
                (100, -0.7),
                (1000, -0.7)
            });

            // Roof Positive ExternalPressure Zones
            RoofCurves_Pos["Zone3"] = ExternalGCpCurve.ForArea(new (double X, double Y)[]
            {
                // Fig. 30.3-2G (7-22 p. 325), printed breakpoints, straight on log area
                (1.0, 0.7),
                (10, 0.7),
                (100, 0.3),
                (1000, 0.3)
            });
            RoofCurves_Pos["Zone2"] = ExternalGCpCurve.ForArea(new (double X, double Y)[]
            {
                // Fig. 30.3-2G (7-22 p. 325), printed breakpoints, straight on log area
                (1.0, 0.7),
                (10, 0.7),
                (100, 0.3),
                (1000, 0.3)
            });
            RoofCurves_Pos["Zone1"] = ExternalGCpCurve.ForArea(new (double X, double Y)[]
            {
                // Fig. 30.3-2G (7-22 p. 325), printed breakpoints, straight on log area
                (1.0, 0.7),
                (10, 0.7),
                (100, 0.3),
                (1000, 0.3)
            });
        } 
    }
}
