namespace ASCE7WindLoadCalculator
{
    public class ParallelToRidge_LowSlope_Roof_High_ASCE7_16 : Chapter27and30_GCpCurveBase
    {
        public override string ChartTitle { get; set; } = "ASCE 7-16 Figure 27.3-1";
        public override string ChartCriteria { get; set; } = "Parallel to Ridge for slope or slope <= 10deg  -- h/L >= 1.0";
        public ParallelToRidge_LowSlope_Roof_High_ASCE7_16(double area=50)
        {
            // Positive ExternalPressure Roof Zones
            RoofCurves_Pos["Zone4"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (0,  -0.18),
                (80, -0.18)
            });
            
            RoofCurves_Pos["Zone3"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (0,  -0.18),
                (80, -0.18)
            });

            RoofCurves_Pos["Zone2"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (0,  -0.18),
                (80, -0.18)
            });

            RoofCurves_Pos["Zone1"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (0,  -0.18),
                (80, -0.18)
            });

            // Most negative values
            RoofCurves_Neg["Zone4"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (0, -1.3 * GetReductionFactor(area)),  // TODO: this first value can be reduced by area of WW roof  <= 100 reduction factor = 1.0, 250 reduction factor = 0.9 and >= 1000 reduction factor = 0.8
                (80, -1.3 * GetReductionFactor(area)),  // TODO: this first value can be reduced by area of WW roof  <= 100 reduction factor = 1.0, 250 reduction factor = 0.9 and >= 1000 reduction factor = 0.8
            });

            RoofCurves_Neg["Zone3"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (0,  -0.7),
                (80, -0.7)
            });
            RoofCurves_Neg["Zone2"] = new ExternalGCpCurve(new (double X, double Y)[]
{
                (0,  -0.7),
                (80, -0.7)
});
            RoofCurves_Neg["Zone1"] = new ExternalGCpCurve(new (double X, double Y)[]
{
                (0,  -0.7),
                (80, -0.7)
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
