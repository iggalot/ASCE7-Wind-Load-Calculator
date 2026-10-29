using System;

namespace ASCE7WindLoadCalculator
{
    public class ExternalGCpCurve
    {
        // Points defining the piecewise linear curve (exactly 8 points for 7 segments)
        private (double X, double Y)[] points;
        public (double X, double Y)[] Points { get => points; }

        public double LowerBoundX => points[0].X;
        public double UpperBoundX => points[points.Length - 1].X;

        /// <summary>
        /// Constructor accepting exactly 8 points (X,Y) defining the curve.
        /// Points must be sorted by ascending X.
        /// </summary>
        public ExternalGCpCurve((double X, double Y)[] points)
        {
            if (points == null) throw new ArgumentNullException(nameof(points));

            for (int i = 1; i < points.Length; i++)
            {
                if (points[i].X < points[i - 1].X)
                    throw new ArgumentException("Points must be sorted by ascending X.");
            }
            this.points = points;
        }

        /// <summary>
        /// Evaluate the piecewise linear curve at given x.
        /// Clamps x to bounds, and linearly interpolates between neighboring points.
        /// </summary>
        public double Evaluate(double x)
        {
            if (x <= LowerBoundX) return points[0].Y;
            if (x >= UpperBoundX) return points[points.Length - 1].Y;

            for (int i = 1; i < points.Length; i++)
            {
                if (x <= points[i].X)
                {
                    double x1 = points[i - 1].X;
                    double y1 = points[i - 1].Y;
                    double x2 = points[i].X;
                    double y2 = points[i].Y;

                    double t = (x - x1) / (x2 - x1);
                    return y1 + t * (y2 - y1);
                }
            }

            // Should never reach here because of prior bounds checks
            return points[points.Length - 1].Y;
        }

        public (double X, double Y)[] GetPoints()
        {
            // Return a copy to avoid external mutation
            return ((double X, double Y)[])points.Clone();
        }
    }
}
