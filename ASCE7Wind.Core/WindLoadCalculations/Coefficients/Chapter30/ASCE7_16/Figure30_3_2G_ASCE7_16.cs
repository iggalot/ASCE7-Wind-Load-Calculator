namespace ASCE7WindLoadCalculator
{
    /// <summary>
    /// ASCE7-16 Figure 30-3-2G
    /// Hip Roofs
    /// h <= 60ft
    /// 20deg < slope <= 27deg
    /// </summary>
    public class Figure30_3_2G_ASCE7_16 : Chapter27and30_GCpCurveBase
    {
        public override string ChartTitle { get; set; } = "ASCE 7-16 Figure 30-3-2G - Hip Roofs";
        public override string ChartCriteria { get; set; } = "h <= 60ft, 20deg < slope <= 27deg";
        public Figure30_3_2G_ASCE7_16()
        {
            // Roof Negative ExternalPressure Zones
            RoofCurves_Neg["Zone3"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, -2.0),
                (10, -2.0),
                (20, -1.78),
                (50, -1.44),
                (100, -1.23),
                (200, -1.0),
                (500, -1.0),
                (1000, -1.0)
            });
            RoofCurves_Neg["Zone2e"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, -2.0),
                (10, -2.0),
                (20, -1.78),
                (50, -1.44),
                (100, -1.23),
                (200, -1.0),
                (500, -1.0),
                (1000, -1.0)
            });
            RoofCurves_Neg["Zone2r"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, -2.0),
                (10, -2.0),
                (20, -1.78),
                (50, -1.44),
                (100, -1.23),
                (200, -1.0),
                (500, -1.0),
                (1000, -1.0)
            });
            RoofCurves_Neg["Zone1"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, -1.4),
                (10, -1.4),
                (20, -1.22),
                (50, -1.0),
                (100, -0.8),
                (200, -0.8),
                (500, -0.8),
                (1000, -0.8)
            });

            // Roof Positive ExternalPressure Zones
            RoofCurves_Pos["Zone3"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, 0.7),
                (10, 0.7),
                (20, 0.6),
                (50, 0.38),
                (100, 0.3),
                (200, 0.3),
                (500, 0.3),
                (1000, 0.3)
            });
            RoofCurves_Pos["Zone2r"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, 0.7),
                (10, 0.7),
                (20, 0.6),
                (50, 0.38),
                (100, 0.3),
                (200, 0.3),
                (500, 0.3),
                (1000, 0.3)
            });
            RoofCurves_Pos["Zone2e"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, 0.7),
                (10, 0.7),
                (20, 0.6),
                (50, 0.38),
                (100, 0.3),
                (200, 0.3),
                (500, 0.3),
                (1000, 0.3)
            });
            RoofCurves_Pos["Zone1"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, 0.7),
                (10, 0.7),
                (20, 0.6),
                (50, 0.38),
                (100, 0.3),
                (200, 0.3),
                (500, 0.3),
                (1000, 0.3)
            });

            // Overhang Zones
            OverhangCurves["Zone3"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, -3.1),
                (7, -3.1),
                (10, -2.8),
                (20, -2.5),
                (50, -2.1),
                (100, -1.8),
                (200, -1.5),
                (1000, -1.5)
            });
            OverhangCurves["Zone2r"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, -2.5),
                (7, -2.5),
                (10, -2.4),
                (20, -2.3),
                (50, -2.18),
                (100, -2.1),
                (200, -2.0),
                (1000, -2.0)
            });
            OverhangCurves["Zone2e"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, -2.5),
                (7, -2.5),
                (10, -2.4),
                (20, -2.3),
                (50, -2.18),
                (100, -2.1),
                (200, -2.0),
                (1000, -2.0)
            });
            OverhangCurves["Zone1"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, -1.9),
                (7, -1.9),
                (10, -1.88),
                (20, -1.85),
                (50, -1.83),
                (100, -1.81),
                (200, -1.8),
                (1000, -1.8)
            });
        } 
    }
}
