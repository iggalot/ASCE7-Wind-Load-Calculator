namespace ASCE7WindLoadCalculator
{
    /// <summary>
    /// Contains the values of the Cp curves for ASCE 7-22 Figure 27.3-1
    /// -- h/L >= 1.0
    /// </summary>
    public class NormToRidge_LargeSlope_Roof_High_ASCE7_22 : Chapter27and30_GCpCurveBase
    {
        public override string ChartTitle { get; set; } = "ASCE 7-16 Figure 27.3-1";
        public override string ChartCriteria { get; set; } = "Normal to Ridge for slope >= 10deg  -- h/L = 1.0";
        public NormToRidge_LargeSlope_Roof_High_ASCE7_22(double slope, double area=50)   // set area to 50 to make reduction factor = 1.0
        {
            // Positive ExternalPressure Roof Zones
            RoofCurves_Pos["ZoneWWR"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (10, -0.18),
                (15, -0.18),
                (20, -0.18),
                (25, 0.0),
                (30, 0.2),
                (35, 0.2),
                (45, 0.3),
                (60, 0.6),
                (80, 0.8)
            });
            // Negative ExternalPressure Roof Zones
            RoofCurves_Neg["ZoneWWR"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (10, -1.3 * GetReductionFactor(area)),  // TODO: this first value can be reduced by area of WW roof  <= 100 reduction factor = 1.0, 250 reduction factor = 0.9 and >= 1000 reduction factor = 0.8
                (15, -1.0),
                (20, -0.7),
                (25, -0.5),
                (30, -0.3),
                (35, -0.2),
                (45, 0.0),
                (60, 0.0),
                (80, 0.0)
            });

            RoofCurves_Neg["ZoneLWR"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (10, -0.7),
                (15, -0.6),
                (20, -0.6),
                (80, -0.6)
            });
        }

        private double GetReductionFactor(double area)
        {
            if (area <= 100)
                return 1.0;
            else if (area >= 1000)
                return 0.8;
            else if (area <= 250)
            {
                // Interpolate between (100, 1.0) and (250, 0.9)
                double x0 = 100, y0 = 1.0;
                double x1 = 250, y1 = 0.9;
                return y0 + (area - x0) * (y1 - y0) / (x1 - x0);
            }
            else
            {
                // Interpolate between (250, 0.9) and (1000, 0.8)
                double x0 = 250, y0 = 0.9;
                double x1 = 1000, y1 = 0.8;
                return y0 + (area - x0) * (y1 - y0) / (x1 - x0);
            }
        }
    }
}
