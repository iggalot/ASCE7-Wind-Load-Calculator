using System;
using System.Collections.Generic;
using System.Linq;

namespace ASCE7WindLoadCalculator
{
    /// <summary>
    /// Two GCp figures blended by a fraction t, zone by zone and at every effective wind area:
    /// GCp = GCp(low) + t [GCp(high) − GCp(low)]. Used for ASCE 7-22 Fig. 30.3-2G note 6 (hip roofs, 27° &lt; θ &lt; 45°:
    /// interpolate between Figs. 30.3-2F and 30.3-2G with t = (θ − 27°) / (45° − 27°)).
    /// Both figures are straight lines on log(area) between their breakpoints, so the blend is exact when it is sampled at
    /// the union of the two figures' breakpoints.
    /// </summary>
    public sealed class BlendedGCpFigure : Chapter27and30_GCpCurveBase
    {
        public override string ChartTitle { get; set; }
        public override string ChartCriteria { get; set; }

        /// <summary>The fraction of the way from the low figure to the high one (0 to 1).</summary>
        public double Fraction { get; }

        public BlendedGCpFigure(Chapter27and30_GCpCurveBase low, Chapter27and30_GCpCurveBase high, double t, string title, string criteria)
        {
            if (low == null) throw new ArgumentNullException(nameof(low));
            if (high == null) throw new ArgumentNullException(nameof(high));
            Fraction = Math.Max(0, Math.Min(1, t));
            ChartTitle = title;
            ChartCriteria = criteria;
            Blend(low.RoofCurves_Neg, high.RoofCurves_Neg, RoofCurves_Neg);
            Blend(low.RoofCurves_Pos, high.RoofCurves_Pos, RoofCurves_Pos);
            Blend(low.OverhangCurves, high.OverhangCurves, OverhangCurves);
            Blend(low.WallCurves_Neg, high.WallCurves_Neg, WallCurves_Neg);
            Blend(low.WallCurves_Pos, high.WallCurves_Pos, WallCurves_Pos);
        }

        private void Blend(Dictionary<string, ExternalGCpCurve> low, Dictionary<string, ExternalGCpCurve> high,
                           Dictionary<string, ExternalGCpCurve> into)
        {
            if (low == null || high == null) return;
            foreach (var kv in low)
            {
                if (!high.TryGetValue(kv.Key, out var h))
                    throw new InvalidOperationException($"Zone {kv.Key} is in one figure and not the other.");
                var xs = kv.Value.GetPoints().Select(p => p.X).Concat(h.GetPoints().Select(p => p.X))
                                 .Distinct().OrderBy(x => x).ToArray();
                var pts = xs.Select(x => (x, kv.Value.Evaluate(x) + Fraction * (h.Evaluate(x) - kv.Value.Evaluate(x)))).ToArray();
                into[kv.Key] = new ExternalGCpCurve(pts, kv.Value.LogX);
            }
        }
    }
}
