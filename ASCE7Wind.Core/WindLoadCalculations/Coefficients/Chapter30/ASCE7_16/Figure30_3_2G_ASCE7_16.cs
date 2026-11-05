namespace ASCE7WindLoadCalculator
{
    /// <summary>
    /// ASCE7-16 Figure 30.3-2G
    /// Hip Roofs
    /// h <= 60ft
    /// 20deg < slope <= 27deg
    /// </summary>
    public class Figure30_3_2G_ASCE7_16 : Chapter27and30_GCpCurveBase
    {
        public override string ChartTitle { get; set; } = "ASCE 7-16 Figure 30.3-2G - Hip Roofs";
        public override string ChartCriteria { get; set; } = "h <= 60ft, 20deg < slope <= 27deg";
        public Figure30_3_2G_ASCE7_16()
        {
            // Roof Negative ExternalPressure Zones
            RoofCurves_Neg["Zone3"] = ExternalGCpCurve.ForArea(new (double X, double Y)[]
            {
                // Fig. 30.3-2G (7-16 p. 342), printed breakpoints, straight on log area
                (1.0, -2.0),
                (10, -2.0),
                (200, -1.0),
                (1000, -1.0)
            });
            RoofCurves_Neg["Zone2e"] = ExternalGCpCurve.ForArea(new (double X, double Y)[]
            {
                // Fig. 30.3-2G (7-16 p. 342), printed breakpoints, straight on log area
                (1.0, -2.0),
                (10, -2.0),
                (200, -1.0),
                (1000, -1.0)
            });
            RoofCurves_Neg["Zone2r"] = ExternalGCpCurve.ForArea(new (double X, double Y)[]
            {
                // Fig. 30.3-2G (7-16 p. 342), printed breakpoints, straight on log area
                (1.0, -2.0),
                (10, -2.0),
                (200, -1.0),
                (1000, -1.0)
            });
            RoofCurves_Neg["Zone1"] = ExternalGCpCurve.ForArea(new (double X, double Y)[]
            {
                // Fig. 30.3-2G (7-16 p. 342), printed breakpoints, straight on log area
                (1.0, -1.4),
                (10, -1.4),
                (100, -0.8),
                (1000, -0.8)
            });

            // Roof Positive ExternalPressure Zones
            RoofCurves_Pos["Zone3"] = ExternalGCpCurve.ForArea(new (double X, double Y)[]
            {
                // Fig. 30.3-2G (7-16 p. 342), printed breakpoints, straight on log area
                (1.0, 0.7),
                (10, 0.7),
                (100, 0.3),
                (1000, 0.3)
            });
            RoofCurves_Pos["Zone2r"] = ExternalGCpCurve.ForArea(new (double X, double Y)[]
            {
                // Fig. 30.3-2G (7-16 p. 342), printed breakpoints, straight on log area
                (1.0, 0.7),
                (10, 0.7),
                (100, 0.3),
                (1000, 0.3)
            });
            RoofCurves_Pos["Zone2e"] = ExternalGCpCurve.ForArea(new (double X, double Y)[]
            {
                // Fig. 30.3-2G (7-16 p. 342), printed breakpoints, straight on log area
                (1.0, 0.7),
                (10, 0.7),
                (100, 0.3),
                (1000, 0.3)
            });
            RoofCurves_Pos["Zone1"] = ExternalGCpCurve.ForArea(new (double X, double Y)[]
            {
                // Fig. 30.3-2G (7-16 p. 342), printed breakpoints, straight on log area
                (1.0, 0.7),
                (10, 0.7),
                (100, 0.3),
                (1000, 0.3)
            });

            // Overhang Zones
            OverhangCurves["Zone3"] = ExternalGCpCurve.ForArea(new (double X, double Y)[]
            {
                // Fig. 30.3-2G (7-16 p. 342), printed breakpoints, straight on log area
                (1.0, -3.1),
                (7, -3.1),
                (200, -1.5),
                (1000, -1.5)
            });
            OverhangCurves["Zone2r"] = ExternalGCpCurve.ForArea(new (double X, double Y)[]
            {
                // Fig. 30.3-2G (7-16 p. 342), printed breakpoints, straight on log area
                (1.0, -2.5),
                (7, -2.5),
                (200, -2.0),
                (1000, -2.0)
            });
            OverhangCurves["Zone2e"] = ExternalGCpCurve.ForArea(new (double X, double Y)[]
            {
                // Fig. 30.3-2G (7-16 p. 342), printed breakpoints, straight on log area
                (1.0, -2.5),
                (7, -2.5),
                (200, -2.0),
                (1000, -2.0)
            });
            OverhangCurves["Zone1"] = ExternalGCpCurve.ForArea(new (double X, double Y)[]
            {
                // Fig. 30.3-2G (7-16 p. 342), printed breakpoints, straight on log area
                (1.0, -1.9),
                (7, -1.9),
                (200, -1.8),
                (1000, -1.8)
            });
        } 
    }
}
