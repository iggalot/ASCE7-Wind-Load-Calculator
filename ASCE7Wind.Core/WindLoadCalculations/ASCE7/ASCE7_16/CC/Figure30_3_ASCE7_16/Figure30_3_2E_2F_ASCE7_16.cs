using System;
using System.Collections.Generic;

namespace ASCE7WindLoadCalculator
{
    /// <summary>
    /// ASCE7-16 Figure 30-3-2E
    /// Hip Roofs
    /// h/B >= 0.8
    /// h <= 60ft
    /// 7deg < slope <= 20deg
    /// </summary>
    public class Figure30_3_2E_2F_ASCE7_16:Chapter27and30_GCpCurveBase
    {
        public override string ChartTitle { get; set; } = "ASCE 7-16 Figure 30-3-2E and 2F - Hip Roofs";
        public override string ChartCriteria { get; set; } = "h <= 60ft, 7deg < slope <= 20deg";
        /// <summary>
        /// ASCE7-16 Figure 30-3-2E (Roof) and 2F (Overhang)
        /// </summary>
        /// <param name="h">mean roof height</param>
        /// <param name="B">width of building -- normal to wind</param>
        public Figure30_3_2E_2F_ASCE7_16(double ratio)
        {
            var low = new FigureDataSet();
            var high = new FigureDataSet();

            // Low h/B <= 0.5
            low.RoofNeg["Zone3"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, -1.8),
                (10, -1.8),
                (20, -1.63),
                (50, -1.4),
                (100, -1.25),
                (200, -1.1),
                (500, -1.1),
                (1000, -1.1)
            });
            low.RoofNeg["Zone2e"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, -1.8),
                (10, -1.8),
                (20, -1.63),
                (50, -1.4),
                (100, -1.25),
                (200, -1.1),
                (500, -1.1),
                (1000, -1.1)
            });
            low.RoofNeg["Zone2r"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, -2.4),
                (10, -2.4),
                (20, -2.15),
                (50, -1.8),
                (100, -1.55),
                (200, -1.3),
                (500, -1.3),
                (1000, -1.3)
            });
            low.RoofNeg["Zone1"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, -1.3),
                (10, -1.3),
                (20, -1.3),
                (50, -1.13),
                (100, -1.0),
                (200, -1.0),
                (500, -1.0),
                (1000, -1.0)
            });

            low.RoofPos["Zone3"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, 0.7),
                (10, 0.7),
                (20, 0.59),
                (50, 0.42),
                (100, 0.3),
                (200, 0.3),
                (500, 0.3),
                (1000, 0.3)
            });
            low.RoofPos["Zone2r"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, 0.7),
                (10, 0.7),
                (20, 0.59),
                (50, 0.42),
                (100, 0.3),
                (200, 0.3),
                (500, 0.3),
                (1000, 0.3)
            });
            low.RoofPos["Zone2e"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, 0.7),
                (10, 0.7),
                (20, 0.59),
                (50, 0.42),
                (100, 0.3),
                (200, 0.3),
                (500, 0.3),
                (1000, 0.3)
            });
            low.RoofPos["Zone1"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, 0.7),
                (10, 0.7),
                (20, 0.59),
                (50, 0.42),
                (100, 0.3),
                (200, 0.3),
                (500, 0.3),
                (1000, 0.3)
            });

            low.Overhang["Zone3"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, -2.9),
                (10, -2.9),
                (20, -2.6),
                (50, -2.2),
                (100, -1.9),
                (200, -1.6),
                (500, -1.6),
                (1000, -1.6)
            });
            low.Overhang["Zone2r"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, -2.9),
                (10, -2.9),
                (20, -2.75),
                (50, -2.6),
                (100, -2.45),
                (200, -2.3),
                (500, -2.3),
                (1000, -2.3)
            });
            low.Overhang["Zone2e"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, -2.3),
                (10, -2.3),
                (20, -2.25),
                (50, -2.2),
                (100, -2.15),
                (200, -2.1),
                (500, -2.1),
                (1000, -2.1)
            });
            low.Overhang["Zone1"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, -1.8),
                (10, -1.8),
                (20, -1.86),
                (50, -1.95),
                (100, -2.0),
                (200, -2.0),
                (500, -2.0),
                (1000, -2.0)
            });

            // High h/B >= 0.8
            high.RoofNeg["Zone3"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, -2.6),
                (10, -2.6),
                (20, -2.35),
                (50, -1.95),
                (100, -1.68),
                (200, -1.4),
                (500, -1.4),
                (1000, -1.4)
            });
            high.RoofNeg["Zone2e"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, -2.6),
                (10, -2.6),
                (20, -2.35),
                (50, -1.95),
                (100, -1.68),
                (200, -1.4),
                (500, -1.4),
                (1000, -1.4)
            });
            high.RoofNeg["Zone2r"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, -2.4),
                (10, -2.4),
                (20, -2.15),
                (50, -1.8),
                (100, -1.55),
                (200, -1.3),
                (500, -1.3),
                (1000, -1.3)
            });
            high.RoofNeg["Zone1"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, -1.8),
                (10, -1.8),
                (20, -1.8),
                (50, -1.34),
                (100, -1.0),
                (200, -1.0),
                (500, -1.0),
                (1000, -1.0)
            });

            high.RoofPos["Zone3"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, 0.7),
                (10, 0.7),
                (20, 0.59),
                (50, 0.42),
                (100, 0.3),
                (200, 0.3),
                (500, 0.3),
                (1000, 0.3)
            });
            high.RoofPos["Zone2r"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, 0.7),
                (10, 0.7),
                (20, 0.59),
                (50, 0.42),
                (100, 0.3),
                (200, 0.3),
                (500, 0.3),
                (1000, 0.3)
            });
            high.RoofPos["Zone2e"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, 0.7),
                (10, 0.7),
                (20, 0.59),
                (50, 0.42),
                (100, 0.3),
                (200, 0.3),
                (500, 0.3),
                (1000, 0.3)
            });
            high.RoofPos["Zone1"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, 0.7),
                (10, 0.7),
                (20, 0.59),
                (50, 0.42),
                (100, 0.3),
                (200, 0.3),
                (500, 0.3),
                (1000, 0.3)
            });

            high.Overhang["Zone3"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, -3.7),
                (10, -3.7),
                (20, -3.3),
                (50, -2.75),
                (100, -2.3),
                (200, -1.9),
                (500, -1.9),
                (1000, -1.9)
            });
            high.Overhang["Zone2e"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, -3.1),
                (10, -3.1),
                (20, -2.95),
                (50, -2.72),
                (100, -2.58),
                (200, -2.4),
                (500, -2.4),
                (1000, -2.4)
            });
            high.Overhang["Zone2r"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, -2.9),
                (10, -2.9),
                (20, -2.78),
                (50, -2.59),
                (100, -2.45),
                (200, -2.3),
                (500, -2.3),
                (1000, -2.3)
            });
            high.Overhang["Zone1"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, -2.3),
                (10, -2.3),
                (20, -2.3),
                (50, -2.12),
                (100, -2.0),
                (200, -2.0),
                (500, -2.0),
                (1000, -2.0)
            });

            if (ratio <= 0.5)
            {
                CopyDataset(low);
            }
            else if (ratio >= 0.8)
            {
                CopyDataset(high);
            }
            else
            {
                double t = (ratio - 0.5) / (0.8 - 0.5);
                CopyDataset(InterpolateDataSet(low, high, t));
            }
        }

        private void CopyDataset(FigureDataSet source)
        {
            foreach (var kv in source.RoofNeg) RoofCurves_Neg[kv.Key] = kv.Value;
            foreach (var kv in source.RoofPos) RoofCurves_Pos[kv.Key] = kv.Value;
            foreach (var kv in source.Overhang) OverhangCurves[kv.Key] = kv.Value;
        }

        private FigureDataSet InterpolateDataSet(FigureDataSet low, FigureDataSet high, double t)
        {
            var result = new FigureDataSet();

            foreach (var key in low.RoofNeg.Keys)
                result.RoofNeg[key] = InterpolateCurve(low.RoofNeg[key], high.RoofNeg[key], t);

            foreach (var key in low.RoofPos.Keys)
                result.RoofPos[key] = InterpolateCurve(low.RoofPos[key], high.RoofPos[key], t);

            foreach (var key in low.Overhang.Keys)
                result.Overhang[key] = InterpolateCurve(low.Overhang[key], high.Overhang[key], t);

            return result;
        }

        private ExternalGCpCurve InterpolateCurve(ExternalGCpCurve low, ExternalGCpCurve high, double t)
        {
            var lowPoints = low.GetPoints();
            var highPoints = high.GetPoints();

            if (lowPoints.Length != highPoints.Length)
                throw new InvalidOperationException("Curves must have the same number of points.");

            var resultPoints = new (double X, double Y)[lowPoints.Length];

            for (int i = 0; i < lowPoints.Length; i++)
            {
                double xLow = lowPoints[i].X;
                double xHigh = highPoints[i].X;

                if (Math.Abs(xLow - xHigh) > 1e-6)
                    throw new InvalidOperationException($"Mismatched X values at index {i}: {xLow} vs {xHigh}");

                double y = Lerp(lowPoints[i].Y, highPoints[i].Y, t);
                resultPoints[i] = (xLow, y); // Use X from either low or high (they are assumed equal)
            }

            return new ExternalGCpCurve(resultPoints);
        }


        private double Lerp(double a, double b, double t)
        {
            return a + (b - a) * t;
        }

        private class FigureDataSet
        {
            public Dictionary<string, ExternalGCpCurve> RoofNeg = new Dictionary<string, ExternalGCpCurve>();
            public Dictionary<string, ExternalGCpCurve> RoofPos = new Dictionary<string, ExternalGCpCurve>();
            public Dictionary<string, ExternalGCpCurve> Overhang = new Dictionary<string, ExternalGCpCurve>();
        }
    }
}
