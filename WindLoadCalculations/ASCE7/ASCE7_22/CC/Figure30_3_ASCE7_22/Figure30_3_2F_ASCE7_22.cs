namespace ASCE7WindLoadCalculator
{
    /// <summary>
    /// ASCE7-22 Figure 30-3-2E
    /// Hip Roofs
    /// h <= 60ft
    /// 20deg < slope <= 27deg
    /// </summary>
    public class Figure30_3_2F_ASCE7_22:Chapter27and30_GCpCurveBase
    {
        public override string ChartTitle { get; set; } = "ASCE 7-22 Figure 30-3-2F - Hip Roofs";
        public override string ChartCriteria { get; set; } = "h <= 60ft, 20deg < slope <= 27deg";
        /// <summary>
        /// ASCE7-22 Figure 30-3-2E (Roof)
        /// </summary>
        public Figure30_3_2F_ASCE7_22()
        {
            // ---- Roof: Negative ExternalPressure Zones ----
            RoofCurves_Neg["Zone3"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, -2.0),
                (10,  -2.0),
                (20,  -1.7),
                (50,  -1.3),
                (100, -1.0),
                (200, -1.0),
                (500, -1.0),
                (1000, -1.0),
            });
            RoofCurves_Neg["Zone2"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, -2.0),
                (10,  -2.0),
                (20,  -1.7),
                (50,  -1.3),
                (100, -1.0),
                (200, -1.0),
                (500, -1.0),
                (1000, -1.0),
            });
            RoofCurves_Neg["Zone1"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, -1.4),
                (10,  -1.4),
                (20,  -1.22),
                (50,  -0.97),
                (100, -0.8),
                (200, -0.8),
                (500, -0.8),
                (1000, -0.8),
            });

            // ---- Roof: Positive ExternalPressure Zones ----
            RoofCurves_Pos["Zone3"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, 0.7),
                (10,  0.7),
                (20,  0.57),
                (50,  0.43),
                (100, 0.3),
                (200, 0.3),
                (500, 0.3),
                (1000, 0.3),
            });
            RoofCurves_Pos["Zone2"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, 0.7),
                (10,  0.7),
                (20,  0.57),
                (50,  0.43),
                (100, 0.3),
                (200, 0.3),
                (500, 0.3),
                (1000, 0.3),
            });
            RoofCurves_Pos["Zone1"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, 0.7),
                (10,  0.7),
                (20,  0.57),
                (50,  0.43),
                (100, 0.3),
                (200, 0.3),
                (500, 0.3),
                (1000, 0.3),
            });
        }
    }
}
