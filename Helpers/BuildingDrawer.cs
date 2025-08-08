using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ASCE7WindLoadCalculator
{
    public static class BuildingDrawer
    {
        /// <summary>
        /// Draws a plan view of the building to the provided Canvas.
        /// The building is drawn with the origin (0,0) in bottom-left corner.
        /// </summary>
        public static void DrawPlan(Canvas canvas, BuildingData buildingData, double marginRatio = 0.1)
        {
            if (canvas == null || buildingData == null) return;

            canvas.Children.Clear();

            double length = buildingData.BuildingLength;
            double width = buildingData.BuildingWidth;

            if (length <= 0 || width <= 0) return;

            double canvasWidth = canvas.ActualWidth > 0 ? canvas.ActualWidth : canvas.Width;
            double canvasHeight = canvas.ActualHeight > 0 ? canvas.ActualHeight : canvas.Height;

            if (canvasWidth <= 0 || canvasHeight <= 0) return;

            double marginX = canvasWidth * marginRatio;
            double marginY = canvasHeight * marginRatio;

            double usableWidth = canvasWidth - 2 * marginX;
            double usableHeight = canvasHeight - 2 * marginY;

            double scaleX = usableWidth / length;
            double scaleY = usableHeight / width;
            double scale = Math.Min(scaleX, scaleY);

            double offsetX = (canvasWidth - length * scale) / 2.0;
            double offsetY = (canvasHeight - width * scale) / 2.0;

            Point ToCanvas(Point pt) => new Point(offsetX + pt.X * scale, canvasHeight - (offsetY + pt.Y * scale));
            double ToCanvasX(double x) => offsetX + x * scale;
            double ToCanvasY(double y) => canvasHeight - (offsetY + y * scale);

            // Draw building outline
            var corners = new[]
            {
                new Point(0, 0),
                new Point(length, 0),
                new Point(length, width),
                new Point(0, width),
    };

            Polygon outline = new Polygon
            {
                Stroke = Brushes.Black,
                StrokeThickness = 2,
                Fill = Brushes.Transparent
            };
            foreach (var pt in corners)
                outline.Points.Add(ToCanvas(pt));
            canvas.Children.Add(outline);

            // Draw ridge line (if applicable)
            if (buildingData.RoofTypeIsSloped())
            {
                Point ridgeStart, ridgeEnd;
                if (buildingData.RidgeDirection == RidgeDirections.RIDGE_DIR_PARALLEL_TO_BLDGLENGTH)
                {
                    double y = width / 2.0;
                    ridgeStart = new Point(0, y);
                    ridgeEnd = new Point(length, y);
                }
                else if (buildingData.RidgeDirection == RidgeDirections.RIDGE_DIR_PERP_TO_BLDGLENGTH)
                {
                    double x = length / 2.0;
                    ridgeStart = new Point(x, 0);
                    ridgeEnd = new Point(x, width);
                }
                else
                {
                    ridgeStart = ridgeEnd = new Point(0, 0);
                }

                if (ridgeStart != ridgeEnd)
                {
                    var ridge = new Line
                    {
                        X1 = ToCanvas(ridgeStart).X,
                        Y1 = ToCanvas(ridgeStart).Y,
                        X2 = ToCanvas(ridgeEnd).X,
                        Y2 = ToCanvas(ridgeEnd).Y,
                        Stroke = Brushes.DarkRed,
                        StrokeThickness = 2,
                        StrokeDashArray = new DoubleCollection { 4, 2 }
                    };
                    canvas.Children.Add(ridge);
                }
            }

            //// Origin marker
            //var origin = new Ellipse
            //{
            //    Width = 6,
            //    Height = 6,
            //    Fill = Brushes.Blue
            //};
            //Point originPt = ToCanvas(new Point(0, 0));
            //Canvas.SetLeft(origin, originPt.X - 3);
            //Canvas.SetTop(origin, originPt.Y - 3);
            //canvas.Children.Add(origin);

            // Length label (bottom edge, centered horizontally)
            TextBlock lengthLabel = new TextBlock
            {
                Text = $"L: {length} ft",
                Foreground = Brushes.Black,
                FontWeight = FontWeights.Bold
            };
            lengthLabel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            double topMargin = 0;
            Size lengthSize = lengthLabel.DesiredSize;
            Canvas.SetLeft(lengthLabel, ToCanvasX(length / 2.0) - lengthSize.Width / 2.0);
            Canvas.SetTop(lengthLabel, ToCanvasY(0) + topMargin);
            canvas.Children.Add(lengthLabel);

            // Width label (left edge, vertical center, rotated -90 degrees)
            TextBlock widthLabel = new TextBlock
            {
                Text = $"W: {width} ft",
                Foreground = Brushes.Black,
                FontWeight = FontWeights.Bold,
                RenderTransformOrigin = new Point(0.5, 0.5),
                RenderTransform = new RotateTransform(-90)
            };
            widthLabel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            Size widthSize = widthLabel.DesiredSize;

            double leftMargin = 8; // pixels to move left outside the building

            double widthLabelX = ToCanvasX(0) - (widthSize.Width / 2.0) - leftMargin;
            double widthLabelY = ToCanvasY(width / 2.0) - (widthSize.Height / 2.0);

            Canvas.SetLeft(widthLabel, widthLabelX);
            Canvas.SetTop(widthLabel, widthLabelY);
            canvas.Children.Add(widthLabel);

            TextBlock title = new TextBlock
            {
                Text = "PLAN VIEW",
                Foreground = Brushes.Black,
                FontWeight = FontWeights.Bold,
                FontSize = 14
            };
            title.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            Size titleSize = title.DesiredSize;

            // Center horizontally
            double titleX = (canvasWidth - titleSize.Width) / 2.0;
            // Place a bit above bottom edge (e.g., 5 px margin)
            double titleY = canvasHeight - titleSize.Height;

            Canvas.SetLeft(title, titleX);
            //            Canvas.SetTop(title, titleY);

            canvas.Children.Add(title);
        }

        public static void DrawElevation_BuildingLength(Canvas canvas, BuildingData buildingData, double marginRatio = 0.1)
        {
            if (canvas == null || buildingData == null) return;

            canvas.Children.Clear();

            double length = buildingData.BuildingLength;
            double eaveHeight = buildingData.BuildingHeight;
            double roofPitchDeg = buildingData.RoofPitch;
            bool isGable = buildingData.RidgeDirection == RidgeDirections.RIDGE_DIR_PERP_TO_BLDGLENGTH;

            if (length <= 0 || eaveHeight <= 0) return;

            double canvasWidth = canvas.ActualWidth > 0 ? canvas.ActualWidth : canvas.Width;
            double canvasHeight = canvas.ActualHeight > 0 ? canvas.ActualHeight : canvas.Height;

            if (canvasWidth <= 0 || canvasHeight <= 0) return;

            // Calculate ridge height if sloped gable roof
            double ridgeHeight = buildingData.RidgeHeight;

            // Scale and margin
            double marginX = canvasWidth * marginRatio;
            double marginY = canvasHeight * marginRatio;

            double usableWidth = canvasWidth - 2 * marginX;
            double usableHeight = canvasHeight - 2 * marginY;

            double scaleX = usableWidth / length;
            double scaleY = usableHeight / ridgeHeight;
            double scale = Math.Min(scaleX, scaleY);

            double offsetX = (canvasWidth - length * scale) / 2.0;
            double offsetY = (canvasHeight - ridgeHeight * scale) / 2.0;

            Point ToCanvas(Point pt)
            {
                return new Point(
                    offsetX + pt.X * scale,
                    canvasHeight - (offsetY + pt.Y * scale)
                );
            }

            // Draw wall elevation outline without bottom line
            Point[] points;

            if (isGable && buildingData.RoofType == RoofTypes.ROOF_TYPE_GABLE)
            {
                double halfLength = length / 2.0;

                points = new[]
                {
            new Point(0, 0),                         // Bottom-left
            new Point(0, eaveHeight),               // Left wall
            new Point(halfLength, ridgeHeight),     // Roof peak
            new Point(length, eaveHeight),          // Right wall top
            new Point(length, 0)                    // Bottom-right
            // NOTE: No return to (0, 0)
        };
            }
            else
            {
                // Flat, hip, or ridge parallel
                points = new[]
                {
            new Point(0, 0),
            new Point(0, eaveHeight),
            new Point(length, eaveHeight),
            new Point(length, 0)
            // NOTE: No return to (0, 0)
        };
            }

            Polyline wallOutline = new Polyline
            {
                Stroke = Brushes.Black,
                StrokeThickness = 2
            };

            foreach (var pt in points)
                wallOutline.Points.Add(ToCanvas(pt));

            canvas.Children.Add(wallOutline);

            // Draw the ground line
            // Add dashed green ground line between bottom of walls
            Point groundLeft = ToCanvas(new Point(-5, 0));
            Point groundRight = ToCanvas(new Point(length + 5, 0));

            Line groundLine = new Line
            {
                X1 = groundLeft.X,
                Y1 = groundLeft.Y,
                X2 = groundRight.X,
                Y2 = groundRight.Y,
                Stroke = Brushes.DarkGreen,
                StrokeThickness = 1.5,
                StrokeDashArray = new DoubleCollection { 4, 2 }
            };

            canvas.Children.Add(groundLine);

            // Draw origin marker
            var origin = new Ellipse
            {
                Width = 6,
                Height = 6,
                Fill = Brushes.Blue
            };
            Point originPt = ToCanvas(new Point(0, 0));
            Canvas.SetLeft(origin, originPt.X - 3);
            Canvas.SetTop(origin, originPt.Y - 3);
            canvas.Children.Add(origin);

            // Label: Wall Width (L:)
            var labelWidth = new TextBlock
            {
                Text = $"L: {length} ft",
                FontWeight = FontWeights.Bold,
                FontSize = 12,
                Foreground = Brushes.DarkSlateBlue
            };
            double midX = offsetX + (length * scale) / 2.0;
            Canvas.SetLeft(labelWidth, midX - 10);
            Canvas.SetTop(labelWidth, canvasHeight - offsetY + 4);
            canvas.Children.Add(labelWidth);

            // Label: Wall Height (H:) on left
            var labelHeight = new TextBlock
            {
                Text = $"H: {eaveHeight} ft",
                FontWeight = FontWeights.Bold,
                FontSize = 12,
                Foreground = Brushes.DarkSlateBlue,
                LayoutTransform = new RotateTransform(-90)
            };
            double midY = canvasHeight - (offsetY + eaveHeight * scale / 2.0);
            Canvas.SetLeft(labelHeight, offsetX - 20);
            Canvas.SetTop(labelHeight, midY - 10);
            canvas.Children.Add(labelHeight);

            // Label: Roof Height (R:) if sloped
            if (buildingData.RidgeHeight > buildingData.BuildingHeight)
            {
                var labelRoof = new TextBlock
                {
                    Text = $"R: {ridgeHeight:F2} ft",
                    FontWeight = FontWeights.Bold,
                    FontSize = 12,
                    Foreground = Brushes.DarkRed
                };

                double ridgeX = offsetX + (length * scale) / 2.0;
                double ridgeY = canvasHeight - (offsetY + ridgeHeight * scale);
                Canvas.SetLeft(labelRoof, ridgeX - 8);
                Canvas.SetTop(labelRoof, ridgeY - 16);
                canvas.Children.Add(labelRoof);
            }

            // Add title at bottom center
            var labelTitle = new TextBlock
            {
                Text = "ELEV. BLDG LENGTH",
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                Foreground = Brushes.DarkSlateGray
            };
            labelTitle.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            double labelX = (canvasWidth - labelTitle.DesiredSize.Width) / 2;
            double labelY = canvasHeight - 20;
            Canvas.SetLeft(labelTitle, labelX);
            canvas.Children.Add(labelTitle);

            // Line from left to right wall at mean roof height
            var meanPtLeft = ToCanvas(new Point(0, buildingData.MeanRoofHeight));
            var meanPtRight = ToCanvas(new Point(length, buildingData.MeanRoofHeight));
            Line dashedLine = new Line
            {
                X1 = meanPtLeft.X,
                Y1 = meanPtLeft.Y,
                X2 = meanPtRight.X,
                Y2 = meanPtRight.Y,
                Stroke = Brushes.DarkRed,
                StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection { 4, 2 }
            };
            canvas.Children.Add(dashedLine);

            // Add label for mean roof height
            var labelMeanRoof = new TextBlock
            {
                Text = $"h: {buildingData.MeanRoofHeight:F2} ft",
                FontWeight = FontWeights.Bold,
                FontSize = 12,
                Foreground = Brushes.DarkRed
            };

            double labelX_h = offsetX + length * scale + 5;
            double labelY_h = meanPtLeft.Y - 10;
            Canvas.SetLeft(labelMeanRoof, labelX_h);
            Canvas.SetTop(labelMeanRoof, labelY_h);
            canvas.Children.Add(labelMeanRoof);
        }

        public static void DrawElevation_BuildingWidth(Canvas canvas, BuildingData buildingData, double marginRatio = 0.1)
        {
            if (canvas == null || buildingData == null) return;

            canvas.Children.Clear();

            double length = buildingData.BuildingWidth;
            double eaveHeight = buildingData.BuildingHeight;
            double roofPitchDeg = buildingData.RoofPitch;
            bool isGable = buildingData.RidgeDirection == RidgeDirections.RIDGE_DIR_PARALLEL_TO_BLDGLENGTH;

            if (length <= 0 || eaveHeight <= 0) return;

            double canvasWidth = canvas.ActualWidth > 0 ? canvas.ActualWidth : canvas.Width;
            double canvasHeight = canvas.ActualHeight > 0 ? canvas.ActualHeight : canvas.Height;

            if (canvasWidth <= 0 || canvasHeight <= 0) return;

            double ridgeHeight = buildingData.RidgeHeight;

            double marginX = canvasWidth * marginRatio;
            double marginY = canvasHeight * marginRatio;

            double usableWidth = canvasWidth - 2 * marginX;
            double usableHeight = canvasHeight - 2 * marginY;

            double scaleX = usableWidth / length;
            double scaleY = usableHeight / ridgeHeight;
            double scale = Math.Min(scaleX, scaleY);

            double offsetX = (canvasWidth - length * scale) / 2.0;
            double offsetY = (canvasHeight - ridgeHeight * scale) / 2.0;

            Point ToCanvas(Point pt)
            {
                return new Point(
                    offsetX + pt.X * scale,
                    canvasHeight - (offsetY + pt.Y * scale)
                );
            }

            // Wall outline points (open base)
            Point[] points;
            if (isGable && buildingData.RoofType == RoofTypes.ROOF_TYPE_GABLE)
            {
                double halfLength = length / 2.0;
                points = new[]
                {
                    new Point(0, 0),
                    new Point(0, eaveHeight),
                    new Point(halfLength, ridgeHeight),
                    new Point(length, eaveHeight),
                    new Point(length, 0)
                };
            }
            else
            {
                points = new[]
                {
                    new Point(0, 0),
                    new Point(0, eaveHeight),
                    new Point(length, eaveHeight),
                    new Point(length, 0)
                };
            }

            // Draw wall outline as open polyline
            Polyline wallOutline = new Polyline
            {
                Stroke = Brushes.Black,
                StrokeThickness = 2
            };
            foreach (var pt in points)
                wallOutline.Points.Add(ToCanvas(pt));
            canvas.Children.Add(wallOutline);

            // Draw dashed green ground line
            Point groundLeft = ToCanvas(new Point(-5, 0));
            Point groundRight = ToCanvas(new Point(length + 5, 0));
            Line groundLine = new Line
            {
                X1 = groundLeft.X,
                Y1 = groundLeft.Y,
                X2 = groundRight.X,
                Y2 = groundRight.Y,
                Stroke = Brushes.DarkGreen,
                StrokeThickness = 1.5,
                StrokeDashArray = new DoubleCollection { 4, 2 }
            };
            canvas.Children.Add(groundLine);

            // Origin marker
            var origin = new Ellipse
            {
                Width = 6,
                Height = 6,
                Fill = Brushes.Blue
            };
            Point originPt = ToCanvas(new Point(0, 0));
            Canvas.SetLeft(origin, originPt.X - 3);
            Canvas.SetTop(origin, originPt.Y - 3);
            canvas.Children.Add(origin);

            // Width label
            var labelWidth = new TextBlock
            {
                Text = $"W: {length} ft",
                FontWeight = FontWeights.Bold,
                FontSize = 12,
                Foreground = Brushes.DarkSlateBlue
            };
            double midX = offsetX + (length * scale) / 2.0;
            Canvas.SetLeft(labelWidth, midX - 10);
            Canvas.SetTop(labelWidth, canvasHeight - offsetY + 4);
            canvas.Children.Add(labelWidth);

            // Height label
            var labelHeight = new TextBlock
            {
                Text = $"H: {eaveHeight} ft",
                FontWeight = FontWeights.Bold,
                FontSize = 12,
                Foreground = Brushes.DarkSlateBlue,
                LayoutTransform = new RotateTransform(-90)
            };
            double midY = canvasHeight - (offsetY + eaveHeight * scale / 2.0);
            Canvas.SetLeft(labelHeight, offsetX - 20);
            Canvas.SetTop(labelHeight, midY - 10);
            canvas.Children.Add(labelHeight);

            // Ridge label (if higher than eave)
            if (buildingData.RidgeHeight > buildingData.BuildingHeight)
            {
                var labelRoof = new TextBlock
                {
                    Text = $"R: {ridgeHeight:F2} ft",
                    FontWeight = FontWeights.Bold,
                    FontSize = 12,
                    Foreground = Brushes.DarkRed
                };
                double ridgeX = offsetX + (length * scale) / 2.0;
                double ridgeY = canvasHeight - (offsetY + ridgeHeight * scale);
                Canvas.SetLeft(labelRoof, ridgeX - 8);
                Canvas.SetTop(labelRoof, ridgeY - 16);
                canvas.Children.Add(labelRoof);
            }

            // Title
            var labelTitle = new TextBlock
            {
                Text = "ELEV. BLDG WIDTH",
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                Foreground = Brushes.DarkSlateGray
            };
            labelTitle.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            double labelX = (canvasWidth - labelTitle.DesiredSize.Width) / 2;
            double labelY = canvasHeight - 20;
            Canvas.SetLeft(labelTitle, labelX);
            canvas.Children.Add(labelTitle);

            // Mean roof height line
            var meanLeft = ToCanvas(new Point(0, buildingData.MeanRoofHeight));
            var meanRight = ToCanvas(new Point(length, buildingData.MeanRoofHeight));
            Line dashedLine = new Line
            {
                X1 = meanLeft.X,
                Y1 = meanLeft.Y,
                X2 = meanRight.X,
                Y2 = meanRight.Y,
                Stroke = Brushes.DarkRed,
                StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection { 4, 2 }
            };
            canvas.Children.Add(dashedLine);

            // Mean roof height label
            var labelMeanRoof = new TextBlock
            {
                Text = $"h: {buildingData.MeanRoofHeight:F2} ft",
                FontWeight = FontWeights.Bold,
                FontSize = 12,
                Foreground = Brushes.DarkRed
            };
            double labelX_h = offsetX + length * scale + 5;
            double labelY_h = meanLeft.Y - 10;
            Canvas.SetLeft(labelMeanRoof, labelX_h);
            Canvas.SetTop(labelMeanRoof, labelY_h);
            canvas.Children.Add(labelMeanRoof);
        }
    }
}
