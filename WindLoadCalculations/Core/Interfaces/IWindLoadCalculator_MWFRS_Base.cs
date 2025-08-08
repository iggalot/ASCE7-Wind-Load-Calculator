namespace ASCE7WindLoadCalculator
{
    public interface IWindLoadCalculator_MWFRS_Base
    {
        // Which figure of Ch30_3_2A thru I to use for CC roof
        Chapter27and30_GCpCurveBase extGCpCurve_Roof { get; set; }

        // The curve of Ch30_3_1 to use for CC walls
        Chapter27and30_GCpCurveBase extGCpCurve_Wall { get; set; }
    }
}
