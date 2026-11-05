namespace ASCE7WindLoadCalculator
{
    /// <summary>
    /// ASCE7-16 Figure 30.3-2C
    /// Gable Roofs
    /// h <= 60ft
    /// 20deg < slope <= 27deg
    /// </summary>
    public class Figure30_3_2C_ASCE7_16 : Chapter27and30_GCpCurveBase
    {
        public override string ChartTitle { get; set; } = "ASCE 7-16 Figure 30.3-2C - Gable Roofs";
        public override string ChartCriteria { get; set; } = "h <= 60ft, 20deg < slope <= 27deg";
        public Figure30_3_2C_ASCE7_16()
        {
            // ---- Roof: Negative ExternalPressure Zones ----
            RoofCurves_Neg["Zone3r"] = ExternalGCpCurve.ForArea(new (double X, double Y)[]
            {
                // Fig. 30.3-2C (7-16 p. 338), printed breakpoints, straight on log area
                (1.0, -3.6),
                (4, -3.6),
                (50, -1.8),
                (1000, -1.8)
            });
            RoofCurves_Neg["Zone3e"] = ExternalGCpCurve.ForArea(new (double X, double Y)[]
            {
                // Fig. 30.3-2C (7-16 p. 338), printed breakpoints, straight on log area
                (1.0, -2.5),
                (10, -2.5),
                (150, -1.2),
                (1000, -1.2)
            });
            RoofCurves_Neg["Zone2n"] = ExternalGCpCurve.ForArea(new (double X, double Y)[]
            {
                // Fig. 30.3-2C (7-16 p. 338), printed breakpoints, straight on log area
                (1.0, -2.5),
                (10, -2.5),
                (150, -1.2),
                (1000, -1.2)
            });
            RoofCurves_Neg["Zone2r"] = ExternalGCpCurve.ForArea(new (double X, double Y)[]
            {
                // Fig. 30.3-2C (7-16 p. 338), printed breakpoints, straight on log area
                (1.0, -2.5),
                (10, -2.5),
                (150, -1.2),
                (1000, -1.2)
            });
            RoofCurves_Neg["Zone2e"] = ExternalGCpCurve.ForArea(new (double X, double Y)[]
            {
                // Fig. 30.3-2C (7-16 p. 338), printed breakpoints, straight on log area
                (1.0, -1.5),
                (20, -1.5),
                (300, -0.8),
                (1000, -0.8)
            });
            RoofCurves_Neg["Zone1"] = ExternalGCpCurve.ForArea(new (double X, double Y)[]
            {
                // Fig. 30.3-2C (7-16 p. 338), printed breakpoints, straight on log area
                (1.0, -1.5),
                (20, -1.5),
                (300, -0.8),
                (1000, -0.8)
            });

            // ---- Roof: Positive ExternalPressure Zones ----
            RoofCurves_Pos["Zone3r"] = ExternalGCpCurve.ForArea(new (double X, double Y)[]
            {
                // Fig. 30.3-2C (7-16 p. 338), printed breakpoints, straight on log area
                (1.0, 0.7),
                (2, 0.7),
                (100, 0.3),
                (1000, 0.3)
            });
            RoofCurves_Pos["Zone3e"] = ExternalGCpCurve.ForArea(new (double X, double Y)[]
            {
                // Fig. 30.3-2C (7-16 p. 338), printed breakpoints, straight on log area
                (1.0, 0.7),
                (2, 0.7),
                (100, 0.3),
                (1000, 0.3)
            });
            RoofCurves_Pos["Zone2n"] = ExternalGCpCurve.ForArea(new (double X, double Y)[]
            {
                // Fig. 30.3-2C (7-16 p. 338), printed breakpoints, straight on log area
                (1.0, 0.7),
                (2, 0.7),
                (100, 0.3),
                (1000, 0.3)
            });
            RoofCurves_Pos["Zone2r"] = ExternalGCpCurve.ForArea(new (double X, double Y)[]
            {
                // Fig. 30.3-2C (7-16 p. 338), printed breakpoints, straight on log area
                (1.0, 0.7),
                (2, 0.7),
                (100, 0.3),
                (1000, 0.3)
            });
            RoofCurves_Pos["Zone2e"] = ExternalGCpCurve.ForArea(new (double X, double Y)[]
            {
                // Fig. 30.3-2C (7-16 p. 338), printed breakpoints, straight on log area
                (1.0, 0.7),
                (2, 0.7),
                (100, 0.3),
                (1000, 0.3)
            });
            RoofCurves_Pos["Zone1"] = ExternalGCpCurve.ForArea(new (double X, double Y)[]
            {
                // Fig. 30.3-2C (7-16 p. 338), printed breakpoints, straight on log area
                (1.0, 0.7),
                (2, 0.7),
                (100, 0.3),
                (1000, 0.3)
            });

            // ---- Overhang: Negative ExternalPressure Zones ----
            OverhangCurves["Zone3r"] = ExternalGCpCurve.ForArea(new (double X, double Y)[]
            {
                // Fig. 30.3-2C (7-16 p. 338), printed breakpoints, straight on log area
                (1.0, -4.7),
                (4, -4.7),
                (50, -2.4),
                (1000, -2.4)
            });
            OverhangCurves["Zone3e"] = ExternalGCpCurve.ForArea(new (double X, double Y)[]
            {
                // Fig. 30.3-2C (7-16 p. 338), printed breakpoints, straight on log area
                (1.0, -3.6),
                (10, -3.6),
                (150, -1.7),
                (1000, -1.7)
            });
            OverhangCurves["Zone2n"] = ExternalGCpCurve.ForArea(new (double X, double Y)[]
            {
                // Fig. 30.3-2C (7-16 p. 338), printed breakpoints, straight on log area
                (1.0, -3.0),
                (10, -3.0),
                (150, -2.2),
                (1000, -2.2)
            });
            OverhangCurves["Zone2r"] = ExternalGCpCurve.ForArea(new (double X, double Y)[]
            {
                // Fig. 30.3-2C (7-16 p. 338), printed breakpoints, straight on log area
                (1.0, -3.0),
                (10, -3.0),
                (150, -2.2),
                (1000, -2.2)
            });

            OverhangCurves["Zone2e"] = ExternalGCpCurve.ForArea(new (double X, double Y)[]
            {
                // Fig. 30.3-2C (7-16 p. 338), printed breakpoints, straight on log area
                (1.0, -2.0),
                (20, -2.0),
                (350, -1.8),
                (1000, -1.8)
            });
            OverhangCurves["Zone1"] = ExternalGCpCurve.ForArea(new (double X, double Y)[]
            {
                // Fig. 30.3-2C (7-16 p. 338), printed breakpoints, straight on log area
                (1.0, -2.0),
                (20, -2.0),
                (350, -1.8),
                (1000, -1.8)
            });
        }
    }
}
