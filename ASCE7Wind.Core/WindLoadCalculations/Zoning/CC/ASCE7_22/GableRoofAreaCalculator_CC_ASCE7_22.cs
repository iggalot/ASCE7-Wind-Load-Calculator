using System.Collections.Generic;

namespace ASCE7WindLoadCalculator
{
    public class GableRoofAreaCalculator_CC_ASCE7_22 : AreaCalculator_CC_Base
    {
        public GableRoofAreaCalculator_CC_ASCE7_22(BuildingData bldg_data)
        {
            buildingData = bldg_data;
        }

        public override void ComputeEffectiveWindAreas()
        {
            // Gable logic
            // Lay out by the ridge the user chose (it may run along either side), not by which side is longer.
            if (buildingData.RidgeDirection == RidgeDirections.RIDGE_DIR_PERP_TO_BLDGLENGTH)      // ridge along y at x = L/2
            {
                Vec2 p1 = new Vec2(0, 0);
                Vec2 p3 = new Vec2(0.5 * buildingData.BuildingLength - CritDim_a, 0);
                Vec2 p4 = new Vec2(0.5 * buildingData.BuildingLength, 0);
                Vec2 p5 = new Vec2(0.5 * buildingData.BuildingLength + CritDim_a, 0);
                Vec2 p7 = new Vec2(buildingData.BuildingLength, 0);

                Vec2 p11 = new Vec2(0, CritDim_a);
                Vec2 p13 = new Vec2(0.5 * buildingData.BuildingLength - CritDim_a, CritDim_a);
                Vec2 p14 = new Vec2(0.5 * buildingData.BuildingLength, CritDim_a);
                Vec2 p15 = new Vec2(0.5 * buildingData.BuildingLength + CritDim_a, CritDim_a);
                Vec2 p17 = new Vec2(buildingData.BuildingLength, CritDim_a);

                Vec2 p21 = new Vec2(0, buildingData.BuildingWidth - CritDim_a);
                Vec2 p23 = new Vec2(0.5 * buildingData.BuildingLength - CritDim_a, buildingData.BuildingWidth - CritDim_a);
                Vec2 p24 = new Vec2(0.5 * buildingData.BuildingLength, buildingData.BuildingWidth - CritDim_a);
                Vec2 p25 = new Vec2(0.5 * buildingData.BuildingLength + CritDim_a, buildingData.BuildingWidth - CritDim_a);
                Vec2 p27 = new Vec2(buildingData.BuildingLength, buildingData.BuildingWidth - CritDim_a);

                Vec2 p31 = new Vec2(0, buildingData.BuildingWidth);
                Vec2 p33 = new Vec2(0.5 * buildingData.BuildingLength - CritDim_a, buildingData.BuildingWidth);
                Vec2 p34 = new Vec2(0.5 * buildingData.BuildingLength, buildingData.BuildingWidth);
                Vec2 p35 = new Vec2(0.5 * buildingData.BuildingLength + CritDim_a, buildingData.BuildingWidth);
                Vec2 p37 = new Vec2(buildingData.BuildingLength, buildingData.BuildingWidth);

                // Bottom row of rectangles
                effWindAreas.Add(2, new EffectiveWindArea("Zone2", new List<Vec2> { p1, p3, p13, p11 }, null));
                effWindAreas.Add(3, new EffectiveWindArea("Zone3", new List<Vec2> { p3, p4, p14, p13 }, null));
                effWindAreas.Add(4, new EffectiveWindArea("Zone3", new List<Vec2> { p4, p5, p15, p14 }, null));
                effWindAreas.Add(5, new EffectiveWindArea("Zone2", new List<Vec2> { p5, p7, p17, p15 }, null));

                // Middle row of rectangles
                effWindAreas.Add(8, new EffectiveWindArea("Zone1", new List<Vec2> { p11, p13, p23, p21 }, null));
                effWindAreas.Add(9, new EffectiveWindArea("Zone2", new List<Vec2> { p13, p14, p24, p23 }, null));
                effWindAreas.Add(10, new EffectiveWindArea("Zone2", new List<Vec2> { p14, p15, p25, p24 }, null));
                effWindAreas.Add(11, new EffectiveWindArea("Zone1", new List<Vec2> { p15, p17, p27, p25 }, null));

                // Top row of rectangles
                effWindAreas.Add(14, new EffectiveWindArea("Zone2", new List<Vec2> { p21, p23, p33, p31 }, null));
                effWindAreas.Add(15, new EffectiveWindArea("Zone3", new List<Vec2> { p23, p24, p34, p33 }, null));
                effWindAreas.Add(16, new EffectiveWindArea("Zone3", new List<Vec2> { p24, p25, p35, p34 }, null));
                effWindAreas.Add(17, new EffectiveWindArea("Zone2", new List<Vec2> { p25, p27, p37, p35 }, null));

                return;
            }
            else if (buildingData.RidgeDirection == RidgeDirections.RIDGE_DIR_PARALLEL_TO_BLDGLENGTH) // ridge along x at y = B/2
            {
                // Figure 30.3-2B / 2C / 2D -- Flat roof and Gable with slope greater than 7

                Vec2 p1 = new Vec2(0, 0);
                Vec2 p2 = new Vec2(CritDim_a, 0);
                Vec2 p3 = new Vec2(buildingData.BuildingLength - CritDim_a, 0);
                Vec2 p4 = new Vec2(buildingData.BuildingLength, 0);

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

                Vec2 p51 = new Vec2(0, buildingData.BuildingWidth);
                Vec2 p52 = new Vec2(CritDim_a, buildingData.BuildingWidth);
                Vec2 p53 = new Vec2(buildingData.BuildingLength - CritDim_a, buildingData.BuildingWidth);
                Vec2 p54 = new Vec2(buildingData.BuildingLength, buildingData.BuildingWidth);

                // Bottom row of rectangles
                effWindAreas.Add(1, new EffectiveWindArea("Zone2", new List<Vec2> { p1, p2, p22, p21 }, null));
                effWindAreas.Add(2, new EffectiveWindArea("Zone1", new List<Vec2> { p2, p3, p23, p22 }, null));
                effWindAreas.Add(3, new EffectiveWindArea("Zone2", new List<Vec2> { p3, p4, p24, p23 }, null));

                //// Top row of rectangles
                effWindAreas.Add(7, new EffectiveWindArea("Zone3", new List<Vec2> { p21, p22, p32, p31 }, null));
                effWindAreas.Add(8, new EffectiveWindArea("Zone2", new List<Vec2> { p22, p23, p33, p32 }, null));
                effWindAreas.Add(9, new EffectiveWindArea("Zone3", new List<Vec2> { p23, p24, p34, p33 }, null));

                // ------------------ RIDGE ---------------- //

                // Bottom row of rectangles
                effWindAreas.Add(10, new EffectiveWindArea("Zone3", new List<Vec2> { p31, p32, p42, p41 }, null));
                effWindAreas.Add(11, new EffectiveWindArea("Zone2", new List<Vec2> { p32, p33, p43, p42 }, null));
                effWindAreas.Add(12, new EffectiveWindArea("Zone3", new List<Vec2> { p33, p34, p44, p43 }, null));

                // Middle row of rectangles
                effWindAreas.Add(13, new EffectiveWindArea("Zone2", new List<Vec2> { p41, p42, p52, p51 }, null));
                effWindAreas.Add(14, new EffectiveWindArea("Zone1", new List<Vec2> { p42, p43, p53, p52 }, null));
                effWindAreas.Add(15, new EffectiveWindArea("Zone2", new List<Vec2> { p43, p44, p54, p53 }, null));

                // Top row of rectangles

                return;
            }

            // Building length and width are equal, so we will choose the vertical direction as the ridge direction
            // (this is an abitrary selection)
            else
            {
                Vec2 p1 = new Vec2(0, 0);
                Vec2 p3 = new Vec2(0.5 * buildingData.BuildingLength - CritDim_a, 0);
                Vec2 p4 = new Vec2(0.5 * buildingData.BuildingLength, 0);
                Vec2 p5 = new Vec2(0.5 * buildingData.BuildingLength + CritDim_a, 0);
                Vec2 p7 = new Vec2(buildingData.BuildingLength, 0);

                Vec2 p11 = new Vec2(0, CritDim_a);
                Vec2 p13 = new Vec2(0.5 * buildingData.BuildingLength - CritDim_a, CritDim_a);
                Vec2 p14 = new Vec2(0.5 * buildingData.BuildingLength, CritDim_a);
                Vec2 p15 = new Vec2(0.5 * buildingData.BuildingLength + CritDim_a, CritDim_a);
                Vec2 p17 = new Vec2(buildingData.BuildingLength, CritDim_a);

                Vec2 p21 = new Vec2(0, buildingData.BuildingWidth - CritDim_a);
                Vec2 p23 = new Vec2(0.5 * buildingData.BuildingLength - CritDim_a, buildingData.BuildingWidth - CritDim_a);
                Vec2 p24 = new Vec2(0.5 * buildingData.BuildingLength, buildingData.BuildingWidth - CritDim_a);
                Vec2 p25 = new Vec2(0.5 * buildingData.BuildingLength + CritDim_a, buildingData.BuildingWidth - CritDim_a);
                Vec2 p27 = new Vec2(buildingData.BuildingLength, buildingData.BuildingWidth - CritDim_a);

                Vec2 p31 = new Vec2(0, buildingData.BuildingWidth);
                Vec2 p33 = new Vec2(0.5 * buildingData.BuildingLength - CritDim_a, buildingData.BuildingWidth);
                Vec2 p34 = new Vec2(0.5 * buildingData.BuildingLength, buildingData.BuildingWidth);
                Vec2 p35 = new Vec2(0.5 * buildingData.BuildingLength + CritDim_a, buildingData.BuildingWidth);
                Vec2 p37 = new Vec2(buildingData.BuildingLength, buildingData.BuildingWidth);

                // Bottom row of rectangles
                effWindAreas.Add(2, new EffectiveWindArea("Zone2", new List<Vec2> { p1, p3, p13, p11 }, null));
                effWindAreas.Add(3, new EffectiveWindArea("Zone3", new List<Vec2> { p3, p4, p14, p13 }, null));
                effWindAreas.Add(4, new EffectiveWindArea("Zone3", new List<Vec2> { p4, p5, p15, p14 }, null));
                effWindAreas.Add(5, new EffectiveWindArea("Zone2", new List<Vec2> { p5, p7, p17, p15 }, null));

                // Middle row of rectangles
                effWindAreas.Add(8, new EffectiveWindArea("Zone1", new List<Vec2> { p11, p13, p23, p21 }, null));
                effWindAreas.Add(9, new EffectiveWindArea("Zone2", new List<Vec2> { p13, p14, p24, p23 }, null));
                effWindAreas.Add(10, new EffectiveWindArea("Zone2", new List<Vec2> { p14, p15, p25, p24 }, null));
                effWindAreas.Add(11, new EffectiveWindArea("Zone1", new List<Vec2> { p15, p17, p27, p25 }, null));

                // Top row of rectangles
                effWindAreas.Add(14, new EffectiveWindArea("Zone2", new List<Vec2> { p21, p23, p33, p31 }, null));
                effWindAreas.Add(15, new EffectiveWindArea("Zone3", new List<Vec2> { p23, p24, p34, p33 }, null));
                effWindAreas.Add(16, new EffectiveWindArea("Zone3", new List<Vec2> { p24, p25, p35, p34 }, null));
                effWindAreas.Add(17, new EffectiveWindArea("Zone2", new List<Vec2> { p25, p27, p37, p35 }, null));
                return;
            }
        }
    }
}
