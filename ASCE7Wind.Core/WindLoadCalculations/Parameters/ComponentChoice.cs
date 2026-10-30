namespace ASCE7WindLoadCalculator
{
    /// <summary>
    /// A cladding component choice on the Building page, as typed: Kind 0 = sheathing/fasteners (10 sq ft),
    /// 1 = framing (Entry = span, ft), 2 = other (Entry = area, sq ft).
    /// </summary>
    public sealed class ComponentChoice
    {
        public const int Sheathing = 0, Framing = 1, Area = 2;

        public ComponentChoice() { }

        public ComponentChoice(int kind, double entry)
        {
            Kind = kind;
            Entry = double.IsNaN(entry) ? 0 : entry;
        }

        public int Kind { get; set; }

        public double Entry { get; set; }
    }
}
