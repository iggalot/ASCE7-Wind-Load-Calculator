using System;
using System.Collections.Generic;

namespace ASCE7WindLoadCalculator
{
    public class HipRoofAreaCalculator_ParallelToRidge_MWFRS_ASCE7_22 : AreaCalculator_ASCE7_22_Base
    {
        public HipRoofAreaCalculator_ParallelToRidge_MWFRS_ASCE7_22(BuildingData bldg_data)
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

            Vec2 A = new Vec2(0, 0);
            Vec2 B = new Vec2(building_length, 0);
            Vec2 C = new Vec2(building_length, building_width);
            Vec2 D = new Vec2(0, building_width);

            int number_of_zones = WindLoadCalculator_Base.GetNumberRoofZones_MWFRS(buildingData.MeanRoofHeight, buildingData.BuildingLength);

            // clear previous wind areas
            effWindAreas.Clear();

            switch (number_of_zones)
            {
                case 4: ComputeAreas_FourZones(); break;
                case 3: ComputeAreas_ThreeZones(); break;
                case 2: ComputeAreas_TwoZones(); break;
                case 1: ComputeAreas_OneZone(); break;
            }
        }

        private void ComputeAreas_FourZones()
        {
            double building_length = buildingData.BuildingLength;
            double building_width = buildingData.BuildingWidth;
            double h = buildingData.MeanRoofHeight;
            double ridge_offset = Math.Min(building_length, building_width) / 2.0;
            Vec2 left_ridge_pt = new Vec2(ridge_offset, 0.5 * building_width);
            Vec2 right_ridge_pt = new Vec2(building_length - ridge_offset, 0.5 * building_width);

            int left_ridge_zone = WindLoadCalculator_Base.GetRoofZoneNumber(left_ridge_pt, buildingData.MeanRoofHeight, buildingData.BuildingLength);
            int right_ridge_zone = WindLoadCalculator_Base.GetRoofZoneNumber(right_ridge_pt, buildingData.MeanRoofHeight, buildingData.BuildingLength);

            double offset1 = 0.5 * buildingData.MeanRoofHeight;
            double offset2 = 1.0 * buildingData.MeanRoofHeight;
            double offset3 = 2.0 * buildingData.MeanRoofHeight;

            Vec2 A = new Vec2(0, 0);
            Vec2 B = new Vec2(building_length, 0);
            Vec2 C = new Vec2(building_length, building_width);
            Vec2 D = new Vec2(0, building_width);

            if (left_ridge_pt.X > right_ridge_pt.X)
            {
                // TODO should we crash here, or switch to a vertical direction?
                throw new NotImplementedException("ERROR:  In ComputeAreas_FourZones(): Left ridge point cannot be to the right of right ridge point");
            }

            // Zone 5
            effWindAreas.Add(1, new EffectiveWindArea("ZoneWWR", new List<Vec2> { A, left_ridge_pt, D }, null));
            effWindAreas.Add(2, new EffectiveWindArea("ZoneLWR", new List<Vec2> { B, C, right_ridge_pt }, null));

            if ((left_ridge_zone == 4) && (right_ridge_zone == 1))
            {
                Vec2 p1 = new Vec2(offset1, 0);
                Vec2 p11 = new Vec2(offset1, 0.5 * building_width);
                Vec2 p31 = new Vec2(offset1, building_width);
                Vec2 p2 = new Vec2(offset2, 0);
                Vec2 p12 = new Vec2(offset2, 0.5 * building_width);
                Vec2 p32 = new Vec2(offset2, building_width);
                Vec2 p3 = new Vec2(offset3, 0);
                Vec2 p13 = new Vec2(offset3, 0.5 * building_width);
                Vec2 p33 = new Vec2(offset3, building_width);

                // Zone 4
                var area1 = new EffectiveWindArea("Zone4", new List<Vec2> { A, p1, p11, left_ridge_pt }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area1.OuterBoundary);
                effWindAreas.Add(10, area1);

                var area2 = new EffectiveWindArea("Zone4", new List<Vec2> { D, left_ridge_pt, p11, p31 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area2.OuterBoundary);
                effWindAreas.Add(11, area2);

                // Zone 3
                var area3 = new EffectiveWindArea("Zone3", new List<Vec2> { p1, p2, p12, p11 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area3.OuterBoundary);
                effWindAreas.Add(20, area3);

                var area4 = new EffectiveWindArea("Zone3", new List<Vec2> { p11, p12, p32, p31 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area4.OuterBoundary);
                effWindAreas.Add(21, area4);

                // Zone 2
                var area5 = new EffectiveWindArea("Zone2", new List<Vec2> { p2, p3, p13, p12 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area5.OuterBoundary);
                effWindAreas.Add(30, area5);

                var area6 = new EffectiveWindArea("Zone2", new List<Vec2> { p12, p13, p33, p32 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area6.OuterBoundary);
                effWindAreas.Add(31, area6);

                // Zone 1
                var area7 = new EffectiveWindArea("Zone1", new List<Vec2> { p3, B, right_ridge_pt, p13 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area7.OuterBoundary);
                effWindAreas.Add(40, area7);

                var area8 = new EffectiveWindArea("Zone1", new List<Vec2> { p13, right_ridge_pt, C, p33 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area8.OuterBoundary);
                effWindAreas.Add(41, area8);
            }
            else if (left_ridge_zone == 4 && right_ridge_zone == 2)
            {
                Vec2 p1 = new Vec2(offset1, 0);
                Vec2 p11 = new Vec2(offset1, 0.5 * building_width);
                Vec2 p31 = new Vec2(offset1, building_width);
                Vec2 p2 = new Vec2(offset2, 0);
                Vec2 p12 = new Vec2(offset2, 0.5 * building_width);
                Vec2 p32 = new Vec2(offset2, building_width);
                Vec2 p3 = new Vec2(offset3, 0);
                Vec2 p13 = new Vec2(offset3, building_length - offset3);
                Vec2 p23 = new Vec2(offset3, building_width - (building_length - offset3));
                Vec2 p33 = new Vec2(offset3, building_width);

                // Zone 4
                var area1 = new EffectiveWindArea("Zone4", new List<Vec2> { A, p1, p11, left_ridge_pt }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area1.OuterBoundary);
                effWindAreas.Add(10, area1);

                var area2 = new EffectiveWindArea("Zone4", new List<Vec2> { D, left_ridge_pt, p11, p31 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area2.OuterBoundary);
                effWindAreas.Add(11, area2);

                // Zone 3
                var area3 = new EffectiveWindArea("Zone3", new List<Vec2> { p1, p2, p12, p11 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area3.OuterBoundary);
                effWindAreas.Add(20, area3);

                var area4 = new EffectiveWindArea("Zone3", new List<Vec2> { p11, p12, p32, p31 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area4.OuterBoundary);
                effWindAreas.Add(21, area4);

                // Zone 2
                var area5 = new EffectiveWindArea("Zone2", new List<Vec2> { p2, p3, p13, right_ridge_pt, p12 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area5.OuterBoundary);
                effWindAreas.Add(30, area5);

                var area6 = new EffectiveWindArea("Zone2", new List<Vec2> { p12, right_ridge_pt, p23, p33, p32 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area6.OuterBoundary);
                effWindAreas.Add(31, area6);

                // Zone 1
                var area7 = new EffectiveWindArea("Zone1", new List<Vec2> { p3, B, p13 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area7.OuterBoundary);
                effWindAreas.Add(40, area7);

                var area8 = new EffectiveWindArea("Zone1", new List<Vec2> { p23, C, p33 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area8.OuterBoundary);
                effWindAreas.Add(41, area8);
            }
            else if (left_ridge_zone == 3 && right_ridge_zone == 1)
            {
                Vec2 p1 = new Vec2(offset1, 0);
                Vec2 p11 = new Vec2(offset1, offset1);
                Vec2 p21 = new Vec2(offset1, building_width - offset1);
                Vec2 p31 = new Vec2(offset1, building_width);

                Vec2 p2 = new Vec2(offset2, 0);
                Vec2 p12 = new Vec2(offset2, 0.5 * building_width);
                Vec2 p32 = new Vec2(offset2, building_width);

                Vec2 p3 = new Vec2(offset3, 0);
                Vec2 p13 = new Vec2(offset3, 0.5 * building_width);
                Vec2 p33 = new Vec2(offset3, building_width);

                //Zone 4
                var area1 = new EffectiveWindArea("Zone4", new List<Vec2> { A, p1, p11 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area1.OuterBoundary);
                effWindAreas.Add(10, area1);

                var area2 = new EffectiveWindArea("Zone4", new List<Vec2> { D, p21, p31 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area2.OuterBoundary);
                effWindAreas.Add(11, area2);

                // Zone 3
                var area3 = new EffectiveWindArea("Zone3", new List<Vec2> { p1, p2, p12, left_ridge_pt, p11 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area3.OuterBoundary);
                effWindAreas.Add(20, area3);

                var area4 = new EffectiveWindArea("Zone3", new List<Vec2> { p21, left_ridge_pt, p12, p32, p31 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area4.OuterBoundary);
                effWindAreas.Add(21, area4);

                // Zone 2
                var area5 = new EffectiveWindArea("Zone2", new List<Vec2> { p2, p3, p13, p12 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area5.OuterBoundary);
                effWindAreas.Add(30, area5);

                var area6 = new EffectiveWindArea("Zone2", new List<Vec2> { p12, p13, p33, p32 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area6.OuterBoundary);
                effWindAreas.Add(31, area6);

                // Zone 1
                var area7 = new EffectiveWindArea("Zone1", new List<Vec2> { p3, B, right_ridge_pt, p13 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area7.OuterBoundary);
                effWindAreas.Add(40, area7);

                var area8 = new EffectiveWindArea("Zone1", new List<Vec2> { p13, right_ridge_pt, C, p33 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area8.OuterBoundary);
                effWindAreas.Add(41, area8);
            }
            else if (left_ridge_zone == 3 && right_ridge_zone == 2)
            {
                Vec2 p1 = new Vec2(offset1, 0);
                Vec2 p11 = new Vec2(offset1, offset1);
                Vec2 p21 = new Vec2(offset1, building_width - offset1);
                Vec2 p31 = new Vec2(offset1, building_width);

                Vec2 p2 = new Vec2(offset2, 0);
                Vec2 p12 = new Vec2(offset2, 0.5 * building_width);
                Vec2 p32 = new Vec2(offset2, building_width);

                Vec2 p3 = new Vec2(offset3, 0);
                Vec2 p13 = new Vec2(offset3, building_length - offset3);
                Vec2 p23 = new Vec2(offset3, building_width - (building_length - offset3));
                Vec2 p33 = new Vec2(offset3, building_width);

                // Zone 4
                var area1 = new EffectiveWindArea("Zone4", new List<Vec2> { A, p1, p11 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area1.OuterBoundary);
                effWindAreas.Add(10, area1);

                var area2 = new EffectiveWindArea("Zone4", new List<Vec2> { D, p21, p31 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area2.OuterBoundary);
                effWindAreas.Add(11, area2);

                // Zone 3
                var area3 = new EffectiveWindArea("Zone3", new List<Vec2> { p1, p2, p12, left_ridge_pt, p11 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area3.OuterBoundary);
                effWindAreas.Add(20, area3);

                var area4 = new EffectiveWindArea("Zone3", new List<Vec2> { p21, left_ridge_pt, p12, p32, p31 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area4.OuterBoundary);
                effWindAreas.Add(21, area4);

                // Zone 2
                var area5 = new EffectiveWindArea("Zone2", new List<Vec2> { p2, p3, p13, right_ridge_pt, p12 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area5.OuterBoundary);
                effWindAreas.Add(30, area5);

                var area6 = new EffectiveWindArea("Zone2", new List<Vec2> { p12, right_ridge_pt, p23, p33, p32 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area6.OuterBoundary);
                effWindAreas.Add(31, area6);

                // Zone 1
                var area7 = new EffectiveWindArea("Zone1", new List<Vec2> { p3, B, p13 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area7.OuterBoundary);
                effWindAreas.Add(40, area7);

                var area8 = new EffectiveWindArea("Zone1", new List<Vec2> { p23,  C, p33 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area8.OuterBoundary);
                effWindAreas.Add(41, area8);
            }
            else if (left_ridge_zone == 2 && right_ridge_zone == 2)
            {
                Vec2 p1 = new Vec2(offset1, 0);
                Vec2 p11 = new Vec2(offset1, offset1);
                Vec2 p21 = new Vec2(offset1, building_width - offset1);
                Vec2 p31 = new Vec2(offset1, building_width);

                Vec2 p2 = new Vec2(offset2, 0);
                Vec2 p12 = new Vec2(offset2, offset2);
                Vec2 p22 = new Vec2(offset2, building_width - offset2);
                Vec2 p32 = new Vec2(offset2, building_width);

                Vec2 p3 = new Vec2(offset3, 0);
                Vec2 p13 = new Vec2(offset3, building_length - offset3);
                Vec2 p23 = new Vec2(offset3, building_width - (building_length - offset3));
                Vec2 p33 = new Vec2(offset3, building_width);

                // Zone 4
                var area1 = new EffectiveWindArea("Zone4", new List<Vec2> { A, p1, p11 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area1.OuterBoundary);
                effWindAreas.Add(10, area1);

                var area2 = new EffectiveWindArea("Zone4", new List<Vec2> { D, p21, p31 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area2.OuterBoundary);
                effWindAreas.Add(11, area2);

                // Zone 3
                var area3 = new EffectiveWindArea("Zone3", new List<Vec2> { p1, p2, p12, p11 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area3.OuterBoundary);
                effWindAreas.Add(20, area3);

                var area4 = new EffectiveWindArea("Zone3", new List<Vec2> { p21, p22, p32, p31 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area4.OuterBoundary);
                effWindAreas.Add(21, area4);

                // Zone 2
                var area5 = new EffectiveWindArea("Zone2", new List<Vec2> { p2, p3, p13, right_ridge_pt, left_ridge_pt, p12 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area5.OuterBoundary);
                effWindAreas.Add(30, area5);

                var area6 = new EffectiveWindArea("Zone2", new List<Vec2> { p22, left_ridge_pt, right_ridge_pt, p23, p33, p32 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area6.OuterBoundary);
                effWindAreas.Add(31, area6);

                // Zone 1
                var area7 = new EffectiveWindArea("Zone1", new List<Vec2> { p3, B, p13 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area7.OuterBoundary);
                effWindAreas.Add(40, area7);

                var area8 = new EffectiveWindArea("Zone1", new List<Vec2> { p23, C, p33 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area8.OuterBoundary);
                effWindAreas.Add(41, area8);
            }
            else if (left_ridge_zone == 2 && right_ridge_zone == 1)
            {
                Vec2 p1 = new Vec2(offset1, 0);
                Vec2 p11 = new Vec2(offset1, offset1);
                Vec2 p21 = new Vec2(offset1, building_width - offset1);
                Vec2 p31 = new Vec2(offset1, building_width);

                Vec2 p2 = new Vec2(offset2, 0);
                Vec2 p12 = new Vec2(offset2, offset2);
                Vec2 p22 = new Vec2(offset2, building_width - offset2);
                Vec2 p32 = new Vec2(offset2, building_width);

                Vec2 p3 = new Vec2(offset3, 0);
                Vec2 p13 = new Vec2(offset3, 0.5 * building_width);
                Vec2 p33 = new Vec2(offset3, building_width);

                // Zone 4
                var area1 = new EffectiveWindArea("Zone4", new List<Vec2> { A, p1, p11 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area1.OuterBoundary);
                effWindAreas.Add(10, area1);

                var area2 = new EffectiveWindArea("Zone4", new List<Vec2> { D, p21, p31 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area2.OuterBoundary);
                effWindAreas.Add(11, area2);

                // Zone 3
                var area3 = new EffectiveWindArea("Zone3", new List<Vec2> { p1, p2, p12, p11 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area3.OuterBoundary);
                effWindAreas.Add(20, area3);

                var area4 = new EffectiveWindArea("Zone3", new List<Vec2> { p21, p22, p32, p31 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area4.OuterBoundary);
                effWindAreas.Add(21, area4);

                // Zone 2
                var area5 = new EffectiveWindArea("Zone2", new List<Vec2> { p2, p3, p13, left_ridge_pt, p12 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area5.OuterBoundary);
                effWindAreas.Add(30, area5);

                var area6 = new EffectiveWindArea("Zone2", new List<Vec2> { p22, left_ridge_pt, p13, p33, p32 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area6.OuterBoundary);
                effWindAreas.Add(31, area6);

                // Zone 1
                var area7 = new EffectiveWindArea("Zone1", new List<Vec2> { p3, B, right_ridge_pt, p13 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area7.OuterBoundary);
                effWindAreas.Add(40, area7);

                var area8 = new EffectiveWindArea("Zone1", new List<Vec2> { p13, right_ridge_pt, C, p33 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area8.OuterBoundary);
                effWindAreas.Add(41, area8);
            }
            else if (left_ridge_zone == 1 && right_ridge_zone == 1)
            {
                Vec2 p1 = new Vec2(offset1, 0);
                Vec2 p11 = new Vec2(offset1, offset1);
                Vec2 p21 = new Vec2(offset1, building_width - offset1);
                Vec2 p31 = new Vec2(offset1, building_width);

                Vec2 p2 = new Vec2(offset2, 0);
                Vec2 p12 = new Vec2(offset2, offset2);
                Vec2 p22 = new Vec2(offset2, building_width - offset2);
                Vec2 p32 = new Vec2(offset2, building_width);

                Vec2 p3 = new Vec2(offset3, 0);
                Vec2 p13 = new Vec2(offset3, offset3);
                Vec2 p23 = new Vec2(offset3, building_width - offset3);
                Vec2 p33 = new Vec2(offset3, building_width);

                // Zone 4
                var area1 = new EffectiveWindArea("Zone4", new List<Vec2> { A, p1, p11 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area1.OuterBoundary);
                effWindAreas.Add(10, area1);

                var area2 = new EffectiveWindArea("Zone4", new List<Vec2> { D, p21, p31 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area2.OuterBoundary);
                effWindAreas.Add(11, area2);

                // Zone 3
                var area3 = new EffectiveWindArea("Zone3", new List<Vec2> { p1, p2, p12, p11 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area3.OuterBoundary);
                effWindAreas.Add(20, area3);

                var area4 = new EffectiveWindArea("Zone3", new List<Vec2> { p21, p22, p32, p31 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area4.OuterBoundary);
                effWindAreas.Add(21, area4);

                // Zone 2
                var area5 = new EffectiveWindArea("Zone2", new List<Vec2> { p2, p3, p13, p12 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area5.OuterBoundary);
                effWindAreas.Add(30, area5);

                var area6 = new EffectiveWindArea("Zone2", new List<Vec2> { p22, p23, p33, p32 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area6.OuterBoundary);
                effWindAreas.Add(31, area6);

                // Zone 1
                var area7 = new EffectiveWindArea("Zone1", new List<Vec2> { p3, B, right_ridge_pt, left_ridge_pt, p13 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area7.OuterBoundary);
                effWindAreas.Add(40, area7);

                var area8 = new EffectiveWindArea("Zone1", new List<Vec2> { p23, left_ridge_pt, right_ridge_pt, C, p33 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area8.OuterBoundary);
                effWindAreas.Add(41, area8);
            }
            else
            {
                throw new NotImplementedException("ERROR:  In ComputeAreas_FourZones() -- Unknown zone configuration" + left_ridge_zone + ", " + right_ridge_zone);
            }
        }

        private void ComputeAreas_ThreeZones()
        {
            double building_length = buildingData.BuildingLength;
            double building_width = buildingData.BuildingWidth;
            double h = buildingData.MeanRoofHeight;
            double ridge_offset = Math.Min(building_length, building_width) / 2.0;
            Vec2 left_ridge_pt = new Vec2(ridge_offset, 0.5 * building_width);
            Vec2 right_ridge_pt = new Vec2(building_length - ridge_offset, 0.5 * building_width);

            int left_ridge_zone = WindLoadCalculator_Base.GetRoofZoneNumber(left_ridge_pt, buildingData.MeanRoofHeight, buildingData.BuildingLength);
            int right_ridge_zone = WindLoadCalculator_Base.GetRoofZoneNumber(right_ridge_pt, buildingData.MeanRoofHeight, buildingData.BuildingLength);

            double offset1 = 0.5 * buildingData.MeanRoofHeight;
            double offset2 = 1.0 * buildingData.MeanRoofHeight;
            double offset3 = 2.0 * buildingData.MeanRoofHeight;

            Vec2 A = new Vec2(0, 0);
            Vec2 B = new Vec2(building_length, 0);
            Vec2 C = new Vec2(building_length, building_width);
            Vec2 D = new Vec2(0, building_width);

            if (left_ridge_pt.X > right_ridge_pt.X)
            {
                // TODO should we crash here, or switch to a vertical direction?
                throw new NotImplementedException("ERROR:  In ComputeAreas_ThreeZones(): Left ridge point cannot be to the right of right ridge point");
            }

            // Zone 5
            effWindAreas.Add(1, new EffectiveWindArea("ZoneWWR", new List<Vec2> { A, left_ridge_pt, D }, null));
            effWindAreas.Add(2, new EffectiveWindArea("ZoneLWR", new List<Vec2> { B, C, right_ridge_pt }, null));

            if ((left_ridge_zone == 4) && (right_ridge_zone == 3))
            {
                Vec2 p1 = new Vec2(offset1, 0);
                Vec2 p11 = new Vec2(offset1, 0.5 * building_width);
                Vec2 p31 = new Vec2(offset1, building_width);
                Vec2 p2 = new Vec2(offset2, 0);
                Vec2 p12 = new Vec2(offset2, (building_length - offset2));
                Vec2 p22 = new Vec2(offset2, building_width - (building_length - offset2));
                Vec2 p32 = new Vec2(offset2, building_width);

                // Zone 4
                var area1 = new EffectiveWindArea("Zone4", new List<Vec2> { A, p1, p11, left_ridge_pt }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area1.OuterBoundary);
                effWindAreas.Add(10, area1);

                var area2 = new EffectiveWindArea("Zone4", new List<Vec2> { D, left_ridge_pt, p11, p31 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area2.OuterBoundary);
                effWindAreas.Add(11, area2);

                // Zone 3
                var area3 = new EffectiveWindArea("Zone3", new List<Vec2> { p1, p2, p12, right_ridge_pt, p11 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area3.OuterBoundary);
                effWindAreas.Add(20, area3);

                var area4 = new EffectiveWindArea("Zone3", new List<Vec2> { p11, right_ridge_pt, p22, p32, p31 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area4.OuterBoundary);
                effWindAreas.Add(21, area4);

                // Zone 2
                var area5 = new EffectiveWindArea("Zone2", new List<Vec2> { p2, B , p12 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area5.OuterBoundary);
                effWindAreas.Add(30, area5);

                var area6 = new EffectiveWindArea("Zone2", new List<Vec2> { p22, C, p32 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area6.OuterBoundary);
                effWindAreas.Add(31, area6);
            }
            else if (left_ridge_zone == 4 && right_ridge_zone == 2)
            {
                Vec2 p1 = new Vec2(offset1, 0);
                Vec2 p11 = new Vec2(offset1, 0.5 * building_width);
                Vec2 p31 = new Vec2(offset1, building_width);
                Vec2 p2 = new Vec2(offset2, 0);
                Vec2 p12 = new Vec2(offset2, 0.5 * building_width);
                Vec2 p32 = new Vec2(offset2, building_width);

                // Zone 4
                var area1 = new EffectiveWindArea("Zone4", new List<Vec2> { A, p1, p11, left_ridge_pt }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area1.OuterBoundary);
                effWindAreas.Add(10, area1);

                var area2 = new EffectiveWindArea("Zone4", new List<Vec2> { D, left_ridge_pt, p11, p31 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area2.OuterBoundary);
                effWindAreas.Add(11, area2);

                // Zone 3
                var area3 = new EffectiveWindArea("Zone3", new List<Vec2> { p1, p2, p12, p11 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area3.OuterBoundary);
                effWindAreas.Add(20, area3);

                var area4 = new EffectiveWindArea("Zone3", new List<Vec2> { p11, p12, p32, p31 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area4.OuterBoundary);
                effWindAreas.Add(21, area4);

                // Zone 2
                var area5 = new EffectiveWindArea("Zone2", new List<Vec2> { p2, B, right_ridge_pt, p12 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area5.OuterBoundary);
                effWindAreas.Add(30, area5);

                var area6 = new EffectiveWindArea("Zone2", new List<Vec2> { p12, right_ridge_pt, C, p32 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area6.OuterBoundary);
                effWindAreas.Add(31, area6);

            }
            else if (left_ridge_zone == 3 && right_ridge_zone == 3)
            {
                Vec2 p1 = new Vec2(offset1, 0);
                Vec2 p11 = new Vec2(offset1, offset1);
                Vec2 p21 = new Vec2(offset1, building_width - offset1);
                Vec2 p31 = new Vec2(offset1, building_width);

                Vec2 p2 = new Vec2(offset2, 0);
                Vec2 p12 = new Vec2(offset2, building_length - offset2);
                Vec2 p22 = new Vec2(offset2, building_width-(building_length - offset2));
                Vec2 p32 = new Vec2(offset2, building_width);

                // Zone 4
                var area1 = new EffectiveWindArea("Zone4", new List<Vec2> { A, p1, p11, left_ridge_pt }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area1.OuterBoundary);
                effWindAreas.Add(10, area1);

                var area2 = new EffectiveWindArea("Zone4", new List<Vec2> { D, left_ridge_pt, p11, p31 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area2.OuterBoundary);
                effWindAreas.Add(11, area2);

                // Zone 3
                var area3 = new EffectiveWindArea("Zone3", new List<Vec2> { p1, p2, p12, p11 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area3.OuterBoundary);
                effWindAreas.Add(20, area3);

                var area4 = new EffectiveWindArea("Zone3", new List<Vec2> { p11, p12, p32, p31 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area4.OuterBoundary);
                effWindAreas.Add(21, area4);

                // Zone 2
                var area5 = new EffectiveWindArea("Zone2", new List<Vec2> { p2, B, right_ridge_pt, p12 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area5.OuterBoundary);
                effWindAreas.Add(30, area5);

                var area6 = new EffectiveWindArea("Zone2", new List<Vec2> { p12, right_ridge_pt, C, p32 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area6.OuterBoundary);
                effWindAreas.Add(31, area6);
            }
            else if (left_ridge_zone == 3 && right_ridge_zone == 2)
            {
                Vec2 p1 = new Vec2(offset1, 0);
                Vec2 p11 = new Vec2(offset1, offset1);
                Vec2 p21 = new Vec2(offset1, building_width - offset1);
                Vec2 p31 = new Vec2(offset1, building_width);

                Vec2 p2 = new Vec2(offset2, 0);
                Vec2 p12 = new Vec2(offset2, 0.5* building_width);
                Vec2 p32 = new Vec2(offset2, building_width);

                // Zone 4
                var area1 = new EffectiveWindArea("Zone4", new List<Vec2> { A, p1, p11}, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area1.OuterBoundary);
                effWindAreas.Add(10, area1);

                var area2 = new EffectiveWindArea("Zone4", new List<Vec2> { D, p21, p31 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area2.OuterBoundary);
                effWindAreas.Add(11, area2);

                // Zone 3
                var area3 = new EffectiveWindArea("Zone3", new List<Vec2> { p1, p2, p12, left_ridge_pt, p11 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area3.OuterBoundary);
                effWindAreas.Add(20, area3);

                var area4 = new EffectiveWindArea("Zone3", new List<Vec2> { p21, left_ridge_pt, p12, p32, p31 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area4.OuterBoundary);
                effWindAreas.Add(21, area4);

                // Zone 2
                var area5 = new EffectiveWindArea("Zone2", new List<Vec2> { p2, B, right_ridge_pt, p12 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area5.OuterBoundary);
                effWindAreas.Add(30, area5);

                var area6 = new EffectiveWindArea("Zone2", new List<Vec2> { p12, right_ridge_pt, C, p32 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area6.OuterBoundary);
                effWindAreas.Add(31, area6);
            } 
            else
            {
                throw new NotImplementedException("ERROR:  In ComputeAreas_ThreeZones() -- Unknown zone configuration" + left_ridge_zone + ", " + right_ridge_zone);
            }
        }

        private void ComputeAreas_TwoZones()
        {
            double building_length = buildingData.BuildingLength;
            double building_width = buildingData.BuildingWidth;
            double h = buildingData.MeanRoofHeight;
            double ridge_offset = Math.Min(building_length, building_width) / 2.0;
            Vec2 left_ridge_pt = new Vec2(ridge_offset, 0.5 * building_width);
            Vec2 right_ridge_pt = new Vec2(building_length - ridge_offset, 0.5 * building_width);

            int left_ridge_zone = WindLoadCalculator_Base.GetRoofZoneNumber(left_ridge_pt, buildingData.MeanRoofHeight, buildingData.BuildingLength);
            int right_ridge_zone = WindLoadCalculator_Base.GetRoofZoneNumber(right_ridge_pt, buildingData.MeanRoofHeight, buildingData.BuildingLength);

            double offset1 = 0.5 * buildingData.MeanRoofHeight;
            double offset2 = 1.0 * buildingData.MeanRoofHeight;
            double offset3 = 2.0 * buildingData.MeanRoofHeight;

            Vec2 A = new Vec2(0, 0);
            Vec2 B = new Vec2(building_length, 0);
            Vec2 C = new Vec2(building_length, building_width);
            Vec2 D = new Vec2(0, building_width);

            if (left_ridge_pt.X > right_ridge_pt.X)
            {
                // TODO should we crash here, or switch to a vertical direction?
                throw new NotImplementedException("ERROR:  In ComputeAreas_TwoZones(): Left ridge point cannot be to the right of right ridge point");
            }

            // Zone 5
            effWindAreas.Add(1, new EffectiveWindArea("ZoneWWR", new List<Vec2> { A, left_ridge_pt, D }, null));
            effWindAreas.Add(2, new EffectiveWindArea("ZoneLWR", new List<Vec2> { B, C, right_ridge_pt }, null));

            // Zone 4
            if ((left_ridge_zone == 4) && (right_ridge_zone == 4))
            {
                Vec2 p1 = new Vec2(offset1, 0);
                Vec2 p11 = new Vec2(offset1, building_length - offset1);
                Vec2 p21 = new Vec2(offset1, building_width - (building_length - offset1));
                Vec2 p31 = new Vec2(offset1, building_width);

                // Zone 4
                var area1 = new EffectiveWindArea("Zone4", new List<Vec2> { A, p1, p11, right_ridge_pt, left_ridge_pt }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area1.OuterBoundary);
                effWindAreas.Add(10, area1);

                var area2 = new EffectiveWindArea("Zone4", new List<Vec2> { D, left_ridge_pt, right_ridge_pt, p21, p31 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area2.OuterBoundary);
                effWindAreas.Add(11, area2);

                // Zone 3
                var area3 = new EffectiveWindArea("Zone3", new List<Vec2> { p1, B, p11 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area3.OuterBoundary);
                effWindAreas.Add(20, area3);

                var area4 = new EffectiveWindArea("Zone3", new List<Vec2> { p21, C, p31 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area4.OuterBoundary);
                effWindAreas.Add(21, area4);
            }
            else if ((left_ridge_zone == 4) && (right_ridge_zone == 3))
            {
                Vec2 p1 = new Vec2(offset1, 0);
                Vec2 p11 = new Vec2(offset1, 0.5 * building_width);
                Vec2 p31 = new Vec2(offset1, building_width);

                // Zone 4
                var area1 = new EffectiveWindArea("Zone4", new List<Vec2> { A, p1, p11, left_ridge_pt }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area1.OuterBoundary);
                effWindAreas.Add(10, area1);

                var area2 = new EffectiveWindArea("Zone4", new List<Vec2> { D, left_ridge_pt, p11, p31 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area2.OuterBoundary);
                effWindAreas.Add(11, area2);

                // Zone 3
                var area3 = new EffectiveWindArea("Zone3", new List<Vec2> { p1, B, right_ridge_pt, p11 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area3.OuterBoundary);
                effWindAreas.Add(20, area3);

                var area4 = new EffectiveWindArea("Zone3", new List<Vec2> { p11, right_ridge_pt, C, p31 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area4.OuterBoundary);
                effWindAreas.Add(21, area4);
            }
            else
            {
                throw new NotImplementedException("ERROR:  In ComputeAreas_TwoZones() -- Unknown zone configuration" + left_ridge_zone + ", " + right_ridge_zone);
            }
        }

        private void ComputeAreas_OneZone()
        {
            double building_length = buildingData.BuildingLength;
            double building_width = buildingData.BuildingWidth;
            double h = buildingData.MeanRoofHeight;
            double ridge_offset = Math.Min(building_length, building_width) / 2.0;
            Vec2 left_ridge_pt = new Vec2(ridge_offset, 0.5 * building_width);
            Vec2 right_ridge_pt = new Vec2(building_length - ridge_offset, 0.5 * building_width);

            int left_ridge_zone = WindLoadCalculator_Base.GetRoofZoneNumber(left_ridge_pt, buildingData.MeanRoofHeight, buildingData.BuildingLength);
            int right_ridge_zone = WindLoadCalculator_Base.GetRoofZoneNumber(right_ridge_pt, buildingData.MeanRoofHeight, buildingData.BuildingLength);

            Vec2 A = new Vec2(0, 0);
            Vec2 B = new Vec2(building_length, 0);
            Vec2 C = new Vec2(building_length, building_width);
            Vec2 D = new Vec2(0, building_width);

            if (left_ridge_pt.X > right_ridge_pt.X)
            {
                // TODO should we crash here, or switch to a vertical direction?
                throw new NotImplementedException("ERROR:  In ComputeAreas_OneZone()Left ridge point cannot be to the right of right ridge point");
            }

            // Zone 5
            effWindAreas.Add(1, new EffectiveWindArea("ZoneWWR", new List<Vec2> { A, left_ridge_pt, D }, null));
            effWindAreas.Add(2, new EffectiveWindArea("ZoneLWR", new List<Vec2> { B, C, right_ridge_pt }, null));

            if ((left_ridge_zone == 4) && (right_ridge_zone == 4))
            {
                // Zone 4
                var area1 = new EffectiveWindArea("Zone4", new List<Vec2> { A, B, right_ridge_pt, left_ridge_pt }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area1.OuterBoundary);
                effWindAreas.Add(10, area1);

                var area2 = new EffectiveWindArea("Zone4", new List<Vec2> { D, left_ridge_pt, right_ridge_pt, C }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area2.OuterBoundary);
                effWindAreas.Add(11, area2);
            }
            else
            {
                throw new NotImplementedException("ERROR:  In ComputeAreas_OneZone() -- Unknown zone configuration" + left_ridge_zone + ", " + right_ridge_zone);
            }
        }
    }
}
