using System.Collections.Generic;
using System.Linq;

namespace ASCE7WindLoadCalculator
{
    /// <summary>
    /// One face of a roof of several sections (owner 2026-11-13: hips, wings and dormers each take the wind by their own
    /// ridge), in the engine's frame: x along <see cref="BuildingData.BuildingLength"/> from the bounding box's south-west
    /// corner, y along its width, z above grade. The walls stay on the bounding box; only the roof is taken face by face.
    /// </summary>
    public sealed class RoofFacePlane
    {
        public string Id { get; set; }
        public string Name { get; set; }
        /// <summary>Its convex pieces on plan (inside the bounding box; the eaves are the overhang's, as for any roof).</summary>
        public List<List<Vec2>> Pieces { get; set; } = new List<List<Vec2>>();
        /// <summary>Its plane: z = Z0 + Zx x + Zy y (ft above grade).</summary>
        public double Z0 { get; set; }
        public double Zx { get; set; }
        public double Zy { get; set; }
        public double SlopeDeg { get; set; }
        /// <summary>Its own section's ridge runs along x (a shed's: its high edge).</summary>
        public bool RidgeAlongX { get; set; }
        /// <summary>
        /// h of its Fig. 27.3-1 values (ft): for the main section the building's (<see cref="BuildingData.MwfrsFigureHeight"/>);
        /// for a wing or dormer its own (ASCE gives none for them; owner 2026-11-13: "if not clear, use the wing's own").
        /// </summary>
        public double H { get; set; }
        /// <summary>Its section's plan size along x and along y (ft): L of its h/L (the main section: the building's).</summary>
        public double Lx { get; set; }
        public double Ly { get; set; }
        /// <summary>The section's name (e.g. "the roof", "east wing", "dormer").</summary>
        public string Section { get; set; }

        /// <summary>The same face with x and y swapped (the engine's turn of the building for the other wind).</summary>
        public RoofFacePlane Swapped() => new RoofFacePlane
        {
            Id = Id, Name = Name, Section = Section, Z0 = Z0, Zx = Zy, Zy = Zx, SlopeDeg = SlopeDeg, RidgeAlongX = !RidgeAlongX,
            H = H, Lx = Ly, Ly = Lx, Pieces = Pieces.Select(p => p.Select(v => new Vec2(v.Y, v.X)).ToList()).ToList(),
        };

        /// <summary>The same face seen with x reversed (x' = L − x): the wind from the other end, as the wind along +x'.</summary>
        public RoofFacePlane Mirrored(double L) => new RoofFacePlane
        {
            Id = Id, Name = Name, Section = Section, Z0 = Z0 + Zx * L, Zx = -Zx, Zy = Zy, SlopeDeg = SlopeDeg, RidgeAlongX = RidgeAlongX,
            H = H, Lx = Lx, Ly = Ly, Pieces = Pieces.Select(p => p.Select(v => new Vec2(L - v.X, v.Y)).Reverse().ToList()).ToList(),
        };
    }

    /// <summary>
    /// A wall standing on a lower roof (a dormer's front or cheek, a taller wing's gable over the main roof): owner
    /// 2026-11-13, the front takes the windward wall's pressure, the cheeks the side walls'. Its area and centroid on its
    /// vertical plane, in the engine's frame.
    /// </summary>
    public sealed class RoofWallPiece
    {
        public string Name { get; set; }
        /// <summary>Its outward normal on plan (a unit vector along ±x or ±y).</summary>
        public Vec2 Normal { get; set; }
        /// <summary>Its area (sq ft) and centroid (plan and height).</summary>
        public double Area { get; set; }
        public Vec2 Centroid { get; set; }
        public double CentroidZ { get; set; }

        public RoofWallPiece Swapped() => new RoofWallPiece
        {
            Name = Name, Area = Area, CentroidZ = CentroidZ, Normal = new Vec2(Normal.Y, Normal.X), Centroid = new Vec2(Centroid.Y, Centroid.X),
        };

        public RoofWallPiece Mirrored(double L) => new RoofWallPiece
        {
            Name = Name, Area = Area, CentroidZ = CentroidZ, Normal = new Vec2(-Normal.X, Normal.Y), Centroid = new Vec2(L - Centroid.X, Centroid.Y),
        };
    }
}
