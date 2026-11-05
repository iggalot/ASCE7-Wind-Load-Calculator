namespace ASCE7WindLoadCalculator
{
    /// <summary>
    /// ASCE7-22 Figure 30.3-2B
    /// Gable Roofs
    /// h <= 60ft
    /// 7deg < slope <= 20deg
    /// </summary>
    public class Figure30_3_2B_ASCE7_22 : Chapter27and30_GCpCurveBase
    {
        public override string ChartTitle { get; set; } = "ASCE 7-22 Figure 30.3-2B - Gable Roofs";
        public override string ChartCriteria { get; set; } = "h <= 60ft, 7deg < slope <= 20deg";
        public Figure30_3_2B_ASCE7_22()
        {
            // ---- Roof: Negative ExternalPressure Zones ----
            RoofCurves_Neg["Zone3"] = ExternalGCpCurve.ForArea(new (double X, double Y)[]
            {
                // Fig. 30.3-2B (7-22 p. 320), printed breakpoints, straight on log area
                (1.0, -3.6),
                (10, -3.6),
                (100, -1.8),
                (1000, -1.8)
            });
            RoofCurves_Neg["Zone2"] = ExternalGCpCurve.ForArea(new (double X, double Y)[]
            {
                // Fig. 30.3-2B (7-22 p. 320), printed breakpoints, straight on log area
                (1.0, -2.7),
                (10, -2.7),
                (200, -1.0),
                (1000, -1.0)
            });
            RoofCurves_Neg["Zone1"] = ExternalGCpCurve.ForArea(new (double X, double Y)[]
            {
                // Fig. 30.3-2B (7-22 p. 320), printed breakpoints, straight on log area
                (1.0, -2.0),
                (10, -2.0),
                (300, -0.5),
                (1000, -0.5)
            });

            // ---- Roof: Positive ExternalPressure Zones ----
            RoofCurves_Pos["Zone3"] = ExternalGCpCurve.ForArea(new (double X, double Y)[]
            {
                // Fig. 30.3-2B (7-22 p. 320), printed breakpoints, straight on log area
                (1.0, 0.6),
                (10, 0.6),
                (200, 0.3),
                (1000, 0.3)
            });
            RoofCurves_Pos["Zone2"] = ExternalGCpCurve.ForArea(new (double X, double Y)[]
            {
                // Fig. 30.3-2B (7-22 p. 320), printed breakpoints, straight on log area
                (1.0, 0.6),
                (10, 0.6),
                (200, 0.3),
                (1000, 0.3)
            });
            RoofCurves_Pos["Zone1"] = ExternalGCpCurve.ForArea(new (double X, double Y)[]
            {
                // Fig. 30.3-2B (7-22 p. 320), printed breakpoints, straight on log area
                (1.0, 0.6),
                (10, 0.6),
                (200, 0.3),
                (1000, 0.3)
            });
        }
    }
}

