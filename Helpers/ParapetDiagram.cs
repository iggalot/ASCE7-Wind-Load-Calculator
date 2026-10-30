using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ASCE7WindLoadCalculator
{
    /// <summary>
    /// Schematic section through a flat-roof building with parapets (not to scale), showing the parapet pressures as
    /// arrows: push (toward a surface) in blue, suction (away from a surface) in red, matching the 3D legend.
    /// </summary>
    public static class ParapetDiagram
    {
        private static readonly Brush Ink = Frozen("#0B0B0B"), InkSecondary = Frozen("#52514E"), Push = Frozen("#1F5FAD"),
                                      Pull = Frozen("#B3261E"), Fill = Frozen("#EEF1F5"), Ground = Frozen("#7A7A75");

        /// <summary>
        /// MWFRS parapets, 27.3.4: pp = qp Kd (GCpn), +1.5 on the windward parapet (pushing on its outer face) and -1.0 on the
        /// leeward parapet (pulling away from its outer face). Both act downwind. Values in psf; heights in ft (labels only).
        /// </summary>
        public static void DrawMwfrs(Canvas c, double wallHeight, double parapetHeight, double qp, double ppWindward, double ppLeeward)
        {
            double w = c.Width, h = c.Height;
            double groundY = h - 28, left = w * 0.30, right = w * 0.70;
            double roofY = groundY - (h - 28 - 70) * 0.62;      // schematic proportions, not to scale
            double topY = roofY - Math.Max(26, (groundY - roofY) * 0.35);
            const double t = 8;                                  // wall and parapet thickness

            // ground, building outline, roof, parapets
            c.Children.Add(Line(left - 60, groundY, right + 60, groundY, Ground, 1.5));
            c.Children.Add(Rect(left, topY, t, groundY - topY, Fill, Ink));
            c.Children.Add(Rect(right - t, topY, t, groundY - topY, Fill, Ink));
            c.Children.Add(Line(left + t, roofY, right - t, roofY, Ink, 2));
            Label(c, "roof", (left + right) / 2 - 12, roofY + 4, InkSecondary, 11);

            // wind
            Arrow(c, 14, topY - 22, left - 70, topY - 22, InkSecondary, 2);
            Label(c, "Wind", 14, topY - 40, InkSecondary, 12);

            // pressure arrows on the parapets (between roof and parapet top)
            double ay = (topY + roofY) / 2;
            Arrow(c, left - 46, ay, left - 2, ay, Push, 3);                    // windward: pushes on the outer face
            Label(c, "+1.5", left - 46, ay - 20, Push, 12, bold: true);
            Label(c, $"{ppWindward:0.0} psf", left - 64, ay + 6, Push, 12);
            Arrow(c, right + 2, ay, right + 46, ay, Pull, 3);                  // leeward: pulls away from the outer face
            Label(c, "-1.0", right + 10, ay - 20, Pull, 12, bold: true);
            Label(c, $"{ppLeeward:0.0} psf", right + 12, ay + 6, Pull, 12);

            // parapet height and qp
            Dim(c, right + 58, topY, roofY, $"{parapetHeight:0.##} ft");
            Label(c, $"qp at the parapet top, z = {wallHeight + parapetHeight:0.#} ft: {qp:0.00} psf", 8, h - 22, InkSecondary, 11);
            Label(c, "Not to scale", w - 74, h - 22, InkSecondary, 10);
        }

        /// <summary>
        /// C&amp;C parapets, Fig. 30.6-1 layout: case A (windward parapet, roof downwind of it) on the left and case B
        /// (leeward parapet, roof upwind of it) on the right. Each face shows its design pressure (psf): push toward the
        /// face in blue, suction away from it in red; the net load on the parapet is written under each case.
        /// </summary>
        public static void DrawCladding(Canvas c, ParapetCladdingCase a, ParapetCladdingCase b)
        {
            double half = c.Width / 2;
            DrawCladdingCase(c, 0, half, a, windwardParapet: true);
            c.Children.Add(Line(half, 10, half, c.Height - 10, Fill, 1));
            DrawCladdingCase(c, half, half, b, windwardParapet: false);
        }

        private static void DrawCladdingCase(Canvas c, double x0, double width, ParapetCladdingCase k, bool windwardParapet)
        {
            double h = c.Height, groundY = h - 46, roofY = groundY - 58, topY = roofY - 52, t = 8;
            double px = windwardParapet ? x0 + width * 0.42 : x0 + width * 0.58 - t;   // parapet x (left edge)
            double ay = (topY + roofY) / 2;

            Label(c, windwardParapet ? "Case A: windward parapet" : "Case B: leeward parapet", x0 + 10, 6, Ink, 12, bold: true);
            Arrow(c, x0 + 12, 34, x0 + 64, 34, InkSecondary, 2);
            Label(c, "Wind", x0 + 70, 26, InkSecondary, 11);

            // wall + parapet, roof on the building side, ground
            c.Children.Add(Rect(px, topY, t, groundY - topY, Fill, Ink));
            if (windwardParapet) c.Children.Add(Line(px + t, roofY, x0 + width - 14, roofY, Ink, 2));
            else c.Children.Add(Line(x0 + 14, roofY, px, roofY, Ink, 2));
            c.Children.Add(Line(x0 + 10, groundY, x0 + width - 10, groundY, Ground, 1.5));
            Label(c, "roof", windwardParapet ? px + 40 : px - 64, roofY + 4, InkSecondary, 11);

            // exterior (front) face is upwind for A, downwind for B; the back face is on the roof side
            double frontX = windwardParapet ? px : px + t, backX = windwardParapet ? px + t : px;
            FaceArrow(c, frontX, ay, outward: windwardParapet ? -1 : 1, k.FrontPressure);
            FaceArrow(c, backX, ay + 18, outward: windwardParapet ? 1 : -1, k.BackPressure);

            Label(c, $"Net on the parapet {k.NetPressure:0.0} psf " + (windwardParapet ? "toward the roof" : "away from the roof"),
                  x0 + 10, groundY + 6, Ink, 11, bold: true);
            Label(c, $"{k.Location}: front {k.FrontSource}, back {k.BackSource}", x0 + 10, groundY + 22, InkSecondary, 10);
        }

        /// <summary>
        /// Arrow on a parapet face. <paramref name="outward"/> is the side the face looks toward (-1 left, +1 right).
        /// Positive pressure pushes toward the face (blue), negative pulls away from it (red).
        /// </summary>
        private static void FaceArrow(Canvas c, double faceX, double y, int outward, double pressure)
        {
            bool push = pressure >= 0;
            double far = faceX + outward * 46, near = faceX + outward * 2;
            if (push) Arrow(c, far, y, near, y, Push, 3); else Arrow(c, near, y, far, y, Pull, 3);
            double lx = outward < 0 ? far - 4 : near + 16;
            Label(c, $"{pressure:+0.0;-0.0} psf", outward < 0 ? lx - 22 : lx, y - 18, push ? Push : Pull, 11, bold: true);
        }

        // ------------------------------------------------------------------ primitives

        private static Line Line(double x1, double y1, double x2, double y2, Brush stroke, double thickness) =>
            new Line { X1 = x1, Y1 = y1, X2 = x2, Y2 = y2, Stroke = stroke, StrokeThickness = thickness };

        private static Rectangle Rect(double x, double y, double width, double height, Brush fill, Brush stroke)
        {
            var r = new Rectangle { Width = width, Height = height, Fill = fill, Stroke = stroke, StrokeThickness = 1.2 };
            Canvas.SetLeft(r, x); Canvas.SetTop(r, y);
            return r;
        }

        private static void Arrow(Canvas c, double x1, double y1, double x2, double y2, Brush stroke, double thickness)
        {
            double dx = x2 - x1, dy = y2 - y1, len = Math.Sqrt(dx * dx + dy * dy);
            if (len < 1e-6) return;
            double ux = dx / len, uy = dy / len, head = 6 + 2 * thickness;
            c.Children.Add(Line(x1, y1, x2 - ux * head * 0.8, y2 - uy * head * 0.8, stroke, thickness));
            c.Children.Add(new Polygon
            {
                Fill = stroke,
                Points = new PointCollection
                {
                    new Point(x2, y2),
                    new Point(x2 - ux * head - uy * head * 0.5, y2 - uy * head + ux * head * 0.5),
                    new Point(x2 - ux * head + uy * head * 0.5, y2 - uy * head - ux * head * 0.5),
                },
            });
        }

        private static void Dim(Canvas c, double x, double yTop, double yBottom, string text)
        {
            c.Children.Add(Line(x, yTop, x, yBottom, InkSecondary, 1));
            c.Children.Add(Line(x - 4, yTop, x + 4, yTop, InkSecondary, 1));
            c.Children.Add(Line(x - 4, yBottom, x + 4, yBottom, InkSecondary, 1));
            Label(c, text, x + 6, (yTop + yBottom) / 2 - 8, InkSecondary, 11);
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
