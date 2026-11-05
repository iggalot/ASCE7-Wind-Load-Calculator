namespace ASCE7WindLoadCalculator
{
    /// <summary>
    /// For parallel to ridge and for roofs with slope < = 10 degs
    /// </summary>
    public class ParallelToRidge_LowSlope_Roof_High : Chapter27and30_GCpCurveBase
    {
        public override string ChartTitle { get; set; } = "ASCE 7-16 and 7-22 Figure 27.3-1";
        public override string ChartCriteria { get; set; } = "Parallel to Ridge for slope or slope <= 10deg  -- h/L >= 1.0";
        /// <param name="reductionFactor">Fig. 27.3-1 note b factor on the −1.3 value (1.0 = unreduced).</param>
        public ParallelToRidge_LowSlope_Roof_High(double reductionFactor = 1.0)
        {
            // Most positive (least negative) ExternalPressure Roof Zones
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
                (0, -1.3 * reductionFactor),   // Fig. 27.3-1 note b: reduced with the area it applies to (Chapter27RoofFigureFactory.AreaReductionFactor)
                (80, -1.3 * reductionFactor),   // Fig. 27.3-1 note b: reduced with the area it applies to (Chapter27RoofFigureFactory.AreaReductionFactor)
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
    }
}
