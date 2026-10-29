using System.Collections.Generic;

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
                Vec2 A = new Vec2(0, 0);
                Vec2 B = new Vec2(buildingData.BuildingLength, 0);
                Vec2 C = new Vec2(0.5 * buildingData.BuildingLength, 0.5 * buildingData.BuildingLength);
                Vec2 D = new Vec2(0.5 * buildingData.BuildingLength, buildingData.BuildingWidth - 0.5 * buildingData.BuildingLength);
                Vec2 E = new Vec2(0, buildingData.BuildingWidth);
                Vec2 F = new Vec2(buildingData.BuildingLength, buildingData.BuildingWidth);

                Vec2 p1 = new Vec2(0, 0);
                Vec2 p2 = new Vec2(CritDim_a, 0);
                Vec2 p3 = new Vec2(buildingData.BuildingLength - CritDim_a, 0);
                Vec2 p4 = new Vec2(buildingData.BuildingLength, 0);

                Vec2 p11 = new Vec2(0, CritDim_a);
                Vec2 p12 = new Vec2(CritDim_a, CritDim_a);
                Vec2 p13 = new Vec2(buildingData.BuildingLength - CritDim_a, CritDim_a);
                Vec2 p14 = new Vec2(buildingData.BuildingLength, CritDim_a);

                Vec2 p21 = new Vec2(0, buildingData.BuildingWidth - CritDim_a);
                Vec2 p22 = new Vec2(CritDim_a, buildingData.BuildingWidth - CritDim_a);
                Vec2 p23 = new Vec2(buildingData.BuildingLength - CritDim_a, buildingData.BuildingWidth - CritDim_a);
                Vec2 p24 = new Vec2(buildingData.BuildingLength, buildingData.BuildingWidth - CritDim_a);

                Vec2 p31 = new Vec2(0, buildingData.BuildingWidth);
                Vec2 p32 = new Vec2(CritDim_a, buildingData.BuildingWidth);
                Vec2 p33 = new Vec2(buildingData.BuildingLength - CritDim_a, buildingData.BuildingWidth);
                Vec2 p34 = new Vec2(buildingData.BuildingLength, buildingData.BuildingWidth);

                effWindAreas.Add(1, new EffectiveWindArea("Zone3", new List<Vec2> { p1, p2, p12, p11 }, null));
                effWindAreas.Add(2, new EffectiveWindArea("Zone2e", new List<Vec2> { p2, p3, p13, p12 }, null));
                effWindAreas.Add(3, new EffectiveWindArea("Zone3", new List<Vec2> { p3, p4, p14, p13 }, null));
                effWindAreas.Add(4, new EffectiveWindArea("Zone2e", new List<Vec2> { p11, p12, p22, p21 }, null));
                effWindAreas.Add(5, new EffectiveWindArea("Zone2e", new List<Vec2> { p13, p14, p24, p23 }, null));
                effWindAreas.Add(6, new EffectiveWindArea("Zone3", new List<Vec2> { p21, p22, p32, p31 }, null));
                effWindAreas.Add(7, new EffectiveWindArea("Zone2e", new List<Vec2> { p22, p23, p33, p32 }, null));
                effWindAreas.Add(8, new EffectiveWindArea("Zone3", new List<Vec2> { p23, p24, p34, p33 }, null));

                // for finding the inset points
                var inset_dist = 1.414 * CritDim_a;

                // lower triangle
                Vec2 p40 = new Vec2(p12.X + inset_dist, p12.Y);
                Vec2 p41 = new Vec2(p13.X - inset_dist, p13.Y);
                Vec2 p42 = new Vec2(C.X, C.Y - inset_dist);
                effWindAreas.Add(9, new EffectiveWindArea("Zone1", new List<Vec2> { p40, p41, p42 }, null));
                effWindAreas.Add(10, new EffectiveWindArea("Zone2r", new List<Vec2> { p12, p40, p42, p41, p13, C }, null));

                // left trapezoid
                Vec2 p50 = new Vec2(p12.X, p12.Y + inset_dist);
                Vec2 p51 = new Vec2(C.X - CritDim_a, C.Y + (inset_dist - CritDim_a));
                Vec2 p52 = new Vec2(D.X - CritDim_a, D.Y - (inset_dist - CritDim_a));
                Vec2 p53 = new Vec2(p22.X, p22.Y - inset_dist);
                effWindAreas.Add(11, new EffectiveWindArea("Zone1", new List<Vec2> { p50, p51, p52, p53 }, null));
                effWindAreas.Add(12, new EffectiveWindArea("Zone2r", new List<Vec2> { p12, C, D, p22, p53, p52, p51, p50 }, null));

                // left trapezoid
                Vec2 p60 = new Vec2(p13.X, p13.Y + inset_dist);
                Vec2 p61 = new Vec2(p23.X, p23.Y - inset_dist);
                Vec2 p62 = new Vec2(D.X + CritDim_a, D.Y - (inset_dist - CritDim_a));
                Vec2 p63 = new Vec2(C.X + CritDim_a, C.Y + (inset_dist - CritDim_a));
                effWindAreas.Add(13, new EffectiveWindArea("Zone1", new List<Vec2> { p60, p61, p62, p63 }, null));
                effWindAreas.Add(14, new EffectiveWindArea("Zone2r", new List<Vec2> { p13, p60, p63, p62, p61, p23, D, C }, null));

                // top triangle
                Vec2 p70 = new Vec2(p22.X + inset_dist, p22.Y);
                Vec2 p71 = new Vec2(p23.X - inset_dist, p23.Y);
                Vec2 p72 = new Vec2(D.X, D.Y + inset_dist);
                effWindAreas.Add(15, new EffectiveWindArea("Zone1", new List<Vec2> { p70, p72, p71 }, null));
                effWindAreas.Add(16, new EffectiveWindArea("Zone2r", new List<Vec2> { p22, D, p23, p71, p72, p70 }, null));
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
                Vec2 A = new Vec2(0, 0);
                Vec2 B = new Vec2(buildingData.BuildingLength, 0);
                Vec2 C = new Vec2(0.5 * buildingData.BuildingWidth, 0.5 * buildingData.BuildingWidth);
                Vec2 D = new Vec2(buildingData.BuildingLength - 0.5 * buildingData.BuildingWidth, 0.5 * buildingData.BuildingWidth);
                Vec2 E = new Vec2(0, buildingData.BuildingWidth);
                Vec2 F = new Vec2(buildingData.BuildingLength, buildingData.BuildingWidth);

                Vec2 p1 = new Vec2(0, 0);
                Vec2 p2 = new Vec2(CritDim_a, 0);
                Vec2 p3 = new Vec2(buildingData.BuildingLength - CritDim_a, 0);
                Vec2 p4 = new Vec2(buildingData.BuildingLength, 0);

                Vec2 p11 = new Vec2(0, CritDim_a);
                Vec2 p12 = new Vec2(CritDim_a, CritDim_a);
                Vec2 p13 = new Vec2(buildingData.BuildingLength - CritDim_a, CritDim_a);
                Vec2 p14 = new Vec2(buildingData.BuildingLength, CritDim_a);

                Vec2 p21 = new Vec2(0, buildingData.BuildingWidth - CritDim_a);
                Vec2 p22 = new Vec2(CritDim_a, buildingData.BuildingWidth - CritDim_a);
                Vec2 p23 = new Vec2(buildingData.BuildingLength - CritDim_a, buildingData.BuildingWidth - CritDim_a);
                Vec2 p24 = new Vec2(buildingData.BuildingLength, buildingData.BuildingWidth - CritDim_a);

                Vec2 p31 = new Vec2(0, buildingData.BuildingWidth);
                Vec2 p32 = new Vec2(CritDim_a, buildingData.BuildingWidth);
                Vec2 p33 = new Vec2(buildingData.BuildingLength - CritDim_a, buildingData.BuildingWidth);
                Vec2 p34 = new Vec2(buildingData.BuildingLength, buildingData.BuildingWidth);

                effWindAreas.Add(1, new EffectiveWindArea("Zone3", new List<Vec2> { p1, p2, p12, p11 }, null));
                effWindAreas.Add(2, new EffectiveWindArea("Zone2e", new List<Vec2> { p2, p3, p13, p12 }, null));
                effWindAreas.Add(3, new EffectiveWindArea("Zone3", new List<Vec2> { p3, p4, p14, p13 }, null));
                effWindAreas.Add(4, new EffectiveWindArea("Zone2e", new List<Vec2> { p11, p12, p22, p21 }, null));
                effWindAreas.Add(5, new EffectiveWindArea("Zone2e", new List<Vec2> { p13, p14, p24, p23 }, null));
                effWindAreas.Add(6, new EffectiveWindArea("Zone3", new List<Vec2> { p21, p22, p32, p31 }, null));
                effWindAreas.Add(7, new EffectiveWindArea("Zone2e", new List<Vec2> { p22, p23, p33, p32 }, null));
                effWindAreas.Add(8, new EffectiveWindArea("Zone3", new List<Vec2> { p23, p24, p34, p33 }, null));

                // for finding the inset points
                var inset_dist = 1.414 * CritDim_a;

                // left triangle
                Vec2 p40 = new Vec2(p12.X, p12.Y + inset_dist);
                Vec2 p41 = new Vec2(C.X - inset_dist, C.Y);
                Vec2 p42 = new Vec2(p22.X, p22.Y - inset_dist);
                effWindAreas.Add(9, new EffectiveWindArea("Zone1", new List<Vec2> { p40, p41, p42 }, null));
                effWindAreas.Add(10, new EffectiveWindArea("Zone2r", new List<Vec2> { p12, C, p22, p42, p41, p40 }, null));

                // lower trapezoid
                Vec2 p50 = new Vec2(p12.X + inset_dist, p12.Y);
                Vec2 p51 = new Vec2(p13.X - inset_dist, p13.Y);
                Vec2 p52 = new Vec2(D.X - (inset_dist - CritDim_a), D.Y - CritDim_a);
                Vec2 p53 = new Vec2(C.X + (inset_dist - CritDim_a), C.Y - CritDim_a);
                effWindAreas.Add(11, new EffectiveWindArea("Zone1", new List<Vec2> { p50, p51, p52, p53 }, null));
                effWindAreas.Add(12, new EffectiveWindArea("Zone2r", new List<Vec2> { p12, p50, p53, p52, p51, p13, D, C }, null));

                // upper trapezoid
                Vec2 p60 = new Vec2(p22.X + inset_dist, p22.Y);
                Vec2 p61 = new Vec2(C.X + (inset_dist - CritDim_a), C.Y + CritDim_a);
                Vec2 p62 = new Vec2(D.X - (inset_dist - CritDim_a), D.Y + CritDim_a);
                Vec2 p63 = new Vec2(p23.X - inset_dist, p23.Y);

                effWindAreas.Add(13, new EffectiveWindArea("Zone1", new List<Vec2> { p60, p61, p62, p63 }, null));
                effWindAreas.Add(14, new EffectiveWindArea("Zone2r", new List<Vec2> { p22, C, D, p23, p63, p62, p61, p60 }, null));

                // right triangle
                Vec2 p70 = new Vec2(p13.X, p13.Y + inset_dist);
                Vec2 p71 = new Vec2(p23.X, p23.Y - inset_dist);
                Vec2 p72 = new Vec2(D.X + inset_dist, D.Y);
                effWindAreas.Add(15, new EffectiveWindArea("Zone1", new List<Vec2> { p70, p71, p72 }, null));
                effWindAreas.Add(16, new EffectiveWindArea("Zone2r", new List<Vec2> { p13, p70, p72, p71, p23, D }, null));
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
                Vec2 A = new Vec2(0, 0);
                Vec2 B = new Vec2(buildingData.BuildingLength, 0);
                Vec2 C = new Vec2(0.5 * buildingData.BuildingLength, 0.5 * buildingData.BuildingWidth);
                Vec2 E = new Vec2(0, buildingData.BuildingWidth);
                Vec2 F = new Vec2(buildingData.BuildingLength, buildingData.BuildingWidth);

                Vec2 p1 = new Vec2(0, 0);
                Vec2 p2 = new Vec2(CritDim_a, 0);
                Vec2 p3 = new Vec2(buildingData.BuildingLength - CritDim_a, 0);
                Vec2 p4 = new Vec2(buildingData.BuildingLength, 0);

                Vec2 p11 = new Vec2(0, CritDim_a);
                Vec2 p12 = new Vec2(CritDim_a, CritDim_a);
                Vec2 p13 = new Vec2(buildingData.BuildingLength - CritDim_a, CritDim_a);
                Vec2 p14 = new Vec2(buildingData.BuildingLength, CritDim_a);

                Vec2 p21 = new Vec2(0, buildingData.BuildingWidth - CritDim_a);
                Vec2 p22 = new Vec2(CritDim_a, buildingData.BuildingWidth - CritDim_a);
                Vec2 p23 = new Vec2(buildingData.BuildingLength - CritDim_a, buildingData.BuildingWidth - CritDim_a);
                Vec2 p24 = new Vec2(buildingData.BuildingLength, buildingData.BuildingWidth - CritDim_a);

                Vec2 p31 = new Vec2(0, buildingData.BuildingWidth);
                Vec2 p32 = new Vec2(CritDim_a, buildingData.BuildingWidth);
                Vec2 p33 = new Vec2(buildingData.BuildingLength - CritDim_a, buildingData.BuildingWidth);
                Vec2 p34 = new Vec2(buildingData.BuildingLength, buildingData.BuildingWidth);

                effWindAreas.Add(1, new EffectiveWindArea("Zone3", new List<Vec2> { p1, p2, p12, p11 }, null));
                effWindAreas.Add(2, new EffectiveWindArea("Zone2e", new List<Vec2> { p2, p3, p13, p12 }, null));
                effWindAreas.Add(3, new EffectiveWindArea("Zone3", new List<Vec2> { p3, p4, p14, p13 }, null));
                effWindAreas.Add(4, new EffectiveWindArea("Zone2e", new List<Vec2> { p11, p12, p22, p21 }, null));
                effWindAreas.Add(5, new EffectiveWindArea("Zone2e", new List<Vec2> { p13, p14, p24, p23 }, null));
                effWindAreas.Add(6, new EffectiveWindArea("Zone3", new List<Vec2> { p21, p22, p32, p31 }, null));
                effWindAreas.Add(7, new EffectiveWindArea("Zone2e", new List<Vec2> { p22, p23, p33, p32 }, null));
                effWindAreas.Add(8, new EffectiveWindArea("Zone3", new List<Vec2> { p23, p24, p34, p33 }, null));

                // for finding the inset points
                var inset_dist = 1.414 * CritDim_a;

                // left triangle
                Vec2 p40 = new Vec2(p12.X, p12.Y + inset_dist);
                Vec2 p41 = new Vec2(C.X - inset_dist, C.Y);
                Vec2 p42 = new Vec2(p22.X, p22.Y - inset_dist);
                effWindAreas.Add(9, new EffectiveWindArea("Zone1", new List<Vec2> { p40, p41, p42 }, null));
                effWindAreas.Add(10, new EffectiveWindArea("Zone2r", new List<Vec2> { p12, C, p22, p42, p41, p40 }, null));

                // lower triangle
                Vec2 p50 = new Vec2(p12.X + inset_dist, p12.Y);
                Vec2 p51 = new Vec2(p13.X - inset_dist, p13.Y);
                Vec2 p52 = new Vec2(C.X, C.Y - inset_dist);
                effWindAreas.Add(11, new EffectiveWindArea("Zone1", new List<Vec2> { p50, p51, p52 }, null));
                effWindAreas.Add(12, new EffectiveWindArea("Zone2r", new List<Vec2> { p12, p50, p52, p51, p13, C }, null));

                // upper triangle
                Vec2 p60 = new Vec2(p22.X + inset_dist, p22.Y);
                Vec2 p61 = new Vec2(C.X, C.Y + inset_dist);
                Vec2 p62 = new Vec2(p23.X - inset_dist, p23.Y);

                effWindAreas.Add(13, new EffectiveWindArea("Zone1", new List<Vec2> { p60, p61, p62 }, null));
                effWindAreas.Add(14, new EffectiveWindArea("Zone2r", new List<Vec2> { p22, C, p23, p62, p61, p60 }, null));

                // right triangle
                Vec2 p70 = new Vec2(p13.X, p13.Y + inset_dist);
                Vec2 p71 = new Vec2(p23.X, p23.Y - inset_dist);
                Vec2 p72 = new Vec2(C.X + inset_dist, C.Y);
                effWindAreas.Add(15, new EffectiveWindArea("Zone1", new List<Vec2> { p70, p71, p72 }, null));
                effWindAreas.Add(16, new EffectiveWindArea("Zone2r", new List<Vec2> { p13, p70, p72, p71, p23, C }, null));
            }
        }
    }
}
