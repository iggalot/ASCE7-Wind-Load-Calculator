using System;

namespace ASCE7WindLoadCalculator
{
    /// <summary>
    /// Immutable 3D point/vector in feet. Model axes: x along BuildingLength (east), y along
    /// BuildingWidth (north), z up from grade. Right-handed.
    /// </summary>
    public readonly struct Vec3 : IEquatable<Vec3>
    {
        public double X { get; }
        public double Y { get; }
        public double Z { get; }

        public Vec3(double x, double y, double z) { X = x; Y = y; Z = z; }

        public static Vec3 operator +(Vec3 a, Vec3 b) => new Vec3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        public static Vec3 operator -(Vec3 a, Vec3 b) => new Vec3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        public static Vec3 operator -(Vec3 a) => new Vec3(-a.X, -a.Y, -a.Z);
        public static Vec3 operator *(Vec3 a, double s) => new Vec3(a.X * s, a.Y * s, a.Z * s);
        public static Vec3 operator *(double s, Vec3 a) => a * s;

        public double Dot(Vec3 b) => X * b.X + Y * b.Y + Z * b.Z;
        public Vec3 Cross(Vec3 b) => new Vec3(Y * b.Z - Z * b.Y, Z * b.X - X * b.Z, X * b.Y - Y * b.X);
        public double Length => Math.Sqrt(Dot(this));
        public Vec3 Normalized() { double l = Length; return l > 0 ? this * (1.0 / l) : this; }

        public static readonly Vec3 UnitX = new Vec3(1, 0, 0);
        public static readonly Vec3 UnitY = new Vec3(0, 1, 0);
        public static readonly Vec3 UnitZ = new Vec3(0, 0, 1);

        public bool Equals(Vec3 o) => X == o.X && Y == o.Y && Z == o.Z;
        public override bool Equals(object obj) => obj is Vec3 v && Equals(v);
        public override int GetHashCode() => unchecked(((X.GetHashCode() * 397) ^ Y.GetHashCode()) * 397 ^ Z.GetHashCode());
        public override string ToString() => $"({X:0.###}, {Y:0.###}, {Z:0.###})";

        /// <summary>Area of triangle abc.</summary>
        public static double TriangleArea(Vec3 a, Vec3 b, Vec3 c) => 0.5 * (b - a).Cross(c - a).Length;
    }
}
