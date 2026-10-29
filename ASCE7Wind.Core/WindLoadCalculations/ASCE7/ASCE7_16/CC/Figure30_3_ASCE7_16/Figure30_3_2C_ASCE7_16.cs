namespace ASCE7WindLoadCalculator
{
    /// <summary>
    /// ASCE7-16 Figure 30-3-2C
    /// Gable Roofs
    /// h <= 60ft
    /// 20deg < slope <= 27deg
    /// </summary>
    public class Figure30_3_2C_ASCE7_16 : Chapter27and30_GCpCurveBase
    {
        public override string ChartTitle { get; set; } = "ASCE 7-16 Figure 30-3-2C - Gable Roofs";
        public override string ChartCriteria { get; set; } = "h <= 60ft, 20deg < slope <= 27deg";
        public Figure30_3_2C_ASCE7_16()
        {
            // ---- Roof: Negative ExternalPressure Zones ----
            RoofCurves_Neg["Zone3r"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, -3.6),
                (4,  -3.6),
                (10, -2.73),
                (20, -2.35),
                (50, -1.8),
                (200, -1.8),
                (500, -1.8),
                (1000, -1.8)
            });
            RoofCurves_Neg["Zone3e"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, -2.5),
                (10,  -2.5),
                (20, -2.15),
                (50, -1.7),
                (100, -1.38),
                (150, -1.2),
                (500, -1.2),
                (1000, -1.2)
            });
            RoofCurves_Neg["Zone2n"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, -2.5),
                (10,  -2.5),
                (20, -2.15),
                (50, -1.7),
                (100, -1.38),
                (150, -1.2),
                (500, -1.2),
                (1000, -1.2)
            });
            RoofCurves_Neg["Zone2r"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, -2.5),
                (10,  -2.5),
                (20, -2.15),
                (50, -1.7),
                (100, -1.38),
                (150, -1.2),
                (500, -1.2),
                (1000, -1.2)
            });
            RoofCurves_Neg["Zone2e"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, -1.5),
                (10,  -1.5),
                (20, -1.5),
                (50, -1.25),
                (100, -1.06),
                (200, -0.89),
                (300, -0.8),
                (1000, -0.8)
            });
            RoofCurves_Neg["Zone1"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, -1.5),
                (10,  -1.5),
                (20, -1.5),
                (50, -1.25),
                (100, -1.06),
                (200, -0.89),
                (300, -0.8),
                (1000, -0.8)
            });

            // ---- Roof: Positive ExternalPressure Zones ----
            RoofCurves_Pos["Zone3r"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, 0.7),
                (2,  0.7),
                (10, 0.53),
                (20, 0.45),
                (50, 0.38),
                (100, 0.3),
                (500, 0.3),
                (1000, 0.3)
            });
            RoofCurves_Pos["Zone3e"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, 0.7),
                (2,  0.7),
                (10, 0.53),
                (20, 0.45),
                (50, 0.38),
                (100, 0.3),
                (500, 0.3),
                (1000, 0.3)
            });
            RoofCurves_Pos["Zone2n"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, 0.7),
                (2,  0.7),
                (10, 0.53),
                (20, 0.45),
                (50, 0.38),
                (100, 0.3),
                (500, 0.3),
                (1000, 0.3)
            });
            RoofCurves_Pos["Zone2r"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, 0.7),
                (2,  0.7),
                (10, 0.53),
                (20, 0.45),
                (50, 0.38),
                (100, 0.3),
                (500, 0.3),
                (1000, 0.3)
            });
            RoofCurves_Pos["Zone2e"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, 0.7),
                (2,  0.7),
                (10, 0.53),
                (20, 0.45),
                (50, 0.38),
                (100, 0.3),
                (500, 0.3),
                (1000, 0.3)
            });
            RoofCurves_Pos["Zone1"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, 0.7),
                (2,  0.7),
                (10, 0.53),
                (20, 0.45),
                (50, 0.38),
                (100, 0.3),
                (500, 0.3),
                (1000, 0.3)
            });

            // ---- Overhang: Negative ExternalPressure Zones ----
            OverhangCurves["Zone3r"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, -4.7),
                (4,  -4.7),
                (10, -3.6),
                (20, -3.1),
                (50, -2.4),
                (100, -2.4),
                (200, -2.4),
                (1000, -2.4)
            });
            OverhangCurves["Zone3e"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, -3.6),
                (10,  -3.6),
                (20, -3.08),
                (50, -2.4),
                (100, -1.97),
                (150, -1.7),
                (500, -1.7),
                (1000, -1.7)
            });
            OverhangCurves["Zone2n"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, -3.0),
                (10,  -3.0),
                (20, -2.8),
                (50, -2.52),
                (100, -2.35),
                (150, -2.2),
                (500, -2.2),
                (1000, -2.2)
            });
            OverhangCurves["Zone2r"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, -3.0),
                (10,  -3.0),
                (20, -2.8),
                (50, -2.52),
                (100, -2.35),
                (150, -2.2),
                (500, -2.2),
                (1000, -2.2)
            });

            OverhangCurves["Zone2e"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, -2.0),
                (10,  -2.0),
                (20, -2.0),
                (50, -1.95),
                (100, -1.9),
                (150, -1.85),
                (350, -1.8),
                (1000, -1.8)
            });
            OverhangCurves["Zone1"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, -2.0),
                (10,  -2.0),
                (20, -2.0),
                (50, -1.95),
                (100, -1.9),
                (150, -1.85),
                (350, -1.8),
                (1000, -1.8)
            });
        }
    }
}
