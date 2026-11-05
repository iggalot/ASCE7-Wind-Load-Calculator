namespace ASCE7WindLoadCalculator
{
    /// <summary>
    /// ASCE 7-16 Figure 30.3-2A
    /// Gable Roofs
    /// h <= 60ft
    /// slope <= 7deg
    /// </summary>
    public class Figure30_3_2A_ASCE7_16 : Chapter27and30_GCpCurveBase
    {
        public override string ChartTitle { get; set; } = "ASCE 7-16 Figure 30.3-2A";
        public override string ChartCriteria { get; set; } = "h <= 60ft, slope <= 7deg";

        public Figure30_3_2A_ASCE7_16()
        {
            // Positive ExternalPressure Roof Zones
            RoofCurves_Pos["Zone3"] = ExternalGCpCurve.ForArea(new (double X, double Y)[]
            {
                (1.0, 0.3),
                (10,  0.3),
                (20, 0.26),
                (50, 0.22),
                (100, 0.2),
                (200, 0.2),
                (500, 0.2),
                (1000, 0.2)
            });
            RoofCurves_Pos["Zone2"] = ExternalGCpCurve.ForArea(new (double X, double Y)[]
            {
                (1.0, 0.3),
                (10,  0.3),
                (20, 0.26),
                (50, 0.22),
                (100, 0.2),
                (200, 0.2),
                (500, 0.2),
                (1000, 0.2)
            });
            RoofCurves_Pos["Zone1"] = ExternalGCpCurve.ForArea(new (double X, double Y)[]
            {
                (1.0, 0.3),
                (10,  0.3),
                (20, 0.26),
                (50, 0.22),
                (100, 0.2),
                (200, 0.2),
                (500, 0.2),
                (1000, 0.2)
            });
            RoofCurves_Pos["Zone1'"] = ExternalGCpCurve.ForArea(new (double X, double Y)[]
            {
                (1.0, 0.3),
                (10,  0.3),
                (20, 0.26),
                (50, 0.22),
                (100, 0.2),
                (200, 0.2),
                (500, 0.2),
                (1000, 0.2)
            });

            // Negative ExternalPressure Roof Zones
            RoofCurves_Neg["Zone3"] = ExternalGCpCurve.ForArea(new (double X, double Y)[]
            {
                (1.0, -3.2),
                (10,  -3.2),
                (20, -2.88),
                (50, -2.48),
                (100, -2.13),
                (200, -1.82),
                (500, -1.4),
                (1000, -1.4)
            });
            RoofCurves_Neg["Zone2"] = ExternalGCpCurve.ForArea(new (double X, double Y)[]
            {
                (1.0, -2.3),
                (10,  -2.3),
                (20, -2.15),
                (50, -1.92),
                (100, -1.78),
                (200, -1.63),
                (500, -1.4),
                (1000, -1.4)
            });
            RoofCurves_Neg["Zone1"] = ExternalGCpCurve.ForArea(new (double X, double Y)[]
            {
                (1.0, -1.7),
                (10,  -1.7),
                (20, -1.6),
                (50, -1.4),
                (100, -1.29),
                (200, -1.18),
                (500, -1.0),
                (1000, -1.0)
            });
            RoofCurves_Neg["Zone1'"] = ExternalGCpCurve.ForArea(new (double X, double Y)[]
            {
                // Fig. 30.3-2A (7-16 p. 336): -0.9 up to 100 sq ft, then a straight line (log area) to -0.4 at 1000
                (1.0, -0.9),
                (100, -0.9),
                (1000, -0.4)
            });


            // Overhang Curves
            OverhangCurves["Zone3"] = ExternalGCpCurve.ForArea(new (double X, double Y)[]
            {
                (1.0, -3.2),
                (10,  -3.2),
                (20, -2.83),
                (50, -2.32),
                (100, -1.97),
                (200, -1.6),
                (500, -1.1),
                (1000, -1.1)
            });
            OverhangCurves["Zone2"] = ExternalGCpCurve.ForArea(new (double X, double Y)[]
            {
                // Fig. 30.3-2A overhang (7-16 p. 336): -2.3 up to 10 sq ft, meets Zones 1/1' at -1.6 at 100, then -1.0 at 500
                (1.0, -2.3),
                (10, -2.3),
                (100, -1.6),
                (500, -1.0),
                (1000, -1.0)
            });
            OverhangCurves["Zone1"] = ExternalGCpCurve.ForArea(new (double X, double Y)[]
            {
                (1.0, -1.7),
                (10,  -1.7),
                (20, -1.65),
                (50, -1.62),
                (100, -1.6),
                (200, -1.32),
                (500, -1.0),
                (1000, -1.0)
            });
            OverhangCurves["Zone1'"] = ExternalGCpCurve.ForArea(new (double X, double Y)[]
            {
                (1.0, -1.7),
                (10,  -1.7),
                (20, -1.65),
                (50, -1.62),
                (100, -1.6),
                (200, -1.32),
                (500, -1.0),
                (1000, -1.0)
            });

        }
    }
}
