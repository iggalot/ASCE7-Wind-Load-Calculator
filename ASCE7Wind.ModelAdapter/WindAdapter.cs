using System;
using System.Collections.Generic;
using System.Linq;
using BuildingModel.Geometry;
using BuildingModel.Model;
using BuildingModel.Statics;

namespace ASCE7WindLoadCalculator.ModelAdapter
{
    /// <summary>
    /// The wind inputs the building model does not hold (they are about the wind, not the building): the enclosure of the
    /// building and of the parapet. Everything about the building, the roof included, comes from the model.
    /// </summary>
    public sealed class WindOptions
    {
        public BuildingEnclosures Enclosure { get; set; } = BuildingEnclosures.BLDG_ENCLOSED;
        public BuildingEnclosures ParapetEnclosure { get; set; } = BuildingEnclosures.BLDG_ENCLOSED;
    }

    /// <summary>The model as the wind engine sees it: the engine's building, the envelope it came from, and what was assumed.</summary>
    public sealed class WindModelInput
    {
        /// <summary>For the wind engine: L, B, stories and plate heights, the roof.</summary>
        public BuildingData Building { get; set; }

        /// <summary>The model's envelope: L × B, the levels and each face's share per level.</summary>
        public BuildingEnvelope Envelope { get; set; }

        /// <summary>What the conversion assumed or could not carry over (the envelope's notes and the wind's own).</summary>
        public List<string> Notes { get; set; } = new List<string>();
    }

    /// <summary>
    /// Building model → wind engine. L and B are the model's enclosing rectangle (L W-E, B S-N), the stories and plate
    /// heights are its levels, and the roof (flat / gable / hip, slope, ridge, parapet, eaves) is the model's roof. Only
    /// the enclosure is the wind app's own input.
    /// </summary>
    public static class WindAdapter
    {
        public static WindModelInput From(Building b, WindOptions options = null)
        {
            if (b == null) throw new ArgumentNullException(nameof(b));
            options = options ?? new WindOptions();
            var env = b.Envelope();
            var input = new WindModelInput { Envelope = env };
            input.Notes.AddRange(env.Notes);

            var roof = b.Roof ?? b.Defaults.NewRoof();
            var data = new BuildingData
            {
                BuildingLength = env.Length,
                BuildingWidth = env.Width,
                RoofType = RoofType(roof.Shape),
                RoofPitch = roof.Pitch,
                OverhangWidth = roof.Overhang,
                ParapetHeight = roof.Parapet,
                ParapetEnclosure = options.ParapetEnclosure,
                EnclosureType = options.Enclosure,
            };
            data.RidgeDirection = Ridge(roof);

            var stories = env.StoryHeights();
            if (stories.Count <= BuildingData.MaxStories)
            {
                data.NumberOfStories = stories.Count;
                data.PlateHeights = stories;
            }
            else
            {
                data.BuildingHeight = env.EaveHeight;
                input.Notes.Add($"{stories.Count} diaphragm levels: the wind engine splits up to {BuildingData.MaxStories}; use the face strips for the levels.");
            }
            input.Building = data;
            Faces(b, env, data, input.Notes);
            return input;
        }

        /// <summary>
        /// A roof of several sections (wings, cross gables, dormers; owner 2026-11-13): its faces for the engine
        /// (<see cref="BuildingData.RoofFaces"/>), each taking the wind by its own section's ridge, and the walls standing on a
        /// lower roof (<see cref="BuildingData.RoofWalls"/>). The walls of the building stay on the bounding box; so do h and
        /// qh. The main section (the largest at the wall tops) takes the building's h and L; a wing or a dormer its own (ASCE
        /// 7 gives none for them: owner 2026-11-13, "use the wing's own"). A plain roof (no sections) is left as it was.
        /// </summary>
        public static void Faces(Building b, BuildingEnvelope env, BuildingData data, List<string> notes)
        {
            var roof = b.Roof;
            if (roof == null || !roof.IsSloped || roof.Sections == null || roof.Sections.Count == 0) return;
            var fr = RoofFrame.Of(b);
            if (fr.NotLaidOut != null || fr.Faces.Count == 0)
            {
                notes?.Add($"The roof's sections could not be laid out ({fr.NotLaidOut ?? "no faces"}): the wind takes the plain {roof.Shape.ToString().ToLowerInvariant()} roof over the bounding box.");
                return;
            }
            double x0 = env.Bounds.X0, y0 = env.Bounds.Y0, eave = env.EaveHeight;
            var main = fr.Sections.Select((s, i) => (s, i)).Where(p => p.s.EaveRise <= 1e-9).OrderByDescending(p => p.s.Walls.Width * p.s.Walls.Height).Select(p => p.i).DefaultIfEmpty(0).First();
            Vec2 E(Point2 p) => new Vec2(p.X - x0, p.Y - y0);
            var faces = new List<RoofFacePlane>();
            foreach (var f in fr.Faces)
            {
                var p = f.Plane;
                var s = fr.Sections[p.Section];
                bool isMain = p.Section == main;
                double theta = p.SlopeDeg, tan = Math.Tan(theta * Math.PI / 180);
                bool ridgeX = s.Shape == RoofShape.Shed ? !p.AlongX : RoofFrame.RidgeWestEast(s);
                double across = ridgeX ? s.Walls.Height : s.Walls.Width;
                double run = s.Shape == RoofShape.Shed ? across : s.Shape == RoofShape.Hip ? Math.Min(s.Walls.Width, s.Walls.Height) / 2 : across / 2;
                double ownEave = eave + s.EaveRise;
                faces.Add(new RoofFacePlane
                {
                    Id = f.Id, Name = $"{s.Name ?? "roof"}, {f.Name}", Section = s.Name,
                    Z0 = eave + p.Z0 + p.Gx * x0 + p.Gy * y0, Zx = p.Gx, Zy = p.Gy, SlopeDeg = theta, RidgeAlongX = ridgeX,
                    H = isMain ? data.MwfrsFigureHeight : theta <= 10 ? ownEave : ownEave + tan * run / 2,
                    Lx = isMain ? data.BuildingLength : s.Walls.Width, Ly = isMain ? data.BuildingWidth : s.Walls.Height,
                    Pieces = f.Pieces.Select(c => c.P.Select(E).ToList()).ToList(),
                });
            }
            data.RoofFaces = faces;
            data.RoofWalls = RoofWalls(fr, roof.Overhang, x0, y0, eave);
            notes?.Add($"The roof's {fr.Sections.Count} sections taken face by face ({faces.Count} faces, each by its own ridge; owner 2026-11-13)" +
                       (data.RoofWalls.Count > 0 ? $", with {data.RoofWalls.Count} wall{(data.RoofWalls.Count == 1 ? "" : "s")} standing on the roof (dormers' fronts and cheeks)" : "") + ".");
        }

        /// <summary>
        /// The walls standing on a lower roof: along each section's wall lines, where another section's roof is under it, the
        /// wall from that roof up to the section's own surface (its eave, its gable's rake), in 1/4 ft steps. A wall standing on
        /// the ground is the building's own (the bounding box): left out.
        /// </summary>
        internal static List<RoofWallPiece> RoofWalls(RoofFrame fr, double overhang, double x0, double y0, double eave)
        {
            var list = new List<RoofWallPiece>();
            const double step = 0.25;
            double Own(int k, double x, double y) => fr.Planes.Where(p => p.Section == k).Select(p => p.Z(x, y)).DefaultIfEmpty(double.NaN).Min();
            for (int k = 0; k < fr.Sections.Count; k++)
            {
                var w = fr.Sections[k].Walls;
                foreach (var (name, a, bEnd, n) in new[]
                {
                    ("south", new Point2(w.X0, w.Y0), new Point2(w.X1, w.Y0), new Vec2(0, -1)),
                    ("north", new Point2(w.X0, w.Y1), new Point2(w.X1, w.Y1), new Vec2(0, 1)),
                    ("west", new Point2(w.X0, w.Y0), new Point2(w.X0, w.Y1), new Vec2(-1, 0)),
                    ("east", new Point2(w.X1, w.Y0), new Point2(w.X1, w.Y1), new Vec2(1, 0)),
                })
                {
                    double len = Math.Sqrt((bEnd.X - a.X) * (bEnd.X - a.X) + (bEnd.Y - a.Y) * (bEnd.Y - a.Y));
                    int m = Math.Max(1, (int)Math.Ceiling(len / step));
                    double ds = len / m, area = 0, cx = 0, cy = 0, cz = 0;
                    for (int i = 0; i < m; i++)
                    {
                        double t = (i + 0.5) / m, x = a.X + t * (bEnd.X - a.X), y = a.Y + t * (bEnd.Y - a.Y);
                        double top = Own(k, x, y), bottom = double.NegativeInfinity;
                        for (int j = 0; j < fr.Sections.Count; j++)
                        {
                            if (j == k) continue;
                            var r = fr.Sections[j].Walls;   // standing on its roof: inside its wall lines (its eaves past them are over open air)
                            if (x <= r.X0 + 1e-6 || x >= r.X1 - 1e-6 || y <= r.Y0 + 1e-6 || y >= r.Y1 - 1e-6) continue;   // strictly inside: on its wall line it is the building's edge (a wing meeting it there: inside the attic)
                            bottom = Math.Max(bottom, Own(j, x, y));
                        }
                        if (double.IsNaN(top) || double.IsNegativeInfinity(bottom) || top - bottom <= 1e-6) continue;
                        double hgt = top - bottom, da = hgt * ds;
                        area += da; cx += da * x; cy += da * y; cz += da * (eave + (top + bottom) / 2);
                    }
                    if (area < 0.5) continue;
                    list.Add(new RoofWallPiece
                    {
                        Name = $"{fr.Sections[k].Name ?? "section"}, {name} wall on the roof", Normal = n, Area = area,
                        Centroid = new Vec2(cx / area - x0, cy / area - y0), CentroidZ = cz / area,
                    });
                }
            }
            return list;
        }

        public static RoofTypes RoofType(RoofShape s) =>
            s == RoofShape.Gable ? RoofTypes.ROOF_TYPE_GABLE : s == RoofShape.Hip ? RoofTypes.ROOF_TYPE_HIP : RoofTypes.ROOF_TYPE_FLAT;

        /// <summary>
        /// The ridge relative to L (the W-E side): a gable whose ridge was chosen W-E runs along L, S-N across it. Left on
        /// Auto, and for a hip, the engine takes the longer side, as the model does.
        /// </summary>
        public static RidgeDirections Ridge(Roof r)
        {
            if (r.Shape != RoofShape.Gable || r.Ridge == RidgeAxis.Auto) return RidgeDirections.RIDGE_DIR_NONE;
            return r.Ridge == RidgeAxis.WestEast ? RidgeDirections.RIDGE_DIR_PARALLEL_TO_BLDGLENGTH : RidgeDirections.RIDGE_DIR_PERP_TO_BLDGLENGTH;
        }
    }
}
