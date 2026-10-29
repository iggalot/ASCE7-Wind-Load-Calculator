namespace ASCE7WindLoadCalculator
{
    /// <summary>
    /// ASCE7-22 Figure 30-3-2B
    /// Gable Roofs
    /// h <= 60ft
    /// 7deg < slope <= 20deg
    /// </summary>
    public class Figure30_3_2B_ASCE7_22 : Chapter27and30_GCpCurveBase
    {
        public override string ChartTitle { get; set; } = "ASCE 7-22 Figure 30-3-2B - Gable Roofs";
        public override string ChartCriteria { get; set; } = "h <= 60ft, 7deg < slope <= 20deg";
        public Figure30_3_2B_ASCE7_22()
        {
            // ---- Roof: Negative ExternalPressure Zones ----
            RoofCurves_Neg["Zone3"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, -3.6),
                (10,  -3.6),
                (20, -3.05),
                (50, -2.35),
                (100, -1.8),
                (200, -1.8),
                (500, -1.8),
                (1000, -1.8),
            });
            RoofCurves_Neg["Zone2"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, -2.7),
                (10,  -2.7),
                (20, -2.3),
                (50, -1.75),
                (100, -1.4),
                (200, -1.0),
                (500, -1.0),
                (1000, -1.0),
            });
            RoofCurves_Neg["Zone1"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, -2.0),
                (10,  -2.0),
                (20, -1.7),
                (50, -1.3),
                (100, -1.0),
                (200, -0.68),
                (300, -0.5),
                (1000, -0.5),
            });

            // ---- Roof: Positive ExternalPressure Zones ----
            RoofCurves_Pos["Zone3"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, 0.6),
                (10,  0.6),
                (20,  0.55),
                (50,  0.43),
                (100, 0.35),
                (200, 0.3),
                (500, 0.3),
                (1000, 0.3),
            });
            RoofCurves_Pos["Zone2"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, 0.6),
                (10,  0.6),
                (20,  0.55),
                (50,  0.43),
                (100, 0.35),
                (200, 0.3),
                (500, 0.3),
                (1000, 0.3),
            });
            RoofCurves_Pos["Zone1"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, 0.6),
                (10,  0.6),
                (20,  0.55),
                (50,  0.43),
                (100, 0.35),
                (200, 0.3),
                (500, 0.3),
                (1000, 0.3),
            });
        }
    }
}

