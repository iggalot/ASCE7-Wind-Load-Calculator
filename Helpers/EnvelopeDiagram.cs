using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ASCE7WindLoadCalculator
{
    /// <summary>
    /// Plan of one Chapter 28 envelope direction (Fig. 28.3-1), wind from the left: windward and leeward walls with their
    /// GCpf, the end zones (2a) at the reference corner shaded, and the roof zones across the ridge for Case A on a gable.
    /// Push (toward a surface) in blue, suction in red, matching the other figures. Not to scale across very long plans.
    /// </summary>
    public static class EnvelopeDiagram
    {
        private static readonly Brush Ink = Frozen("#0B0B0B"), InkSecondary = Frozen("#52514E"), Push = Frozen("#1F5FAD"),
                                      Pull = Frozen("#B3261E"), Fill = Frozen("#EEF1F5"), EndFill = Frozen("#DCE3EE");

        public static void Draw(Canvas c, EnvelopeDirection e)
        {
            double w = c.Width, h = c.Height;
            double availW = w - 200, availH = h - 56;
            double s = Math.Min(availW / e.DepthAlongWind, availH / e.WidthNormalToWind);
            double pw = e.DepthAlongWind * s, ph = e.WidthNormalToWind * s;
            double x0 = 100 + (availW - pw) / 2, y0 = 22 + (availH - ph) / 2;
            double endFt = e.EndZoneWidth, end = endFt * s;

            // building, end-zone band at the reference corner (bottom), ridge for the transverse case
            c.Children.Add(Rect(x0, y0, pw, ph, Fill, Ink));
            c.Children.Add(Rect(x0, y0 + ph - end, pw, end, EndFill, null));
            c.Children.Add(Rect(x0, y0, pw, ph, null, Ink));
            if (e.Transverse) c.Children.Add(new Line { X1 = x0 + pw / 2, Y1 = y0, X2 = x0 + pw / 2, Y2 = y0 + ph, Stroke = InkSecondary, StrokeThickness = 1, StrokeDashArray = new DoubleCollection { 4, 3 } });

            // wind
            Arrow(c, 8, y0 + ph / 2 - 30, 48, y0 + ph / 2 - 30, InkSecondary, 2);
            Label(c, "Wind", 8, y0 + ph / 2 - 50, InkSecondary, 11);

            Func<string, double> g = z => e.Zones.FirstOrDefault(r => r.Zone == z)?.GCpf ?? double.NaN;
            string ww = e.LoadCase == "A" ? "1" : "5", lw = e.LoadCase == "A" ? "4" : "6";
            Zone(c, ww, g(ww), x0 - 92, y0 + (ph - end) / 2 - 8);
            Zone(c, ww + "E", g(ww + "E"), x0 - 92, y0 + ph - end / 2 - 8);
            Zone(c, lw, g(lw), x0 + pw + 8, y0 + (ph - end) / 2 - 8);
            Zone(c, lw + "E", g(lw + "E"), x0 + pw + 8, y0 + ph - end / 2 - 8);
            if (e.Transverse)
            {
                Zone(c, "2", g("2"), x0 + pw * 0.25 - 26, y0 + (ph - end) / 2 - 8);
                Zone(c, "3", g("3"), x0 + pw * 0.75 - 26, y0 + (ph - end) / 2 - 8);
            }

            Label(c, $"End zones {(e.LoadCase == "B" ? "a" : "2a")} = {endFt:0.#} ft at the reference corner (shaded); then each other corner in turn.", 8, h - 30, InkSecondary, 11);
            Label(c, $"Load Case {e.LoadCase}" + (e.LoadCase == "A" ? $", roof angle {e.Theta:0.#}°" : ", wind along the ridge") + ". GCpf shown; + push, − suction.", 8, h - 16, InkSecondary, 11);
        }

        private static void Zone(Canvas c, string zone, double gcpf, double x, double y)
        {
            if (double.IsNaN(gcpf)) return;
            Label(c, $"{zone}: {gcpf:+0.00;−0.00}", x, y, gcpf >= 0 ? Push : Pull, 12, bold: true);
        }

        private static Rectangle Rect(double x, double y, double width, double height, Brush fill, Brush stroke)
        {
            var r = new Rectangle { Width = Math.Max(0, width), Height = Math.Max(0, height), Fill = fill, Stroke = stroke, StrokeThickness = stroke == null ? 0 : 1.2 };
            Canvas.SetLeft(r, x); Canvas.SetTop(r, y);
            return r;
        }

        private static void Arrow(Canvas c, double x1, double y1, double x2, double y2, Brush stroke, double thickness)
        {
            c.Children.Add(new Line { X1 = x1, Y1 = y1, X2 = x2 - 8, Y2 = y2, Stroke = stroke, StrokeThickness = thickness });
            c.Children.Add(new Polygon { Fill = stroke, Points = new PointCollection { new Point(x2, y2), new Point(x2 - 10, y2 - 5), new Point(x2 - 10, y2 + 5) } });
        }

        private static void Label(Canvas c, string text, double x, double y, Brush fg, double size, bool bold = false)
        {
            var tb = new TextBlock { Text = text, Foreground = fg, FontSize = size, FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal };
            Canvas.SetLeft(tb, x); Canvas.SetTop(tb, y);
            c.Children.Add(tb);
        }

        private static Brush Frozen(string hex)
        {
            var b = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
            b.Freeze();
            return b;
        }
    }
}
