using System;
using System.Collections.Generic;
using System.Windows;

namespace ASCE7WindLoadCalculator
{
    public class WallAreaCalculator_NonGableEnd_CC_ASCE7_22 : AreaCalculator_CC_ASCE7_22_Base
    {
        public WallAreaCalculator_NonGableEnd_CC_ASCE7_22(BuildingData bldg_data, string note_string="")
        {
                buildingData = bldg_data;
                Note = note_string;
        }

        public override void ComputeEffectiveWindAreas()
        {
            double length = buildingData.BuildingLength;

            // Corners of the wall planes -- assumed to be perpendicular to wind
            Point A = new Point(0, 0);
            Point B = new Point(length, 0);
            Point C = new Point(length, buildingData.BuildingHeight);
            Point D = new Point(0, buildingData.BuildingHeight);

            Point p1, p2, p3, p4;
            if (length > 2 * CritDim_a)
            {
                p1 = new Point(CritDim_a, 0);
                p2 = new Point(CritDim_a, buildingData.BuildingHeight);
                p3 = new Point(length - CritDim_a, 0);
                p4 = new Point(length - CritDim_a, buildingData.BuildingHeight);
                effWindAreas.Add(1, new EffectiveWindArea("Zone5", new List<Point> { A, p1, p2, D }, null));
                effWindAreas.Add(2, new EffectiveWindArea("Zone4", new List<Point> { p1, p3, p4, p2 }, null));
                effWindAreas.Add(3, new EffectiveWindArea("Zone5", new List<Point> { p3, B, C, p4 }, null));

            }
            else if (length < 2 * CritDim_a)
            {
                effWindAreas.Add(1, new EffectiveWindArea("Zone5", new List<Point> { A, B, C, D }, null));
            }
            else
            {
                throw new Exception("ERROR: Wall length is too short.");
            }
        }
    }
}
