using System.Collections.Generic;

namespace ASCE7WindLoadCalculator
{
    public class GableRoofAreaCalculator_CC_ASCE7_16 : AreaCalculator_CC_Base
    {
        public GableRoofAreaCalculator_CC_ASCE7_16(BuildingData bldg_data)
        {
            buildingData = bldg_data;
        }

        public override void ComputeEffectiveWindAreas()
        {
            /// <summary>
            /// The critical width dimenstion "a" used throughout chapter 30
            /// -- minimum of 0.4 * building height and 0.1 * min(building Length, building width)
            /// but not less than 4% of smallest dimension or 3 ft.
            /// </summary>
            
            // Gable logic
            if (buildingData.BuildingLength < buildingData.BuildingWidth)
            {
                Vec2 p1 = new Vec2(0, 0);
                Vec2 p2 = new Vec2(CritDim_a, 0);
                Vec2 p3 = new Vec2(0.5 * buildingData.BuildingLength - CritDim_a, 0);
                Vec2 p4 = new Vec2(0.5 * buildingData.BuildingLength, 0);
                Vec2 p5 = new Vec2(0.5 * buildingData.BuildingLength + CritDim_a, 0);
                Vec2 p6 = new Vec2(buildingData.BuildingLength - CritDim_a, 0);
                Vec2 p7 = new Vec2(buildingData.BuildingLength, 0);

                Vec2 p11 = new Vec2(0, CritDim_a);
                Vec2 p12 = new Vec2(CritDim_a, CritDim_a);
                Vec2 p13 = new Vec2(0.5 * buildingData.BuildingLength - CritDim_a, CritDim_a);
                Vec2 p14 = new Vec2(0.5 * buildingData.BuildingLength, CritDim_a);
                Vec2 p15 = new Vec2(0.5 * buildingData.BuildingLength + CritDim_a, CritDim_a);
                Vec2 p16 = new Vec2(buildingData.BuildingLength - CritDim_a, CritDim_a);
                Vec2 p17 = new Vec2(buildingData.BuildingLength, CritDim_a);

                Vec2 p21 = new Vec2(0, buildingData.BuildingWidth - CritDim_a);
                Vec2 p22 = new Vec2(CritDim_a, buildingData.BuildingWidth - CritDim_a);
                Vec2 p23 = new Vec2(0.5 * buildingData.BuildingLength - CritDim_a, buildingData.BuildingWidth - CritDim_a);
                Vec2 p24 = new Vec2(0.5 * buildingData.BuildingLength, buildingData.BuildingWidth - CritDim_a);
                Vec2 p25 = new Vec2(0.5 * buildingData.BuildingLength + CritDim_a, buildingData.BuildingWidth - CritDim_a);
                Vec2 p26 = new Vec2(buildingData.BuildingLength - CritDim_a, buildingData.BuildingWidth - CritDim_a);
                Vec2 p27 = new Vec2(buildingData.BuildingLength, buildingData.BuildingWidth - CritDim_a);

                Vec2 p31 = new Vec2(0, buildingData.BuildingWidth);
                Vec2 p32 = new Vec2(CritDim_a, buildingData.BuildingWidth);
                Vec2 p33 = new Vec2(0.5 * buildingData.BuildingLength - CritDim_a, buildingData.BuildingWidth);
                Vec2 p34 = new Vec2(0.5 * buildingData.BuildingLength, buildingData.BuildingWidth);
                Vec2 p35 = new Vec2(0.5 * buildingData.BuildingLength + CritDim_a, buildingData.BuildingWidth);
                Vec2 p36 = new Vec2(buildingData.BuildingLength - CritDim_a, buildingData.BuildingWidth);
                Vec2 p37 = new Vec2(buildingData.BuildingLength, buildingData.BuildingWidth);

                // Bottom row of rectangles
                effWindAreas.Add(1, new EffectiveWindArea("Zone3e", new List<Vec2> { p1, p2, p12, p11 }, null));
                effWindAreas.Add(2, new EffectiveWindArea("Zone2n", new List<Vec2> { p2, p3, p13, p12 }, null));
                effWindAreas.Add(3, new EffectiveWindArea("Zone3r", new List<Vec2> { p3, p4, p14, p13 }, null));
                effWindAreas.Add(4, new EffectiveWindArea("Zone3r", new List<Vec2> { p4, p5, p15, p14 }, null));
                effWindAreas.Add(5, new EffectiveWindArea("Zone2n", new List<Vec2> { p5, p6, p16, p15 }, null));
                effWindAreas.Add(6, new EffectiveWindArea("Zone3e", new List<Vec2> { p6, p7, p17, p16 }, null));

                // Middle row of rectangles
                effWindAreas.Add(7, new EffectiveWindArea("Zone2e", new List<Vec2> { p11, p12, p22, p21 }, null));
                effWindAreas.Add(8, new EffectiveWindArea("Zone1", new List<Vec2> { p12, p13, p23, p22 }, null));
                effWindAreas.Add(9, new EffectiveWindArea("Zone2r", new List<Vec2> { p13, p14, p24, p23 }, null));
                effWindAreas.Add(10, new EffectiveWindArea("Zone2r", new List<Vec2> { p14, p15, p25, p24 }, null));
                effWindAreas.Add(11, new EffectiveWindArea("Zone1", new List<Vec2> { p15, p16, p26, p25 }, null));
                effWindAreas.Add(12, new EffectiveWindArea("Zone2e", new List<Vec2> { p16, p17, p27, p26 }, null));

                // Top row of rectangles
                effWindAreas.Add(13, new EffectiveWindArea("Zone3e", new List<Vec2> { p21, p22, p32, p31 }, null));
                effWindAreas.Add(14, new EffectiveWindArea("Zone2n", new List<Vec2> { p22, p23, p33, p32 }, null));
                effWindAreas.Add(15, new EffectiveWindArea("Zone3r", new List<Vec2> { p23, p24, p34, p33 }, null));
                effWindAreas.Add(16, new EffectiveWindArea("Zone3r", new List<Vec2> { p24, p25, p35, p34 }, null));
                effWindAreas.Add(17, new EffectiveWindArea("Zone2n", new List<Vec2> { p25, p26, p36, p35 }, null));
                effWindAreas.Add(18, new EffectiveWindArea("Zone3e", new List<Vec2> { p26, p27, p37, p36 }, null));

                return;
            }
            else if (buildingData.BuildingLength > buildingData.BuildingWidth)
            {
                // Figure 30.3-2B / 2C / 2D -- Flat roof and Gable with slope greater than 7

                Vec2 p1 = new Vec2(0, 0);
                Vec2 p2 = new Vec2(CritDim_a, 0);
                Vec2 p3 = new Vec2(buildingData.BuildingLength - CritDim_a, 0);
                Vec2 p4 = new Vec2(buildingData.BuildingLength, 0);

                Vec2 p11 = new Vec2(0, CritDim_a);
                Vec2 p12 = new Vec2(CritDim_a, CritDim_a);
                Vec2 p13 = new Vec2(buildingData.BuildingLength - CritDim_a, CritDim_a);
                Vec2 p14 = new Vec2(buildingData.BuildingLength, CritDim_a);

                Vec2 p21 = new Vec2(0, 0.5 * buildingData.BuildingWidth - CritDim_a);
                Vec2 p22 = new Vec2(CritDim_a, 0.5 * buildingData.BuildingWidth - CritDim_a);
                Vec2 p23 = new Vec2(buildingData.BuildingLength - CritDim_a, 0.5 * buildingData.BuildingWidth - CritDim_a);
                Vec2 p24 = new Vec2(buildingData.BuildingLength, 0.5 * buildingData.BuildingWidth - CritDim_a);

                Vec2 p31 = new Vec2(0, 0.5 * buildingData.BuildingWidth);
                Vec2 p32 = new Vec2(CritDim_a, 0.5 * buildingData.BuildingWidth);
                Vec2 p33 = new Vec2(buildingData.BuildingLength - CritDim_a, 0.5 * buildingData.BuildingWidth);
                Vec2 p34 = new Vec2(buildingData.BuildingLength, 0.5 * buildingData.BuildingWidth);

                Vec2 p41 = new Vec2(0, 0.5 * buildingData.BuildingWidth + CritDim_a);
                Vec2 p42 = new Vec2(CritDim_a, 0.5 * buildingData.BuildingWidth + CritDim_a);
                Vec2 p43 = new Vec2(buildingData.BuildingLength - CritDim_a, 0.5 * buildingData.BuildingWidth + CritDim_a);
                Vec2 p44 = new Vec2(buildingData.BuildingLength, 0.5 * buildingData.BuildingWidth + CritDim_a);

                Vec2 p51 = new Vec2(0, buildingData.BuildingWidth - CritDim_a);
                Vec2 p52 = new Vec2(CritDim_a, buildingData.BuildingWidth - CritDim_a);
                Vec2 p53 = new Vec2(buildingData.BuildingLength - CritDim_a, buildingData.BuildingWidth - CritDim_a);
                Vec2 p54 = new Vec2(buildingData.BuildingLength, buildingData.BuildingWidth - CritDim_a);

                Vec2 p61 = new Vec2(0, buildingData.BuildingWidth);
                Vec2 p62 = new Vec2(CritDim_a, buildingData.BuildingWidth);
                Vec2 p63 = new Vec2(buildingData.BuildingLength - CritDim_a, buildingData.BuildingWidth);
                Vec2 p64 = new Vec2(buildingData.BuildingLength, buildingData.BuildingWidth);


                // Bottom row of rectangles
                effWindAreas.Add(1, new EffectiveWindArea("Zone3e", new List<Vec2> { p1, p2, p12, p11 }, null));
                effWindAreas.Add(2, new EffectiveWindArea("Zone2e", new List<Vec2> { p2, p3, p13, p12 }, null));
                effWindAreas.Add(3, new EffectiveWindArea("Zone3e", new List<Vec2> { p3, p4, p14, p13 }, null));

                // Middle row of rectangles
                effWindAreas.Add(4, new EffectiveWindArea("Zone2n", new List<Vec2> { p11, p12, p22, p21 }, null));
                effWindAreas.Add(5, new EffectiveWindArea("Zone1", new List<Vec2> { p12, p13, p23, p22 }, null));
                effWindAreas.Add(6, new EffectiveWindArea("Zone2n", new List<Vec2> { p13, p14, p24, p23 }, null));

                //// Top row of rectangles
                effWindAreas.Add(7, new EffectiveWindArea("Zone3r", new List<Vec2> { p21, p22, p32, p31 }, null));
                effWindAreas.Add(8, new EffectiveWindArea("Zone2r", new List<Vec2> { p22, p23, p33, p32 }, null));
                effWindAreas.Add(9, new EffectiveWindArea("Zone3r", new List<Vec2> { p23, p24, p34, p33 }, null));

                // ------------------ RIDGE ---------------- //

                // Bottom row of rectangles
                effWindAreas.Add(10, new EffectiveWindArea("Zone3r", new List<Vec2> { p31, p32, p42, p41 }, null));
                effWindAreas.Add(11, new EffectiveWindArea("Zone2r", new List<Vec2> { p32, p33, p43, p42 }, null));
                effWindAreas.Add(12, new EffectiveWindArea("Zone3r", new List<Vec2> { p33, p34, p44, p43 }, null));

                // Middle row of rectangles
                effWindAreas.Add(13, new EffectiveWindArea("Zone2n", new List<Vec2> { p41, p42, p52, p51 }, null));
                effWindAreas.Add(14, new EffectiveWindArea("Zone1", new List<Vec2> { p42, p43, p53, p52 }, null));
                effWindAreas.Add(15, new EffectiveWindArea("Zone2n", new List<Vec2> { p43, p44, p54, p53 }, null));

                // Top row of rectangles
                effWindAreas.Add(16, new EffectiveWindArea("Zone3e", new List<Vec2> { p51, p52, p62, p61 }, null));
                effWindAreas.Add(17, new EffectiveWindArea("Zone2e", new List<Vec2> { p52, p53, p63, p62 }, null));
                effWindAreas.Add(18, new EffectiveWindArea("Zone3e", new List<Vec2> { p53, p54, p64, p63 }, null));

                return;
            }

            // Building length and width are equal, so all zone 1s are triangles
            else
            {
                // Figure 30.3-2B / 2C / 2D -- Flat roof and Gable with slope greater than 7
                Vec2 p1 = new Vec2(0, 0);
                Vec2 p2 = new Vec2(CritDim_a, 0);
                Vec2 p3 = new Vec2(buildingData.BuildingLength - CritDim_a, 0);
                Vec2 p4 = new Vec2(buildingData.BuildingLength, 0);

                Vec2 p11 = new Vec2(0, CritDim_a);
                Vec2 p12 = new Vec2(CritDim_a, CritDim_a);
                Vec2 p13 = new Vec2(buildingData.BuildingLength - CritDim_a, CritDim_a);
                Vec2 p14 = new Vec2(buildingData.BuildingLength, CritDim_a);

                Vec2 p21 = new Vec2(0, 0.5 * buildingData.BuildingWidth - CritDim_a);
                Vec2 p22 = new Vec2(CritDim_a, 0.5 * buildingData.BuildingWidth - CritDim_a);
                Vec2 p23 = new Vec2(buildingData.BuildingLength - CritDim_a, 0.5 * buildingData.BuildingWidth - CritDim_a);
                Vec2 p24 = new Vec2(buildingData.BuildingLength, 0.5 * buildingData.BuildingWidth - CritDim_a);

                Vec2 p31 = new Vec2(0, 0.5 * buildingData.BuildingWidth);
                Vec2 p32 = new Vec2(CritDim_a, 0.5 * buildingData.BuildingWidth);
                Vec2 p33 = new Vec2(buildingData.BuildingLength - CritDim_a, 0.5 * buildingData.BuildingWidth);
                Vec2 p34 = new Vec2(buildingData.BuildingLength, 0.5 * buildingData.BuildingWidth);

                Vec2 p41 = new Vec2(0, 0.5 * buildingData.BuildingWidth + CritDim_a);
                Vec2 p42 = new Vec2(CritDim_a, 0.5 * buildingData.BuildingWidth + CritDim_a);
                Vec2 p43 = new Vec2(buildingData.BuildingLength - CritDim_a, 0.5 * buildingData.BuildingWidth + CritDim_a);
                Vec2 p44 = new Vec2(buildingData.BuildingLength, 0.5 * buildingData.BuildingWidth + CritDim_a);

                Vec2 p51 = new Vec2(0, buildingData.BuildingWidth - CritDim_a);
                Vec2 p52 = new Vec2(CritDim_a, buildingData.BuildingWidth - CritDim_a);
                Vec2 p53 = new Vec2(buildingData.BuildingLength - CritDim_a, buildingData.BuildingWidth - CritDim_a);
                Vec2 p54 = new Vec2(buildingData.BuildingLength, buildingData.BuildingWidth - CritDim_a);

                Vec2 p61 = new Vec2(0, buildingData.BuildingWidth);
                Vec2 p62 = new Vec2(CritDim_a, buildingData.BuildingWidth);
                Vec2 p63 = new Vec2(buildingData.BuildingLength - CritDim_a, buildingData.BuildingWidth);
                Vec2 p64 = new Vec2(buildingData.BuildingLength, buildingData.BuildingWidth);


                // Bottom row of rectangles
                effWindAreas.Add(1, new EffectiveWindArea("Zone3e", new List<Vec2> { p1, p2, p12, p11 }, null));
                effWindAreas.Add(2, new EffectiveWindArea("Zone2e", new List<Vec2> { p2, p3, p13, p12 }, null));
                effWindAreas.Add(3, new EffectiveWindArea("Zone3e", new List<Vec2> { p3, p4, p14, p13 }, null));

                // Middle row of rectangles
                effWindAreas.Add(4, new EffectiveWindArea("Zone2n", new List<Vec2> { p11, p12, p22, p21 }, null));
                effWindAreas.Add(5, new EffectiveWindArea("Zone1", new List<Vec2> { p12, p13, p23, p22 }, null));
                effWindAreas.Add(6, new EffectiveWindArea("Zone2n", new List<Vec2> { p13, p14, p24, p23 }, null));

                //// Top row of rectangles
                effWindAreas.Add(7, new EffectiveWindArea("Zone3r", new List<Vec2> { p21, p22, p32, p31 }, null));
                effWindAreas.Add(8, new EffectiveWindArea("Zone2r", new List<Vec2> { p22, p23, p33, p32 }, null));
                effWindAreas.Add(9, new EffectiveWindArea("Zone3r", new List<Vec2> { p23, p24, p34, p33 }, null));

                // Bottom row of rectangles
                effWindAreas.Add(10, new EffectiveWindArea("Zone3r", new List<Vec2> { p31, p32, p42, p41 }, null));
                effWindAreas.Add(11, new EffectiveWindArea("Zone2r", new List<Vec2> { p32, p33, p43, p42 }, null));
                effWindAreas.Add(12, new EffectiveWindArea("Zone3r", new List<Vec2> { p33, p34, p44, p43 }, null));

                // Middle row of rectangles
                effWindAreas.Add(13, new EffectiveWindArea("Zone2n", new List<Vec2> { p41, p42, p52, p51 }, null));
                effWindAreas.Add(14, new EffectiveWindArea("Zone1", new List<Vec2> { p42, p43, p53, p52 }, null));
                effWindAreas.Add(15, new EffectiveWindArea("Zone2n", new List<Vec2> { p43, p44, p54, p53 }, null));

                // Top row of rectangles
                effWindAreas.Add(16, new EffectiveWindArea("Zone3e", new List<Vec2> { p51, p52, p62, p61 }, null));
                effWindAreas.Add(17, new EffectiveWindArea("Zone2e", new List<Vec2> { p52, p53, p63, p62 }, null));
                effWindAreas.Add(18, new EffectiveWindArea("Zone3e", new List<Vec2> { p53, p54, p64, p63 }, null));

                return;
            }
        }
    }
}
