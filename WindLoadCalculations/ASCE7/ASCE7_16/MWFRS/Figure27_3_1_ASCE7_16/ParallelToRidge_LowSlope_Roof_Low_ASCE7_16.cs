namespace ASCE7WindLoadCalculator
{
    public class ParallelToRidge_LowSlope_Roof_Low_ASCE7_16 : Chapter27and30_GCpCurveBase
    {
        public override string ChartTitle { get; set; } = "ASCE 7-16 Figure 27.3-1";
        public override string ChartCriteria { get; set; } = "Parallel to Ridge for slope or slope <= 10deg  -- h/L <= 0.5";
        public ParallelToRidge_LowSlope_Roof_Low_ASCE7_16()
        {
            // Most Positive (least negative) ExternalPressure Roof Zones
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
                (0,  -0.9),
                (80, -0.9)
            });

            RoofCurves_Neg["Zone3"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (0,  -0.9),
                (80, -0.9)
            });
            
            RoofCurves_Neg["Zone2"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (0,  -0.5),
                (80, -0.5)
            });
            
            RoofCurves_Neg["Zone1"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (0,  -0.3),
                (80, -0.3)
            });
        }
    }
}
