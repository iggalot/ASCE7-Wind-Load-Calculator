using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ASCE7WindLoadCalculator
{
    public static class FigureDrawer
    {
        public static void DrawCurvesOnCanvas(Canvas canvas, Chapter27and30_GCpCurveBase figure)
        {
            double canvasWidth = canvas.ActualWidth > 0 ? canvas.ActualWidth : canvas.Width;
            double canvasHeight = canvas.ActualHeight > 0 ? canvas.ActualHeight : canvas.Height;

            double xMin = 1;
            double xMax = 1000;
            double yMinNeg = -4;
            double yMaxPos = 1;

            canvas.Children.Clear();

            List<double> xTickValues = new List<double> { 1, 10, 20, 50, 100, 200, 500, 1000 };
            DrawGrid(canvas, canvasWidth, canvasHeight, xMin, xMax, yMinNeg, yMaxPos, xTickValues);

            var allCurves = new List<(string label, ExternalGCpCurve curve, bool isNegative)>();

            if(figure != null)
            {
                foreach (var kvp in figure.RoofCurves_Neg)
                    allCurves.Add((kvp.Key, kvp.Value, true));
                foreach (var kvp in figure.RoofCurves_Pos)
                    allCurves.Add((kvp.Key, kvp.Value, false));
                foreach (var kvp in figure.WallCurves_Neg)
                    allCurves.Add((kvp.Key, kvp.Value, true));
                foreach (var kvp in figure.WallCurves_Pos)
                    allCurves.Add((kvp.Key, kvp.Value, false));
            }

            var groupedByShape = allCurves
                .GroupBy(item => string.Join("_", item.curve.GetPoints().Select(p => $"{p.X:F3}_{p.Y:F3}")))
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var group in groupedByShape)
            {
                var curveGroup = group.Value;

                for (int i = 0; i < curveGroup.Count; i++)
                {
                    var (label, curve, isNegative) = curveGroup[i];
                    Brush color = isNegative ? Brushes.Red : Brushes.Blue;

                    DrawCurve(canvas, curve, label, color, isNegative,
                              canvasWidth, canvasHeight, xMin, xMax, yMinNeg, yMaxPos,
                              zoneIndex: i);
                }
            }
        }

        private static void DrawCurve(Canvas canvas, ExternalGCpCurve curve, string label, Brush color, bool isNegative,
                                      double canvasWidth, double canvasHeight,
                                      double xMin, double xMax, double yMin, double yMax,
                                      int zoneIndex)
        {
            Polyline line = new Polyline
            {
                Stroke = color,
                StrokeThickness = 2
            };

            var points = curve.GetPoints();
            List<Point> linePoints = new List<Point>();

            foreach (var (x, y) in points)
            {
                double px = ((Math.Log10(x) - Math.Log10(xMin)) / (Math.Log10(xMax) - Math.Log10(xMin))) * canvasWidth;
                double py = ((y - yMin) / (yMax - yMin)) * canvasHeight;
                linePoints.Add(new Point(px, py));
            }

            foreach (Point pt in linePoints)
                line.Points.Add(pt);

            canvas.Children.Add(line);

            string displayLabel = label.StartsWith("Zone", StringComparison.OrdinalIgnoreCase)
                ? label.Substring(4).TrimStart()
                : label;

            TextBlock labelText = new TextBlock
            {
                Text = displayLabel,
                Foreground = color,
                FontSize = 10,
                Background = Brushes.White
            };

            labelText.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            double labelW = labelText.DesiredSize.Width;
            double labelH = labelText.DesiredSize.Height;

            double startX = 3;
            startX = Math.Max(xMin, Math.Min(xMax, startX));
            double pxStart = ((Math.Log10(startX) - Math.Log10(xMin)) / (Math.Log10(xMax) - Math.Log10(xMin))) * canvasWidth;

            double yAtStartX = curve.Evaluate(startX);
            double pyAtStartX = ((yAtStartX - yMin) / (yMax - yMin)) * canvasHeight;

            double circleCenterX = pxStart + zoneIndex * 18;
            double circleCenterY = pyAtStartX - labelH / 2 - 2.5;
            double padding = 1;
            double circleDiameter = Math.Max(labelW, labelH) + padding * 2;

            Ellipse circle = new Ellipse
            {
                Width = circleDiameter,
                Height = circleDiameter,
                Stroke = color,
                StrokeThickness = 1,
                Fill = Brushes.White
            };
            Canvas.SetLeft(circle, circleCenterX - circleDiameter / 2);
            Canvas.SetTop(circle, circleCenterY - circleDiameter / 2);
            canvas.Children.Add(circle);

            Canvas.SetLeft(labelText, circleCenterX - labelW / 2);
            Canvas.SetTop(labelText, circleCenterY - labelH / 2);
            canvas.Children.Add(labelText);

            TextBlock leftYLabel = new TextBlock
            {
                Text = points.First().Y.ToString("0.00"),
                Foreground = color,
                FontSize = 10,
                Background = Brushes.White
            };
            leftYLabel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            Canvas.SetLeft(leftYLabel, linePoints.First().X - 15 - leftYLabel.DesiredSize.Width / 2);
            Canvas.SetTop(leftYLabel, linePoints.First().Y - leftYLabel.DesiredSize.Height / 2);
            canvas.Children.Add(leftYLabel);

            TextBlock rightYLabel = new TextBlock
            {
                Text = points.Last().Y.ToString("0.00"),
                Foreground = color,
                FontSize = 10,
                Background = Brushes.White
            };
            rightYLabel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            Canvas.SetLeft(rightYLabel, linePoints.Last().X + 15 - rightYLabel.DesiredSize.Width / 2);
            Canvas.SetTop(rightYLabel, linePoints.Last().Y - rightYLabel.DesiredSize.Height / 2);
            canvas.Children.Add(rightYLabel);
        }

        private static void DrawGrid(Canvas canvas, double canvasWidth, double canvasHeight,
                                     double xMin, double xMax, double yMin, double yMax,
                                     IEnumerable<double> xValuesToLabel)
        {
            foreach (double xVal in xValuesToLabel)
            {
                if (xVal <= 0) continue;

                double px = ((Math.Log10(xVal) - Math.Log10(xMin)) / (Math.Log10(xMax) - Math.Log10(xMin))) * canvasWidth;

                canvas.Children.Add(new Line
                {
                    X1 = px,
                    Y1 = 0,
                    X2 = px,
                    Y2 = canvasHeight,
                    Stroke = Brushes.Gray,
                    StrokeThickness = 0.5,
                    StrokeDashArray = new DoubleCollection() { 4, 2 }
                });

                canvas.Children.Add(new Line
                {
                    X1 = px,
                    Y1 = canvasHeight - 6,
                    X2 = px,
                    Y2 = canvasHeight,
                    Stroke = Brushes.Black,
                    StrokeThickness = 1
                });

                TextBlock xLabel = new TextBlock
                {
                    Text = xVal >= 1000 ? $"{xVal / 1000:0.#}k" : xVal.ToString("0"),
                    FontSize = 10,
                    Foreground = Brushes.Black
                };
                xLabel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                Canvas.SetLeft(xLabel, px - xLabel.DesiredSize.Width / 2);
                Canvas.SetTop(xLabel, canvasHeight - 18);
                canvas.Children.Add(xLabel);
            }

            double yStep = 0.2;
            int yDivisions = (int)Math.Round((yMax - yMin) / yStep);
            for (int i = 0; i <= yDivisions; i++)
            {
                double y = yMin + i * yStep;
                double py = ((y - yMin) / (yMax - yMin)) * canvasHeight;

                canvas.Children.Add(new Line
                {
                    X1 = 0,
                    Y1 = py,
                    X2 = canvasWidth,
                    Y2 = py,
                    Stroke = Brushes.LightGray,
                    StrokeThickness = 0.5
                });

                TextBlock yLabel = new TextBlock
                {
                    Text = y.ToString("0.0"),
                    FontSize = 10,
                    Foreground = Brushes.Black
                };
                yLabel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                Canvas.SetLeft(yLabel, 2);
                Canvas.SetTop(yLabel, py - yLabel.DesiredSize.Height / 2);
                canvas.Children.Add(yLabel);
            }

            double[] dashedYVals = new double[] { -4, -3.5, -3, -2.5, -2, -1.5, -1, -0.5, 0, 0.5, 1 };
            foreach (double y in dashedYVals)
            {
                if (y < yMin || y > yMax) continue;

                double py = ((y - yMin) / (yMax - yMin)) * canvasHeight;

                canvas.Children.Add(new Line
                {
                    X1 = 0,
                    Y1 = py,
                    X2 = canvasWidth,
                    Y2 = py,
                    Stroke = Brushes.Gray,
                    StrokeThickness = 1,
                    StrokeDashArray = new DoubleCollection() { 4, 2 }
                });
            }
        }
    }
}
