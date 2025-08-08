using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ASCE7WindLoadCalculator
{
    public static class EffectiveWindAreaRenderer
    {
        public static Path DrawEffectiveWindArea(Canvas canvas, BuildingData buildingData, EffectiveWindArea area,
            Rect boundingRect, double offsetX, double offsetY, 
            Brush fillBrush, Brush strokeBrush, double strokeThickness = 1, double marginRatio = 0.1)
        {
            var length = buildingData.BuildingLength;
            
            // Step 1: Gather all world-space points
            var allPoints = area.OuterBoundary.Concat(area.Holes.SelectMany(h => h)).ToList();

            // Step 2: Compute world bounds
            double minX = allPoints.Min(p => p.X);
            double maxX = allPoints.Max(p => p.X);
            double minY = allPoints.Min(p => p.Y);
            double maxY = allPoints.Max(p => p.Y);

            double worldWidth = maxX - minX;
            double worldHeight = maxY - minY;

            double canvasWidth = canvas.ActualWidth > 0 ? canvas.ActualWidth : canvas.Width;
            double canvasHeight = canvas.ActualHeight > 0 ? canvas.ActualHeight : canvas.Height;

            if (canvasWidth <= 0 || canvasHeight <= 0 || worldWidth == 0 || worldHeight == 0)
                return null; // prevent divide by zero

            double marginX = canvasWidth * marginRatio;
            double marginY = canvasHeight * marginRatio;

            double usableWidth = canvasWidth - 2 * marginX;
            double usableHeight = canvasHeight - 2 * marginY;

            double ridgeHeight = buildingData.RidgeHeight;

            double scaleX = usableWidth / boundingRect.Width;
            double scaleY = usableHeight / boundingRect.Height;

            // Step 3: Compute scale (preserving aspect ratio)
            double scale = Math.Min(scaleX, scaleY);

            //double offsetX = (canvasWidth - length * scale);
            //double offsetY = (canvasHeight - ridgeHeight * scale);
            //double offsetX = (usableWidth - (boundingRect.X + boundingRect.Width)) / 2.0;
            //double offsetY = (usableHeight - (boundingRect.Y + boundingRect.Height)) / 2.0; ;
            var geometry = new PathGeometry { FillRule = FillRule.EvenOdd };

            // Step 5: Add transformed outer boundary
            var outerFigure = CreatePathFigure(area.OuterBoundary, scale, offsetX, offsetY, canvasHeight);
            geometry.Figures.Add(outerFigure);

            // Step 6: Add transformed holes
            foreach (var hole in area.Holes)
            {
                var holeFigure = CreatePathFigure(hole, scale, offsetX, offsetY, canvasHeight);
                geometry.Figures.Add(holeFigure);
            }

            // Step 7: Draw
            var path = new Path
            {
                Data = geometry,
                Fill = fillBrush,
                Stroke = strokeBrush,
                StrokeThickness = strokeThickness
            };

            canvas.Children.Add(path);
            return path;
        }

        private static PathFigure CreatePathFigure(List<Point> points, double scale, double offsetX, double offsetY, double canvasHeight)
        {
            double ToCanvasX(double x) => offsetX + x * scale;
            double ToCanvasY(double y) => canvasHeight - (offsetY + y * scale);
            Point ToCanvas(Point pt)
            {
                return new Point(
                    ToCanvasX(pt.X),
                    ToCanvasY(pt.Y)
                );
            }

            var transformedPoints = points.Select(ToCanvas).ToList();

            var figure = new PathFigure
            {
                StartPoint = transformedPoints[0],
                IsClosed = true,
                IsFilled = true
            };

            var segments = new List<LineSegment>();
            for (int i = 1; i < transformedPoints.Count; i++)
            {
                segments.Add(new LineSegment(transformedPoints[i], true));
            }

            figure.Segments = new PathSegmentCollection(segments);
            return figure;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="region">Should be in the form of "1", "2e", etc. The regions will need a full name of with a prefix of "Zone"
        /// so "Zone1" becomes "1"</param>
        /// <returns></returns>
        public static Brush GetColorForRegion(string region)
        {
            switch (region)
            {
                case "1":
                    return Brushes.Red;
                case "1'":
                    return Brushes.IndianRed;
                case "2":
                    return Brushes.Yellow;
                case "2e":
                    return Brushes.LightYellow;
                case "2r":
                    return Brushes.Goldenrod;
                case "2n":
                    return Brushes.YellowGreen;
                case "3":
                    return Brushes.Green;
                case "3e":
                    return Brushes.GreenYellow;
                case "3r":
                    return Brushes.LightGreen;
                case "4":
                    return Brushes.MediumOrchid;
                case "5":
                    return Brushes.Purple;
                case "WWR":
                    return Brushes.LightGray;
                case "LWR":
                    return Brushes.Gray;
                case "WW":
                    return Brushes.LightGray;
                case "LW":
                    return Brushes.Gray;
                case "SW":
                    return Brushes.DarkGray;
                default:
                    return Brushes.Black;
            }
        }
    }

}
