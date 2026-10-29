using System;
using System.Collections.Generic;

namespace ASCE7WindLoadCalculator
{
    /// <summary>
    /// Fig. 30.3-1 wall zones for a gable-end wall, including the gable triangle above the eave.
    /// Zone 5 edge strips of width "a" at each corner (following the rake), Zone 4 between them.
    /// Coordinates: x along the wall from one corner, y up from grade; the apex is at mid-span.
    /// </summary>
    public class WallAreaCalculator_GableEnd_CC : AreaCalculator_CC_Base
    {
        /// <summary>Horizontal length of this (gable-end) wall (ft).</summary>
        public double WallLength { get; }

        public WallAreaCalculator_GableEnd_CC(BuildingData bldg_data, double wallLength, string note_string = "")
        {
            buildingData = bldg_data;
            WallLength = wallLength;
            Note = note_string;
        }

        public override void ComputeEffectiveWindAreas()
        {
            double length = WallLength;
            double h = buildingData.BuildingHeight;
            double a = CritDim_a;
            double tan = Math.Tan(buildingData.RoofPitch * Math.PI / 180.0);

            var A = new Vec2(0, 0);
            var B = new Vec2(length, 0);
            var C = new Vec2(length, h);
            var D = new Vec2(0, h);
            var apex = new Vec2(length / 2.0, h + tan * length / 2.0);

            if (length > 2 * a)
            {
                double rakeHeightAtA = h + tan * a; // rake height a in from each corner
                var p1 = new Vec2(a, 0);
                var p2 = new Vec2(a, rakeHeightAtA);
                var p3 = new Vec2(length - a, 0);
                var p4 = new Vec2(length - a, rakeHeightAtA);
                effWindAreas.Add(1, new EffectiveWindArea("Zone5", new List<Vec2> { A, p1, p2, D }));
                effWindAreas.Add(2, new EffectiveWindArea("Zone4", new List<Vec2> { p1, p3, p4, apex, p2 }));
                effWindAreas.Add(3, new EffectiveWindArea("Zone5", new List<Vec2> { p3, B, C, p4 }));
            }
            else
            {
                effWindAreas.Add(1, new EffectiveWindArea("Zone5", new List<Vec2> { A, B, C, apex, D }));
            }
        }
    }
}
