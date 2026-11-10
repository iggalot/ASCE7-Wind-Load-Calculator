using System.Collections.Generic;

namespace ASCE7WindLoadCalculator
{
    /// <summary>
    /// Fig. 30.3-1 wall zones for a rectangular wall (eave wall, or any wall of a flat/hip roof):
    /// Zone 5 edge strips of width "a" at each corner, Zone 4 between them.
    /// Coordinates: x along the wall from one corner, y up from grade.
    /// </summary>
    public class WallAreaCalculator_NonGableEnd_CC : AreaCalculator_CC_Base
    {
        /// <summary>Horizontal length of this wall (ft).</summary>
        public double WallLength { get; }

        public WallAreaCalculator_NonGableEnd_CC(BuildingData bldg_data, double wallLength, string note_string = "")
        {
            buildingData = bldg_data;
            WallLength = wallLength;
            Note = note_string;
        }

        protected override bool IsWallFigure => true;

        public override void ComputeEffectiveWindAreas()
        {
            double length = WallLength;
            double h = buildingData.BuildingHeight;
            double a = CritDim_a;

            var A = new Vec2(0, 0);
            var B = new Vec2(length, 0);
            var C = new Vec2(length, h);
            var D = new Vec2(0, h);

            if (length > 2 * a)
            {
                var p1 = new Vec2(a, 0);
                var p2 = new Vec2(a, h);
                var p3 = new Vec2(length - a, 0);
                var p4 = new Vec2(length - a, h);
                effWindAreas.Add(1, new EffectiveWindArea("Zone5", new List<Vec2> { A, p1, p2, D }));
                effWindAreas.Add(2, new EffectiveWindArea("Zone4", new List<Vec2> { p1, p3, p4, p2 }));
                effWindAreas.Add(3, new EffectiveWindArea("Zone5", new List<Vec2> { p3, B, C, p4 }));
            }
            else
            {
                // The two corner strips meet or overlap: the whole wall is Zone 5.
                effWindAreas.Add(1, new EffectiveWindArea("Zone5", new List<Vec2> { A, B, C, D }));
            }
        }
    }
}
