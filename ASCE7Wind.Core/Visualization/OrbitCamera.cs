using System;

namespace ASCE7WindLoadCalculator
{
    /// <summary>
    /// Orbit camera around a target point, in model coordinates (z up). Mouse gestures map to
    /// <see cref="Orbit"/>, <see cref="Pan"/> and <see cref="Zoom"/>; the UI only converts the result.
    /// </summary>
    public sealed class OrbitCamera
    {
        public const double FieldOfViewDeg = 40;
        private const double MinPitch = -5 * Math.PI / 180;   // slightly below the horizon
        private const double MaxPitch = 89.5 * Math.PI / 180; // just short of straight down

        public Vec3 Target { get; private set; }

        /// <summary>Azimuth of the camera position about the target, radians from +x toward +y.</summary>
        public double Yaw { get; private set; }

        /// <summary>Elevation above the horizontal, radians.</summary>
        public double Pitch { get; private set; }

        public double Distance { get; private set; }

        public double MinDistance { get; private set; } = 1;
        public double MaxDistance { get; private set; } = 10000;

        public Vec3 Position => Target + Offset;
        public Vec3 LookDirection => -Offset;
        public Vec3 Up => Vec3.UnitZ;

        private Vec3 Offset => new Vec3(
            Distance * Math.Cos(Pitch) * Math.Cos(Yaw),
            Distance * Math.Cos(Pitch) * Math.Sin(Yaw),
            Distance * Math.Sin(Pitch));

        /// <summary>Screen-right direction in model space.</summary>
        public Vec3 Right => LookDirection.Cross(Up).Normalized();

        /// <summary>Screen-up direction in model space.</summary>
        public Vec3 ScreenUp => Right.Cross(LookDirection).Normalized();

        public OrbitCamera(Vec3 target, double yawDeg, double pitchDeg, double distance)
        {
            Target = target;
            Set(yawDeg, pitchDeg, distance);
        }

        public void Set(double yawDeg, double pitchDeg, double distance)
        {
            Yaw = yawDeg * Math.PI / 180;
            Pitch = Clamp(pitchDeg * Math.PI / 180, MinPitch, MaxPitch);
            Distance = Clamp(distance, MinDistance, MaxDistance);
        }

        /// <summary>Drag to rotate: pixels to radians.</summary>
        public void Orbit(double dxPixels, double dyPixels, double radiansPerPixel = 0.008)
        {
            Yaw -= dxPixels * radiansPerPixel;
            Pitch = Clamp(Pitch + dyPixels * radiansPerPixel, MinPitch, MaxPitch);
        }

        /// <summary>Drag to pan: moves the target so the scene follows the pointer.</summary>
        public void Pan(double dxPixels, double dyPixels, double viewportHeightPixels)
        {
            if (viewportHeightPixels <= 0) return;
            double worldPerPixel = 2 * Distance * Math.Tan(FieldOfViewDeg * Math.PI / 360) / viewportHeightPixels;
            Target = Target - Right * (dxPixels * worldPerPixel) + ScreenUp * (dyPixels * worldPerPixel);
        }

        /// <summary>Wheel to zoom: factor &lt; 1 moves closer.</summary>
        public void Zoom(double factor) => Distance = Clamp(Distance * factor, MinDistance, MaxDistance);

        /// <summary>Frames a bounding sphere of the given radius around <paramref name="center"/>.</summary>
        public void Frame(Vec3 center, double radius)
        {
            Target = center;
            double d = 1.15 * radius / Math.Sin(FieldOfViewDeg * Math.PI / 360);
            MinDistance = Math.Max(0.5, 0.15 * radius);
            MaxDistance = 20 * radius + 10;
            Distance = Clamp(d, MinDistance, MaxDistance);
        }

        private static double Clamp(double v, double lo, double hi) => v < lo ? lo : (v > hi ? hi : v);

        // ---- Named views --------------------------------------------------------------------------

        public enum Preset { Isometric, Top, South, West }

        public void Apply(Preset preset)
        {
            switch (preset)
            {
                case Preset.Isometric: Yaw = Rad(-125); Pitch = Rad(28); break;
                case Preset.Top: Yaw = Rad(-90); Pitch = MaxPitch; break;
                case Preset.South: Yaw = Rad(-90); Pitch = Rad(8); break;
                case Preset.West: Yaw = Rad(180); Pitch = Rad(8); break;
            }
        }

        private static double Rad(double deg) => deg * Math.PI / 180;
    }
}
