using System;
using System.Collections.Generic;
using System.Windows;

namespace ASCE7WindLoadCalculator
{
    public class FlatRoofAreaCalculator_CC_ASCE7_22 : AreaCalculator_CC_ASCE7_22_Base
    {
        public FlatRoofAreaCalculator_CC_ASCE7_22(BuildingData bldg_data)
        {
            buildingData = bldg_data;
        }

        private static bool IsValidRectangle(double L, double B, double offset)
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
                        new Point(offset1, offset1),
                        new Point(L - offset1, offset1),
                        new Point(L - offset1, B - offset1),
                        new Point(offset1, B - offset1)
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
                    new Point(offset2, offset2),
                    new Point(L - offset2, offset2),
                    new Point(L - offset2, B - offset2),
                    new Point(offset2, B - offset2)
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
            Point p13 = new Point(0, 0);
            Point p14 = new Point(0.6 * buildingData.MeanRoofHeight, 0);
            Point p15 = new Point(0.6 * buildingData.MeanRoofHeight, 0.2 * buildingData.MeanRoofHeight);
            Point p16 = new Point(0.2 * buildingData.MeanRoofHeight, 0.2 * buildingData.MeanRoofHeight);
            Point p17 = new Point(0.2 * buildingData.MeanRoofHeight, 0.6 * buildingData.MeanRoofHeight);
            Point p18 = new Point(0, 0.6 * buildingData.MeanRoofHeight);

            var roof_area_3_1 = new EffectiveWindArea(
                "Zone3",
                new List<Point> { p13, p14, p15, p16, p17, p18 },
                null
                );

            // lower right
            Point p19 = new Point(buildingData.BuildingLength, 0);
            Point p20 = new Point(buildingData.BuildingLength, 0.6 * buildingData.MeanRoofHeight);
            Point p21 = new Point(buildingData.BuildingLength - 0.2 * buildingData.MeanRoofHeight, 0.6 * buildingData.MeanRoofHeight);
            Point p22 = new Point(buildingData.BuildingLength - 0.2 * buildingData.MeanRoofHeight, 0.2 * buildingData.MeanRoofHeight);
            Point p23 = new Point(buildingData.BuildingLength - 0.6 * buildingData.MeanRoofHeight, 0.2 * buildingData.MeanRoofHeight);
            Point p24 = new Point(buildingData.BuildingLength - 0.6 * buildingData.MeanRoofHeight, 0);


            var roof_area_3_2 = new EffectiveWindArea(
                "Zone3",
                new List<Point> { p19, p20, p21, p22, p23, p24 },
                null
                );

            // upper right
            Point p25 = new Point(buildingData.BuildingLength - 0.6 * buildingData.MeanRoofHeight, buildingData.BuildingWidth);
            Point p26 = new Point(buildingData.BuildingLength - 0.6 * buildingData.MeanRoofHeight, buildingData.BuildingWidth - 0.2 * buildingData.MeanRoofHeight);
            Point p27 = new Point(buildingData.BuildingLength - 0.2 * buildingData.MeanRoofHeight, buildingData.BuildingWidth - 0.2 * buildingData.MeanRoofHeight);
            Point p28 = new Point(buildingData.BuildingLength - 0.2 * buildingData.MeanRoofHeight, buildingData.BuildingWidth - 0.6 * buildingData.MeanRoofHeight);
            Point p29 = new Point(buildingData.BuildingLength, buildingData.BuildingWidth - 0.6 * buildingData.MeanRoofHeight);
            Point p30 = new Point(buildingData.BuildingLength, buildingData.BuildingWidth);

            var roof_area_3_3 = new EffectiveWindArea(
                "Zone3",
                new List<Point> { p25, p26, p27, p28, p29, p30 },
                null
                );

            // upper left
            Point p31 = new Point(0, buildingData.BuildingWidth);
            Point p32 = new Point(0, buildingData.BuildingWidth - 0.6 * buildingData.MeanRoofHeight);
            Point p33 = new Point(0.2 * buildingData.MeanRoofHeight, buildingData.BuildingWidth - 0.6 * buildingData.MeanRoofHeight);
            Point p34 = new Point(0.2 * buildingData.MeanRoofHeight, buildingData.BuildingWidth - 0.2 * buildingData.MeanRoofHeight);
            Point p35 = new Point(0.6 * buildingData.MeanRoofHeight, buildingData.BuildingWidth - 0.2 * buildingData.MeanRoofHeight);
            Point p36 = new Point(0.6 * buildingData.MeanRoofHeight, buildingData.BuildingWidth);

            var roof_area_3_4 = new EffectiveWindArea(
                "Zone3",
                new List<Point> { p31, p32, p33, p34, p35, p36 },
                null
                );

            // Zone 2 - Outer
            var outer1 = new[]
            {
                new Point(0, 0),
                new Point(L, 0),
                new Point(L, B),
                new Point(0, B)
            };

            var hole1 = (z2 != null ? new[] { z2.OuterBoundary, roof_area_3_1.OuterBoundary, roof_area_3_2.OuterBoundary, roof_area_3_3.OuterBoundary, roof_area_3_4.OuterBoundary } : null);

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
