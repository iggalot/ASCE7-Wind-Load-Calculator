using System;
using System.Collections.Generic;
using System.Linq;

namespace ASCE7WindLoadCalculator
{
    /// <summary>Which MWFRS procedure supplies the shear-wall loads.</summary>
    public enum LateralProcedure
    {
        /// <summary>Chapter 27, directional procedure (Fig. 27.3-8 load cases).</summary>
        Directional = 0,

        /// <summary>Chapter 28 Part 1, envelope procedure for low-rise buildings (Fig. 28.3-1).</summary>
        Envelope = 1,
    }

    /// <summary>
    /// One Fig. 28.3-1 zone (or the part of a zone on one side of the plan center) acting on the building, wind along +x of
    /// the calculation axes. Sign convention: p is positive toward the surface (push), negative away from it (suction);
    /// n is the outward unit normal; the force on the building is F = −p A n; r runs from the center of mass to the centroid
    /// of the zone, where the pressure resultant acts; Mz = (r × F)z = rx Fy − ry Fx, counterclockwise (seen from above) positive.
    /// </summary>
    public sealed class EnvelopeZoneForce
    {
        /// <summary>Zone label, e.g. "1", "1E", "2", "5E"; "P" for the parapets.</summary>
        public string Zone { get; internal set; }

        /// <summary>Where the zone acts, e.g. "Windward wall (x = 0)".</summary>
        public string Surface { get; internal set; }

        /// <summary>Extent of this row along the surface, e.g. "y = 8 to 20 ft".</summary>
        public string Extent { get; internal set; }

        /// <summary>External pressure coefficient; null for the parapet row (28.3.2 combined coefficient).</summary>
        public double? GCpf { get; internal set; }

        /// <summary>p = qh Kd (GCpf) (psf), + toward the surface, − away (suction).</summary>
        public double? Pressure { get; internal set; }

        /// <summary>True area of the zone (sq ft): wall elevation area, or roof area along the slope.</summary>
        public double Area { get; internal set; }

        /// <summary>Outward unit normal.</summary>
        public double Nx { get; internal set; }
        public double Ny { get; internal set; }
        public double Nz { get; internal set; }
        public string Normal => IsParapet ? "" : $"({Fmt(Nx)}, {Fmt(Ny)}, {Fmt(Nz)})";

        /// <summary>Centroid of the zone in plan (ft), from the reference corner (origin of the calculation axes).</summary>
        public double Xc { get; internal set; }
        public double Yc { get; internal set; }

        /// <summary>r = centroid − center of mass (ft).</summary>
        public double Rx { get; internal set; }
        public double Ry { get; internal set; }

        /// <summary>F = −p A n (kips).</summary>
        public double Fx { get; internal set; }
        public double Fy { get; internal set; }
        public double Fz { get; internal set; }

        /// <summary>Mz = rx Fy − ry Fx (kip-ft), CCW +.</summary>
        public double Mz { get; internal set; }

        /// <summary>Factor on this row in the torsional case: 1, or for a T zone beyond the plan center the T coefficient over
        /// the row's own (ASCE 7-16: 0.25, Fig. 28.3-1 note 5; ASCE 7-22: the printed Fig. 28.3-2 T value / <see cref="GCpf"/>).</summary>
        public double TorsionalFactor { get; internal set; } = 1.0;

        /// <summary>The T coefficient of a T zone (null otherwise): the printed Fig. 28.3-2 value (7-22) or 0.25 × GCpf (7-16).</summary>
        public double? TorsionalGCpf { get; internal set; }

        /// <summary>"×1", or the T zone and its coefficient, e.g. "1T ×0.25" (7-16) or "1T 0.10 (printed)" (7-22).</summary>
        public string TorsionalLabel { get; internal set; }

        /// <summary>The roof plane the row lies on (roof rows; null otherwise).</summary>
        public string RoofPlane { get; internal set; }

        /// <summary>Roof rows: the row's outline in plan (calculation axes), the zone's rectangle cut to one roof plane.</summary>
        internal List<Vec2> PlanPolygon { get; set; }

        public bool IsRoof { get; internal set; }
        public bool IsParapet { get; internal set; }

        /// <summary>Where the row lies, in the calculation axes. Walls: the side (0: x = 0, 1: x = D, 2: y = 0, 3: y = W) and
        /// the stretch [S0, S1] along it (y for sides 0 and 1, x for 2 and 3). Roofs: Side −1 and the plan rectangle
        /// [S0, S1] × [T0, T1] (x by y).</summary>
        internal int Side { get; set; } = -1;
        internal double S0 { get; set; }
        internal double S1 { get; set; }
        internal double T0 { get; set; }
        internal double T1 { get; set; }

        private static string Fmt(double v) => Math.Abs(v) < 5e-4 ? "0" : v.ToString("0.###");
    }

    /// <summary>
    /// Chapter 28 envelope results for one plan direction, in the calculation axes: x along the wind, y across it, z up,
    /// origin at the reference corner (windward wall at x = 0, end zones at y = 0), center of mass at (D/2, W/2).
    /// </summary>
    public sealed class EnvelopeDirection
    {
        public string Direction { get; internal set; }

        /// <summary>True when the wind is normal to the ridge (Load Case A at the roof angle); false parallel to it or on a flat roof.</summary>
        public bool Transverse { get; internal set; }

        /// <summary>"A" (transverse, at the roof angle, or a flat roof) or "B" (along the ridge, zones 5 and 6).</summary>
        public string LoadCase { get; internal set; }

        /// <summary>Roof angle the Case A coefficients were read at (deg): the roof angle across the ridge, and for a hip roof in
        /// both load cases; 0 for Case B on a gable or flat roof.</summary>
        public double Theta { get; internal set; }

        public double Qh { get; internal set; }
        public double Kd { get; internal set; }

        /// <summary>Zone width a (ft), Fig. 28.3-1 note 9.</summary>
        public double A { get; internal set; }

        /// <summary>Case A: how far from the windward eave a negative zone 2/2E coefficient applies, min(0.5 D, 2.5 he) (ft);
        /// beyond it, to the ridge, the zone 3/3E coefficient. 0 for Case B.</summary>
        public double Zone2Reach { get; internal set; }

        /// <summary>Plan dimension normal to the wind, W (ft).</summary>
        public double WidthNormalToWind { get; internal set; }

        /// <summary>Plan dimension parallel to the wind, D (ft).</summary>
        public double DepthAlongWind { get; internal set; }

        /// <summary>Center of mass taken at the plan center (ft, calculation axes).</summary>
        public double ComX { get; internal set; }
        public double ComY { get; internal set; }

        /// <summary>Every zone row, walls then roof then parapet. Zones that become T zones are split at the plan center.</summary>
        public List<EnvelopeZoneForce> Zones { get; } = new List<EnvelopeZoneForce>();

        /// <summary>ΣFx of the wall rows (kips), basic case.</summary>
        public double WallsKips { get; internal set; }

        /// <summary>ΣFx of the roof rows (kips), basic case; 0 when the roof is neglected.</summary>
        public double RoofKips { get; internal set; }

        /// <summary>True when the roof was left out because its ΣFx is negative (the shear may not be less than with the roof neglected).</summary>
        public bool RoofNeglected { get; internal set; }

        /// <summary>Windward + leeward parapet force (28.3.2), 0 without a parapet.</summary>
        public double ParapetKips { get; internal set; }

        /// <summary>Basic case: Vx = ΣFx (along the wind), Vy = ΣFy (across), Mz = ΣMz about the center of mass (CCW +).</summary>
        public double BasicKips { get; internal set; }
        public double BasicCrossKips { get; internal set; }
        public double BasicTorsion { get; internal set; }

        /// <summary>Torsional case (Fig. 28.3-1): every row times its torsional factor.</summary>
        public double TorsionalKips { get; internal set; }
        public double TorsionalCrossKips { get; internal set; }
        public double TorsionalTorsion { get; internal set; }
        public bool TorsionalRoofNeglected { get; internal set; }

        /// <summary>Width of the windward/leeward end zones at the reference corner (ft): 2a for Case A, a for 5E/6E.</summary>
        public double EndZoneWidth { get; internal set; }

        /// <summary>Width of the side-wall and roof end zones along the wind for Case B (1E/4E, 2E/3E): 2a (ft).</summary>
        public double SideEndZoneWidth { get; internal set; }

        /// <summary>Chapter 28 minimum (28.3.4 in 7-16, 28.3.6 in 7-22), a separate load case: 16 psf on walls (and parapet),
        /// 8 psf on the roof, projected.</summary>
        public double MinimumKips { get; internal set; }

        /// <summary>The figure height h used (ft): the mean roof height, or the eave height for θ ≤ 10° (Fig. 28.3-1 notation).</summary>
        public double H { get; internal set; }

        /// <summary>Kz used for qh (Chapter 28: 0.70 in Exposure B below 30 ft, Table 26.10-1 footnote).</summary>
        public double Kz { get; internal set; }

        /// <summary>True for a hip roof (Fig. C28.3-3 in 7-22, C28.3-2 in 7-16): the roof takes the Case A (Load Case 1) table at
        /// the roof angle in both load cases.</summary>
        public bool HipRoof { get; internal set; }

        /// <summary>True when the T zones take the printed ASCE 7-22 Fig. 28.3-2 coefficients; false for 25% (ASCE 7-16).</summary>
        public bool PrintedTorsional { get; internal set; }

        /// <summary>Code notes on how this direction was formed (flat-roof ridge, hip roof, zone width exception, Kz, parapets).</summary>
        public List<string> Notes { get; } = new List<string>();
        public double ProjectedWallArea { get; internal set; }
        public double ProjectedRoofArea { get; internal set; }
    }

    /// <summary>
    /// Chapter 28 Part 1, envelope procedure for low-rise buildings (ASCE 7-16 and 7-22): p = qh Kd (GCpf), Fig. 28.3-1 and
    /// Fig. 28.3-2. Internal pressure acts equally on opposite faces and cancels in the horizontal force of an enclosed
    /// building. Each zone gets its own pressure, area, outward normal and centroid; the force F = −p A n is summed as a vector
    /// and its moment taken about the center of mass, so opposite walls cancel only in the sums, with their signs.
    /// </summary>
    public static class EnvelopeLateralLoads
    {
        public const double TorsionalFraction = 0.25;   // ASCE 7-16 Fig. 28.3-1 note 5: T zones at 25% of the full pressures

        // ASCE 7-22 Fig. 28.3-2, Load Case 3 (torsional Case A): roof angle (deg) -> zones 1T, 2T, 3T, 4T, as printed. Linear between rows.
        private static readonly (double Theta, double[] G)[] CaseAT =
        {
            (0,  new[] { 0.10, -0.17, -0.09, -0.07 }),
            (5,  new[] { 0.10, -0.17, -0.09, -0.07 }),
            (20, new[] { 0.13, -0.17, -0.12, -0.11 }),
            (30, new[] { 0.14,  0.05, -0.11, -0.09 }),
            (45, new[] { 0.14,  0.05, -0.11, -0.09 }),
            (90, new[] { 0.14,  0.14, -0.09, -0.09 }),
        };

        /// <summary>ASCE 7-22 Fig. 28.3-2, Load Case 4 (torsional Case B): zones 5T and 6T, as printed.</summary>
        public static readonly double GCpf5T = 0.10, GCpf6T = -0.07;

        /// <summary>ASCE 7-22 Fig. 28.3-2 Load Case 3 T coefficients at roof angle theta (deg), zones 1T, 2T, 3T, 4T.</summary>
        public static double[] CaseATCoefficients(double theta) => Interpolate(CaseAT, theta);

        private static double[] Interpolate((double Theta, double[] G)[] table, double theta)
        {
            theta = Math.Max(0, Math.Min(90, theta));
            for (int i = 1; i < table.Length; i++)
            {
                if (theta > table[i].Theta) continue;
                var (t0, g0) = table[i - 1]; var (t1, g1) = table[i];
                double f = t1 > t0 ? (theta - t0) / (t1 - t0) : 0;
                return g0.Select((v, k) => v + f * (g1[k] - v)).ToArray();
            }
            return table[table.Length - 1].G.ToArray();
        }

        // Fig. 28.3-1 Load Case A: roof angle (deg) -> zones 1, 2, 3, 4, 1E, 2E, 3E, 4E. Linear between rows.
        private static readonly (double Theta, double[] G)[] CaseA =
        {
            (0,  new[] { 0.40, -0.69, -0.37, -0.29, 0.61, -1.07, -0.53, -0.43 }),
            (5,  new[] { 0.40, -0.69, -0.37, -0.29, 0.61, -1.07, -0.53, -0.43 }),
            (20, new[] { 0.53, -0.69, -0.48, -0.43, 0.80, -1.07, -0.69, -0.64 }),
            (30, new[] { 0.56,  0.21, -0.43, -0.37, 0.69,  0.27, -0.53, -0.48 }),
            (45, new[] { 0.56,  0.21, -0.43, -0.37, 0.69,  0.27, -0.53, -0.48 }),
            (90, new[] { 0.56,  0.56, -0.37, -0.37, 0.69,  0.69, -0.48, -0.48 }),
        };

        /// <summary>Fig. 28.3-1 Load Case B (all roof angles), windward and leeward end walls: 5, 6, 5E, 6E.</summary>
        public static readonly double GCpf5 = 0.40, GCpf6 = -0.29, GCpf5E = 0.61, GCpf6E = -0.43;

        /// <summary>Fig. 28.3-1 Load Case B, side walls 1 and 4 and roof 2 and 3, interior and end zones.</summary>
        public static readonly double GCpfB1 = -0.45, GCpfB1E = -0.48, GCpfB2 = -0.69, GCpfB2E = -1.07, GCpfB3 = -0.37, GCpfB3E = -0.53;

        /// <summary>Case A GCpf at roof angle theta (deg), zones 1, 2, 3, 4, 1E, 2E, 3E, 4E.</summary>
        public static double[] CaseACoefficients(double theta) => Interpolate(CaseA, theta);

        /// <summary>The least plan dimension above which the Fig. 28.3-1 note 9 exception caps a at 0.8h (θ 0 to 7°), ft.</summary>
        public const double LargeBuildingLeastDimension = 300;

        /// <summary>Zone width a (ft), Fig. 28.3-1 note 9 (7-16 and 7-22): 10% of the least horizontal dimension or 0.4h,
        /// whichever is smaller, but not less than 4% of the least horizontal dimension or 3 ft. Exception: for θ = 0 to 7°
        /// and a least horizontal dimension over 300 ft, a is limited to 0.8h at most. h is the figure height (the eave height
        /// for θ ≤ 10°, <see cref="BuildingData.MwfrsFigureHeight"/>).</summary>
        public static double ZoneWidthA(BuildingData b)
        {
            double least = Math.Min(b.BuildingLength, b.BuildingWidth), h = b.MwfrsFigureHeight;
            double a = Math.Max(Math.Min(0.1 * least, 0.4 * h), Math.Max(0.04 * least, 3.0));
            return LargeFlatException(b) ? Math.Min(a, 0.8 * h) : a;
        }

        /// <summary>True when the note 9 exception applies: θ = 0 to 7° and a least horizontal dimension over 300 ft.</summary>
        public static bool LargeFlatException(BuildingData b) =>
            (b.RoofTypeIsFlat() || b.RoofPitch <= 7) && Math.Min(b.BuildingLength, b.BuildingWidth) > LargeBuildingLeastDimension;

        /// <summary>Reasons Chapter 28 Part 1 does not apply to this building; empty when it does.</summary>
        public static List<string> Applicability(BuildingData b)
        {
            var why = new List<string>();
            double least = Math.Min(b.BuildingLength, b.BuildingWidth);
            if (b.MeanRoofHeight > 60) why.Add($"mean roof height {b.MeanRoofHeight:0.#} ft is over 60 ft (low-rise limit, 26.2)");
            if (b.MeanRoofHeight > least) why.Add($"mean roof height {b.MeanRoofHeight:0.#} ft exceeds the least plan dimension {least:0.#} ft (low-rise, 26.2)");
            if (b.EnclosureType == BuildingEnclosures.BLDG_OPEN) why.Add("open buildings are not covered by Chapter 28 Part 1");
            return why;
        }

        /// <summary>
        /// The Fig. 28.3-1 coefficient sets a wind along +x of <paramref name="b"/> (its own frame) takes.
        /// A gable or hip roof takes Case A (Load Case 1) across the ridge and Case B (Load Case 2) along it. A flat roof is run
        /// as though it had a ridge at mid-building, once with the ridge parallel to the long side and once parallel to the
        /// short side (as the user directed, following the commentary's assumed wind directions): each plan direction is then
        /// across one ridge and along the other, so it takes Case A (read at the 0-5° row) and Case B.
        /// </summary>
        public static string[] CoefficientCases(BuildingData b)
        {
            if (b.RoofTypeIsFlat()) return new[] { "A", "B" };
            return new[] { b.RidgeDirection == RidgeDirections.RIDGE_DIR_PERP_TO_BLDGLENGTH ? "A" : "B" };
        }

        /// <param name="coefficientCase">"A" or "B"; null for the first of <see cref="CoefficientCases"/>.</param>
        public static EnvelopeDirection Direction(WindLoadCalculator_MWFRS mwfrs, string direction, string coefficientCase = null)
        {
            var b = mwfrs.buildingData;
            var model = new WindGeometry(b);
            double W = b.BuildingWidth, D = b.BuildingLength;
            var edition = mwfrs.Edition;
            var exposure = mwfrs.Parameters.ExposureCategory;
            // h of Fig. 28.3-1 (eave height for θ ≤ 10°) and Kz per Table 26.10-1 footnote a (0.70 in Exposure B below 30 ft)
            double h = b.MwfrsFigureHeight, kz = mwfrs.GetKzEnvelope(h, exposure);
            double qh = mwfrs.CalculateDynamicWindPressureEnvelope(h), kd = mwfrs.PressureKd, q = qh * kd;
            double a = ZoneWidthA(b);
            bool hip = b.RoofType == RoofTypes.ROOF_TYPE_HIP, sloped = b.RoofTypeIsSloped();
            bool transverse = sloped && b.RidgeDirection == RidgeDirections.RIDGE_DIR_PERP_TO_BLDGLENGTH; // ridge across the wind
            if (coefficientCase == null) coefficientCase = CoefficientCases(b)[0];
            if (sloped && coefficientCase != (transverse ? "A" : "B"))
                throw new ArgumentException($"This roof takes Case {(transverse ? "A" : "B")} in this direction.", nameof(coefficientCase));
            bool caseB = coefficientCase == "B";
            // Case A is read at the roof angle across the ridge; a hip roof's roof reads the Case A table at its angle in Load
            // Case 2 as well (Fig. C28.3-3 note 1 in 7-22, C28.3-2 in 7-16)
            double theta = sloped && (transverse || hip) ? b.RoofPitch : 0;
            double cx = D / 2, cy = W / 2;
            bool printed = edition.PrintedTorsionalTable;
            var tA = CaseATCoefficients(theta);

            var r = new EnvelopeDirection
            {
                Direction = direction, Transverse = transverse, LoadCase = caseB ? "B" : "A", Theta = theta,
                Qh = qh, Kd = kd, A = a, WidthNormalToWind = W, DepthAlongWind = D, ComX = cx, ComY = cy,
                H = h, Kz = kz, HipRoof = hip, PrintedTorsional = printed,
            };

            bool eave = b.RoofTypeIsFlat() || b.RoofPitch <= 10;
            r.Notes.Add($"h = {h:0.##} ft, the {(eave ? "eave height (θ ≤ 10°)" : "mean roof height")} (Fig. 28.3-1 notation, {edition.Name}).");
            if (exposure == WindExposureCategories.WIND_EXP_CAT_B && h < 30)
                r.Notes.Add($"Kz = {WindLoadCalculator_Base.EnvelopeMinimumKzExposureB:0.00} at h = {h:0.##} ft (Table 26.10-1 footnote a: Chapter 28, Exposure B, z < 30 ft).");
            if (LargeFlatException(b))
                r.Notes.Add($"a is limited to 0.8h = {0.8 * h:0.##} ft (Fig. 28.3-1 note 9 exception: θ 0 to 7°, least horizontal dimension over {LargeBuildingLeastDimension:0} ft).");
            if (b.RoofTypeIsFlat())
                r.Notes.Add("Flat roof: run as though it had a ridge at mid-building, once parallel to the long side and once parallel to the short side, so each direction takes Case A (0-5° row) and Case B.");
            if (hip)
                r.Notes.Add($"Hip roof ({edition.EnvelopeHipFigure}, note 1): Load Case {(caseB ? 2 : 1)} zones; the roof takes the Case A (Load Case 1) coefficients at θ = {theta:0.#}° in both load cases.");
            r.Notes.Add(printed
                ? "Torsional case: T zones take the printed Fig. 28.3-2 coefficients (ASCE 7-22)."
                : "Torsional case: T zones at 25% of the full design pressures (Fig. 28.3-1 note 5, ASCE 7-16).");
            if (b.HasParapet)
                r.Notes.Add($"Parapets: the Chapter 28 parapet provisions ({edition.EnvelopeParapetSection}) are not covered here. The parapet force carried is the Chapter 27 value (27.3.4, GCpn +1.5 / −1.0 at qp), one resultant with no zones and no torsional reduction.");

            // the T coefficient printed for a zone (7-22): 1T-4T of Load Case 3 at θ, 5T and 6T of Load Case 4
            double? PrintedT(string zone)
            {
                switch (zone.Replace("E", ""))
                {
                    case "1": return tA[0];
                    case "2": return tA[1];
                    case "3": return tA[2];
                    case "4": return tA[3];
                    case "5": return GCpf5T;
                    case "6": return GCpf6T;
                    default: return null;
                }
            }

            // End zones at the reference corner: 2a for Case A (1E/4E, 2E/3E); a for the end walls along the ridge (5E/6E,
            // Fig. 28.3-2 Cases 2 and 4, 7-16 and 7-22 alike, confirmed by the user); 2a along the wind for the Case B side
            // walls and roof (1E/4E, 2E/3E).
            double end = Math.Min((caseB ? 1 : 2) * a, W), sideEnd = Math.Min(2 * a, D);
            r.EndZoneWidth = end;
            r.SideEndZoneWidth = sideEnd;

            // tZone: the row lies beyond the plan center and becomes a T zone in the torsional case
            void AddRow(string zone, string surface, string extent, double gcpf, double area, double nx, double ny, double nz,
                        double xc, double yc, bool tZone, bool roof)
            {
                double p = q * gcpf, f = -p * area / 1000.0;           // F = −p A n
                double factor = 1.0; double? gT = null; string label = "×1";
                if (tZone)
                {
                    string t = zone.Replace("E", "") + "T";
                    if (printed && PrintedT(zone) is double pt && gcpf != 0)
                    {
                        gT = pt; factor = pt / gcpf; label = $"{t} {pt:0.00} (Fig. 28.3-2)";
                    }
                    else
                    {
                        gT = TorsionalFraction * gcpf; factor = TorsionalFraction; label = $"{t} ×{TorsionalFraction:0.##}";
                    }
                }
                var z = new EnvelopeZoneForce
                {
                    Zone = zone, Surface = surface, Extent = extent, GCpf = gcpf, Pressure = p, Area = area,
                    Nx = nx + 0.0, Ny = ny + 0.0, Nz = nz + 0.0, Xc = xc, Yc = yc, Rx = xc - cx, Ry = yc - cy,
                    Fx = f * nx + 0.0, Fy = f * ny + 0.0, Fz = f * nz + 0.0,
                    TorsionalFactor = factor, TorsionalGCpf = gT, TorsionalLabel = label, IsRoof = roof,
                };
                z.Mz = z.Rx * z.Fy - z.Ry * z.Fx + 0.0;
                r.Zones.Add(z);
            }

            // Pieces of [s0, s1] split at the plan center (y = W/2) when the part beyond becomes a T zone.
            IEnumerable<(double s0, double s1, bool t)> Pieces(double s0, double s1, bool splitT)
            {
                if (s1 - s0 < 1e-9) yield break;
                if (!splitT || s1 <= cy + 1e-9) { yield return (s0, s1, false); yield break; }
                if (s0 >= cy - 1e-9) { yield return (s0, s1, true); yield break; }
                yield return (s0, cy, false);
                yield return (cy, s1, true);
            }

            // side 0: x = 0 (n = −x), 1: x = D (n = +x), 2: y = 0 (n = −y), 3: y = W (n = +y)
            void Wall(int side, string zone, string surface, double gcpf, double s0, double s1, bool splitT)
            {
                double L = side < 2 ? W : D;
                s1 = Math.Min(s1, L);
                Func<double, double> ht = side == 0 ? (s => model.RoofHeightAt(0, s)) : side == 1 ? (s => model.RoofHeightAt(D, s))
                                        : side == 2 ? (s => model.RoofHeightAt(s, 0)) : (Func<double, double>)(s => model.RoofHeightAt(s, W));
                double nx = side == 0 ? -1 : side == 1 ? 1 : 0, ny = side == 2 ? -1 : side == 3 ? 1 : 0;
                foreach (var (p0, p1, t) in Pieces(s0, s1, splitT && side < 2))
                {
                    var (area, sc) = Integrate(ht, p0, p1, L / 2);
                    double xc = side == 0 ? 0 : side == 1 ? D : sc, yc = side == 2 ? 0 : side == 3 ? W : sc;
                    AddRow(zone, surface, $"{(side < 2 ? "y" : "x")} = {p0:0.##} to {p1:0.##} ft", gcpf, area, nx, ny, 0, xc, yc, t, false);
                    var row = r.Zones[r.Zones.Count - 1];
                    row.Side = side; row.S0 = p0; row.S1 = p1;
                }
            }

            // Roof zone over the plan rectangle [x0, x1] × [y0, y1], cut by each roof plane: one row per plane it covers, with
            // that plane's slope, outward normal, true area (plan / nz) and centroid. On a gable or flat roof the zone
            // rectangles already lie on one plane; on a hip roof they cross the hip lines (Fig. C28.3-3 / C28.3-2).
            var planes = model.Roofs.ToList();
            void Roof(string zone, string surface, double gcpf, double x0, double x1, double y0, double y1, bool splitT)
            {
                x1 = Math.Min(x1, D); y1 = Math.Min(y1, W);
                if (x1 - x0 < 1e-9) return;
                foreach (var (p0, p1, t) in Pieces(y0, y1, splitT))
                {
                    var rect = new List<Vec2> { new Vec2(x0, p0), new Vec2(x1, p0), new Vec2(x1, p1), new Vec2(x0, p1) };
                    foreach (var plane in planes)
                    {
                        var poly = PolygonOps.ClipToConvex(rect, PolygonOps.CounterClockwise(plane.PlanRegion.ToList()));
                        double plan = poly.Count >= 3 ? Math.Abs(PolygonOps.SignedArea(poly)) : 0;
                        if (plan < 1e-6) continue;
                        var c = WindCaseEnvelope.Centroid(poly);
                        var n = plane.Normal;                      // outward (upward) unit normal of the plane
                        string on = planes.Count > 1 ? $", {plane.Name}" : "";
                        AddRow(zone, surface + on, $"x = {x0:0.##} to {x1:0.##}, y = {p0:0.##} to {p1:0.##} ft", gcpf, plan / n.Z,
                               n.X, n.Y, n.Z, c.X, c.Y, t, true);
                        var row = r.Zones[r.Zones.Count - 1];
                        row.S0 = x0; row.S1 = x1; row.T0 = p0; row.T1 = p1;
                        row.RoofPlane = plane.Name;
                        row.PlanPolygon = PolygonOps.CounterClockwise(poly);
                    }
                }
            }

            if (!caseB)
            {
                // Case A: windward wall 1/1E, leeward wall 4/4E, roof 2/2E (windward half, eave to ridge) and 3/3E (leeward
                // half). T zones (torsional case) beyond the plan center. The end walls carry no Case A zone.
                var g = CaseACoefficients(theta);
                double ww = g[0], r2 = g[1], r3 = g[2], lw = g[3], wwE = g[4], r2E = g[5], r3E = g[6], lwE = g[7];
                Wall(0, "1E", "Windward wall (x = 0), end zone", wwE, 0, end, true);
                Wall(0, "1", "Windward wall (x = 0)", ww, end, W, true);
                Wall(1, "4E", "Leeward wall (x = D), end zone", lwE, 0, end, true);
                Wall(1, "4", "Leeward wall (x = D)", lw, end, W, true);
                // Fig. 28.3-1 note (7-10 note 8, 7-16 and 7-22 alike): a negative GCpf in zone 2 or 2E applies only for a
                // distance from the roof edge of min(0.5 × the dimension parallel to the wind, 2.5 he); the rest of the windward
                // slope up to the ridge takes the zone 3 or 3E coefficient. 0.5 D is the ridge, so only 2.5 he can cut it short.
                double reach = Math.Min(0.5 * D, 2.5 * b.BuildingHeight);
                r.Zone2Reach = reach;
                void Windward(string zone, string zone3, double g2, double g3, double y0, double y1, string surface)
                {
                    if (g2 >= 0 || reach >= cx - 1e-9) { Roof(zone, surface, g2, 0, cx, y0, y1, true); return; }
                    Roof(zone, surface, g2, 0, reach, y0, y1, true);
                    Roof(zone3, surface + $" beyond {reach:0.##} ft (2.5 he), zone {zone3} coefficient", g3, reach, cx, y0, y1, true);
                }
                Windward("2E", "3E", r2E, r3E, 0, end, "Windward roof, end zone");
                Windward("2", "3", r2, r3, end, W, "Windward roof");
                Roof("3E", "Leeward roof, end zone", r3E, cx, D, 0, end, true);
                Roof("3", "Leeward roof", r3, cx, D, end, W, true);
            }
            else
            {
                // Case B, wind along the ridge: windward end wall 5/5E (5E a wide), leeward end wall 6/6E, side wall at the
                // reference corner 1/1E and the far side wall 4/4E (end zones 2a from the windward end), roof halves 2/2E (next
                // to wall 1) and 3/3E. T zones: 5T/6T beyond the plan center. A hip roof (Load Case 2 of the commentary figure)
                // keeps this layout but its roof takes the Case A table at the roof angle (zone 2/2E next to wall 1, 3/3E beyond).
                var g = CaseACoefficients(theta);
                double b2 = hip ? g[1] : GCpfB2, b2E = hip ? g[5] : GCpfB2E, b3 = hip ? g[2] : GCpfB3, b3E = hip ? g[6] : GCpfB3E;
                Wall(0, "5E", "Windward end wall (x = 0), end zone", GCpf5E, 0, end, true);
                Wall(0, "5", "Windward end wall (x = 0)", GCpf5, end, W, true);
                Wall(1, "6E", "Leeward end wall (x = D), end zone", GCpf6E, 0, end, true);
                Wall(1, "6", "Leeward end wall (x = D)", GCpf6, end, W, true);
                Wall(2, "1E", "Side wall (y = 0), end zone", GCpfB1E, 0, sideEnd, false);
                Wall(2, "1", "Side wall (y = 0)", GCpfB1, sideEnd, D, false);
                Wall(3, "4E", "Side wall (y = W), end zone", GCpfB1E, 0, sideEnd, false);
                Wall(3, "4", "Side wall (y = W)", GCpfB1, sideEnd, D, false);
                Roof("2E", "Roof half next to wall 1, end zone", b2E, 0, sideEnd, 0, cy, false);
                Roof("2", "Roof half next to wall 1", b2, sideEnd, D, 0, cy, false);
                Roof("3E", "Roof half next to wall 4, end zone", b3E, 0, sideEnd, cy, W, false);
                Roof("3", "Roof half next to wall 4", b3, sideEnd, D, cy, W, false);
            }

            // parapets: the Chapter 28 parapet provisions are not covered (see Notes); the Chapter 27 force (27.3.4) is carried:
            // windward and leeward forces along the wind over the full width; resultant on y = W/2, no moment
            r.ParapetKips = mwfrs.ParapetKips;
            if (r.ParapetKips != 0)
                r.Zones.Add(new EnvelopeZoneForce
                {
                    Zone = "P", Surface = $"Parapets, windward + leeward (Chapter 27 value, 27.3.4; Chapter 28 {edition.EnvelopeParapetSection} not covered)", Extent = $"y = 0 to {W:0.##} ft",
                    Area = 2 * b.ParapetHeight * W, Xc = cx, Yc = cy, Fx = r.ParapetKips, IsParapet = true, TorsionalLabel = "×1",
                });

            // resultants; the roof is left out of a case when its ΣFx is negative (the shear may not be less than without it)
            double Roofx(bool torsional) => r.Zones.Where(z => z.IsRoof).Sum(z => z.Fx * (torsional ? z.TorsionalFactor : 1));
            r.RoofNeglected = Roofx(false) < 0;
            r.TorsionalRoofNeglected = Roofx(true) < 0;
            (double vx, double vy, double mz) Sum(bool torsional, bool neglectRoof)
            {
                var rows = r.Zones.Where(z => !(z.IsRoof && neglectRoof)).ToList();
                Func<EnvelopeZoneForce, double> k = z => torsional ? z.TorsionalFactor : 1.0;
                return (rows.Sum(z => z.Fx * k(z)) + 0.0, rows.Sum(z => z.Fy * k(z)) + 0.0, rows.Sum(z => z.Mz * k(z)) + 0.0);
            }
            (r.BasicKips, r.BasicCrossKips, r.BasicTorsion) = Sum(false, r.RoofNeglected);
            (r.TorsionalKips, r.TorsionalCrossKips, r.TorsionalTorsion) = Sum(true, r.TorsionalRoofNeglected);
            r.WallsKips = r.Zones.Where(z => !z.IsRoof && !z.IsParapet).Sum(z => z.Fx);
            r.RoofKips = r.RoofNeglected ? 0 : Roofx(false);

            var (wallArea, roofArea) = WindLateralLoads.ProjectedAreas(model);
            if (b.HasParapet) wallArea += b.ParapetHeight * W;
            r.ProjectedWallArea = wallArea;
            r.ProjectedRoofArea = roofArea;
            r.MinimumKips = (WindLateralLoads.MinimumWallPressure * wallArea + WindLateralLoads.MinimumRoofPressure * roofArea) / 1000.0;
            return r;
        }

        /// <summary>
        /// Area under h(s) over [s0, s1] and the centroid of that area along s. h is linear on each side of <paramref name="peak"/>
        /// (flat top, or a gable end), so Simpson's rule on each piece is exact.
        /// </summary>
        internal static (double area, double centroid) Integrate(Func<double, double> h, double s0, double s1, double peak)
        {
            double A = 0, M = 0;
            var cuts = peak > s0 && peak < s1 ? new[] { s0, peak, s1 } : new[] { s0, s1 };
            for (int i = 0; i + 1 < cuts.Length; i++)
            {
                double u = cuts[i], v = cuts[i + 1], m = (u + v) / 2, w = (v - u) / 6;
                A += w * (h(u) + 4 * h(m) + h(v));
                M += w * (u * h(u) + 4 * m * h(m) + v * h(v));
            }
            return (A, A > 0 ? M / A : (s0 + s1) / 2);
        }

        /// <summary>
        /// Each building corner in turn as the reference corner (Fig. 28.3-1), for one wind axis, in the calculation axes.
        /// From (Vx, Vy, Mz) worked out with the reference corner at (0, 0) and the wind along +x:
        /// corner (0, W): mirror y → W − y, so ry and Fy change sign and Mz = rx Fy − ry Fx changes sign: (Vx, −Vy, −Mz);
        /// corner (D, W): the building turned 180°, wind along −x: r and F both change sign, Mz does not: (−Vx, −Vy, Mz);
        /// corner (D, 0): turned and mirrored: (−Vx, Vy, −Mz).
        /// </summary>
        public static IEnumerable<(string label, double vx, double vy, double mz)> Corners(double vx, double vy, double mz)
        {
            yield return ("+ wind, ref. corner (0, 0)", vx, vy, mz);
            yield return ("+ wind, ref. corner (0, W)", vx, -vy + 0.0, -mz + 0.0);
            yield return ("− wind, ref. corner (D, W)", -vx + 0.0, -vy + 0.0, mz);
            yield return ("− wind, ref. corner (D, 0)", -vx + 0.0, vy, -mz + 0.0);
        }

        /// <summary>
        /// Calculation axes to building axes: along the length they coincide; along the width the axes are turned 90° CCW,
        /// so (Vx, Vy, Mz) → (−Vy, Vx, Mz).
        /// </summary>
        public static (double vx, double vy, double mt) ToBuilding(bool alongLength, double vx, double vy, double mz) =>
            alongLength ? (vx, vy, mz) : (-vy + 0.0, vx, mz);

        /// <summary>
        /// Chapter 28 load cases for the shear walls: per wind axis, each of the four corners as the reference corner (the wind
        /// from either side), the basic case and (unless exempt) the torsional case, and the minimum (28.3.4 in 7-16, 28.3.6 in 7-22) from either side as a separate load case.
        /// </summary>
        /// <param name="torsionalExempt">28.3.2.1 exceptions: one-story buildings with h &lt;= 30 ft, one- and two-story light-frame buildings, or
        /// one- and two-story buildings with flexible diaphragms need not be designed for the torsional load cases.</param>
        public static WindLateralResult Compute(WindAnalysisResult analysis, bool torsionalExempt)
        {
            if (!(analysis?.MwfrsAlongLength is WindLoadCalculator_MWFRS mx) || !(analysis.MwfrsAlongWidth is WindLoadCalculator_MWFRS my))
                throw new ArgumentException("Both MWFRS directions are needed.", nameof(analysis));

            var ex = Direction(mx, "along length");
            var ey = Direction(my, "along width");
            var r = new WindLateralResult
            {
                Procedure = LateralProcedure.Envelope,
                EnvelopeAlongLength = ex, EnvelopeAlongWidth = ey,
                AlongLength = Summary(ex), AlongWidth = Summary(ey),
                OnlyCases1And3 = torsionalExempt,
            };

            foreach (var (e0, isX) in new[] { (ex, true), (ey, false) })
            {
                string dir = isX ? "along length" : "along width";
                var calc = isX ? mx : my;
                foreach (var set in CoefficientCases(calc.buildingData))
                {
                    var e = set == e0.LoadCase ? e0 : Direction(calc, dir, set);   // a flat roof: Case A and Case B each way
                    r.EnvelopeDirections.Add(e);
                    void Cases(int n, string kind, double vx, double vy, double mz)
                    {
                        foreach (var c in Corners(vx, vy, mz))
                        {
                            var (bx, by, mt) = ToBuilding(isX, c.vx, c.vy, c.mz);
                            r.LoadCases.Add(new WindLoadCase { CaseNumber = n, Name = $"Ch 28 {kind}, {dir}, {c.label}", Vx = bx, Vy = by, Mt = mt });
                        }
                    }
                    Cases(1, $"basic {e.LoadCase}", e.BasicKips, e.BasicCrossKips, e.BasicTorsion);
                    if (!torsionalExempt) Cases(2, $"torsional {e.LoadCase}", e.TorsionalKips, e.TorsionalCrossKips, e.TorsionalTorsion);
                }
            }
            // the minimum (28.3.4 in 7-16, 28.3.6 in 7-22) as a separate load case in addition to the others (C28.3.4 / C28.3.6)
            WindLateralLoads.AddMinimumCases(r.LoadCases, ex.MinimumKips, ey.MinimumKips, "Ch 28, " + mx.Edition.EnvelopeMinimumSection);
            return r;
        }

        private static DirectionalWindForce Summary(EnvelopeDirection e) => new DirectionalWindForce
        {
            Direction = e.Direction,
            WallsKips = e.WallsKips,
            RoofKips = e.RoofKips,
            ParapetKips = e.ParapetKips,
            MinimumKips = e.MinimumKips,
            ProjectedWallArea = e.ProjectedWallArea,
            ProjectedRoofArea = e.ProjectedRoofArea,
            WidthNormalToWind = e.WidthNormalToWind,
        };
    }
}
