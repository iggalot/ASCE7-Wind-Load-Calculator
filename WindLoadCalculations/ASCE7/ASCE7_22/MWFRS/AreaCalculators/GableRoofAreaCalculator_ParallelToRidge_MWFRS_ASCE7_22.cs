using System.Collections.Generic;
using System.Windows;

namespace ASCE7WindLoadCalculator
{
    public class GableRoofAreaCalculator_ParallelToRidge_MWFRS_ASCE7_22 : AreaCalculator_ASCE7_22_Base
    {
        public GableRoofAreaCalculator_ParallelToRidge_MWFRS_ASCE7_22(BuildingData bldg_data)
        {
            buildingData = bldg_data;
        }

        public override void ComputeEffectiveWindAreas()
        {
            //if (optionalParams == null) return;

            double building_length = buildingData.BuildingLength;
            double building_width = buildingData.BuildingWidth;
            double h = buildingData.MeanRoofHeight;

            // the divisions of the flat roof or parallel to ridge for theta <= 10 deg
            double offset1 = 0.5 * h;
            double offset2 = 1.0 * h;
            double offset3 = 2.0 * h;


            // Define when wind is parallel to ridge (parallel to BuildingLength) dimension
            // assumes wind is blowing from the left (west) to the right (east)
            // D---p21---p22---p23---C
            // | 4  | 3  |  2   |  1 |
            // E===p11===p12===p13===F  Ridge "="
            // | 4  | 3  |  2   |  1 |
            // A---p1----p2----p3----B
            Point A = new Point(0, 0);
            Point B = new Point(building_length, 0);
            Point C = new Point(building_length, building_width);
            Point D = new Point(0, building_width);
            Point E = new Point(0, 0.5 * building_width);
            Point F = new Point(building_length, 0.5 * building_width);

            Point p1, p2, p3, p11, p12, p13, p21, p22, p23;

            // Zone 4
            if (IsValidRectangle_BuildingLength(building_length, building_width, offset1))
            {
                p1 = new Point(offset1, 0);
                p11 = new Point(offset1, 0.5 * building_width);
                p21 = new Point(offset1, building_width);

                effWindAreas.Add(1, new EffectiveWindArea("Zone4", new List<Point> { A, p1, p11, E }, null));
                effWindAreas.Add(2, new EffectiveWindArea("Zone4", new List<Point> { E, p11, p21, D }, null));
            }
            else
            {
                // doesn't fit in building
                effWindAreas.Add(1, new EffectiveWindArea("Zone4", new List<Point> { A, B, F, E }, null));
                effWindAreas.Add(2, new EffectiveWindArea("Zone4", new List<Point> { E, F, C, D }, null));
                return;
            }

            // Zone 3
            if (IsValidRectangle_BuildingLength(building_length, building_width, offset2))
            {
                p2 = new Point(offset2, 0);
                p12 = new Point(offset2, 0.5 * building_width);
                p22 = new Point(offset2, building_width);

                effWindAreas.Add(3, new EffectiveWindArea("Zone3", new List<Point> { p1, p2, p12, p11 }, null));
                effWindAreas.Add(4, new EffectiveWindArea("Zone3", new List<Point> { p11, p12, p22, p21 }, null));
            }
            else
            {
                // doesn't fit in building -- extend end of zone to end of building
                effWindAreas.Add(3, new EffectiveWindArea("Zone3", new List<Point> { p1, B, F, p11 }, null));
                effWindAreas.Add(4, new EffectiveWindArea("Zone3", new List<Point> { p11, F, C, p21 }, null));
                return;
            }

            // Zone 2 and Zone 1
            if (IsValidRectangle_BuildingLength(building_length, building_width, offset3))
            {
                p3 = new Point(offset3, 0);
                p13 = new Point(offset3, 0.5 * building_width);
                p23 = new Point(offset3, building_width);

                // Zone 2
                effWindAreas.Add(5, new EffectiveWindArea("Zone2", new List<Point> { p2, p3, p13, p12 }, null));
                effWindAreas.Add(6, new EffectiveWindArea("Zone2", new List<Point> { p12, p13, p23, p22 }, null));

                // Zone 1
                effWindAreas.Add(7, new EffectiveWindArea("Zone1", new List<Point> { p3, B, F, p13 }, null));
                effWindAreas.Add(8, new EffectiveWindArea("Zone1", new List<Point> { p13, F, C, p23 }, null));
            }
            else
            {
                // Zone 2
                effWindAreas.Add(5, new EffectiveWindArea("Zone2", new List<Point> { p2, B, F, p12 }, null));
                effWindAreas.Add(6, new EffectiveWindArea("Zone2", new List<Point> { p12, F, C, p22 }, null));
                return;
            }
        }
    }
}
