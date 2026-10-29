namespace ASCE7WindLoadCalculator
{
    /// <summary>
    /// ASCE 7-22 Figure 30-3-2A
    /// Gable Roofs
    /// h <= 60ft
    /// slope <= 7deg
    /// </summary>
    public class Figure30_3_1_ASCE7_22 : Chapter27and30_GCpCurveBase
    {
        public override string ChartTitle { get; set; } = "ASCE 7-22 Figure 30-3-1";
        public override string ChartCriteria { get; set; } = "h <= 60ft, Walls";

        public Figure30_3_1_ASCE7_22()
        {
            // Positive ExternalPressure Wall Zones
            WallCurves_Pos["Zone5"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, 1.0),
                (10,  1.0),
                (20, 0.94),
                (50, 0.83),
                (100, 0.81),
                (200, 0.78),
                (500, 0.7),
                (1000, 0.7)
            });

            WallCurves_Pos["Zone4"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, 1.0),
                (10,  1.0),
                (20, 0.94),
                (50, 0.83),
                (100, 0.81),
                (200, 0.78),
                (500, 0.7),
                (1000, 0.7)
            });

            // Negative ExternalPressure Wall Zones
            WallCurves_Neg["Zone5"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, -1.4),
                (10, -1.4),
                (20, -1.3),
                (50, -1.15),
                (100, -1.05),
                (200, -0.93),
                (500, -0.8),
                (1000, -0.8)
            });
            WallCurves_Neg["Zone4"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, -1.1),
                (10, -1.1),
                (20, -1.05),
                (50, -0.97),
                (100, -0.93),
                (200, -0.85),
                (500, -0.8),
                (1000, -0.8)
            });
        }
    }
}
