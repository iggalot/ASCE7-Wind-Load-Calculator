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

            if (buildingData.HasRoofFaces && buildingData.HasWallLines)
            {
                // the plan's own walls (owner 2026-11-13): the walls facing the wind, those facing away and those along it, each
                // set projected across its direction (RoofFaceProfile.Silhouette: its force along the wind). On the building each
                // wall takes its set's pressure on its own outline (WindCases, WindScene)
                effWindAreas.Add(10, new EffectiveWindArea("ZoneWW", RoofFaceProfile.Silhouette(buildingData, new Vec2(-1, 0)), null));
                effWindAreas.Add(20, new EffectiveWindArea("ZoneLW", RoofFaceProfile.Silhouette(buildingData, new Vec2(1, 0)), null));
                effWindAreas.Add(30, new EffectiveWindArea("ZoneSW", RoofFaceProfile.Silhouette(buildingData, new Vec2(0, -1)), null));
                return;
            }
            if (buildingData.HasRoofFaces)
            {
                // a roof of sections (owner 2026-11-13): each wall's top is the faces' own profile over its edge. The wind is
                // along +x: the windward wall at x = 0, the leeward at x = L, the side walls taken at y = 0 (their pressures
                // are across the wind; the north side's own profile drawn the same)
                effWindAreas.Add(10, new EffectiveWindArea("ZoneWW", RoofFaceProfile.WallOutline(buildingData, 0, 0, 0, width), null));
                effWindAreas.Add(20, new EffectiveWindArea("ZoneLW", RoofFaceProfile.WallOutline(buildingData, length, 0, length, width), null));
                effWindAreas.Add(30, new EffectiveWindArea("ZoneSW", RoofFaceProfile.WallOutline(buildingData, 0, 0, length, 0), null));
                return;
            }

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
                    ridge = new Vec2(0.5 * width, buildingData.BuildingHeight + Math.Tan(buildingData.RoofPitch * Math.PI / 180.0) * width / 2.0);  // gable spans the width when the ridge runs along the length
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
