using System.Collections.Generic;
using System.Windows;

namespace ASCE7WindLoadCalculator
{
    public class HipRoofAreaCalculator_CC_ASCE7_22 : AreaCalculator_CC_ASCE7_22_Base
    {
        public HipRoofAreaCalculator_CC_ASCE7_22(BuildingData bldg_data)
        {
            buildingData = bldg_data;
        }

        public override void ComputeEffectiveWindAreas()
        {
            // for finding the inset points
            double inset_dist = 1.414 * CritDim_a;

            // Hip logic
            // Figure 30.3-2E / 2F / 2G / 2H / 2I -- Flat roof and Gable with slope greater than 7
            // Map if Length is less than width -- ridge is vertical on map
            //  E-----F
            //  | \ / |
            //  |  D  |
            //  |  |  |
            //  |  C  |
            //  | / \ |
            //  A=----B 
            if (buildingData.BuildingLength < buildingData.BuildingWidth)
            {
                // Corners of the hip roof planes
                Point A = new Point(0, 0);
                Point B = new Point(buildingData.BuildingLength, 0);
                Point C = new Point(0.5 * buildingData.BuildingLength, 0.5 * buildingData.BuildingLength);
                Point D = new Point(0.5 * buildingData.BuildingLength, buildingData.BuildingWidth - 0.5 * buildingData.BuildingLength);
                Point E = new Point(0, buildingData.BuildingWidth);
                Point F = new Point(buildingData.BuildingLength, buildingData.BuildingWidth);

                Point p1 = new Point(CritDim_a, CritDim_a); // lower left corner
                Point p2 = new Point(CritDim_a + inset_dist, CritDim_a); // lower left 1st intermediate
                Point p3 = new Point(buildingData.BuildingLength - CritDim_a - inset_dist, CritDim_a); // lower right 1st intermediate
                Point p4 = new Point(buildingData.BuildingLength - CritDim_a, CritDim_a); // lower right
                Point p5 = new Point(C.X, C.Y - inset_dist); // upper corner of lower triangle

                // lower triangle
                effWindAreas.Add(1, new EffectiveWindArea("Zone3", new List<Point> { A, B, p4, p1 }, null));
                effWindAreas.Add(2, new EffectiveWindArea("Zone2", new List<Point> { p1, p2, p5, p3, p4, C }, null));
                effWindAreas.Add(3, new EffectiveWindArea("Zone1", new List<Point> { p2, p3, p5 }, null));

                // left trapezoid
                Point p6 = new Point(p1.X, p1.Y + inset_dist);
                Point p7 = new Point(p1.X, buildingData.BuildingWidth - CritDim_a - inset_dist);
                Point p8 = new Point(p1.X, buildingData.BuildingWidth - CritDim_a);

                Point p9 = new Point(C.X - CritDim_a, C.Y + (inset_dist - CritDim_a));
                Point p10 = new Point(D.X - CritDim_a, D.Y - (inset_dist - CritDim_a));

                effWindAreas.Add(4, new EffectiveWindArea("Zone3", new List<Point> { A, p1, p8, E }, null));
                effWindAreas.Add(5, new EffectiveWindArea("Zone2", new List<Point> { p1, C, D, p8, p7, p10, p9, p6 }, null));
                effWindAreas.Add(6, new EffectiveWindArea("Zone1", new List<Point> { p6, p9, p10, p7 }, null));

                Point p20 = new Point(p4.X, p4.Y + inset_dist);
                Point p21 = new Point(p4.X, buildingData.BuildingWidth - CritDim_a - inset_dist);
                Point p22 = new Point(p4.X, buildingData.BuildingWidth - CritDim_a);

                Point p23 = new Point(C.X + CritDim_a, C.Y + (inset_dist - CritDim_a));
                Point p24 = new Point(D.X + CritDim_a, D.Y - (inset_dist - CritDim_a));

                // right trapezoid
                effWindAreas.Add(7, new EffectiveWindArea("Zone3", new List<Point> { B, F, p22, p4 }, null));
                effWindAreas.Add(8, new EffectiveWindArea("Zone2", new List<Point> { p4, p20, p23, p24, p21, p22, D, C }, null));
                effWindAreas.Add(9, new EffectiveWindArea("Zone1", new List<Point> { p20, p21, p24, p23 }, null));
                 
                // top triangle
                Point p31 = new Point(D.X, D.Y + inset_dist);
                Point p32 = new Point(p8.X + inset_dist, p8.Y);
                Point p33 = new Point(p22.X - inset_dist, p22.Y);
                effWindAreas.Add(10, new EffectiveWindArea("Zone3", new List<Point> { E, p8, p22, F }, null));
                effWindAreas.Add(11, new EffectiveWindArea("Zone2", new List<Point> { p8, D, p22, p33, p31, p32, p22 }, null));
                effWindAreas.Add(12, new EffectiveWindArea("Zone1", new List<Point> { p32, p31, p33 }, null));
            }
            else if (buildingData.BuildingLength > buildingData.BuildingWidth)
            {
                // Figure 30.3-2E / 2F / 2G / 2H / 2I -- Flat roof and Gable with slope greater than 7
                // Map if Length is less than width -- ridge is vertical on map
                //  E--------F
                //  |\      /|
                //  | C----D |
                //  |/      \|
                //  A--------B
                //
                // Corners of the hip roof planes
                Point A = new Point(0, 0);
                Point B = new Point(buildingData.BuildingLength, 0);
                Point C = new Point(0.5 * buildingData.BuildingWidth, 0.5 * buildingData.BuildingWidth);
                Point D = new Point(buildingData.BuildingLength - 0.5 * buildingData.BuildingWidth, 0.5 * buildingData.BuildingWidth);
                Point E = new Point(0, buildingData.BuildingWidth);
                Point F = new Point(buildingData.BuildingLength, buildingData.BuildingWidth);

                // bottom trapezoid
                Point p1 = new Point(CritDim_a, CritDim_a);
                Point p2 = new Point(CritDim_a + inset_dist, CritDim_a);
                Point p3 = new Point(buildingData.BuildingLength - inset_dist - CritDim_a, CritDim_a);
                Point p4 = new Point(buildingData.BuildingLength - CritDim_a, CritDim_a);
                Point p5 = new Point(C.X + inset_dist - CritDim_a, C.Y - CritDim_a);
                Point p6 = new Point(D.X - inset_dist + CritDim_a, D.Y - CritDim_a);

                effWindAreas.Add(1, new EffectiveWindArea("Zone3", new List<Point> { A, B, p4, p1 }, null));
                effWindAreas.Add(2, new EffectiveWindArea("Zone2", new List<Point> { p1, p2, p5, p6, p3, p4, D, C }, null));
                effWindAreas.Add(3, new EffectiveWindArea("Zone1", new List<Point> { p2, p3, p6, p5 }, null));

                // left triangle
                Point p7 = new Point(CritDim_a, inset_dist + CritDim_a);
                Point p8 = new Point(C.X - inset_dist, C.Y);
                Point p9 = new Point(CritDim_a, buildingData.BuildingWidth - inset_dist - CritDim_a);
                Point p13 = new Point(CritDim_a, buildingData.BuildingWidth - CritDim_a);

                effWindAreas.Add(4, new EffectiveWindArea("Zone3", new List<Point> { A, p1, p13, E }, null));
                effWindAreas.Add(5, new EffectiveWindArea("Zone2", new List<Point> { p1, C, p13, p9, p8, p7 }, null));
                effWindAreas.Add(6, new EffectiveWindArea("Zone1", new List<Point> { p7, p8, p9 }, null));

                //// right triangle
                Point p10 = new Point(buildingData.BuildingLength - CritDim_a, inset_dist + CritDim_a);
                Point p11 = new Point(buildingData.BuildingLength - CritDim_a, buildingData.BuildingWidth - inset_dist - CritDim_a);
                Point p12 = new Point(D.X + inset_dist, D.Y);
                Point p18 = new Point(buildingData.BuildingLength - CritDim_a, buildingData.BuildingWidth - CritDim_a);

                effWindAreas.Add(7, new EffectiveWindArea("Zone3", new List<Point> { B, F, p18, p4 }, null));
                effWindAreas.Add(8, new EffectiveWindArea("Zone2", new List<Point> { p4, p10, p12, p11, p18, D }, null));
                effWindAreas.Add(9, new EffectiveWindArea("Zone1", new List<Point> { p10, p11, p12 }, null));

                // top trapezoid
                Point p14 = new Point(CritDim_a + inset_dist, buildingData.BuildingWidth - CritDim_a);
                Point p17 = new Point(buildingData.BuildingLength - inset_dist - CritDim_a, buildingData.BuildingWidth - CritDim_a);
                Point p15 = new Point(C.X + inset_dist - CritDim_a, C.Y + CritDim_a);
                Point p16 = new Point(D.X - inset_dist + CritDim_a, D.Y + CritDim_a);

                effWindAreas.Add(10, new EffectiveWindArea("Zone3", new List<Point> { E, p13, p18, F }, null));
                effWindAreas.Add(11, new EffectiveWindArea("Zone2", new List<Point> { p13, C, D, p18, p17, p16, p15, p14 }, null));
                effWindAreas.Add(12, new EffectiveWindArea("Zone1", new List<Point> { p14, p15, p16, p17 }, null));
            }

            // Building length and width are equal, so all zone 1s are triangles
            else
            {
                // Figure 30.3-2E / 2F / 2G / 2H / 2I -- Flat roof and Gable with slope greater than 7
                // Map if Length is less than width -- ridge is vertical on map
                //  D---E
                //  |\ /|
                //  | C |
                //  |/ \|
                //  A---B
                //

                // Corners of the hip roof planes
                Point A = new Point(0, 0);
                Point B = new Point(buildingData.BuildingLength, 0);
                Point C = new Point(0.5 * buildingData.BuildingLength, 0.5 * buildingData.BuildingWidth);
                Point D = new Point(0, buildingData.BuildingWidth);
                Point E = new Point(buildingData.BuildingLength, buildingData.BuildingWidth);

                // Bottom triangle
                Point p1 = new Point(CritDim_a, CritDim_a);
                Point p2 = new Point(inset_dist + CritDim_a, CritDim_a);
                Point p3 = new Point(C.X, C.Y - inset_dist);
                Point p4 = new Point(buildingData.BuildingLength - inset_dist - CritDim_a, CritDim_a);
                Point p5 = new Point(buildingData.BuildingLength - CritDim_a, CritDim_a);

                effWindAreas.Add(1, new EffectiveWindArea("Zone3", new List<Point> { A, B, p5, p1 }, null));
                effWindAreas.Add(2, new EffectiveWindArea("Zone2", new List<Point> { p1, p2, p3, p4, p5, C }, null));
                effWindAreas.Add(3, new EffectiveWindArea("Zone1", new List<Point> { p2, p4, p3 }, null));

                // left triangle
                Point p6 = new Point(CritDim_a, inset_dist + CritDim_a);
                Point p7 = new Point(C.X - inset_dist, C.Y);
                Point p8 = new Point(CritDim_a, buildingData.BuildingWidth - inset_dist - CritDim_a);
                Point p9 = new Point(CritDim_a, buildingData.BuildingWidth - CritDim_a);

                effWindAreas.Add(4, new EffectiveWindArea("Zone3", new List<Point> { A, p1, p9, D }, null));
                effWindAreas.Add(5, new EffectiveWindArea("Zone2", new List<Point> { p1, C, p9, p8, p7, p6 }, null));
                effWindAreas.Add(6, new EffectiveWindArea("Zone1", new List<Point> { p6, p7, p8 }, null));

                // right triangle
                Point p10 = new Point(buildingData.BuildingLength - CritDim_a, inset_dist + CritDim_a);
                Point p11 = new Point(C.X + inset_dist, C.Y);
                Point p12 = new Point(buildingData.BuildingLength - CritDim_a, buildingData.BuildingWidth - inset_dist - CritDim_a);
                Point p13 = new Point(buildingData.BuildingLength - CritDim_a, buildingData.BuildingWidth - CritDim_a);

                effWindAreas.Add(7, new EffectiveWindArea("Zone3", new List<Point> { B, E, p13, p5 }, null));
                effWindAreas.Add(8, new EffectiveWindArea("Zone2", new List<Point> { p5, p10, p11, p12, p13, C }, null));
                effWindAreas.Add(9, new EffectiveWindArea("Zone1", new List<Point> { p10, p12, p11 }, null));

                // top triangle
                Point p14 = new Point(inset_dist + CritDim_a, buildingData.BuildingWidth - CritDim_a);
                Point p15 = new Point(C.X, C.Y + inset_dist);
                Point p16 = new Point(buildingData.BuildingLength - inset_dist - CritDim_a, buildingData.BuildingWidth - CritDim_a);

                effWindAreas.Add(10, new EffectiveWindArea("Zone3", new List<Point> { D, p9, p13, E }, null));
                effWindAreas.Add(11, new EffectiveWindArea("Zone2", new List<Point> { p9, C, p13, p16, p15, p14 }, null));
                effWindAreas.Add(12, new EffectiveWindArea("Zone1", new List<Point> { p14, p15, p16 }, null));

            }
        }
    }
}
