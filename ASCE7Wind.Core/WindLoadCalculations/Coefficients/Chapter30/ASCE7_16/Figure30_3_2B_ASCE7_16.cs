namespace ASCE7WindLoadCalculator
{
    /// <summary>
    /// ASCE7-16 Figure 30-3-2B
    /// Gable Roofs
    /// h <= 60ft
    /// 7deg < slope <= 20deg
    /// </summary>
    public class Figure30_3_2B_ASCE7_16 : Chapter27and30_GCpCurveBase
    {
        public override string ChartTitle { get; set; } = "ASCE 7-16 Figure 30-3-2B - Gable Roofs";
        public override string ChartCriteria { get; set; } = "h <= 60ft, 7deg < slope <= 20deg";

        public Figure30_3_2B_ASCE7_16()
        {

            // ---- Roof: Negative ExternalPressure Zones ----
            RoofCurves_Neg["Zone3r"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, -3.6),
                (10,  -3.6),
                (20, -3.05),
                (50, -2.34),
                (100, -1.8),
                (200, -1.8),
                (500, -1.8),
                (1000, -1.8)
            });
            RoofCurves_Neg["Zone3e"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, -3.0),
                (10,  -3.0),
                (20, -2.57),
                (50, -2.0),
                (100, -1.6),
                (200, -1.1),
                (250, -1.0),
                (1000, -1.0)
            });
            RoofCurves_Neg["Zone2n"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, -3.0),
                (10,  -3.0),
                (20, -2.57),
                (50, -2.0),
                (100, -1.6),
                (200, -1.1),
                (250, -1.0),
                (1000, -1.0)
            });
            RoofCurves_Neg["Zone2r"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, -3.0),
                (10,  -3.0),
                (20, -2.57),
                (50, -2.0),
                (100, -1.6),
                (200, -1.1),
                (250, -1.0),
                (1000, -1.0)
            });
            RoofCurves_Neg["Zone2e"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, -2.0),
                (10,  -2.0),
                (20, -2.0),
                (50, -1.17),
                (100, -0.5),
                (200, -0.5),
                (250, -0.5),
                (1000, -0.5)
            });
            RoofCurves_Neg["Zone1"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, -2.0),
                (10,  -2.0),
                (20, -2.0),
                (50, -1.17),
                (100, -0.5),
                (200, -0.5),
                (250, -0.5),
                (1000, -0.5)
            });

            // ---- Roof: Positive ExternalPressure Zones ----
            RoofCurves_Pos["Zone3r"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, 0.7),
                (2.0, 0.7),
                (10, 0.53),
                (20, 0.45),
                (50, 0.38),
                (100, 0.3 ),
                (500, 0.3),
                (1000, 0.3)
            });
            RoofCurves_Pos["Zone3e"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, 0.7),
                (2.0, 0.7),
                (10, 0.53),
                (20, 0.45),
                (50, 0.38),
                (100, 0.3 ),
                (500, 0.3),
                (1000, 0.3)
            });
            RoofCurves_Pos["Zone2n"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, 0.7),
                (2.0, 0.7),
                (10, 0.53),
                (20, 0.45),
                (50, 0.38),
                (100, 0.3 ),
                (500, 0.3),
                (1000, 0.3)
            });
            RoofCurves_Pos["Zone2r"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, 0.7),
                (2.0, 0.7),
                (10, 0.53),
                (20, 0.45),
                (50, 0.38),
                (100, 0.3 ),
                (500, 0.3),
                (1000, 0.3)
            });
            RoofCurves_Pos["Zone2e"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, 0.7),
                (2.0, 0.7),
                (10, 0.53),
                (20, 0.45),
                (50, 0.38),
                (100, 0.3 ),
                (500, 0.3),
                (1000, 0.3)
            });
            RoofCurves_Pos["Zone1"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, 0.7),
                (2.0, 0.7),
                (10, 0.53),
                (20, 0.45),
                (50, 0.38),
                (100, 0.3 ),
                (500, 0.3),
                (1000, 0.3)
            });

            // ---- Overhang: Negative ExternalPressure Zones ----
            OverhangCurves["Zone3r"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, -4.7),
                (10,  -4.7),
                (20, -4.0),
                (50, -3.05),
                (100, -2.3),
                (200, -2.3),
                (500, -2.3),
                (1000, -2.3)
            });
            OverhangCurves["Zone3e"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, -4.1),
                (10,  -4.1),
                (20, -3.55),
                (50, -2.8),
                (100, -2.22),
                (200, -1.62),
                (250, -1.5),
                (1000, -1.5)
            });
            OverhangCurves["Zone2n"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, -3.5),
                (10,  -3.5),
                (20, -3.18),
                (50, -2.72),
                (100, -2.42),
                (200, -2.1),
                (250, -2.0),
                (1000, -2.0)
            });
            OverhangCurves["Zone2r"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, -3.5),
                (10,  -3.5),
                (20, -3.18),
                (50, -2.72),
                (100, -2.42),
                (200, -2.1),
                (250, -2.0),
                (1000, -2.0)
            });
            OverhangCurves["Zone2e"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, -2.5),
                (10,  -2.5),
                (20, -2.5),
                (50, -1.95),
                (100, -1.5),
                (200, -1.5),
                (500, -1.5),
                (1000, -1.5)
            });
            OverhangCurves["Zone1"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, -2.5),
                (10,  -2.5),
                (20, -2.5),
                (50, -1.95),
                (100, -1.5),
                (200, -1.5),
                (500, -1.5),
                (1000, -1.5)
            });
        }
    }
}

