using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace ASCE7WindLoadCalculator
{
    /// <summary>
    /// Converts the engine's WPF-free geometry types to WPF types at the drawing boundary.
    /// </summary>
    public static class GeometryInterop
    {
        public static Point ToPoint(this Vec2 v) => new Point(v.X, v.Y);

        public static Vec2 ToVec2(this Point p) => new Vec2(p.X, p.Y);

        public static Rect ToRect(this Bounds2 b) => b.IsEmpty ? Rect.Empty : new Rect(b.X, b.Y, b.Width, b.Height);

        public static List<Point> ToPoints(this IEnumerable<Vec2> pts) => pts.Select(ToPoint).ToList();
    }
}
