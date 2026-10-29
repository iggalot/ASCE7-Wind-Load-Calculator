using System;
using System.Collections.Generic;

namespace ASCE7WindLoadCalculator
{
    public class WallAreaCalculator_MWFRS : AreaCalculator_Base
    {
        public WallAreaCalculator_MWFRS(BuildingData bldg_data, string note_string = "")
        {
            buildingData = bldg_data;
            Note = note_string;
        }

        public override void ComputeEffectiveWindAreas()
        {
            double length = this.buildingData.BuildingLength;
            double width = this.buildingData.BuildingWidth;

            // Corners of the wall planes -- assumed to be perpendicular to wind
            Vec2 A = new Vec2(0, 0);
            Vec2 B = new Vec2(width, 0);
            Vec2 C = new Vec2(width, buildingData.BuildingHeight);
            Vec2 D = new Vec2(0, buildingData.BuildingHeight);

            Vec2 E = new Vec2(0, 0);
            Vec2 F = new Vec2(length, 0);
            Vec2 G = new Vec2(length, buildingData.BuildingHeight);
            Vec2 H = new Vec2(0, buildingData.BuildingHeight);
            Vec2 ridge;
            
            // check if we have a gable end
            if(buildingData.RoofType == RoofTypes.ROOF_TYPE_GABLE)
            {
                if(buildingData.RidgeDirection == RidgeDirections.RIDGE_DIR_PERP_TO_BLDGLENGTH)
                {
                    ridge = new Vec2(0.5 * length, buildingData.BuildingHeight + Math.Tan(buildingData.RoofPitch * Math.PI / 180.0) * length / 2.0);

                    effWindAreas.Add(10, new EffectiveWindArea("ZoneWW", new List<Vec2> { A, B, C, D }, null));
                    effWindAreas.Add(20, new EffectiveWindArea("ZoneLW", new List<Vec2> { A, B, C, D }, null));
                    effWindAreas.Add(30, new EffectiveWindArea("ZoneSW", new List<Vec2> { E, F, G, ridge, H }, null));
                } else
                {
                    ridge = new Vec2(0.5 * width, buildingData.BuildingHeight + Math.Tan(buildingData.RoofPitch * Math.PI / 180.0) * length / 2.0);
                    effWindAreas.Add(10, new EffectiveWindArea("ZoneWW", new List<Vec2> { A, B, C, ridge, D }, null));
                    effWindAreas.Add(20, new EffectiveWindArea("ZoneLW", new List<Vec2> { A, B, C, ridge, D }, null));
                    effWindAreas.Add(30, new EffectiveWindArea("ZoneSW", new List<Vec2> { E, F, G, H }, null));
                }
            } else
            {
                effWindAreas.Add(10, new EffectiveWindArea("ZoneWW", new List<Vec2> { A, B, C, D }, null));
                effWindAreas.Add(20, new EffectiveWindArea("ZoneLW", new List<Vec2> { A, B, C, D }, null));
                effWindAreas.Add(30, new EffectiveWindArea("ZoneSW", new List<Vec2> { E, F, G, H }, null));

            }
        }
    }
}
