using System.Collections.Generic;

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
                Vec2 A = new Vec2(0, 0);
                Vec2 B = new Vec2(buildingData.BuildingLength, 0);
                Vec2 C = new Vec2(0.5 * buildingData.BuildingLength, 0.5 * buildingData.BuildingLength);
                Vec2 D = new Vec2(0.5 * buildingData.BuildingLength, buildingData.BuildingWidth - 0.5 * buildingData.BuildingLength);
                Vec2 E = new Vec2(0, buildingData.BuildingWidth);
                Vec2 F = new Vec2(buildingData.BuildingLength, buildingData.BuildingWidth);

                Vec2 p1 = new Vec2(CritDim_a, CritDim_a); // lower left corner
                Vec2 p2 = new Vec2(CritDim_a + inset_dist, CritDim_a); // lower left 1st intermediate
                Vec2 p3 = new Vec2(buildingData.BuildingLength - CritDim_a - inset_dist, CritDim_a); // lower right 1st intermediate
                Vec2 p4 = new Vec2(buildingData.BuildingLength - CritDim_a, CritDim_a); // lower right
                Vec2 p5 = new Vec2(C.X, C.Y - inset_dist); // upper corner of lower triangle

                // lower triangle
                effWindAreas.Add(1, new EffectiveWindArea("Zone3", new List<Vec2> { A, B, p4, p1 }, null));
                effWindAreas.Add(2, new EffectiveWindArea("Zone2", new List<Vec2> { p1, p2, p5, p3, p4, C }, null));
                effWindAreas.Add(3, new EffectiveWindArea("Zone1", new List<Vec2> { p2, p3, p5 }, null));

                // left trapezoid
                Vec2 p6 = new Vec2(p1.X, p1.Y + inset_dist);
                Vec2 p7 = new Vec2(p1.X, buildingData.BuildingWidth - CritDim_a - inset_dist);
                Vec2 p8 = new Vec2(p1.X, buildingData.BuildingWidth - CritDim_a);

                Vec2 p9 = new Vec2(C.X - CritDim_a, C.Y + (inset_dist - CritDim_a));
                Vec2 p10 = new Vec2(D.X - CritDim_a, D.Y - (inset_dist - CritDim_a));

                effWindAreas.Add(4, new EffectiveWindArea("Zone3", new List<Vec2> { A, p1, p8, E }, null));
                effWindAreas.Add(5, new EffectiveWindArea("Zone2", new List<Vec2> { p1, C, D, p8, p7, p10, p9, p6 }, null));
                effWindAreas.Add(6, new EffectiveWindArea("Zone1", new List<Vec2> { p6, p9, p10, p7 }, null));

                Vec2 p20 = new Vec2(p4.X, p4.Y + inset_dist);
                Vec2 p21 = new Vec2(p4.X, buildingData.BuildingWidth - CritDim_a - inset_dist);
                Vec2 p22 = new Vec2(p4.X, buildingData.BuildingWidth - CritDim_a);

                Vec2 p23 = new Vec2(C.X + CritDim_a, C.Y + (inset_dist - CritDim_a));
                Vec2 p24 = new Vec2(D.X + CritDim_a, D.Y - (inset_dist - CritDim_a));

                // right trapezoid
                effWindAreas.Add(7, new EffectiveWindArea("Zone3", new List<Vec2> { B, F, p22, p4 }, null));
                effWindAreas.Add(8, new EffectiveWindArea("Zone2", new List<Vec2> { p4, p20, p23, p24, p21, p22, D, C }, null));
                effWindAreas.Add(9, new EffectiveWindArea("Zone1", new List<Vec2> { p20, p21, p24, p23 }, null));
                 
                // top triangle
                Vec2 p31 = new Vec2(D.X, D.Y + inset_dist);
                Vec2 p32 = new Vec2(p8.X + inset_dist, p8.Y);
                Vec2 p33 = new Vec2(p22.X - inset_dist, p22.Y);
                effWindAreas.Add(10, new EffectiveWindArea("Zone3", new List<Vec2> { E, p8, p22, F }, null));
                effWindAreas.Add(11, new EffectiveWindArea("Zone2", new List<Vec2> { p8, D, p22, p33, p31, p32, p22 }, null));
                effWindAreas.Add(12, new EffectiveWindArea("Zone1", new List<Vec2> { p32, p31, p33 }, null));
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

                // bottom trapezoid
                Vec2 p1 = new Vec2(CritDim_a, CritDim_a);
                Vec2 p2 = new Vec2(CritDim_a + inset_dist, CritDim_a);
                Vec2 p3 = new Vec2(buildingData.BuildingLength - inset_dist - CritDim_a, CritDim_a);
                Vec2 p4 = new Vec2(buildingData.BuildingLength - CritDim_a, CritDim_a);
                Vec2 p5 = new Vec2(C.X + inset_dist - CritDim_a, C.Y - CritDim_a);
                Vec2 p6 = new Vec2(D.X - inset_dist + CritDim_a, D.Y - CritDim_a);

                effWindAreas.Add(1, new EffectiveWindArea("Zone3", new List<Vec2> { A, B, p4, p1 }, null));
                effWindAreas.Add(2, new EffectiveWindArea("Zone2", new List<Vec2> { p1, p2, p5, p6, p3, p4, D, C }, null));
                effWindAreas.Add(3, new EffectiveWindArea("Zone1", new List<Vec2> { p2, p3, p6, p5 }, null));

                // left triangle
                Vec2 p7 = new Vec2(CritDim_a, inset_dist + CritDim_a);
                Vec2 p8 = new Vec2(C.X - inset_dist, C.Y);
                Vec2 p9 = new Vec2(CritDim_a, buildingData.BuildingWidth - inset_dist - CritDim_a);
                Vec2 p13 = new Vec2(CritDim_a, buildingData.BuildingWidth - CritDim_a);

                effWindAreas.Add(4, new EffectiveWindArea("Zone3", new List<Vec2> { A, p1, p13, E }, null));
                effWindAreas.Add(5, new EffectiveWindArea("Zone2", new List<Vec2> { p1, C, p13, p9, p8, p7 }, null));
                effWindAreas.Add(6, new EffectiveWindArea("Zone1", new List<Vec2> { p7, p8, p9 }, null));

                //// right triangle
                Vec2 p10 = new Vec2(buildingData.BuildingLength - CritDim_a, inset_dist + CritDim_a);
                Vec2 p11 = new Vec2(buildingData.BuildingLength - CritDim_a, buildingData.BuildingWidth - inset_dist - CritDim_a);
                Vec2 p12 = new Vec2(D.X + inset_dist, D.Y);
                Vec2 p18 = new Vec2(buildingData.BuildingLength - CritDim_a, buildingData.BuildingWidth - CritDim_a);

                effWindAreas.Add(7, new EffectiveWindArea("Zone3", new List<Vec2> { B, F, p18, p4 }, null));
                effWindAreas.Add(8, new EffectiveWindArea("Zone2", new List<Vec2> { p4, p10, p12, p11, p18, D }, null));
                effWindAreas.Add(9, new EffectiveWindArea("Zone1", new List<Vec2> { p10, p11, p12 }, null));

                // top trapezoid
                Vec2 p14 = new Vec2(CritDim_a + inset_dist, buildingData.BuildingWidth - CritDim_a);
                Vec2 p17 = new Vec2(buildingData.BuildingLength - inset_dist - CritDim_a, buildingData.BuildingWidth - CritDim_a);
                Vec2 p15 = new Vec2(C.X + inset_dist - CritDim_a, C.Y + CritDim_a);
                Vec2 p16 = new Vec2(D.X - inset_dist + CritDim_a, D.Y + CritDim_a);

                effWindAreas.Add(10, new EffectiveWindArea("Zone3", new List<Vec2> { E, p13, p18, F }, null));
                effWindAreas.Add(11, new EffectiveWindArea("Zone2", new List<Vec2> { p13, C, D, p18, p17, p16, p15, p14 }, null));
                effWindAreas.Add(12, new EffectiveWindArea("Zone1", new List<Vec2> { p14, p15, p16, p17 }, null));
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
                Vec2 D = new Vec2(0, buildingData.BuildingWidth);
                Vec2 E = new Vec2(buildingData.BuildingLength, buildingData.BuildingWidth);

                // Bottom triangle
                Vec2 p1 = new Vec2(CritDim_a, CritDim_a);
                Vec2 p2 = new Vec2(inset_dist + CritDim_a, CritDim_a);
                Vec2 p3 = new Vec2(C.X, C.Y - inset_dist);
                Vec2 p4 = new Vec2(buildingData.BuildingLength - inset_dist - CritDim_a, CritDim_a);
                Vec2 p5 = new Vec2(buildingData.BuildingLength - CritDim_a, CritDim_a);

                effWindAreas.Add(1, new EffectiveWindArea("Zone3", new List<Vec2> { A, B, p5, p1 }, null));
                effWindAreas.Add(2, new EffectiveWindArea("Zone2", new List<Vec2> { p1, p2, p3, p4, p5, C }, null));
                effWindAreas.Add(3, new EffectiveWindArea("Zone1", new List<Vec2> { p2, p4, p3 }, null));

                // left triangle
                Vec2 p6 = new Vec2(CritDim_a, inset_dist + CritDim_a);
                Vec2 p7 = new Vec2(C.X - inset_dist, C.Y);
                Vec2 p8 = new Vec2(CritDim_a, buildingData.BuildingWidth - inset_dist - CritDim_a);
                Vec2 p9 = new Vec2(CritDim_a, buildingData.BuildingWidth - CritDim_a);

                effWindAreas.Add(4, new EffectiveWindArea("Zone3", new List<Vec2> { A, p1, p9, D }, null));
                effWindAreas.Add(5, new EffectiveWindArea("Zone2", new List<Vec2> { p1, C, p9, p8, p7, p6 }, null));
                effWindAreas.Add(6, new EffectiveWindArea("Zone1", new List<Vec2> { p6, p7, p8 }, null));

                // right triangle
                Vec2 p10 = new Vec2(buildingData.BuildingLength - CritDim_a, inset_dist + CritDim_a);
                Vec2 p11 = new Vec2(C.X + inset_dist, C.Y);
                Vec2 p12 = new Vec2(buildingData.BuildingLength - CritDim_a, buildingData.BuildingWidth - inset_dist - CritDim_a);
                Vec2 p13 = new Vec2(buildingData.BuildingLength - CritDim_a, buildingData.BuildingWidth - CritDim_a);

                effWindAreas.Add(7, new EffectiveWindArea("Zone3", new List<Vec2> { B, E, p13, p5 }, null));
                effWindAreas.Add(8, new EffectiveWindArea("Zone2", new List<Vec2> { p5, p10, p11, p12, p13, C }, null));
                effWindAreas.Add(9, new EffectiveWindArea("Zone1", new List<Vec2> { p10, p12, p11 }, null));

                // top triangle
                Vec2 p14 = new Vec2(inset_dist + CritDim_a, buildingData.BuildingWidth - CritDim_a);
                Vec2 p15 = new Vec2(C.X, C.Y + inset_dist);
                Vec2 p16 = new Vec2(buildingData.BuildingLength - inset_dist - CritDim_a, buildingData.BuildingWidth - CritDim_a);

                effWindAreas.Add(10, new EffectiveWindArea("Zone3", new List<Vec2> { D, p9, p13, E }, null));
                effWindAreas.Add(11, new EffectiveWindArea("Zone2", new List<Vec2> { p9, C, p13, p16, p15, p14 }, null));
                effWindAreas.Add(12, new EffectiveWindArea("Zone1", new List<Vec2> { p14, p15, p16 }, null));

            }
        }
    }
}
