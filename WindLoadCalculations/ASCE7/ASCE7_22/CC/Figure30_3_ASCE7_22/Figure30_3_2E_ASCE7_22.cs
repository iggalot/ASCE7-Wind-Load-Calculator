namespace ASCE7WindLoadCalculator
{
    /// <summary>
    /// ASCE7-22 Figure 30-3-2E
    /// Hip Roofs
    /// h <= 60ft
    /// 7deg < slope <= 20deg
    /// </summary>
    public class Figure30_3_2E_ASCE7_22:Chapter27and30_GCpCurveBase
    {
        public override string ChartTitle { get; set; } = "ASCE 7-22 Figure 30-3-2E - Hip Roofs";
        public override string ChartCriteria { get; set; } = "h <= 60ft, 7deg < slope <= 20deg";
        /// <summary>
        /// ASCE7-22 Figure 30-3-2E (Roof)
        /// </summary>
        public Figure30_3_2E_ASCE7_22()
        {
            // ---- Roof: Negative ExternalPressure Zones ----
            RoofCurves_Neg["Zone3"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, -2.6),
                (10,  -2.6),
                (20,  -2.32),
                (50,  -1.95),
                (100, -1.68),
                (200, -1.4),
                (500, -1.4),
                (1000, -1.4),
            });
            RoofCurves_Neg["Zone2"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, -2.4),
                (10,  -2.4),
                (20,  -2.15),
                (50,  -1.8),
                (100, -1.55),
                (200, -1.3),
                (500, -1.3),
                (1000, -1.3),
            });
            RoofCurves_Neg["Zone1"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, -1.8),
                (10,  -1.8),
                (20,  -1.55),
                (50,  -1.28),
                (100, -1.05),
                (200, -0.8),
                (500, -0.8),
                (1000, -0.8),
            });

            // ---- Roof: Positive ExternalPressure Zones ----
            RoofCurves_Pos["Zone3"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, 0.7),
                (10,  0.7),
                (20,  0.58),
                (50,  0.45),
                (100, 0.3),
                (200, 0.3),
                (500, 0.3),
                (1000, 0.3),
            });
            RoofCurves_Pos["Zone2"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, 0.7),
                (10,  0.7),
                (20,  0.58),
                (50,  0.45),
                (100, 0.3),
                (200, 0.3),
                (500, 0.3),
                (1000, 0.3),
            });
            RoofCurves_Pos["Zone1"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, 0.7),
                (10,  0.7),
                (20,  0.58),
                (50,  0.45),
                (100, 0.3),
                (200, 0.3),
                (500, 0.3),
                (1000, 0.3),
            });
        }
    }
}
