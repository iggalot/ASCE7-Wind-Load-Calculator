using System.Collections.Generic;

namespace ASCE7WindLoadCalculator
{
    /// <summary>
    /// A class for holding the Cp or Gcp curve data for chapters 27 and 30
    /// -- Chapter 30 is the GCp vs Ae curves for different roof types.
    /// -- Chapter 27 is the Cp values vs. roof slopes
    /// </summary>
    public abstract class Chapter27and30_GCpCurveBase
    {
        public abstract string ChartTitle { get; set; }
        public abstract string ChartCriteria { get; set; }
        public Dictionary<string, ExternalGCpCurve> RoofCurves_Pos { get; set; }
        public Dictionary<string, ExternalGCpCurve> RoofCurves_Neg { get; set; }
        public Dictionary<string, ExternalGCpCurve> OverhangCurves { get; set; } = null;
        public Dictionary<string, ExternalGCpCurve> WallCurves_Pos { get; set; }
        public Dictionary<string, ExternalGCpCurve> WallCurves_Neg { get; set; }

        protected Chapter27and30_GCpCurveBase()
        {
            RoofCurves_Pos = new Dictionary<string, ExternalGCpCurve>();
            RoofCurves_Neg = new Dictionary<string, ExternalGCpCurve>();
            OverhangCurves = new Dictionary<string, ExternalGCpCurve>();
            WallCurves_Pos = new Dictionary<string, ExternalGCpCurve>();
            WallCurves_Neg = new Dictionary<string, ExternalGCpCurve>();
        }
    }
}
