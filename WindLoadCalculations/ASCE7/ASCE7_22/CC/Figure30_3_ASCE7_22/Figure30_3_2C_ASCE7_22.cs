namespace ASCE7WindLoadCalculator
{
    /// <summary>
    /// ASCE7-22 Figure 30-3-2C
    /// Gable Roofs
    /// h <= 60ft
    /// 20deg < slope <= 27deg
    /// </summary>
    public class Figure30_3_2C_ASCE7_22 : Chapter27and30_GCpCurveBase
    {
        public override string ChartTitle { get; set; } = "ASCE 7-22 Figure 30-3-2C - Gable Roofs";
        public override string ChartCriteria { get; set; } = "h <= 60ft, 20deg < slope <= 27deg";
        public Figure30_3_2C_ASCE7_22()
        {
            // ---- Roof: Negative ExternalPressure Zones ----
            RoofCurves_Neg["Zone3"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, -3.0),
                (10,  -3.0),
                (20,  -2.52),
                (50,  -1.9),
                (100, -1.4),
                (200, -1.4),
                (500, -1.4),
                (1000, -1.4),
            });
            RoofCurves_Neg["Zone2"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, -2.5),
                (10,  -2.5),
                (20,  -2.12),
                (50,  -1.6),
                (100, -1.2),
                (200, -1.2),
                (500, -1.2),
                (1000, -1.2),
            });
            RoofCurves_Neg["Zone1"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, -1.5),
                (10,  -1.5),
                (20,  -1.33),
                (50,  -1.12),
                (100, -0.95),
                (200, -0.8),
                (500, -0.8),
                (1000, -0.8),
            });

            // ---- Roof: Positive ExternalPressure Zones ----
            RoofCurves_Pos["Zone3"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, 0.6),
                (10,  0.6),
                (20,  0.53),
                (50,  0.45),
                (100, 0.37),
                (200, 0.3),
                (500, 0.3),
                (1000, 0.3),
            });
            RoofCurves_Pos["Zone2"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, 0.6),
                (10,  0.6),
                (20,  0.53),
                (50,  0.45),
                (100, 0.37),
                (200, 0.3),
                (500, 0.3),
                (1000, 0.3),
            });
            RoofCurves_Pos["Zone1"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, 0.6),
                (10,  0.6),
                (20,  0.53),
                (50,  0.45),
                (100, 0.37),
                (200, 0.3),
                (500, 0.3),
                (1000, 0.3),
            });
        }
    }
}
