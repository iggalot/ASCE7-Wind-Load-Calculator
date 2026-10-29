using System;
using System.Collections.Generic;

namespace ASCE7WindLoadCalculator
{
    public class WallAreaCalculator_GableEnd_CC : AreaCalculator_CC_Base
    {
        public WallAreaCalculator_GableEnd_CC(BuildingData bldg_data, string note_string="")
        {
            buildingData = bldg_data;
            Note = note_string;
        }

        public override void ComputeEffectiveWindAreas()
        {
            double length = buildingData.BuildingLength;

            // Corners of the wall planes -- assumed to be perpendicular to wind
            Vec2 A = new Vec2(0, 0);
            Vec2 B = new Vec2(length, 0);
            Vec2 C = new Vec2(length, buildingData.BuildingHeight);
            Vec2 D = new Vec2(0, buildingData.BuildingHeight);
            Vec2 E = new Vec2(length, buildingData.BuildingHeight + Math.Tan(buildingData.RoofPitch * Math.PI / 180.0) * length / 2.0);

            Vec2 p1, p2, p3, p4, p5, p6;
            if (length > 2 * CritDim_a)
            {
                var ht_at_roof = buildingData.BuildingHeight + Math.Tan(buildingData.RoofPitch * Math.PI / 180.0) * CritDim_a;
                p1 = new Vec2(CritDim_a, 0);
                p2 = new Vec2(CritDim_a, ht_at_roof);
                p3 = new Vec2(length - CritDim_a, 0);
                p4 = new Vec2(length - CritDim_a, ht_at_roof);

                effWindAreas.Add(1, new EffectiveWindArea("Zone5", new List<Vec2> { A, p1, p2, D }, null));
                effWindAreas.Add(2, new EffectiveWindArea("Zone4", new List<Vec2> { p1, p3, p4, E, p2 }, null));
                effWindAreas.Add(3, new EffectiveWindArea("Zone5", new List<Vec2> { p3, B, C, p4 }, null));
            }
            else if (length < 2 * CritDim_a)
            {
                effWindAreas.Add(1, new EffectiveWindArea("Zone5", new List<Vec2> { A, B, C, E, D }, null));
            }
            else
            {
                throw new Exception("ERROR: Wall length is too short.");
            }
        }
    }
}
