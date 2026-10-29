using System;
using System.Collections.Generic;

namespace ASCE7WindLoadCalculator
{
    public class FlatRoofAreaCalculator_CC_ASCE7_16 : AreaCalculator_CC_ASCE7_16_Base
    {
        public FlatRoofAreaCalculator_CC_ASCE7_16(BuildingData bldg_data)
        {
            buildingData = bldg_data;
        }

        public static bool IsValidRectangle(double L, double B, double offset)
        {
            // Valid if offset is positive and less than half the smallest dimension
            return offset > 0 && 2 * offset < L && 2 * offset < B;
        }

        public override void ComputeEffectiveWindAreas()
        {
            // Existing logic from ComputeFlatRoofAreas
            var L = buildingData.BuildingLength;
            var B = buildingData.BuildingWidth;

            double h = buildingData.MeanRoofHeight;
            double offset1 = 1.2 * h;
            double offset2 = 0.6 * h;

            var zones = new Dictionary<int, EffectiveWindArea>();

            // Zone 1' - Center (innermost)
            EffectiveWindArea z3 = null;
            if (IsValidRectangle(L, B, offset1))
            {
                z3 = TryCreateZone(
                    1,
                    "Zone1'",
                    new[]
                    {
                        new Vec2(offset1, offset1),
                        new Vec2(L - offset1, offset1),
                        new Vec2(L - offset1, B - offset1),
                        new Vec2(offset1, B - offset1)
                    });

                if (z3 != null)
                    zones[1] = z3;
            }
            else
            {
                Console.WriteLine("Zone 1' skipped: invalid offset or too large for roof size.");
            }

            // Zone 1 - Middle band
            EffectiveWindArea z2 = null;
            if (IsValidRectangle(L, B, offset2))
            {
                var outer2 = new[]
                {
                    new Vec2(offset2, offset2),
                    new Vec2(L - offset2, offset2),
                    new Vec2(L - offset2, B - offset2),
                    new Vec2(offset2, B - offset2)
                };

                var hole2 = z3 != null ? new[] { z3.OuterBoundary } : null;

                z2 = TryCreateZone(2, "Zone1", outer2, hole2);
                if (z2 != null)
                    zones[2] = z2;
            }
            else
            {
                Console.WriteLine("Zone 1 skipped: invalid offset or too large for roof size.");
            }

            // region 3 corner zones
            // lower left
            Vec2 p13 = new Vec2(0, 0);
            Vec2 p14 = new Vec2(0.6 * buildingData.MeanRoofHeight, 0);
            Vec2 p15 = new Vec2(0.6 * buildingData.MeanRoofHeight, 0.2 * buildingData.MeanRoofHeight);
            Vec2 p16 = new Vec2(0.2 * buildingData.MeanRoofHeight, 0.2 * buildingData.MeanRoofHeight);
            Vec2 p17 = new Vec2(0.2 * buildingData.MeanRoofHeight, 0.6 * buildingData.MeanRoofHeight);
            Vec2 p18 = new Vec2(0, 0.6 * buildingData.MeanRoofHeight);

            var roof_area_3_1 = new EffectiveWindArea(
                "Zone3",
                new List<Vec2> { p13, p14, p15, p16, p17, p18 },
                null
                );

            // lower right
            Vec2 p19 = new Vec2(buildingData.BuildingLength, 0);
            Vec2 p20 = new Vec2(buildingData.BuildingLength, 0.6 * buildingData.MeanRoofHeight);
            Vec2 p21 = new Vec2(buildingData.BuildingLength - 0.2 * buildingData.MeanRoofHeight, 0.6 * buildingData.MeanRoofHeight);
            Vec2 p22 = new Vec2(buildingData.BuildingLength - 0.2 * buildingData.MeanRoofHeight, 0.2 * buildingData.MeanRoofHeight);
            Vec2 p23 = new Vec2(buildingData.BuildingLength - 0.6 * buildingData.MeanRoofHeight, 0.2 * buildingData.MeanRoofHeight);
            Vec2 p24 = new Vec2(buildingData.BuildingLength - 0.6 * buildingData.MeanRoofHeight, 0);


            var roof_area_3_2 = new EffectiveWindArea(
                "Zone3",
                new List<Vec2> { p19, p20, p21, p22, p23, p24 },
                null
                );

            // upper right
            Vec2 p25 = new Vec2(buildingData.BuildingLength - 0.6 * buildingData.MeanRoofHeight, buildingData.BuildingWidth);
            Vec2 p26 = new Vec2(buildingData.BuildingLength - 0.6 * buildingData.MeanRoofHeight, buildingData.BuildingWidth - 0.2 * buildingData.MeanRoofHeight);
            Vec2 p27 = new Vec2(buildingData.BuildingLength - 0.2 * buildingData.MeanRoofHeight, buildingData.BuildingWidth - 0.2 * buildingData.MeanRoofHeight);
            Vec2 p28 = new Vec2(buildingData.BuildingLength - 0.2 * buildingData.MeanRoofHeight, buildingData.BuildingWidth - 0.6 * buildingData.MeanRoofHeight);
            Vec2 p29 = new Vec2(buildingData.BuildingLength, buildingData.BuildingWidth - 0.6 * buildingData.MeanRoofHeight);
            Vec2 p30 = new Vec2(buildingData.BuildingLength, buildingData.BuildingWidth);

            var roof_area_3_3 = new EffectiveWindArea(
                "Zone3",
                new List<Vec2> { p25, p26, p27, p28, p29, p30 },
                null
                );

            // upper left
            Vec2 p31 = new Vec2(0, buildingData.BuildingWidth);
            Vec2 p32 = new Vec2(0, buildingData.BuildingWidth - 0.6 * buildingData.MeanRoofHeight);
            Vec2 p33 = new Vec2(0.2 * buildingData.MeanRoofHeight, buildingData.BuildingWidth - 0.6 * buildingData.MeanRoofHeight);
            Vec2 p34 = new Vec2(0.2 * buildingData.MeanRoofHeight, buildingData.BuildingWidth - 0.2 * buildingData.MeanRoofHeight);
            Vec2 p35 = new Vec2(0.6 * buildingData.MeanRoofHeight, buildingData.BuildingWidth - 0.2 * buildingData.MeanRoofHeight);
            Vec2 p36 = new Vec2(0.6 * buildingData.MeanRoofHeight, buildingData.BuildingWidth);

            var roof_area_3_4 = new EffectiveWindArea(
                "Zone3",
                new List<Vec2> { p31, p32, p33, p34, p35, p36 },
                null
                );




            // Zone 2 - Outer
            var outer1 = new[]
            {
                new Vec2(0, 0),
                new Vec2(L, 0),
                new Vec2(L, B),
                new Vec2(0, B)
            };

            var hole1 = (z2 != null ? new[] { z2.OuterBoundary, roof_area_3_1.OuterBoundary, roof_area_3_2.OuterBoundary , roof_area_3_3.OuterBoundary , roof_area_3_4.OuterBoundary} : null);

            var z1 = TryCreateZone(3, "Zone2", outer1, hole1);
            if (z1 != null)
                zones[3] = z1;

            // Save or use your zones dictionary here...
            effWindAreas = zones;

            effWindAreas.Add(4, roof_area_3_1);  //z3
            effWindAreas.Add(5, roof_area_3_2);  //z3
            effWindAreas.Add(6, roof_area_3_3);  //z3
            effWindAreas.Add(7, roof_area_3_4);  //z3

            return;

        }
    }

}
