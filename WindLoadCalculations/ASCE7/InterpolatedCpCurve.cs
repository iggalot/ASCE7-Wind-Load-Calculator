using System;
using System.Collections.Generic;

namespace ASCE7WindLoadCalculator
{
    public class InterpolatedCpCurve : Chapter27and30_GCpCurveBase
    {
        public override string ChartTitle { get; set; } = "ASCE 7-16 Figure 27.3-1";
        public override string ChartCriteria { get; set; }

        public InterpolatedCpCurve(double h_over_L, double slope, double low_val, double high_val, 
            Chapter27and30_GCpCurveBase lower_curve, Chapter27and30_GCpCurveBase upper_curve, double area = 50)
        {
            ChartCriteria = $"Slope = {slope:0.##} — interpolated h/L = {h_over_L:0.###}";

            // Roof zones

            foreach (var key in lower_curve.RoofCurves_Pos.Keys)
            {
                RoofCurves_Pos[key] = GCpCurveInterpolator.Interpolate(
                lower_curve.RoofCurves_Pos[key],
                upper_curve.RoofCurves_Pos[key],
                h_over_L, low_val, high_val);
            }

            foreach (var key in lower_curve.RoofCurves_Neg.Keys)
            {
                RoofCurves_Neg[key] = GCpCurveInterpolator.Interpolate(
                lower_curve.RoofCurves_Neg[key],
                upper_curve.RoofCurves_Neg[key],
                h_over_L, low_val, high_val);
            }
        }
    }

    public static class GCpCurveInterpolator
    {
        public static ExternalGCpCurve Interpolate(
            ExternalGCpCurve lowCurve,
            ExternalGCpCurve highCurve,
            double inputValue,
            double lowPoint,
            double highPoint)
        {
            if (inputValue < lowPoint || inputValue > highPoint)
                throw new ArgumentOutOfRangeException(nameof(inputValue), $"Value must be between {lowPoint} and {highPoint}.");

            double t = (inputValue - lowPoint) / (highPoint - lowPoint);

            var pLow = lowCurve.Points;
            var pHigh = highCurve.Points;

            if (pLow.Length != pHigh.Length)
                throw new InvalidOperationException("Curves must have the same number of points.");

            var interpolated = new List<(double X, double Y)>();

            for (int i = 0; i < pLow.Length; i++)
            {
                if (pLow[i].X != pHigh[i].X)
                    throw new InvalidOperationException("Curves must have matching X coordinates.");

                double x = pLow[i].X;
                double y = pLow[i].Y + (pHigh[i].Y - pLow[i].Y) * t;

                interpolated.Add((x, y));
            }

            return new ExternalGCpCurve(interpolated.ToArray());
        }
    }

}
