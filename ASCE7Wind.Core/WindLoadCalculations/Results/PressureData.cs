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
