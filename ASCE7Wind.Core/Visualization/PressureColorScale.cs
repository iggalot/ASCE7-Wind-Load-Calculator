using System;
using System.Collections.Generic;

namespace ASCE7WindLoadCalculator
{
    /// <summary>
    /// Diverging color scale for wind pressure: red arm for suction (negative, pulls away from the
    /// surface), blue arm for pressure (positive, pushes on it), neutral gray at zero. Equal steps per
    /// arm; symmetric about zero so equal magnitudes read as equally strong.
    /// </summary>
    public sealed class PressureColorScale
    {
        public readonly struct Rgb
        {
            public byte R { get; }
            public byte G { get; }
            public byte B { get; }
            public Rgb(byte r, byte g, byte b) { R = r; G = g; B = b; }
            public static Rgb FromHex(string hex) =>
                new Rgb(Convert.ToByte(hex.Substring(1, 2), 16), Convert.ToByte(hex.Substring(3, 2), 16), Convert.ToByte(hex.Substring(5, 2), 16));
            public override string ToString() => $"#{R:x2}{G:x2}{B:x2}";
        }

        /// <summary>Neutral midpoint (zero pressure).</summary>
        public static readonly Rgb Neutral = Rgb.FromHex("#f0efec");

        /// <summary>Color for zones with no applicable coefficient.</summary>
        public static readonly Rgb NoData = Rgb.FromHex("#c3c2b7");

        // Pressure arm: blue ramp steps 150 / 300 / 450 / 650.
        private static readonly Rgb[] PressureArm = { Neutral, Rgb.FromHex("#b7d3f6"), Rgb.FromHex("#6da7ec"), Rgb.FromHex("#2a78d6"), Rgb.FromHex("#104281") };

        // Suction arm: red steps of matching lightness.
        private static readonly Rgb[] SuctionArm = { Neutral, Rgb.FromHex("#f6c4c0"), Rgb.FromHex("#ee8a86"), Rgb.FromHex("#e34948"), Rgb.FromHex("#9b1c1c") };

        /// <summary>Magnitude (psf) that maps to the darkest color on each arm.</summary>
        public double MaxAbs { get; }

        public PressureColorScale(double maxAbsPsf)
        {
            MaxAbs = maxAbsPsf > 0 && !double.IsNaN(maxAbsPsf) ? maxAbsPsf : 1.0;
        }

        public Rgb ColorFor(double psf)
        {
            if (double.IsNaN(psf)) return NoData;
            double t = Math.Max(-1, Math.Min(1, psf / MaxAbs));
            var arm = t < 0 ? SuctionArm : PressureArm;
            double s = Math.Abs(t) * (arm.Length - 1);
            int i = Math.Min((int)Math.Floor(s), arm.Length - 2);
            double f = s - i;
            return Lerp(arm[i], arm[i + 1], f);
        }

        private static Rgb Lerp(Rgb a, Rgb b, double f) => new Rgb(
            (byte)Math.Round(a.R + (b.R - a.R) * f),
            (byte)Math.Round(a.G + (b.G - a.G) * f),
            (byte)Math.Round(a.B + (b.B - a.B) * f));

        /// <summary>Legend tick values (psf) from -MaxAbs to +MaxAbs on a "nice" step, including 0.</summary>
        public IReadOnlyList<double> Ticks()
        {
            double step = NiceStep(MaxAbs / 4);
            var ticks = new List<double>();
            for (double v = -Math.Floor(MaxAbs / step) * step; v <= MaxAbs + 1e-9; v += step)
                ticks.Add(Math.Abs(v) < 1e-9 ? 0 : v);
            return ticks;
        }

        /// <summary>1, 2, 2.5 or 5 times a power of ten, at least <paramref name="raw"/>.</summary>
        public static double NiceStep(double raw)
        {
            if (raw <= 0 || double.IsNaN(raw)) return 1;
            double p = Math.Pow(10, Math.Floor(Math.Log10(raw)));
            foreach (var m in new[] { 1.0, 2.0, 2.5, 5.0, 10.0 })
                if (m * p >= raw - 1e-12) return m * p;
            return 10 * p;
        }
    }
}
