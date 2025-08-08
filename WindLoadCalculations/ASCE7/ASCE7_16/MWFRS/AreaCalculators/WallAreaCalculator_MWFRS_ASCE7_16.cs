using System;
using System.Collections.Generic;
using System.Windows;

namespace ASCE7WindLoadCalculator
{
    public class WallAreaCalculator_MWFRS_ASCE7_16 : AreaCalculator_MWFRS_ASCE7_16_Base
    {
        public WallAreaCalculator_MWFRS_ASCE7_16(BuildingData bldg_data, string note_string = "")
        {
            buildingData = bldg_data;
            Note = note_string;
        }

        public override void ComputeEffectiveWindAreas()
        {
            double length = this.buildingData.BuildingLength;
            double width = this.buildingData.BuildingWidth;

            // Corners of the wall planes -- assumed to be perpendicular to wind
            Point A = new Point(0, 0);
            Point B = new Point(width, 0);
            Point C = new Point(width, buildingData.BuildingHeight);
            Point D = new Point(0, buildingData.BuildingHeight);

            Point E = new Point(0, 0);
            Point F = new Point(length, 0);
            Point G = new Point(length, buildingData.BuildingHeight);
            Point H = new Point(0, buildingData.BuildingHeight);
            Point ridge;
            
            // check if we have a gable end
            if(buildingData.RoofType == RoofTypes.ROOF_TYPE_GABLE)
            {
                if(buildingData.RidgeDirection == RidgeDirections.RIDGE_DIR_PERP_TO_BLDGLENGTH)
                {
                    ridge = new Point(0.5 * length, buildingData.BuildingHeight + Math.Tan(buildingData.RoofPitch * Math.PI / 180.0) * length / 2.0);

                    effWindAreas.Add(10, new EffectiveWindArea("ZoneWW", new List<Point> { A, B, C, D }, null));
                    effWindAreas.Add(20, new EffectiveWindArea("ZoneLW", new List<Point> { A, B, C, D }, null));
                    effWindAreas.Add(30, new EffectiveWindArea("ZoneSW", new List<Point> { E, F, G, ridge, H }, null));
                } else
                {
                    ridge = new Point(0.5 * width, buildingData.BuildingHeight + Math.Tan(buildingData.RoofPitch * Math.PI / 180.0) * length / 2.0);
                    effWindAreas.Add(10, new EffectiveWindArea("ZoneWW", new List<Point> { A, B, C, ridge, D }, null));
                    effWindAreas.Add(20, new EffectiveWindArea("ZoneLW", new List<Point> { A, B, C, ridge, D }, null));
                    effWindAreas.Add(30, new EffectiveWindArea("ZoneSW", new List<Point> { E, F, G, H }, null));
                }
            } else
            {
                effWindAreas.Add(10, new EffectiveWindArea("ZoneWW", new List<Point> { A, B, C, D }, null));
                effWindAreas.Add(20, new EffectiveWindArea("ZoneLW", new List<Point> { A, B, C, D }, null));
                effWindAreas.Add(30, new EffectiveWindArea("ZoneSW", new List<Point> { E, F, G, H }, null));

            }
        }
    }
}
