namespace ASCE7WindLoadCalculator
{
    /// <summary>
    /// ASCE 7-16 Figs. 30.3-2H (roof) and 30.3-2I (overhang), hip roofs 27° &lt; θ ≤ 45° (pp. 343-344). The small-area values
    /// (and the large-area value of Zone 1) are equations in θ; between the printed breakpoints each curve is a straight line
    /// on log(area). Amin = 9 − 0.135θ sq ft (Zone 3: areas below Amin take the value at Amin, note 7) and Amax = 280 − 5θ
    /// sq ft (Zone 2e: areas above Amax take the value at Amax, note 8). At θ = 45° the figures show the plotted values.
    /// </summary>
    public class Figure30_3_2H_2I_ASCE7_16 : Chapter27and30_GCpCurveBase
    {
        public override string ChartTitle { get; set; } = "ASCE 7-16 Figure 30.3-2H and 2I - Hip Roofs";
        public override string ChartCriteria { get; set; } = "h <= 60ft, 27deg < slope <= 45deg";

        public Figure30_3_2H_2I_ASCE7_16(double slope)
        {
            // the figures cover 27° to 45°; a steeper roof (outside them, warned elsewhere as for gables) is read at 45°
            double t = System.Math.Min(45, System.Math.Max(27, slope));
            double amin = 9 - 0.135 * t;      // Zone 3 (2H and 2I)
            double amax = 280 - 5.0 * t;      // Zone 2e (2H and 2I)
            ChartCriteria = $"h <= 60ft, 27deg < slope <= 45deg; slope = {t:0.#}deg, Amin = {amin:0.##} sq ft, Amax = {amax:0.#} sq ft";

            // Fig. 30.3-2H, roof
            double r1 = -0.6175 - 0.0200 * t, r1Large = -0.0950 - 0.0135 * t;
            double r2e = 0.2000 - 0.0670 * t, r2r = 1.0000 - 0.0820 * t, r3 = 1.25 - 0.108 * t;
            RoofCurves_Neg["Zone1"] = ExternalGCpCurve.ForArea(new (double X, double Y)[] { (1.0, r1), (10, r1), (200, r1Large), (1000, r1Large) });
            RoofCurves_Neg["Zone2e"] = ExternalGCpCurve.ForArea(new (double X, double Y)[] { (1.0, r2e), (3, r2e), (amax, -0.8), (1000, -0.8) });
            RoofCurves_Neg["Zone2r"] = ExternalGCpCurve.ForArea(new (double X, double Y)[] { (1.0, r2r), (7, r2r), (100, -1.0), (1000, -1.0) });
            RoofCurves_Neg["Zone3"] = ExternalGCpCurve.ForArea(new (double X, double Y)[] { (1.0, r3), (amin, r3), (50, -1.0), (1000, -1.0) });
            foreach (var zone in new[] { "Zone3", "Zone2r", "Zone2e", "Zone1" })   // all zones: +0.9 to 3 sq ft, +0.3 from 100
                RoofCurves_Pos[zone] = ExternalGCpCurve.ForArea(new (double X, double Y)[] { (1.0, 0.9), (3, 0.9), (100, 0.3), (1000, 0.3) });

            // Fig. 30.3-2I, overhang
            double o1 = -1.4175 - 0.02 * t, o1Large = -0.895 - 0.0135 * t;
            double o2e = -0.60 - 0.067 * t, o2r = 0.20 - 0.082 * t, o3 = 0.45 - 0.108 * t;
            OverhangCurves["Zone1"] = ExternalGCpCurve.ForArea(new (double X, double Y)[] { (1.0, o1), (10, o1), (200, o1Large), (1000, o1Large) });
            OverhangCurves["Zone2e"] = ExternalGCpCurve.ForArea(new (double X, double Y)[] { (1.0, o2e), (3, o2e), (amax, -1.6), (1000, -1.6) });
            OverhangCurves["Zone2r"] = ExternalGCpCurve.ForArea(new (double X, double Y)[] { (1.0, o2r), (7, o2r), (100, -1.8), (1000, -1.8) });
            OverhangCurves["Zone3"] = ExternalGCpCurve.ForArea(new (double X, double Y)[] { (1.0, o3), (amin, o3), (50, -1.8), (1000, -1.8) });
        }
    }
}
