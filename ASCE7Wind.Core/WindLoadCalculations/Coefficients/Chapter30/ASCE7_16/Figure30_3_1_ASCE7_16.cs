namespace ASCE7WindLoadCalculator
{
    /// <summary>
    /// ASCE 7-22 Figure 30.3-2A
    /// Gable Roofs
    /// h <= 60ft
    /// slope <= 7deg
    /// </summary>
    public class Figure30_3_1_ASCE7_16 : Chapter27and30_GCpCurveBase
    {
        public override string ChartTitle { get; set; } = "ASCE 7-16 Figure 30.3-1";
        public override string ChartCriteria { get; set; } = "h <= 60ft, Walls";
        public Figure30_3_1_ASCE7_16()
        {
            // Positive ExternalPressure Wall Zones
            WallCurves_Pos["Zone5"] = ExternalGCpCurve.ForArea(new (double X, double Y)[]
            {
                // Fig. 30.3-1 walls (7-16 p. 335, 7-22 p. 317), printed breakpoints, straight on log area
                (1.0, 1.0),
                (10, 1.0),
                (500, 0.7),
                (1000, 0.7)
            });

            WallCurves_Pos["Zone4"] = ExternalGCpCurve.ForArea(new (double X, double Y)[]
            {
                // Fig. 30.3-1 walls (7-16 p. 335, 7-22 p. 317), printed breakpoints, straight on log area
                (1.0, 1.0),
                (10, 1.0),
                (500, 0.7),
                (1000, 0.7)
            });

            // Negative ExternalPressure Wall Zones
            WallCurves_Neg["Zone5"] = ExternalGCpCurve.ForArea(new (double X, double Y)[]
            {
                // Fig. 30.3-1 walls (7-16 p. 335, 7-22 p. 317), printed breakpoints, straight on log area
                (1.0, -1.4),
                (10, -1.4),
                (500, -0.8),
                (1000, -0.8)
            });
            WallCurves_Neg["Zone4"] = ExternalGCpCurve.ForArea(new (double X, double Y)[]
            {
                // Fig. 30.3-1 walls (7-16 p. 335, 7-22 p. 317), printed breakpoints, straight on log area
                (1.0, -1.1),
                (10, -1.1),
                (500, -0.8),
                (1000, -0.8)
            });
        }
    }
}
