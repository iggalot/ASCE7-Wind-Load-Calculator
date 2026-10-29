namespace ASCE7WindLoadCalculator
{
    /// <summary>
    /// ASCE7-16 Figure 30-3-2D
    /// Gable Roofs
    /// h <= 60ft
    /// 27deg < slope <= 45deg
    /// </summary>
    public class Figure30_3_2D_ASCE7_16 : Chapter27and30_GCpCurveBase
    {
        public override string ChartTitle { get; set; } = "ASCE 7-16 Figure 30-3-2D - Gable Roofs";
        public override string ChartCriteria { get; set; } = "h <= 60ft, 27deg < slope <= 45deg";
        public Figure30_3_2D_ASCE7_16()
        {
            // ---- Roof: Negative ExternalPressure Zones ----
            RoofCurves_Neg["Zone3e"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, -3.2),
                (2,  -3.2),
                (10, -2.5),
                (20, -2.2),
                (50, -1.8),
                (100, -1.5),
                (300, -1.0),
                (1000, -1.0)
            });
            RoofCurves_Neg["Zone3r"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, -2.0),
                (10,  -2.0),
                (20, -1.77),
                (50, -1.45),
                (100, -1.23),
                (200, -1.0),
                (500, -1.0),
                (1000, -1.0)
            });
            RoofCurves_Neg["Zone2n"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, -2.0),
                (10,  -2.0),
                (20, -1.77),
                (50, -1.45),
                (100, -1.23),
                (200, -1.0),
                (500, -1.0),
                (1000, -1.0)
            });
            RoofCurves_Neg["Zone2r"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, -1.8),
                (10,  -1.8),
                (20, -1.5),
                (50, -1.1),
                (100, -0.8),
                (200, -0.8),
                (500, -0.8),
                (1000, -0.8)
            });
            RoofCurves_Neg["Zone2e"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, -1.8),
                (10,  -1.8),
                (20, -1.5),
                (50, -1.1),
                (100, -0.8),
                (200, -0.8),
                (500, -0.8),
                (1000, -0.8)
            });
            RoofCurves_Neg["Zone1"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, -1.8),
                (10,  -1.8),
                (20, -1.5),
                (50, -1.1),
                (100, -0.8),
                (200, -0.8),
                (500, -0.8),
                (1000, -0.8)
            });

            // ---- Roof: Positive ExternalPressure Zones ----
            RoofCurves_Pos["Zone3e"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, 0.9),
                (10, 0.9),
                (20, 0.8),
                (50, 0.6),
                (100, 0.5),
                (200, 0.5),
                (500, 0.5),
                (1000, 0.5)
            });
            RoofCurves_Pos["Zone3r"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, 0.9),
                (10, 0.9),
                (20, 0.8),
                (50, 0.6),
                (100, 0.5),
                (200, 0.5),
                (500, 0.5),
                (1000, 0.5)
            });
            RoofCurves_Pos["Zone2n"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, 0.9),
                (10, 0.9),
                (20, 0.8),
                (50, 0.6),
                (100, 0.5),
                (200, 0.5),
                (500, 0.5),
                (1000, 0.5)
            });
            RoofCurves_Pos["Zone2r"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, 0.9),
                (10, 0.9),
                (20, 0.8),
                (50, 0.6),
                (100, 0.5),
                (200, 0.5),
                (500, 0.5),
                (1000, 0.5)
            });
            RoofCurves_Pos["Zone2e"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, 0.9),
                (10, 0.9),
                (20, 0.8),
                (50, 0.6),
                (100, 0.5),
                (200, 0.5),
                (500, 0.5),
                (1000, 0.5)
            });
            RoofCurves_Pos["Zone1"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, 0.9),
                (10, 0.9),
                (20, 0.8),
                (50, 0.6),
                (100, 0.5),
                (200, 0.5),
                (500, 0.5),
                (1000, 0.5)
            });

            // ---- Overhang: Negative ExternalPressure Zones ----
            OverhangCurves["Zone3e"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, -4.0),
                (2, -4.0),
                (10, -3.3),
                (20, -3.0),
                (50, -2.6),
                (200, -2.0),
                (300, -1.8),
                (1000, -1.8)
            });
            OverhangCurves["Zone3r"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, -2.8),
                (10, -2.8),
                (20, -2.58),
                (50, -2.36),
                (100, -2.03),
                (200, -1.8),
                (500, -1.8),
                (1000, -1.8)
            });
            OverhangCurves["Zone2n"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, -2.8),
                (10, -2.8),
                (20, -2.58),
                (50, -2.36),
                (100, -2.03),
                (200, -1.8),
                (500, -1.8),
                (1000, -1.8)
            });
            OverhangCurves["Zone2r"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, -2.6),
                (10, -2.6),
                (20, -2.3),
                (50, -1.9),
                (100, -1.6),
                (200, -1.6),
                (500, -1.6),
                (1000, -1.6)
            });
            OverhangCurves["Zone2e"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, -2.6),
                (10, -2.6),
                (20, -2.3),
                (50, -1.9),
                (100, -1.6),
                (200, -1.6),
                (500, -1.6),
                (1000, -1.6)
            });
            OverhangCurves["Zone1"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, -2.6),
                (10, -2.6),
                (20, -2.3),
                (50, -1.9),
                (100, -1.6),
                (200, -1.6),
                (500, -1.6),
                (1000, -1.6)
            });
        }
    }
}
