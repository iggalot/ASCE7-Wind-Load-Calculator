using System;
using System.Collections.Generic;
using System.Windows;

namespace ASCE7WindLoadCalculator
{
    public class HipRoofAreaCalculator_PerpToRidge_MWFRS_ASCE7_16 : AreaCalculator_ASCE7_16_Base
    {
        public HipRoofAreaCalculator_PerpToRidge_MWFRS_ASCE7_16(BuildingData bldg_data)
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

            Point A = new Point(0, 0);
            Point B = new Point(building_length, 0);
            Point C = new Point(building_length, building_width);
            Point D = new Point(0, building_width);

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
            Point bottom_ridge_pt = new Point(ridge_offset, ridge_offset);
            Point top_ridge_pt = new Point(ridge_offset, building_width - ridge_offset);

            double offset1 = 0.5 * h;
            double offset2 = 1.0 * h;
            double offset3 = 2.0 * h;

            int top_ridge_zone = WindLoadCalculator_Base.GetRoofZoneNumber(top_ridge_pt, buildingData.MeanRoofHeight, buildingData.BuildingLength);
            int bottom_ridge_zone = WindLoadCalculator_Base.GetRoofZoneNumber(bottom_ridge_pt, buildingData.MeanRoofHeight, buildingData.BuildingLength);

            Point A = new Point(0, 0);
            Point B = new Point(building_length, 0);
            Point C = new Point(building_length, building_width);
            Point D = new Point(0, building_width);

            if (top_ridge_pt.Y < bottom_ridge_pt.Y)
            {
                // TODO should we crash here, or switch to a vertical direction?
                throw new NotImplementedException("ERROR:  In ComputeAreas_OneZone(): Top ridge point cannot be below bottom ridge point");
            }

            // Zone 5
            effWindAreas.Add(1, new EffectiveWindArea("ZoneWWR", new List<Point> { A, bottom_ridge_pt, top_ridge_pt, D }, null));
            effWindAreas.Add(2, new EffectiveWindArea("ZoneLWR", new List<Point> { B, C, top_ridge_pt, bottom_ridge_pt }, null));

            // ridge cannot be in zone 3 or 4 and have four roof zones, can it?
            if (top_ridge_zone == 4 || top_ridge_zone == 3)
            {
                throw new NotImplementedException("ERROR:  In ComputeAreas_ThreeZones(): Top ridge point cannot be in zone 3 or 4 and be four zones");
            }

            else if (top_ridge_zone == 2)
            {
                Point p1 = new Point(offset1, 0);
                Point p11 = new Point(offset1, offset1);
                Point p21 = new Point(offset1, building_width - offset1);
                Point p31 = new Point(offset1, building_width);

                Point p2 = new Point(offset2, 0);
                Point p12 = new Point(offset2, offset2);
                Point p22 = new Point(offset2, building_width - offset2);
                Point p32 = new Point(offset2, building_width);

                Point p3 = new Point(offset3, 0);
                Point p13 = new Point(offset3, building_length - offset3);
                Point p23 = new Point(offset3, building_width - (building_length - offset3));
                Point p33 = new Point(offset3, building_width);

                // Zone 4
                var area1 = new EffectiveWindArea("Zone4", new List<Point> { A, p1, p11 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area1.OuterBoundary);
                effWindAreas.Add(10, area1);

                var area2 = new EffectiveWindArea("Zone4", new List<Point> { D, p21, p31 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area2.OuterBoundary);
                effWindAreas.Add(11, area2);

                var area3 = new EffectiveWindArea("Zone3", new List<Point> { p1, p2, p12, p11 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area3.OuterBoundary);
                effWindAreas.Add(20, area3);

                var area4 = new EffectiveWindArea("Zone3", new List<Point> { p21, p22, p32, p31 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area4.OuterBoundary);
                effWindAreas.Add(21, area4);

                var area5 = new EffectiveWindArea("Zone2", new List<Point> { p2, p3, p13, bottom_ridge_pt, p12 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area5.OuterBoundary);
                effWindAreas.Add(30, area5);

                var area6 = new EffectiveWindArea("Zone2", new List<Point> { p22, top_ridge_pt, p23, p33, p32 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area6.OuterBoundary);
                effWindAreas.Add(31, area6);

                var area7 = new EffectiveWindArea("Zone1", new List<Point> { p3, B, p13 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area7.OuterBoundary);
                effWindAreas.Add(40, area7);

                var area8 = new EffectiveWindArea("Zone1", new List<Point> { p23, C, p33 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area8.OuterBoundary);
                effWindAreas.Add(41, area8);
            }
            else if (top_ridge_zone == 1)
            {
                Point p1 = new Point(offset1, 0);
                Point p11 = new Point(offset1, offset1);
                Point p21 = new Point(offset1, building_width - offset1);
                Point p31 = new Point(offset1, building_width);

                Point p2 = new Point(offset2, 0);
                Point p12 = new Point(offset2, offset2);
                Point p22 = new Point(offset2, building_width - offset2);
                Point p32 = new Point(offset2, building_width);

                Point p3 = new Point(offset3, 0);
                Point p13 = new Point(offset3, offset3);
                Point p23 = new Point(offset3, building_width - offset3);
                Point p33 = new Point(offset3, building_width);

                // Zone 4
                var area1 = new EffectiveWindArea("Zone4", new List<Point> { A, p1, p11 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area1.OuterBoundary);
                effWindAreas.Add(10, area1);

                var area2 = new EffectiveWindArea("Zone4", new List<Point> { D, p21, p31 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area2.OuterBoundary);
                effWindAreas.Add(11, area2);

                var area3 = new EffectiveWindArea("Zone3", new List<Point> { p1, p2, p12, p11 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area3.OuterBoundary);
                effWindAreas.Add(20, area3);

                var area4 = new EffectiveWindArea("Zone3", new List<Point> { p21, p22, p32, p31 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area4.OuterBoundary);
                effWindAreas.Add(21, area4);

                var area5 = new EffectiveWindArea("Zone2", new List<Point> { p2, p3, p13, p12 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area5.OuterBoundary);
                effWindAreas.Add(30, area5);

                var area6 = new EffectiveWindArea("Zone2", new List<Point> { p22, p23, p33, p32 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area6.OuterBoundary);
                effWindAreas.Add(31, area6);

                var area7 = new EffectiveWindArea("Zone1", new List<Point> { p3, B, bottom_ridge_pt, p13 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area7.OuterBoundary);
                effWindAreas.Add(40, area7);

                var area8 = new EffectiveWindArea("Zone1", new List<Point> { p23, top_ridge_pt, C, p33 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area8.OuterBoundary);
                effWindAreas.Add(41, area8);
            }
            else
            {
                throw new NotImplementedException("ERROR:  In ComputeAreas_ThreeZones() -- Unknown zone configuration, Ridge is in zone " + top_ridge_zone);
            }
        }

        private void ComputeAreas_ThreeZones()
        {
            double building_length = buildingData.BuildingLength;
            double building_width = buildingData.BuildingWidth;
            double h = buildingData.MeanRoofHeight;
            double ridge_offset = Math.Min(building_length, building_width) / 2.0;
            Point bottom_ridge_pt = new Point(ridge_offset, ridge_offset);
            Point top_ridge_pt = new Point(ridge_offset, building_width - ridge_offset);

            double offset1 = 0.5 * h;
            double offset2 = 1.0 * h;
            double offset3 = 2.0 * h;

            int top_ridge_zone = WindLoadCalculator_Base.GetRoofZoneNumber(top_ridge_pt, buildingData.MeanRoofHeight, buildingData.BuildingLength);
            int bottom_ridge_zone = WindLoadCalculator_Base.GetRoofZoneNumber(bottom_ridge_pt, buildingData.MeanRoofHeight, buildingData.BuildingLength);

            Point A = new Point(0, 0);
            Point B = new Point(building_length, 0);
            Point C = new Point(building_length, building_width);
            Point D = new Point(0, building_width);

            if (top_ridge_pt.Y < bottom_ridge_pt.Y)
            {
                // TODO should we crash here, or switch to a vertical direction?
                throw new NotImplementedException("ERROR:  In ComputeAreas_OneZone(): Top ridge point cannot be below bottom ridge point");
            }

            // Zone 5
            effWindAreas.Add(1, new EffectiveWindArea("ZoneWWR", new List<Point> { A, bottom_ridge_pt, top_ridge_pt, D }, null));
            effWindAreas.Add(2, new EffectiveWindArea("ZoneLWR", new List<Point> { B, C, top_ridge_pt, bottom_ridge_pt }, null));

            // ridge cannot be in zone 4 and have three roof zones, can it?
            if (top_ridge_zone == 4)
            {
                throw new NotImplementedException("ERROR:  In ComputeAreas_ThreeZones(): Top ridge point cannot be in zone 4"); 
            }

            else if (top_ridge_zone == 3)
            {
                Point p1 = new Point(offset1, 0);
                Point p11 = new Point(offset1, offset1);
                Point p21 = new Point(offset1, building_width - offset1);
                Point p31 = new Point(offset1, building_width);

                Point p2 = new Point(offset2, 0);
                Point p12 = new Point(offset2, building_length - offset2);
                Point p22 = new Point(offset2, building_width - (building_length - offset2));
                Point p32 = new Point(offset2, building_width);

                // Zone 4
                var area1 = new EffectiveWindArea("Zone4", new List<Point> { A, p1, p11 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area1.OuterBoundary);
                effWindAreas.Add(10, area1);

                var area2 = new EffectiveWindArea("Zone4", new List<Point> { D, p21, p31 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area2.OuterBoundary);
                effWindAreas.Add(11, area2);

                var area3 = new EffectiveWindArea("Zone3", new List<Point> { p1, p2, p12, bottom_ridge_pt, p11 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area3.OuterBoundary);
                effWindAreas.Add(20, area3);

                var area4 = new EffectiveWindArea("Zone3", new List<Point> { p21, top_ridge_pt, p22, p32, p31 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area4.OuterBoundary);
                effWindAreas.Add(21, area4);

                var area5 = new EffectiveWindArea("Zone2", new List<Point> { p2, B, p12 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area5.OuterBoundary);
                effWindAreas.Add(30, area5);

                var area6 = new EffectiveWindArea("Zone2", new List<Point> { p22, C, p32 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area6.OuterBoundary);
                effWindAreas.Add(31, area6);
            }
            else
            {
                throw new NotImplementedException("ERROR:  In ComputeAreas_ThreeZones() -- Unknown zone configuration, Ridge is in zone " + top_ridge_zone);
            }
        }

        private void ComputeAreas_TwoZones()
        {
            double building_length = buildingData.BuildingLength;
            double building_width = buildingData.BuildingWidth;
            double h = buildingData.MeanRoofHeight;
            double ridge_offset = Math.Min(building_length, building_width) / 2.0;
            Point bottom_ridge_pt = new Point(ridge_offset, ridge_offset);
            Point top_ridge_pt = new Point(ridge_offset, building_width - ridge_offset);

            double offset1 = 0.5 * h;
            double offset2 = 1.0 * h;
            double offset3 = 2.0 * h;

            int top_ridge_zone = WindLoadCalculator_Base.GetRoofZoneNumber(top_ridge_pt, buildingData.MeanRoofHeight, buildingData.BuildingLength);
            int bottom_ridge_zone = WindLoadCalculator_Base.GetRoofZoneNumber(bottom_ridge_pt, buildingData.MeanRoofHeight, buildingData.BuildingLength);

            Point A = new Point(0, 0);
            Point B = new Point(building_length, 0);
            Point C = new Point(building_length, building_width);
            Point D = new Point(0, building_width);

            if (top_ridge_pt.Y < bottom_ridge_pt.Y)
            {
                // TODO should we crash here, or switch to a vertical direction?
                throw new NotImplementedException("ERROR:  In ComputeAreas_OneZone(): Top ridge point cannot be below bottom ridge point");
            }

            // Zone 5
            effWindAreas.Add(1, new EffectiveWindArea("ZoneWWR", new List<Point> { A, bottom_ridge_pt, top_ridge_pt, D }, null));
            effWindAreas.Add(2, new EffectiveWindArea("ZoneLWR", new List<Point> { B, C, top_ridge_pt, bottom_ridge_pt }, null));

            if (top_ridge_zone == 4)
            {
                Point p1 = new Point(offset1, 0);
                Point p11 = new Point(offset1, building_length - offset1);
                Point p21 = new Point(offset1, building_width - (building_length - offset1));
                Point p31 = new Point(offset1, building_width);

                // Zone 4
                var area1 = new EffectiveWindArea("Zone4", new List<Point> { A, p1, p11, bottom_ridge_pt }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area1.OuterBoundary);
                effWindAreas.Add(10, area1);

                var area2 = new EffectiveWindArea("Zone4", new List<Point> { D, top_ridge_pt, p21, p31 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area2.OuterBoundary);
                effWindAreas.Add(11, area2);

                var area3 = new EffectiveWindArea("Zone3", new List<Point> { p1, B, p11 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area3.OuterBoundary);
                effWindAreas.Add(20, area3);

                var area4 = new EffectiveWindArea("Zone3", new List<Point> { p21, C, p31 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area4.OuterBoundary);
                effWindAreas.Add(21, area4);
            }

            if (top_ridge_zone == 3)
            {
                Point p1 = new Point(offset1, 0);
                Point p11 = new Point(offset1, building_length - offset1);
                Point p21 = new Point(offset1, building_width - (building_length - offset1));
                Point p31 = new Point(offset1, building_width);

                // Zone 4
                var area1 = new EffectiveWindArea("Zone4", new List<Point> { A, p1, p11 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area1.OuterBoundary);
                effWindAreas.Add(10, area1);

                var area2 = new EffectiveWindArea("Zone4", new List<Point> { D, p21, p31 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area2.OuterBoundary);
                effWindAreas.Add(11, area2);

                var area3 = new EffectiveWindArea("Zone3", new List<Point> { p1, B, bottom_ridge_pt, p11 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area3.OuterBoundary);
                effWindAreas.Add(20, area3);

                var area4 = new EffectiveWindArea("Zone3", new List<Point> { p21, top_ridge_pt, p31 }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area4.OuterBoundary);
                effWindAreas.Add(21, area4);
            }
            else
            {
                throw new NotImplementedException("ERROR:  In ComputeAreas_TwoZone() -- Unknown zone configuration, Ridge is in zone " + top_ridge_zone);
            }
        }

        private void ComputeAreas_OneZone()
        {
            double building_length = buildingData.BuildingLength;
            double building_width = buildingData.BuildingWidth;
            double h = buildingData.MeanRoofHeight;
            double ridge_offset = Math.Min(building_length, building_width) / 2.0;
            Point bottom_ridge_pt = new Point(ridge_offset, ridge_offset);
            Point top_ridge_pt = new Point (ridge_offset, building_width-ridge_offset);

            int top_ridge_zone = WindLoadCalculator_Base.GetRoofZoneNumber(top_ridge_pt, buildingData.MeanRoofHeight, buildingData.BuildingLength);
            int bottom_ridge_zone = WindLoadCalculator_Base.GetRoofZoneNumber(bottom_ridge_pt, buildingData.MeanRoofHeight, buildingData.BuildingLength);

            Point A = new Point(0, 0);
            Point B = new Point(building_length, 0);
            Point C = new Point(building_length, building_width);
            Point D = new Point(0, building_width);

            if (top_ridge_pt.Y < bottom_ridge_pt.Y)
            {
                // TODO should we crash here, or switch to a vertical direction?
                throw new NotImplementedException("ERROR:  In ComputeAreas_OneZone(): Top ridge point cannot be below bottom ridge point");
            }

            // Zone 5
            effWindAreas.Add(1, new EffectiveWindArea("ZoneWWR", new List<Point> { A, bottom_ridge_pt, top_ridge_pt, D }, null));
            effWindAreas.Add(2, new EffectiveWindArea("ZoneLWR", new List<Point> { B, C, top_ridge_pt, bottom_ridge_pt }, null));

            if (top_ridge_zone == 4)
            {
                // Zone 4
                var area1 = new EffectiveWindArea("Zone4", new List<Point> { A, B, bottom_ridge_pt }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area1.OuterBoundary);
                effWindAreas.Add(10, area1);

                var area2 = new EffectiveWindArea("Zone4", new List<Point> { D, top_ridge_pt, C }, null);
                EffectiveWindArea.RemoveConsecutiveDuplicates(area2.OuterBoundary);
                effWindAreas.Add(11, area2);
            }
            else
            {
                throw new NotImplementedException("ERROR:  In ComputeAreas_OneZone(): Top ridge point cannot be below bottom ridge point");
            }
        }
    }
}
