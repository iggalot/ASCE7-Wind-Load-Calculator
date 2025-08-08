using System;
using System.Collections.Generic;

namespace ASCE7WindLoadCalculator
{
    /// <summary>
    /// ASCE7-16 Interpolated Figures 30-3-2F && 30-3-2G
    /// Hip Roofs
    /// h <= 60ft
    /// 27deg < slope < 45deg
    /// </summary>
    ///         //ChartTitle = "ASCE 7-22 Interpolated 30-3-2F-2G - Hip Roofs";
    //    ChartCriteria = "h <= 60ft, 27deg < slope < 45deg";

    public class Figure30_3_2F_2G_INTERPOLATED_ASCE7_22 : Chapter27and30_GCpCurveBase
    {
        public override string ChartTitle { get; set; } = "ASCE 7-16 Figure 30-3-2F and 2G Interpolated - Hip Roofs";
        public override string ChartCriteria { get; set; } = "h <= 60ft, 7deg < slope <= 20deg";

        private readonly Chapter27and30_GCpCurveBase lowerFigure; // e.g. 27deg figure
        private readonly Chapter27and30_GCpCurveBase upperFigure; // e.g. 45deg figure
        private readonly double lowerAngle;
        private readonly double upperAngle;
        private readonly double targetAngle;

        /// <summary>
        /// Constructs an interpolated figure between two base figures.
        /// Angles must satisfy lowerAngle < targetAngle < upperAngle.
        /// </summary>
        public Figure30_3_2F_2G_INTERPOLATED_ASCE7_22(
            Chapter27and30_GCpCurveBase lowerFigure,
            double lowerAngle,
            Chapter27and30_GCpCurveBase upperFigure,
            double upperAngle,
            double targetAngle)
        {
            if (lowerAngle >= upperAngle)
                throw new ArgumentException("lowerAngle must be less than upperAngle");
            if (targetAngle < lowerAngle || targetAngle > upperAngle)
                throw new ArgumentException("targetAngle must be between lowerAngle and upperAngle");

            this.lowerFigure = lowerFigure ?? throw new ArgumentNullException(nameof(lowerFigure));
            this.upperFigure = upperFigure ?? throw new ArgumentNullException(nameof(upperFigure));
            this.lowerAngle = lowerAngle;
            this.upperAngle = upperAngle;
            this.targetAngle = targetAngle;

            ChartTitle = $"Interpolated GCp Curves between {lowerAngle}° and {upperAngle}° at {targetAngle}°";
            ChartCriteria = $"Interpolation between {lowerAngle}° and {upperAngle}° roof slopes";

            RoofCurves_Neg = InterpolateCurves(lowerFigure.RoofCurves_Neg, upperFigure.RoofCurves_Neg);
            RoofCurves_Pos = InterpolateCurves(lowerFigure.RoofCurves_Pos, upperFigure.RoofCurves_Pos);
        }

        private Dictionary<string, ExternalGCpCurve> InterpolateCurves(
            IReadOnlyDictionary<string, ExternalGCpCurve> lowerCurves,
            IReadOnlyDictionary<string, ExternalGCpCurve> upperCurves)
        {
            var result = new Dictionary<string, ExternalGCpCurve>();

            foreach (var zone in lowerCurves.Keys)
            {
                if (!upperCurves.ContainsKey(zone))
                    throw new InvalidOperationException($"Upper curves missing zone '{zone}'.");

                ExternalGCpCurve lowerCurve = lowerCurves[zone];
                ExternalGCpCurve upperCurve = upperCurves[zone];

                var interpolatedPoints = InterpolateCurvePoints(
                    lowerCurve, upperCurve, lowerAngle, upperAngle, targetAngle);

                result[zone] = new ExternalGCpCurve(interpolatedPoints);
            }

            return result;
        }

        private (double X, double Y)[] InterpolateCurvePoints(
            ExternalGCpCurve lowerCurve,
            ExternalGCpCurve upperCurve,
            double lowerAngle,
            double upperAngle,
            double targetAngle)
        {
            // Both curves must have the same number of points (e.g. 8)
            var lowerPoints = lowerCurve.GetPoints();
            var upperPoints = upperCurve.GetPoints();

            if (lowerPoints.Length != upperPoints.Length)
                throw new InvalidOperationException("Curves must have the same number of points.");

            var interpolated = new (double X, double Y)[lowerPoints.Length];

            double t = (targetAngle - lowerAngle) / (upperAngle - lowerAngle);

            for (int i = 0; i < lowerPoints.Length; i++)
            {
                double xLower = lowerPoints[i].X;
                double xUpper = upperPoints[i].X;
                if (Math.Abs(xLower - xUpper) > 1e-6)
                    throw new InvalidOperationException($"Curve points X mismatch at index {i}: {xLower} vs {xUpper}");

                double yLower = lowerPoints[i].Y;
                double yUpper = upperPoints[i].Y;

                double yInterp = yLower + t * (yUpper - yLower);
                interpolated[i] = (xLower, yInterp);
            }

            return interpolated;
        }
    }
}
