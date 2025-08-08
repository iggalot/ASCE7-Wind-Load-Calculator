using System;
using System.Collections.Generic;
using System.Windows;

namespace ASCE7WindLoadCalculator
{
    public class GableRoofAreaCalculator_PerpToRidge_MWFRS_ASCE7_22 : AreaCalculator_ASCE7_22_Base
    {
        public GableRoofAreaCalculator_PerpToRidge_MWFRS_ASCE7_22(BuildingData bldg_data)
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

            double ridge_offset = Math.Min(building_length, building_width) / 2.0;
            Point bottom_ridge_pt = new Point(ridge_offset, ridge_offset);
            Point top_ridge_pt = new Point(ridge_offset, building_width - ridge_offset);

            Point A = new Point(0, 0);
            Point B = new Point(building_length, 0);
            Point C = new Point(building_length, building_width);
            Point D = new Point(0, building_width);

            // Mid length points
            Point E = new Point(0.5 * building_length, 0);
            Point F = new Point(0.5 * building_length, building_width);

            // WWR and LWR roof zones
            effWindAreas.Add(1, new EffectiveWindArea("ZoneWWR", new List<Point> { D, A, E, F }, null));
            effWindAreas.Add(2, new EffectiveWindArea("ZoneLWR", new List<Point> { F, E, B, C }, null));

        }
    }
}
