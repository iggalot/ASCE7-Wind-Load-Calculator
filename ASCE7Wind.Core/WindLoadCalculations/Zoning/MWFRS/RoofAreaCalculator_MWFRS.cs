using System;
using System.Collections.Generic;
using System.Linq;

namespace ASCE7WindLoadCalculator
{
    /// <summary>
    /// MWFRS roof zones of Fig. 27.3-1 for flat, gable and hip roofs, with the wind blowing along +x
    /// (the BuildingLength direction; the other direction is analysed on the building turned 90 degrees).
    ///
    /// Each roof plane is classified by how it meets the wind:
    /// - planes facing the wind (rising or falling along x) with slope &gt;= 10 deg are the windward roof
    ///   (ZoneWWR) or leeward roof (ZoneLWR), with Cp read against slope;
    /// - every other plane, and every plane when the slope is &lt; 10 deg, is divided into the distance bands
    ///   0-h/2 (Zone4), h/2-h (Zone3), h-2h (Zone2) and beyond 2h (Zone1) from the windward edge.
    /// Built on <see cref="BuildingModel"/>, so it covers any plan proportions without special cases.
    /// </summary>
    public class RoofAreaCalculator_MWFRS : AreaCalculator_Base
    {
        /// <summary>Roof slope at and above which wind normal to a plane uses the windward/leeward roof Cp.</summary>
        public const double NormalToRidgeMinSlope = 10.0;

        public RoofAreaCalculator_MWFRS(BuildingData bldg_data)
        {
            buildingData = bldg_data;
        }

        public override void ComputeEffectiveWindAreas()
        {
            var model = new BuildingModel(buildingData);
            double h = buildingData.MeanRoofHeight;
            double L = buildingData.BuildingLength;
            bool steep = buildingData.RoofPitch >= NormalToRidgeMinSlope;

            // Band edges measured from the windward (x = 0) edge.
            var edges = new[] { 0.0, 0.5 * h, h, 2 * h, double.PositiveInfinity };
            var bandLabels = new[] { "Zone4", "Zone3", "Zone2", "Zone1" };

            int id = 1;
            foreach (var plane in model.Roofs)
            {
                bool facesWind = Math.Abs(plane.Zx) > 1e-12; // plane rises or falls along the wind
                if (steep && facesWind)
                {
                    string label = plane.Zx > 0 ? "ZoneWWR" : "ZoneLWR"; // rising toward downwind faces the wind
                    effWindAreas.Add(id++, new EffectiveWindArea(label, plane.PlanRegion));
                    continue;
                }

                for (int b = 0; b < bandLabels.Length; b++)
                {
                    double x0 = edges[b], x1 = Math.Min(edges[b + 1], L + 1);
                    if (x0 >= L) break;
                    var band = new List<Vec2> { new Vec2(x0, -1), new Vec2(x1, -1), new Vec2(x1, buildingData.BuildingWidth + 1), new Vec2(x0, buildingData.BuildingWidth + 1) };
                    var piece = PolygonOps.ClipToConvex(plane.PlanRegion, band);
                    if (piece.Count >= 3 && Math.Abs(PolygonOps.SignedArea(piece)) > MinZoneArea)
                        effWindAreas.Add(id++, new EffectiveWindArea(bandLabels[b], piece));
                }
            }
        }
    }
}
