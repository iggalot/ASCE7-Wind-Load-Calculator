using System;
using System.Collections.Generic;

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
            Vec2 bottom_ridge_pt = new Vec2(ridge_offset, ridge_offset);
            Vec2 top_ridge_pt = new Vec2(ridge_offset, building_width - ridge_offset);

            Vec2 A = new Vec2(0, 0);
            Vec2 B = new Vec2(building_length, 0);
            Vec2 C = new Vec2(building_length, building_width);
            Vec2 D = new Vec2(0, building_width);

            // Mid length points
            Vec2 E = new Vec2(0.5 * building_length, 0);
            Vec2 F = new Vec2(0.5 * building_length, building_width);

            // WWR and LWR roof zones
            effWindAreas.Add(1, new EffectiveWindArea("ZoneWWR", new List<Vec2> { D, A, E, F }, null));
            effWindAreas.Add(2, new EffectiveWindArea("ZoneLWR", new List<Vec2> { F, E, B, C }, null));

        }
    }
}
