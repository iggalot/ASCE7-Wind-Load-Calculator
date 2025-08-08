using System.Collections.Generic;
using System.Windows;

namespace ASCE7WindLoadCalculator
{
    public class HipRoofAreaCalculator_CC_ASCE7_16 : AreaCalculator_CC_ASCE7_16_Base
    {
        public HipRoofAreaCalculator_CC_ASCE7_16(BuildingData bldg_data)
        {
            buildingData = bldg_data;
        }

        public override void ComputeEffectiveWindAreas()
        {
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

                Point p1 = new Point(0, 0);
                Point p2 = new Point(CritDim_a, 0);
                Point p3 = new Point(buildingData.BuildingLength - CritDim_a, 0);
                Point p4 = new Point(buildingData.BuildingLength, 0);

                Point p11 = new Point(0, CritDim_a);
                Point p12 = new Point(CritDim_a, CritDim_a);
                Point p13 = new Point(buildingData.BuildingLength - CritDim_a, CritDim_a);
                Point p14 = new Point(buildingData.BuildingLength, CritDim_a);

                Point p21 = new Point(0, buildingData.BuildingWidth - CritDim_a);
                Point p22 = new Point(CritDim_a, buildingData.BuildingWidth - CritDim_a);
                Point p23 = new Point(buildingData.BuildingLength - CritDim_a, buildingData.BuildingWidth - CritDim_a);
                Point p24 = new Point(buildingData.BuildingLength, buildingData.BuildingWidth - CritDim_a);

                Point p31 = new Point(0, buildingData.BuildingWidth);
                Point p32 = new Point(CritDim_a, buildingData.BuildingWidth);
                Point p33 = new Point(buildingData.BuildingLength - CritDim_a, buildingData.BuildingWidth);
                Point p34 = new Point(buildingData.BuildingLength, buildingData.BuildingWidth);

                effWindAreas.Add(1, new EffectiveWindArea("Zone3", new List<Point> { p1, p2, p12, p11 }, null));
                effWindAreas.Add(2, new EffectiveWindArea("Zone2e", new List<Point> { p2, p3, p13, p12 }, null));
                effWindAreas.Add(3, new EffectiveWindArea("Zone3", new List<Point> { p3, p4, p14, p13 }, null));
                effWindAreas.Add(4, new EffectiveWindArea("Zone2e", new List<Point> { p11, p12, p22, p21 }, null));
                effWindAreas.Add(5, new EffectiveWindArea("Zone2e", new List<Point> { p13, p14, p24, p23 }, null));
                effWindAreas.Add(6, new EffectiveWindArea("Zone3", new List<Point> { p21, p22, p32, p31 }, null));
                effWindAreas.Add(7, new EffectiveWindArea("Zone2e", new List<Point> { p22, p23, p33, p32 }, null));
                effWindAreas.Add(8, new EffectiveWindArea("Zone3", new List<Point> { p23, p24, p34, p33 }, null));

                // for finding the inset points
                var inset_dist = 1.414 * CritDim_a;

                // lower triangle
                Point p40 = new Point(p12.X + inset_dist, p12.Y);
                Point p41 = new Point(p13.X - inset_dist, p13.Y);
                Point p42 = new Point(C.X, C.Y - inset_dist);
                effWindAreas.Add(9, new EffectiveWindArea("Zone1", new List<Point> { p40, p41, p42 }, null));
                effWindAreas.Add(10, new EffectiveWindArea("Zone2r", new List<Point> { p12, p40, p42, p41, p13, C }, null));

                // left trapezoid
                Point p50 = new Point(p12.X, p12.Y + inset_dist);
                Point p51 = new Point(C.X - CritDim_a, C.Y + (inset_dist - CritDim_a));
                Point p52 = new Point(D.X - CritDim_a, D.Y - (inset_dist - CritDim_a));
                Point p53 = new Point(p22.X, p22.Y - inset_dist);
                effWindAreas.Add(11, new EffectiveWindArea("Zone1", new List<Point> { p50, p51, p52, p53 }, null));
                effWindAreas.Add(12, new EffectiveWindArea("Zone2r", new List<Point> { p12, C, D, p22, p53, p52, p51, p50 }, null));

                // left trapezoid
                Point p60 = new Point(p13.X, p13.Y + inset_dist);
                Point p61 = new Point(p23.X, p23.Y - inset_dist);
                Point p62 = new Point(D.X + CritDim_a, D.Y - (inset_dist - CritDim_a));
                Point p63 = new Point(C.X + CritDim_a, C.Y + (inset_dist - CritDim_a));
                effWindAreas.Add(13, new EffectiveWindArea("Zone1", new List<Point> { p60, p61, p62, p63 }, null));
                effWindAreas.Add(14, new EffectiveWindArea("Zone2r", new List<Point> { p13, p60, p63, p62, p61, p23, D, C }, null));

                // top triangle
                Point p70 = new Point(p22.X + inset_dist, p22.Y);
                Point p71 = new Point(p23.X - inset_dist, p23.Y);
                Point p72 = new Point(D.X, D.Y + inset_dist);
                effWindAreas.Add(15, new EffectiveWindArea("Zone1", new List<Point> { p70, p72, p71 }, null));
                effWindAreas.Add(16, new EffectiveWindArea("Zone2r", new List<Point> { p22, D, p23, p71, p72, p70 }, null));
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

                Point p1 = new Point(0, 0);
                Point p2 = new Point(CritDim_a, 0);
                Point p3 = new Point(buildingData.BuildingLength - CritDim_a, 0);
                Point p4 = new Point(buildingData.BuildingLength, 0);

                Point p11 = new Point(0, CritDim_a);
                Point p12 = new Point(CritDim_a, CritDim_a);
                Point p13 = new Point(buildingData.BuildingLength - CritDim_a, CritDim_a);
                Point p14 = new Point(buildingData.BuildingLength, CritDim_a);

                Point p21 = new Point(0, buildingData.BuildingWidth - CritDim_a);
                Point p22 = new Point(CritDim_a, buildingData.BuildingWidth - CritDim_a);
                Point p23 = new Point(buildingData.BuildingLength - CritDim_a, buildingData.BuildingWidth - CritDim_a);
                Point p24 = new Point(buildingData.BuildingLength, buildingData.BuildingWidth - CritDim_a);

                Point p31 = new Point(0, buildingData.BuildingWidth);
                Point p32 = new Point(CritDim_a, buildingData.BuildingWidth);
                Point p33 = new Point(buildingData.BuildingLength - CritDim_a, buildingData.BuildingWidth);
                Point p34 = new Point(buildingData.BuildingLength, buildingData.BuildingWidth);

                effWindAreas.Add(1, new EffectiveWindArea("Zone3", new List<Point> { p1, p2, p12, p11 }, null));
                effWindAreas.Add(2, new EffectiveWindArea("Zone2e", new List<Point> { p2, p3, p13, p12 }, null));
                effWindAreas.Add(3, new EffectiveWindArea("Zone3", new List<Point> { p3, p4, p14, p13 }, null));
                effWindAreas.Add(4, new EffectiveWindArea("Zone2e", new List<Point> { p11, p12, p22, p21 }, null));
                effWindAreas.Add(5, new EffectiveWindArea("Zone2e", new List<Point> { p13, p14, p24, p23 }, null));
                effWindAreas.Add(6, new EffectiveWindArea("Zone3", new List<Point> { p21, p22, p32, p31 }, null));
                effWindAreas.Add(7, new EffectiveWindArea("Zone2e", new List<Point> { p22, p23, p33, p32 }, null));
                effWindAreas.Add(8, new EffectiveWindArea("Zone3", new List<Point> { p23, p24, p34, p33 }, null));

                // for finding the inset points
                var inset_dist = 1.414 * CritDim_a;

                // left triangle
                Point p40 = new Point(p12.X, p12.Y + inset_dist);
                Point p41 = new Point(C.X - inset_dist, C.Y);
                Point p42 = new Point(p22.X, p22.Y - inset_dist);
                effWindAreas.Add(9, new EffectiveWindArea("Zone1", new List<Point> { p40, p41, p42 }, null));
                effWindAreas.Add(10, new EffectiveWindArea("Zone2r", new List<Point> { p12, C, p22, p42, p41, p40 }, null));

                // lower trapezoid
                Point p50 = new Point(p12.X + inset_dist, p12.Y);
                Point p51 = new Point(p13.X - inset_dist, p13.Y);
                Point p52 = new Point(D.X - (inset_dist - CritDim_a), D.Y - CritDim_a);
                Point p53 = new Point(C.X + (inset_dist - CritDim_a), C.Y - CritDim_a);
                effWindAreas.Add(11, new EffectiveWindArea("Zone1", new List<Point> { p50, p51, p52, p53 }, null));
                effWindAreas.Add(12, new EffectiveWindArea("Zone2r", new List<Point> { p12, p50, p53, p52, p51, p13, D, C }, null));

                // upper trapezoid
                Point p60 = new Point(p22.X + inset_dist, p22.Y);
                Point p61 = new Point(C.X + (inset_dist - CritDim_a), C.Y + CritDim_a);
                Point p62 = new Point(D.X - (inset_dist - CritDim_a), D.Y + CritDim_a);
                Point p63 = new Point(p23.X - inset_dist, p23.Y);

                effWindAreas.Add(13, new EffectiveWindArea("Zone1", new List<Point> { p60, p61, p62, p63 }, null));
                effWindAreas.Add(14, new EffectiveWindArea("Zone2r", new List<Point> { p22, C, D, p23, p63, p62, p61, p60 }, null));

                // right triangle
                Point p70 = new Point(p13.X, p13.Y + inset_dist);
                Point p71 = new Point(p23.X, p23.Y - inset_dist);
                Point p72 = new Point(D.X + inset_dist, D.Y);
                effWindAreas.Add(15, new EffectiveWindArea("Zone1", new List<Point> { p70, p71, p72 }, null));
                effWindAreas.Add(16, new EffectiveWindArea("Zone2r", new List<Point> { p13, p70, p72, p71, p23, D }, null));
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
                Point E = new Point(0, buildingData.BuildingWidth);
                Point F = new Point(buildingData.BuildingLength, buildingData.BuildingWidth);

                Point p1 = new Point(0, 0);
                Point p2 = new Point(CritDim_a, 0);
                Point p3 = new Point(buildingData.BuildingLength - CritDim_a, 0);
                Point p4 = new Point(buildingData.BuildingLength, 0);

                Point p11 = new Point(0, CritDim_a);
                Point p12 = new Point(CritDim_a, CritDim_a);
                Point p13 = new Point(buildingData.BuildingLength - CritDim_a, CritDim_a);
                Point p14 = new Point(buildingData.BuildingLength, CritDim_a);

                Point p21 = new Point(0, buildingData.BuildingWidth - CritDim_a);
                Point p22 = new Point(CritDim_a, buildingData.BuildingWidth - CritDim_a);
                Point p23 = new Point(buildingData.BuildingLength - CritDim_a, buildingData.BuildingWidth - CritDim_a);
                Point p24 = new Point(buildingData.BuildingLength, buildingData.BuildingWidth - CritDim_a);

                Point p31 = new Point(0, buildingData.BuildingWidth);
                Point p32 = new Point(CritDim_a, buildingData.BuildingWidth);
                Point p33 = new Point(buildingData.BuildingLength - CritDim_a, buildingData.BuildingWidth);
                Point p34 = new Point(buildingData.BuildingLength, buildingData.BuildingWidth);

                effWindAreas.Add(1, new EffectiveWindArea("Zone3", new List<Point> { p1, p2, p12, p11 }, null));
                effWindAreas.Add(2, new EffectiveWindArea("Zone2e", new List<Point> { p2, p3, p13, p12 }, null));
                effWindAreas.Add(3, new EffectiveWindArea("Zone3", new List<Point> { p3, p4, p14, p13 }, null));
                effWindAreas.Add(4, new EffectiveWindArea("Zone2e", new List<Point> { p11, p12, p22, p21 }, null));
                effWindAreas.Add(5, new EffectiveWindArea("Zone2e", new List<Point> { p13, p14, p24, p23 }, null));
                effWindAreas.Add(6, new EffectiveWindArea("Zone3", new List<Point> { p21, p22, p32, p31 }, null));
                effWindAreas.Add(7, new EffectiveWindArea("Zone2e", new List<Point> { p22, p23, p33, p32 }, null));
                effWindAreas.Add(8, new EffectiveWindArea("Zone3", new List<Point> { p23, p24, p34, p33 }, null));

                // for finding the inset points
                var inset_dist = 1.414 * CritDim_a;

                // left triangle
                Point p40 = new Point(p12.X, p12.Y + inset_dist);
                Point p41 = new Point(C.X - inset_dist, C.Y);
                Point p42 = new Point(p22.X, p22.Y - inset_dist);
                effWindAreas.Add(9, new EffectiveWindArea("Zone1", new List<Point> { p40, p41, p42 }, null));
                effWindAreas.Add(10, new EffectiveWindArea("Zone2r", new List<Point> { p12, C, p22, p42, p41, p40 }, null));

                // lower triangle
                Point p50 = new Point(p12.X + inset_dist, p12.Y);
                Point p51 = new Point(p13.X - inset_dist, p13.Y);
                Point p52 = new Point(C.X, C.Y - inset_dist);
                effWindAreas.Add(11, new EffectiveWindArea("Zone1", new List<Point> { p50, p51, p52 }, null));
                effWindAreas.Add(12, new EffectiveWindArea("Zone2r", new List<Point> { p12, p50, p52, p51, p13, C }, null));

                // upper triangle
                Point p60 = new Point(p22.X + inset_dist, p22.Y);
                Point p61 = new Point(C.X, C.Y + inset_dist);
                Point p62 = new Point(p23.X - inset_dist, p23.Y);

                effWindAreas.Add(13, new EffectiveWindArea("Zone1", new List<Point> { p60, p61, p62 }, null));
                effWindAreas.Add(14, new EffectiveWindArea("Zone2r", new List<Point> { p22, C, p23, p62, p61, p60 }, null));

                // right triangle
                Point p70 = new Point(p13.X, p13.Y + inset_dist);
                Point p71 = new Point(p23.X, p23.Y - inset_dist);
                Point p72 = new Point(C.X + inset_dist, C.Y);
                effWindAreas.Add(15, new EffectiveWindArea("Zone1", new List<Point> { p70, p71, p72 }, null));
                effWindAreas.Add(16, new EffectiveWindArea("Zone2r", new List<Point> { p13, p70, p72, p71, p23, C }, null));
            }
        }
    }
}
