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
    /// Built on <see cref="WindGeometry"/>, so it covers any plan proportions without special cases.
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
            var model = new WindGeometry(buildingData);
            if (buildingData.HasRoofFaces) { FaceByFace(model); return; }
            double h = buildingData.MwfrsFigureHeight;   // Fig. 27.3-1: eave height for θ ≤ 10°
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

        /// <summary>
        /// A roof of several sections (owner 2026-11-13), the wind along +x, each face by its own section's ridge:
        /// - its section's ridge across the wind (along y), the face sloping along the wind and θ ≥ 10°: the windward roof
        ///   (rising downwind) or the leeward roof, normal-to-ridge values at its slope and its section's h/L;
        /// - every other face (its ridge along the wind, a hip end when the wind is across the ridge, below 10°): the
        ///   distance bands 0 to h/2, h/2 to h, h to 2h, past 2h of the parallel-to-ridge values, measured from the face's own
        ///   windward edge (owner 2026-11-13), h its section's. A hip end with the wind along the main ridge is so taken: the
        ///   ASCE 7 subcommittee's reading (ASCE Standards Exchange, "MWFRS wind pressures on hipped end, wind parallel to
        ///   ridge"), the parallel-to-ridge values.
        /// Each zone keeps its face (<see cref="EffectiveWindArea.Face"/>): its values are its face's own.
        /// </summary>
        private void FaceByFace(WindGeometry model)
        {
            int id = 1;
            var labels = new[] { "Zone4", "Zone3", "Zone2", "Zone1" };
            foreach (var group in model.Roofs.Where(r => r.RoofFace != null).GroupBy(r => r.RoofFace))
            {
                var f = group.Key;
                bool normal = !f.RidgeAlongX && f.SlopeDeg >= NormalToRidgeMinSlope && Math.Abs(f.Zx) > 1e-12;
                if (normal)
                {
                    foreach (var plane in group)
                        effWindAreas.Add(id++, new EffectiveWindArea(f.Zx > 0 ? "ZoneWWR" : "ZoneLWR", plane.PlanRegion) { Face = f });
                    continue;
                }
                double x0 = group.SelectMany(p => p.PlanRegion).Min(v => v.X), x1 = group.SelectMany(p => p.PlanRegion).Max(v => v.X);
                double h = f.H;
                var edges = new[] { x0, x0 + 0.5 * h, x0 + h, x0 + 2 * h, double.PositiveInfinity };
                foreach (var plane in group)
                    for (int b = 0; b < labels.Length; b++)
                    {
                        double a0 = edges[b], a1 = Math.Min(edges[b + 1], x1 + 1);
                        if (a0 >= x1) break;
                        var band = new List<Vec2> { new Vec2(a0, -1), new Vec2(a1, -1), new Vec2(a1, buildingData.BuildingWidth + 1), new Vec2(a0, buildingData.BuildingWidth + 1) };
                        var piece = PolygonOps.ClipToConvex(plane.PlanRegion, band);
                        if (piece.Count >= 3 && Math.Abs(PolygonOps.SignedArea(piece)) > MinZoneArea)
                            effWindAreas.Add(id++, new EffectiveWindArea(labels[b], piece) { Face = f });
                    }
            }
        }
    }
}
