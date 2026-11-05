namespace ASCE7WindLoadCalculator
{
    public class PressureData
    {
        public  int AreaID { get; set; }
        public double qh { get; set; }
        public double GCp { get; set; }
        public double GCpi { get; set; }
        public double ExternalPressure;
        public double InternalPressure;

        /// <summary>
        /// The coefficient before the Fig. 27.3-1 note b area reduction of the −1.3 value (null when no reduction was applied).
        /// <see cref="GCp"/> and <see cref="ExternalPressure"/> are the reduced values.
        /// </summary>
        public double? GCpUnreduced { get; set; }

        /// <summary>External pressure before the Fig. 27.3-1 note b reduction (psf); equals <see cref="ExternalPressure"/> when not reduced.</summary>
        public double UnreducedExternalPressure => GCpUnreduced.HasValue && GCp != 0 ? ExternalPressure * GCpUnreduced.Value / GCp : ExternalPressure;

        /// <summary>Fig. 27.3-1 note b factor applied to the −1.3 value (1 when not reduced).</summary>
        public double AreaReductionFactor { get; set; } = 1.0;

        /// <summary>Roof area the −1.3 value applies to (sq ft), which sets <see cref="AreaReductionFactor"/>.</summary>
        public double ReductionArea { get; set; }

        /// <summary>The citation for a reduced value; empty when not reduced.</summary>
        public string ReductionNote => GCpUnreduced.HasValue
            ? $"Fig. 27.3-1 note b: Cp {GCpUnreduced.Value:0.000} unreduced; −1.3 × {AreaReductionFactor:0.000} for {ReductionArea:0} sq ft gives {GCp:0.000}"
            : string.Empty;

        /// <summary>
        /// Net pressure before any minimum: external combined with the internal pressure that makes it largest
        /// (suction minus internal, push-in plus internal).
        /// </summary>
        public double CalculatedNetPressure => ExternalPressure < 0 ? ExternalPressure - InternalPressure : ExternalPressure + InternalPressure;

        /// <summary>Minimum net pressure magnitude (psf), e.g. 16 psf for C&amp;C (30.2.2); 0 for none.</summary>
        public double MinimumNet { get; set; }

        /// <summary>True when the minimum is larger than the calculated net pressure.</summary>
        public bool MinimumGoverns => MinimumNet > 0 && System.Math.Abs(CalculatedNetPressure) < MinimumNet;

        /// <summary>Design net pressure: the calculated value, raised to the minimum (same direction) when that governs.</summary>
        public double NetPressure =>
            MinimumGoverns ? (ExternalPressure < 0 ? -MinimumNet : MinimumNet) : CalculatedNetPressure;
    }
}
