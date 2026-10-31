namespace ASCE7WindLoadCalculator
{
    /// <summary>
    /// One C&amp;C parapet load case (Section 30.6, Fig. 30.6-1) at one location. Pressures in psf, positive toward the
    /// surface. Each face gets p = qp Kd [(GCp) - (GCpi)], evaluated under positive and under negative internal pressure
    /// (30.6), with GCpi from the parapet envelope's porosity (Table 26.13-1) and the 30.2.2 minimum applied; the
    /// internal pressure cancels in the net load on the whole parapet.
    /// </summary>
    public sealed class ParapetCladdingCase
    {
        /// <summary>"A" (windward parapet) or "B" (leeward parapet).</summary>
        public string Case { get; internal set; }

        /// <summary>"corner" (within a of a corner) or "edge" (elsewhere along the parapet).</summary>
        public string Location { get; internal set; }

        /// <summary>Velocity pressure at the top of the parapet (psf).</summary>
        public double Qp { get; internal set; }

        /// <summary>|GCpi| of the parapet envelope (Table 26.13-1).</summary>
        public double GCpi { get; internal set; }

        /// <summary>Zone the front (exterior) face reads, e.g. "Zone5 (wall, +)".</summary>
        public string FrontSource { get; internal set; }
        public double GCpFront { get; internal set; }
        /// <summary>Front face under +GCpi and under -GCpi (psf).</summary>
        public double FrontPressurePosGCpi { get; internal set; }
        public double FrontPressureNegGCpi { get; internal set; }
        /// <summary>The larger of the two front-face pressures (psf, signed).</summary>
        public double FrontPressure { get; internal set; }

        /// <summary>Zone the back (roof-side) face reads, e.g. "Zone3 (roof, -)".</summary>
        public string BackSource { get; internal set; }
        public double GCpBack { get; internal set; }
        /// <summary>Back face under +GCpi and under -GCpi (psf).</summary>
        public double BackPressurePosGCpi { get; internal set; }
        public double BackPressureNegGCpi { get; internal set; }
        /// <summary>The larger of the two back-face pressures (psf, signed).</summary>
        public double BackPressure { get; internal set; }

        /// <summary>
        /// Net horizontal pressure on the whole parapet (psf, magnitude): qp Kd (GCp front - GCp back). Case A pushes the
        /// windward parapet toward the roof; case B pushes the leeward parapet outward, away from the roof.
        /// </summary>
        public double NetPressure { get; internal set; }
    }
}
