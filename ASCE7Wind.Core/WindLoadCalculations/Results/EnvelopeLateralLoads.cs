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

    /// <summary>One Fig. 28.3-1 zone acting on the building for one wind direction (one reference corner).</summary>
    public sealed class EnvelopeZoneForce
    {
        /// <summary>Zone label, e.g. "1", "1E", "2", "5E".</summary>
        public string Zone { get; internal set; }

        /// <summary>Where the zone acts, e.g. "windward wall, end zone".</summary>
        public string Surface { get; internal set; }

        public double GCpf { get; internal set; }

        /// <summary>p = qh Kd (GCpf), external only (psf, positive toward the surface).</summary>
        public double Pressure { get; internal set; }

        /// <summary>Area projected on a vertical plane normal to the wind (ft²).</summary>
        public double ProjectedArea { get; internal set; }

        /// <summary>Horizontal force in the wind direction (kips).</summary>
        public double Kips { get; internal set; }
    }

    /// <summary>Chapter 28 envelope results for one wind direction.</summary>
    public sealed class EnvelopeDirection
    {
        public string Direction { get; internal set; }

        /// <summary>True when the wind is normal to the ridge (Load Case A at the roof angle); false parallel to it or on a flat roof.</summary>
        public bool Transverse { get; internal set; }

        /// <summary>"A" (transverse, at the roof angle) or "B" (longitudinal, zones 5 and 6).</summary>
        public string LoadCase { get; internal set; }

        /// <summary>Roof angle the Case A coefficients were read at (deg); 0 for Case B.</summary>
        public double Theta { get; internal set; }

        public double Qh { get; internal set; }
        public double Kd { get; internal set; }

        /// <summary>Zone width a (ft), Fig. 28.3-1 note 9; the end zones are 2a wide.</summary>
        public double A { get; internal set; }

        /// <summary>Plan dimension normal to the wind (ft).</summary>
        public double WidthNormalToWind { get; internal set; }

        /// <summary>Plan dimension parallel to the wind (ft).</summary>
        public double DepthAlongWind { get; internal set; }

        /// <summary>Zone forces for the basic case, reference corner at one end of the windward wall.</summary>
        public List<EnvelopeZoneForce> Zones { get; } = new List<EnvelopeZoneForce>();

        public double WallsKips { get; internal set; }

        /// <summary>Horizontal component of the roof pressures (kips); 0 when the roof is neglected.</summary>
        public double RoofKips { get; internal set; }

        /// <summary>True when the roof was left out because it reduced the total (Fig. 28.3-1 note: shear not less than without the roof).</summary>
        public bool RoofNeglected { get; internal set; }

        /// <summary>Windward + leeward parapet force (28.3.2), 0 without a parapet.</summary>
        public double ParapetKips { get; internal set; }

        public double BasicKips => WallsKips + RoofKips + ParapetKips;

        /// <summary>Torsion of the basic case about the plan center (kip-ft, magnitude): the end zones are at one end.</summary>
        public double BasicTorsion { get; internal set; }

        /// <summary>Torsional case (Fig. 28.3-1): zones on the half away from the reference corner at 25%.</summary>
        public double TorsionalKips { get; internal set; }
        public double TorsionalTorsion { get; internal set; }

        /// <summary>28.3.4 minimum, a separate load case: 16 psf on walls (and parapet), 8 psf on the roof, projected.</summary>
        public double MinimumKips { get; internal set; }
        public double ProjectedWallArea { get; internal set; }
        public double ProjectedRoofArea { get; internal set; }
    }

    /// <summary>
    /// Chapter 28 Part 1, envelope procedure for low-rise buildings (ASCE 7-16 and 7-22): p = qh Kd [(GCpf) - (GCpi)],
    /// Fig. 28.3-1. Internal pressure cancels in the horizontal force on an enclosed building. For each plan direction the
    /// windward and leeward walls (and the roof's horizontal component across the ridge) are integrated in strips along the
    /// windward wall, with the end zones (2a wide) at the reference corner; each corner in turn is covered by sending both
    /// torsion signs.
    /// </summary>
    public static class EnvelopeLateralLoads
    {
        public const double TorsionalFraction = 0.25;   // Fig. 28.3-1 torsional load cases: T zones at 25%
        private const int Strips = 1200;

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

        /// <summary>Case A GCpf at roof angle theta (deg), zones 1, 2, 3, 4, 1E, 2E, 3E, 4E.</summary>
        public static double[] CaseACoefficients(double theta)
        {
            theta = Math.Max(0, Math.Min(90, theta));
            for (int i = 1; i < CaseA.Length; i++)
            {
                if (theta > CaseA[i].Theta) continue;
                var (t0, g0) = CaseA[i - 1]; var (t1, g1) = CaseA[i];
                double f = t1 > t0 ? (theta - t0) / (t1 - t0) : 0;
                return g0.Select((v, k) => v + f * (g1[k] - v)).ToArray();
            }
            return CaseA[CaseA.Length - 1].G.ToArray();
        }

        /// <summary>Zone width a (ft), Fig. 28.3-1 note 9: 10% of the least horizontal dimension or 0.4h, whichever is
        /// smaller, but not less than 4% of the least horizontal dimension or 3 ft.</summary>
        public static double ZoneWidthA(BuildingData b)
        {
            double least = Math.Min(b.BuildingLength, b.BuildingWidth);
            return Math.Max(Math.Min(0.1 * least, 0.4 * b.MeanRoofHeight), Math.Max(0.04 * least, 3.0));
        }

        /// <summary>Reasons Chapter 28 Part 1 does not apply to this building; empty when it does.</summary>
        public static List<string> Applicability(BuildingData b)
        {
            var why = new List<string>();
            double least = Math.Min(b.BuildingLength, b.BuildingWidth);
            if (b.MeanRoofHeight > 60) why.Add($"mean roof height {b.MeanRoofHeight:0.#} ft is over 60 ft (low-rise limit, 26.2)");
            if (b.MeanRoofHeight > least) why.Add($"mean roof height {b.MeanRoofHeight:0.#} ft exceeds the least plan dimension {least:0.#} ft (low-rise, 26.2)");
            if (b.RoofType == RoofTypes.ROOF_TYPE_HIP) why.Add("hip roofs are not covered here (flat and gable roofs only)");
            if (b.EnclosureType == BuildingEnclosures.BLDG_OPEN) why.Add("open buildings are not covered by Chapter 28 Part 1");
            return why;
        }

        /// <summary>
        /// Envelope results for the wind along +x of <paramref name="mwfrs"/>'s building (its own frame: the windward wall
        /// spans BuildingWidth, the wind travels along BuildingLength).
        /// </summary>
        public static EnvelopeDirection Direction(WindLoadCalculator_MWFRS mwfrs, string direction)
        {
            var b = mwfrs.buildingData;
            var model = new BuildingModel(b);
            double W = b.BuildingWidth, D = b.BuildingLength, he = b.BuildingHeight;
            double qh = mwfrs.CalculateDynamicWindPressure(b.MeanRoofHeight), kd = mwfrs.PressureKd, q = qh * kd;
            double a = ZoneWidthA(b), end = Math.Min(2 * a, W);
            bool gable = b.RoofType == RoofTypes.ROOF_TYPE_GABLE;
            bool transverse = gable && b.RidgeDirection == RidgeDirections.RIDGE_DIR_PERP_TO_BLDGLENGTH; // ridge across the wind
            double theta = transverse ? b.RoofPitch : 0;

            var r = new EnvelopeDirection
            {
                Direction = direction, Transverse = transverse, LoadCase = transverse || !gable ? "A" : "B", Theta = theta,
                Qh = qh, Kd = kd, A = a, WidthNormalToWind = W, DepthAlongWind = D,
            };

            // coefficients: windward wall, leeward wall (interior, end); roof zones 2, 3 (interior, end) when transverse
            double ww, wwE, lw, lwE, r2 = 0, r2E = 0, r3 = 0, r3E = 0;
            string zWW, zLW;
            if (r.LoadCase == "A")
            {
                var g = CaseACoefficients(theta);
                ww = g[0]; r2 = g[1]; r3 = g[2]; lw = g[3]; wwE = g[4]; r2E = g[5]; r3E = g[6]; lwE = g[7];
                zWW = "1"; zLW = "4";
            }
            else
            {
                ww = GCpf5; lw = GCpf6; wwE = GCpf5E; lwE = GCpf6E;
                zWW = "5"; zLW = "6";
            }

            // roof across the ridge: windward slope plan depth D/2; a negative Zone 2 acts only up to min(0.5 D, 2.5 he)
            // from the windward edge, the rest of the windward slope takes Zone 3 (Fig. 28.3-1 note).
            double tan = transverse ? Math.Tan(theta * Math.PI / 180) : 0, half = D / 2;
            double Zone2Depth(double g2) => g2 < 0 ? Math.Min(half, Math.Min(0.5 * D, 2.5 * he)) : half;

            var acc = new Dictionary<string, EnvelopeZoneForce>();
            void Add(string zone, string surface, double gcpf, double projArea, double lbs)
            {
                if (!acc.TryGetValue(zone, out var z))
                    acc[zone] = z = new EnvelopeZoneForce { Zone = zone, Surface = surface, GCpf = gcpf, Pressure = q * gcpf };
                z.ProjectedArea += projArea; z.Kips += lbs / 1000.0;
            }

            // strips along the windward wall, reference corner at y = 0
            double dy = W / Strips, wallLbs = 0, roofLbs = 0, wallM = 0, roofM = 0, wallT = 0, roofT = 0, wallTM = 0, roofTM = 0;
            for (int i = 0; i < Strips; i++)
            {
                double y = (i + 0.5) * dy, arm = y - W / 2;       // force along +x at y; torsion taken as F * (y - W/2)
                bool isEnd = y < end;
                double hw = model.RoofHeightAt(0, y), area = hw * dy;
                double gW = isEnd ? wwE : ww, gL = isEnd ? lwE : lw;
                double fw = q * (gW - gL) * area;
                Add(zWW + (isEnd ? "E" : ""), "windward wall" + (isEnd ? ", end zone" : ""), gW, area, q * gW * area);
                Add(zLW + (isEnd ? "E" : ""), "leeward wall" + (isEnd ? ", end zone" : ""), gL, area, -q * gL * area);

                double fr = 0;
                if (transverse)
                {
                    double g2 = isEnd ? r2E : r2, g3 = isEnd ? r3E : r3, d2 = Zone2Depth(g2);
                    double rise2 = d2 * tan, rise3w = (half - d2) * tan, riseL = half * tan;
                    string s2 = isEnd ? "2E" : "2", s3 = isEnd ? "3E" : "3";
                    Add(s2, "windward roof" + (isEnd ? ", end zone" : ""), g2, rise2 * dy, q * g2 * rise2 * dy);
                    if (rise3w > 0) Add(s3 + " (windward)", "windward roof beyond the Zone 2 limit", g3, rise3w * dy, q * g3 * rise3w * dy);
                    Add(s3, "leeward roof" + (isEnd ? ", end zone" : ""), g3, riseL * dy, -q * g3 * riseL * dy);
                    fr = q * (g2 * rise2 + g3 * rise3w - g3 * riseL) * dy;
                }

                double t = y > W / 2 ? TorsionalFraction : 1.0;  // torsional case: far half at 25%
                wallLbs += fw; roofLbs += fr; wallM += fw * arm; roofM += fr * arm;
                wallT += fw * t; roofT += fr * t; wallTM += fw * t * arm; roofTM += fr * t * arm;
            }

            r.Zones.AddRange(acc.Values);
            r.RoofNeglected = roofLbs < 0;
            bool useRoof = !r.RoofNeglected;
            r.WallsKips = wallLbs / 1000.0;
            r.RoofKips = useRoof ? roofLbs / 1000.0 : 0;
            r.ParapetKips = mwfrs.ParapetKips;
            r.BasicTorsion = Math.Abs(wallM + (useRoof ? roofM : 0)) / 1000.0;
            r.TorsionalKips = (wallT + (useRoof ? roofT : 0)) / 1000.0 + r.ParapetKips;
            r.TorsionalTorsion = Math.Abs(wallTM + (useRoof ? roofTM : 0)) / 1000.0;

            var (wallArea, roofArea) = WindLateralLoads.ProjectedAreas(model);
            if (b.HasParapet) wallArea += b.ParapetHeight * W;
            r.ProjectedWallArea = wallArea;
            r.ProjectedRoofArea = roofArea;
            r.MinimumKips = (WindLateralLoads.MinimumWallPressure * wallArea + WindLateralLoads.MinimumRoofPressure * roofArea) / 1000.0;
            return r;
        }

        /// <summary>
        /// Chapter 28 load cases for the shear walls: per direction, the basic case and (unless exempt) the torsional case,
        /// each with both torsion signs (the reference corner at either end of the windward wall), and the 28.3.4 minimum
        /// as its own case.
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

            void Add(int n, string name, double vx, double vy, double mt) =>
                r.LoadCases.Add(new WindLoadCase { CaseNumber = n, Name = name, Vx = vx, Vy = vy, Mt = mt });

            foreach (var (e, isX) in new[] { (ex, true), (ey, false) })
            {
                string dir = isX ? "along length" : "along width";
                foreach (int s in new[] { 1, -1 })
                    Add(1, $"Ch 28 basic {e.LoadCase}, {dir}, {(s > 0 ? "+" : "-")} torsion", isX ? e.BasicKips : 0, isX ? 0 : e.BasicKips, s * e.BasicTorsion);
                if (!torsionalExempt)
                    foreach (int s in new[] { 1, -1 })
                        Add(2, $"Ch 28 torsional {e.LoadCase}, {dir}, {(s > 0 ? "+" : "-")} torsion", isX ? e.TorsionalKips : 0, isX ? 0 : e.TorsionalKips, s * e.TorsionalTorsion);
                Add(3, $"Ch 28 minimum (28.3.4), {dir}", isX ? e.MinimumKips : 0, isX ? 0 : e.MinimumKips, 0);
            }
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
