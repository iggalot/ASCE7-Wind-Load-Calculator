using System;

namespace ASCE7WindLoadCalculator
{
    /// <summary>
    /// Immutable 2D point/vector in feet. Replaces System.Windows.Point so the engine has no WPF dependency.
    /// </summary>
    public readonly struct Vec2 : IEquatable<Vec2>
    {
        public double X { get; }
        public double Y { get; }

        public Vec2(double x, double y) { X = x; Y = y; }

        public static Vec2 operator +(Vec2 a, Vec2 b) => new Vec2(a.X + b.X, a.Y + b.Y);
        public static Vec2 operator -(Vec2 a, Vec2 b) => new Vec2(a.X - b.X, a.Y - b.Y);
        public static Vec2 operator *(Vec2 a, double s) => new Vec2(a.X * s, a.Y * s);
        public static bool operator ==(Vec2 a, Vec2 b) => a.Equals(b);
        public static bool operator !=(Vec2 a, Vec2 b) => !a.Equals(b);

        public bool Equals(Vec2 other) => X == other.X && Y == other.Y;
        public override bool Equals(object obj) => obj is Vec2 v && Equals(v);
        public override int GetHashCode() => unchecked((X.GetHashCode() * 397) ^ Y.GetHashCode());
        public override string ToString() => $"({X:0.###}, {Y:0.###})";
    }

    /// <summary>
    /// Axis-aligned bounding rectangle. Replaces System.Windows.Rect in the engine.
    /// </summary>
    public readonly struct Bounds2
    {
        public double X { get; }
        public double Y { get; }
        public double Width { get; }
        public double Height { get; }
        public bool IsEmpty { get; }

        public Bounds2(double x, double y, double width, double height)
        {
            X = x; Y = y; Width = width; Height = height; IsEmpty = false;
        }

        private Bounds2(bool empty) { X = Y = Width = Height = 0; IsEmpty = empty; }

        public static Bounds2 Empty { get; } = new Bounds2(true);
    }
}
