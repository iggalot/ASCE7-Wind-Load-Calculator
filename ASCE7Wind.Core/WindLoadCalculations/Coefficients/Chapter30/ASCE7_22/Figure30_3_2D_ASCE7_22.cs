namespace ASCE7WindLoadCalculator
{
    /// <summary>
    /// ASCE7-22 Figure 30-3-2D
    /// Gable Roofs
    /// h <= 60ft
    /// 27deg < slope <= 45deg
    /// </summary>
    public class Figure30_3_2D_ASCE7_22 : Chapter27and30_GCpCurveBase
    {
        public override string ChartTitle { get; set; } = "ASCE 7-22 Figure 30-3-2D - Gable Roofs";
        public override string ChartCriteria { get; set; } = "h <= 60ft, 27deg < slope <= 45deg";
        public Figure30_3_2D_ASCE7_22()
        {
            // ---- Roof: Negative ExternalPressure Zones ----
            RoofCurves_Neg["Zone3"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, -2.5),
                (10,  -2.5),
                (20,  -2.15),
                (50,  -1.7),
                (100, -1.35),
                (200, -1.0),
                (500, -1.0),
                (1000, -1.0),
            });
            RoofCurves_Neg["Zone2"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, -2.0),
                (10,  -2.0),
                (20,  -1.77),
                (50,  -1.45),
                (100, -1.25),
                (200, -1.0),
                (500, -1.0),
                (1000, -1.0),
            });
            RoofCurves_Neg["Zone1"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, -1.8),
                (10,  -1.8),
                (20,  -1.5),
                (50,  -1.1),
                (100, -0.8),
                (200, -0.8),
                (500, -0.8),
                (1000, -0.8),
            });

            // ---- Roof: Positive ExternalPressure Zones ----
            RoofCurves_Pos["Zone3"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, 0.9),
                (10,  0.9),
                (20,  0.8),
                (50,  0.68),
                (100, 0.58),
                (200, 0.5),
                (500, 0.5),
                (1000, 0.5),
            });
            RoofCurves_Pos["Zone2"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, 0.9),
                (10,  0.9),
                (20,  0.8),
                (50,  0.68),
                (100, 0.58),
                (200, 0.5),
                (500, 0.5),
                (1000, 0.5),
            });
            RoofCurves_Pos["Zone1"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, 0.9),
                (10,  0.9),
                (20,  0.8),
                (50,  0.68),
                (100, 0.58),
                (200, 0.5),
                (500, 0.5),
                (1000, 0.5),
            });
        }
    }
}
