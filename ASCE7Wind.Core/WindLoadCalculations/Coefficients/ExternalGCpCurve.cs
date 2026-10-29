using System;

namespace ASCE7WindLoadCalculator
{
    /// <summary>
    /// Piecewise-linear coefficient curve y(x), clamped to its end values outside the sampled range.
    /// Chapter 30 GCp figures plot GCp against effective wind area on a logarithmic axis, so their curves
    /// interpolate on log10(x) (<see cref="ForArea"/>); Chapter 27 Cp tables interpolate linearly.
    /// </summary>
    public sealed class ExternalGCpCurve
    {
        private readonly (double X, double Y)[] points;
        private readonly double[] interpX; // X or log10(X), precomputed for Evaluate

        public (double X, double Y)[] Points => points;
        public double LowerBoundX => points[0].X;
        public double UpperBoundX => points[points.Length - 1].X;

        /// <summary>True when interpolation is linear in log10(x).</summary>
        public bool LogX { get; }

        /// <param name="points">Samples sorted by ascending X.</param>
        /// <param name="logX">Interpolate on log10(X); requires X &gt; 0.</param>
        public ExternalGCpCurve((double X, double Y)[] points, bool logX = false)
        {
            if (points == null) throw new ArgumentNullException(nameof(points));
            if (points.Length == 0) throw new ArgumentException("A curve needs at least one point.", nameof(points));

            for (int i = 1; i < points.Length; i++)
                if (points[i].X < points[i - 1].X)
                    throw new ArgumentException("Points must be sorted by ascending X.", nameof(points));

            this.points = points;
            LogX = logX;
            interpX = new double[points.Length];
            for (int i = 0; i < points.Length; i++)
            {
                if (logX && points[i].X <= 0)
                    throw new ArgumentException("Log-scale curves need X > 0.", nameof(points));
                interpX[i] = logX ? Math.Log10(points[i].X) : points[i].X;
            }
        }

        /// <summary>A Chapter 30 GCp-vs-effective-area curve (log-area interpolation).</summary>
        public static ExternalGCpCurve ForArea((double X, double Y)[] points) => new ExternalGCpCurve(points, logX: true);

        public double Evaluate(double x)
        {
            if (x <= points[0].X) return points[0].Y;
            int last = points.Length - 1;
            if (x >= points[last].X) return points[last].Y;

            double xi = LogX ? Math.Log10(x) : x;
            for (int i = 1; i <= last; i++)
            {
                if (x <= points[i].X)
                {
                    double x1 = interpX[i - 1], x2 = interpX[i];
                    double y1 = points[i - 1].Y, y2 = points[i].Y;
                    if (x2 == x1) return y2; // duplicate X: take the later sample
                    return y1 + (xi - x1) / (x2 - x1) * (y2 - y1);
                }
            }
            return points[last].Y;
        }

        /// <summary>Copy of the samples (callers cannot mutate the curve).</summary>
        public (double X, double Y)[] GetPoints() => ((double X, double Y)[])points.Clone();
    }
}
