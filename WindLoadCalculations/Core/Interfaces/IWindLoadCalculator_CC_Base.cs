namespace ASCE7WindLoadCalculator
{

    /// <summary>
    ///  In ASCE7-16, the dynamic wind pressure coefficient includes Kd in the dynamic wind calculation.  
    ///  In ASCE7_22 it does not -- instead applying Kd to the external and net pressure calculations separately.  
    ///  In the end the calculations are the same.  This now better matches these codes.  
    ///  But dyanmic wind pressure qz and qh will be drastically different.
    /// </summary>
    public interface IWindLoadCalculator_CC_Base
    {
        // Which figure of Ch30_3_2A thru I to use for CC roof
        Chapter27and30_GCpCurveBase extGCpCurve_Roof { get; set; }

        // The curve of Ch30_3_1 to use for CC walls
        Chapter27and30_GCpCurveBase extGCpCurve_Wall { get; set; }
    }
}
