using System;
using System.Collections.Generic;
using System.Linq;

namespace ASCE7WindLoadCalculator
{
    /// <summary>Which of the two Fig. 27.3-1 roof values a wind takes: Case A, the first value listed (the larger suction),
    /// or Case B, the second. Both are cases in their own right (on a flat roof both are suction, of different size and
    /// layout); neither is a fallback for the other.</summary>
    public enum RoofValue { CaseA, CaseB }

    /// <summary>
    /// One piece of the envelope in one wind case, with the pressure that case's own wind puts on it: a wall or roof zone of
    /// one wind, or, for a quartering wind (Fig. 27.3-8 Cases 3 and 4), the overlap of a zone of each of its two winds.
    /// Pressures are positive toward the surface; the internal pressure positive pushing out from inside.
    /// </summary>
    public sealed class WindCaseZone
    {
        /// <summary>The zone(s) it comes from: "ZoneWWR (S)", or "Zone4 (W) + ZoneWWR (S)" for a quartering wind.</summary>
        public string Label { get; internal set; }
        public BuildingFace Face { get; internal set; }
        public SurfaceKind Kind { get; internal set; }
        public string SurfaceName { get; internal set; }

        /// <summary>The surface it lies on (its frame maps the outline to 3D).</summary>
        public BuildingSurface Surface { get; internal set; }

        /// <summary>Outline in the surface's own frame: plan (x, y) for a roof, (along, up) for a wall (ft).</summary>
        public IReadOnlyList<Vec2> Outline { get; internal set; }

        /// <summary>The outline in 3D model coordinates (ft).</summary>
        public IReadOnlyList<Vec3> Corners { get; internal set; }

        /// <summary>True surface area (ft²).</summary>
        public double Area { get; internal set; }
        public Vec3 Normal { get; internal set; }
        public Vec3 Centroid { get; internal set; }

        /// <summary>External pressure (psf, + toward the surface), this case's factor included. A windward wall band carries
        /// the average of qz G Cp over its height.</summary>
        public double ExternalPsf { get; internal set; }

        /// <summary>Internal pressure (psf): qh (GCpi) with this case's sign, + pushing out from inside.</summary>
        public double InternalPsf { get; internal set; }

        /// <summary>Net pressure toward the surface (psf): external − internal.</summary>
        public double NetPsf => ExternalPsf - InternalPsf;

        /// <summary>Net force on the piece (kips, model axes): F = −p A n.</summary>
        public Vec3 ForceKips => Normal * (-NetPsf * Area / 1000.0);
    }

    /// <summary>
    /// One wind case: one wind (or, for Cases 3 and 4, the two winds of a quartering direction at once), with one roof value
    /// for each wind and one sign of the internal pressure. Every zone keeps the pressure this case's wind puts on it; nothing
    /// is taken from another case. Vx, Vy and Mt are the Fig. 27.3-8 forces for the shear walls.
    /// </summary>
    public sealed class WindCase
    {
        public string Name { get; internal set; }

        /// <summary>Fig. 27.3-8 load case, 1 to 4 (Chapter 27); for Chapter 28 the 7-22 load case 1 to 4 (Figs. 28.3-1 and
        /// 28.3-2): 1 transverse (Case A), 2 longitudinal (Case B), 3 and 4 their torsional cases. 0 for the separate minimum
        /// load case (<see cref="WindLateralLoads.MinimumCaseNumber"/>).</summary>
        public int FigureCase { get; internal set; }

        /// <summary>The procedure the case comes from: Chapter 27 (directional) or Chapter 28 (envelope).</summary>
        public LateralProcedure Procedure { get; internal set; } = LateralProcedure.Directional;

        /// <summary>Chapter 28: the reference corner of the case ("SW", "SE", "NW", "NE"); null for Chapter 27.</summary>
        public string ReferenceCorner { get; internal set; }

        /// <summary>Chapter 28: the Fig. 28.3-1 coefficient set, "A" or "B"; null for Chapter 27.</summary>
        public string CoefficientCase { get; internal set; }

        /// <summary>Where the wind comes from: "W", "E", "S", "N", or "SW", "SE", "NW", "NE" for Cases 3 and 4.</summary>
        public string From { get; internal set; }

        /// <summary>The roof value of the wind along the length (W or E), null when that wind is not part of the case.</summary>
        public RoofValue? RoofAlongLength { get; internal set; }

        /// <summary>The roof value of the wind along the width (S or N), null when that wind is not part of the case.</summary>
        public RoofValue? RoofAlongWidth { get; internal set; }

        /// <summary>+1 for +GCpi, −1 for −GCpi (0 for an open building).</summary>
        public int InternalSign { get; internal set; }

        /// <summary>Fig. 27.3-8 factor on each wind: 1, 0.75 or 0.563 (0 when that wind is not part of the case).</summary>
        public double FactorAlongLength { get; internal set; }
        public double FactorAlongWidth { get; internal set; }

        public List<WindCaseZone> Zones { get; } = new List<WindCaseZone>();

        /// <summary>Shear wall forces (kips, kip-ft; +x east, +y north, Mt counter-clockwise), Fig. 27.3-8 on the calculated force
        /// of each wind (walls, roof with this case's roof value, and parapets). Cases 3 and 4 in ASCE 7-22: the walls and parapets
        /// at 0.75 / 0.563 and the roof zones as resolved (Fig. 27.3-8 note 2). The minimum case: the minimum force.</summary>
        public double Vx { get; internal set; }
        public double Vy { get; internal set; }
        public double Mt { get; internal set; }

        /// <summary>True for the separate minimum load case (27.1.5, or 28.3.4 / 28.3.6 for Chapter 28; <see cref="FigureCase"/> 0),
        /// "applied as a separate load case in addition to the normal load cases" (C27.1.5). It does not raise the other cases.</summary>
        public bool IsMinimumCase => FigureCase == WindLateralLoads.MinimumCaseNumber;

        /// <summary>Chapter 28: true when the roof was left out of Vx, Vy and Mt because its horizontal force is against the
        /// wind (the shear may not be less than with the roof neglected). The roof zones are still in the case, for uplift.</summary>
        public bool RoofNeglected { get; internal set; }

        /// <summary>
        /// The pressure up on the soffit of a windward roof overhang (psf, + up), for the wind along the length (on the W or E
        /// eave, whichever is windward) and along the width (S or N); 0 when that wind is not part of the case. Chapter 27:
        /// Cp 0.8 (27.3.3) at this case's factor; Chapter 28: GCp 0.7 (28.3.3). The overhangs are not in <see cref="Zones"/>; the
        /// top of an overhang takes the external pressure of the roof zone it comes out of.
        /// </summary>
        public double SoffitPsfAlongLength { get; internal set; }
        public double SoffitPsfAlongWidth { get; internal set; }

        /// <summary>Net upward force of the roof zones (kips, + up), internal pressure included.</summary>
        public double UpliftKips => Zones.Where(z => z.Kind == SurfaceKind.Roof).Sum(z => z.ForceKips.Z);

        /// <summary>The shear wall load case (the same name).</summary>
        public WindLoadCase ToLoadCase() => new WindLoadCase { CaseNumber = FigureCase, Name = Name, Vx = Vx, Vy = Vy, Mt = Mt };

        /// <summary>What Vx, Vy and Mt are made of, so they can be shared out over the stories (<see cref="StoryWindLoads.ComputeCases"/>):
        /// they add up to Vx, Vy and Mt.</summary>
        internal List<ForcePart> Parts { get; } = new List<ForcePart>();
    }

    /// <summary>How a part of a case's force is shared out over the stories.</summary>
    internal enum PartShare
    {
        /// <summary>Wall pressure (qz up the wall): each story takes the wall force on its strip.</summary>
        Walls,
        /// <summary>A uniform wall pressure (the minimum's 16 psf): each story takes its strip's share of the projected wall area.</summary>
        WallArea,
        /// <summary>The roof and parapets: all on the top story.</summary>
        Top,
        /// <summary>Chapter 28: in the proportion of that direction's forces on each story.</summary>
        Direction,
    }

    internal struct ForcePart
    {
        public bool AlongLength;   // the wind whose strips it follows
        public PartShare Share;
        public double Vx, Vy, Mt;
    }

    /// <summary>
    /// Every Chapter 27 wind case of a building, each with its own zone pressures (walls and roof together) and its forces.
    /// Winds from W and E use the along-length calculation (E mirrored), from S and N the along-width one (N mirrored).
    /// Each wind comes with each of its two Fig. 27.3-1 roof values and each sign of GCpi, as separate cases.
    /// Case 1: 4 winds × 2 roof × 2 GCpi = 16. Case 2: × 2 torsion = 32. Case 3: 4 quartering directions × 4 roof pairs × 2 GCpi
    /// = 32, resolved area by area: the walls at 0.75 × the pressure of each of the two winds; the roof at 0.75 × each (ASCE
    /// 7-16), or 100% of the larger of the two winds' roof pressures (ASCE 7-22 note 2). Case 4: 0.563 (roof 0.75 × the larger
    /// in 7-22), × 4 torsion = 128. The minimum (27.1.5): one case per wind (4), separate from the others (C27.1.5).
    /// Not in the zones: parapets (their force is in Vx, Vy) and the overhang soffits.
    /// </summary>
    public static partial class WindCases
    {
        public const double Case2Factor = 0.75, Case3Factor = 0.75, Case4Factor = 0.563;

        /// <summary>Height of the windward wall bands above the 15 ft line (ft), as drawn.</summary>
        public const double BandHeight = 5.0;

        private sealed class Piece
        {
            public BuildingSurface Surface;
            public List<Vec2> Poly;      // CCW, in the surface's frame
            public double Ext;           // psf, + toward the surface
            public string Label;
        }

        private sealed class Wind
        {
            public string From;
            public bool AlongLength;
            public int Sign;             // +1 from W or S (toward +x or +y), −1 from E or N
            public WindLoadCalculator_MWFRS Calc;
            public BuildingFace WindwardFace;
            public Dictionary<RoofValue, List<Piece>> Pieces = new Dictionary<RoofValue, List<Piece>>();
            public Dictionary<RoofValue, double> Design = new Dictionary<RoofValue, double>();
            public double WallsAndParapets;  // kips along the wind
            public double Walls;             // the walls alone (kips along the wind)
            public double MinimumKips;       // 27.1.5
            public double MinimumWallKips, MinimumRoofKips;  // its two parts: 16 psf on the walls, 8 psf on the roof projected
        }

        /// <param name="onlyCases1And3">The Fig. 27.3-8 exception: Cases 1 and 3 only.</param>
        public static List<WindCase> Build(WindAnalysisResult analysis, bool onlyCases1And3 = false)
        {
            if (!(analysis?.MwfrsAlongLength is WindLoadCalculator_MWFRS mx) || !(analysis.MwfrsAlongWidth is WindLoadCalculator_MWFRS my))
                throw new ArgumentException("Both MWFRS directions are needed.", nameof(analysis));
            var b = mx.buildingData;
            var g = new WindGeometry(b);
            double L = b.BuildingLength, B = b.BuildingWidth;

            var winds = new[]
            {
                MakeWind("W", true, +1, mx, g, p => p),
                MakeWind("E", true, -1, mx, g, p => new Vec2(L - p.X, p.Y)),
                MakeWind("S", false, +1, my, g, p => new Vec2(p.Y, p.X)),
                MakeWind("N", false, -1, my, g, p => new Vec2(p.Y, B - p.X)),
            }.ToDictionary(w => w.From);

            double gcpi = mx.GetGCpi();
            double qi = mx.CalculateDynamicWindPressure(b.MwfrsFigureHeight) * gcpi * mx.PressureKd;
            var signs = gcpi > 0 ? new[] { +1, -1 } : new[] { 0 };
            string Gcpi(int s) => s > 0 ? "+GCpi" : s < 0 ? "−GCpi" : "GCpi 0";
            string Roof(RoofValue v) => v == RoofValue.CaseA ? "Case A" : "Case B";
            string T(int s) => s > 0 ? "+" : "−";
            double ex = WindLateralLoads.CaseEccentricity * B, ey = WindLateralLoads.CaseEccentricity * L;
            var values = new[] { RoofValue.CaseA, RoofValue.CaseB };
            // ASCE 7-22 Fig. 27.3-8 note 2: in Cases 3 and 4 each roof area takes 100% of the larger of the roof pressures of
            // its two winds in Case 1 (Case 3) or Case 2 (Case 4); ASCE 7-16: the roof at 75% / 56.3% of both, like the walls
            bool larger = mx.Edition.QuarteringRoofTakesLarger;
            var cases = new List<WindCase>();

            WindCase Make(int n, string name, string from, RoofValue? rx, RoofValue? ry, double fx, double fy, int s,
                          IEnumerable<Piece> pieces, double vx, double vy, double mt)
            {
                var c = new WindCase
                {
                    Name = name, FigureCase = n, From = from, RoofAlongLength = rx, RoofAlongWidth = ry, InternalSign = s,
                    FactorAlongLength = fx, FactorAlongWidth = fy, Vx = vx + 0.0, Vy = vy + 0.0, Mt = mt + 0.0,
                    SoffitPsfAlongLength = fx * mx.OverhangBottomPressure, SoffitPsfAlongWidth = fy * my.OverhangBottomPressure,
                };
                foreach (var p in pieces) c.Zones.Add(ToZone(p, s * qi));
                cases.Add(c);
                return c;
            }

            // a wind's force (kips along the wind, before its direction sign) as story parts: the walls on their strips, the rest
            // (roof, parapets: `top`) on the top story; f the Fig. 27.3-8 factor on the force, mt the torsion per kip of it
            IEnumerable<ForcePart> Along(Wind w, double f, double walls, double top, double mt)
            {
                double ux = w.AlongLength ? w.Sign : 0, uy = w.AlongLength ? 0 : w.Sign;
                yield return new ForcePart { AlongLength = w.AlongLength, Share = PartShare.Walls, Vx = ux * f * walls, Vy = uy * f * walls, Mt = mt * walls };
                yield return new ForcePart { AlongLength = w.AlongLength, Share = PartShare.Top, Vx = ux * f * top, Vy = uy * f * top, Mt = mt * top };
            }

            // Cases 1 and 2: one wind
            foreach (var w in winds.Values)
                foreach (var v in values)
                    foreach (int s in signs)
                    {
                        double V = w.Design[v];
                        RoofValue? rx = w.AlongLength ? v : (RoofValue?)null, ry = w.AlongLength ? (RoofValue?)null : v;
                        double vx = w.AlongLength ? w.Sign * V : 0, vy = w.AlongLength ? 0 : w.Sign * V;
                        Make(1, $"Case 1, from {w.From}, roof {Roof(v)}, {Gcpi(s)}", w.From, rx, ry,
                             w.AlongLength ? 1 : 0, w.AlongLength ? 0 : 1, s, w.Pieces[v], vx, vy, 0)
                            .Parts.AddRange(Along(w, 1, w.Walls, V - w.Walls, 0));
                        if (onlyCases1And3) continue;
                        double e = w.AlongLength ? ex : ey, f = Case2Factor;
                        foreach (int t in new[] { +1, -1 })
                            Make(2, $"Case 2, from {w.From}, roof {Roof(v)}, {Gcpi(s)}, torsion {T(t)}", w.From, rx, ry,
                                 w.AlongLength ? f : 0, w.AlongLength ? 0 : f, s, Scaled(w.Pieces[v], f), f * vx, f * vy, t * f * V * e)
                                .Parts.AddRange(Along(w, f, w.Walls, V - w.Walls, t * f * e));
                    }

            // Cases 3 and 4: the quartering winds, resolved area by area from their two winds
            foreach (var (xs, ys) in new[] { ("W", "S"), ("E", "S"), ("W", "N"), ("E", "N") })
            {
                var wx = winds[xs]; var wy = winds[ys];
                string from = ys + xs;
                foreach (var rx in values)
                    foreach (var ry in values)
                    {
                        double Vx = wx.Design[rx], Vy = wy.Design[ry];
                        var p3 = Overlay(wx.Pieces[rx], Case3Factor, wy.Pieces[ry], Case3Factor, larger ? 1.0 : (double?)null);
                        var p4 = onlyCases1And3 ? null : Overlay(wx.Pieces[rx], Case4Factor, wy.Pieces[ry], Case4Factor, larger ? Case2Factor : (double?)null);
                        // forces: 7-16 the Fig. 27.3-8 factor on each wind's force; 7-22 the walls and parapets at that factor
                        // and the roof as resolved (the horizontal force of its zones)
                        (double x, double y) Forces(List<Piece> pieces, double f)
                        {
                            if (!larger) return (f * wx.Sign * Vx, f * wy.Sign * Vy);
                            var (rfx, rfy) = RoofForce(pieces);
                            return (f * wx.Sign * wx.WallsAndParapets + rfx, f * wy.Sign * wy.WallsAndParapets + rfy);
                        }
                        var (v3x, v3y) = Forces(p3, Case3Factor);
                        var (v4x, v4y) = onlyCases1And3 ? (0.0, 0.0) : Forces(p4, Case4Factor);
                        // story parts of one of the two winds: its walls (at the factor) on their strips, the rest of the case's
                        // force along that wind (roof, parapets) on the top story; the torsion mt per kip of that wind's force
                        IEnumerable<ForcePart> Quarter(Wind w, double f, double D, double v, double mt)
                        {
                            double walls = f * w.Sign * w.Walls;
                            double ux = w.AlongLength ? 1 : 0, uy = w.AlongLength ? 0 : 1;
                            yield return new ForcePart { AlongLength = w.AlongLength, Share = PartShare.Walls, Vx = ux * walls, Vy = uy * walls, Mt = mt * w.Walls };
                            yield return new ForcePart { AlongLength = w.AlongLength, Share = PartShare.Top, Vx = ux * (v - walls), Vy = uy * (v - walls), Mt = mt * (D - w.Walls) };
                        }
                        foreach (int s in signs)
                        {
                            string roofs = $"roof {xs} {Roof(rx)} / {ys} {Roof(ry)}";
                            var c3 = Make(3, $"Case 3, from {from}, {roofs}, {Gcpi(s)}", from, rx, ry, Case3Factor, Case3Factor, s, p3, v3x, v3y, 0);
                            c3.Parts.AddRange(Quarter(wx, Case3Factor, Vx, v3x, 0));
                            c3.Parts.AddRange(Quarter(wy, Case3Factor, Vy, v3y, 0));
                            if (onlyCases1And3) continue;
                            // torsion 0.563 (V e) about each axis, as Fig. 27.3-8 Case 4 (75% of Case 2)
                            foreach (int t1 in new[] { +1, -1 })
                                foreach (int t2 in new[] { +1, -1 })
                                {
                                    var c4 = Make(4, $"Case 4, from {from}, {roofs}, {Gcpi(s)}, torsion {T(t1)}{T(t2)}", from, rx, ry,
                                                  Case4Factor, Case4Factor, s, p4, v4x, v4y, Case4Factor * (t1 * Vx * ex + t2 * Vy * ey));
                                    c4.Parts.AddRange(Quarter(wx, Case4Factor, Vx, v4x, Case4Factor * t1 * ex));
                                    c4.Parts.AddRange(Quarter(wy, Case4Factor, Vy, v4y, Case4Factor * t2 * ey));
                                }
                        }
                    }
            }

            // The minimum (27.1.5), a separate load case for each wind (C27.1.5): 16 psf on the windward wall (its whole
            // elevation, gable included) and 8 psf on the roof area projected on a vertical plane normal to the wind. The roof
            // part acts on that projection, not on a roof surface, so it is in the force but not drawn as a zone. No internal
            // pressure (it cancels in the horizontal force), no torsion.
            foreach (var w in winds.Values)
                cases.Add(MinimumCase(g, w));
            return cases;
        }

        private static WindCase MinimumCase(WindGeometry g, Wind w)
        {
            double V = w.MinimumKips;
            var c = new WindCase
            {
                Name = $"Minimum (27.1.5), from {w.From}: 16 psf walls + 8 psf roof (projected)", FigureCase = WindLateralLoads.MinimumCaseNumber,
                From = w.From, InternalSign = 0, FactorAlongLength = w.AlongLength ? 1 : 0, FactorAlongWidth = w.AlongLength ? 0 : 1,
                Vx = (w.AlongLength ? w.Sign * V : 0) + 0.0, Vy = (w.AlongLength ? 0 : w.Sign * V) + 0.0, Mt = 0,
            };
            c.Parts.AddRange(MinimumParts(w.AlongLength, w.Sign, w.MinimumWallKips, w.MinimumRoofKips));
            var wall = g.Surface(w.WindwardFace);
            if (wall != null)
                c.Zones.Add(ToZone(new Piece
                {
                    Surface = wall, Ext = WindLateralLoads.MinimumWallPressure, Label = $"Minimum wall 16 psf ({w.From})",
                    Poly = PolygonOps.CounterClockwise(wall.Outline.Select(p => new Vec2((p - wall.Origin).Dot(wall.U), (p - wall.Origin).Dot(wall.V))).ToList()),
                }, 0));
            return c;
        }

        /// <summary>The minimum's story parts: 16 psf on each story's wall strip (by its area), 8 psf on the roof on the top story.</summary>
        private static IEnumerable<ForcePart> MinimumParts(bool alongLength, int sign, double wallKips, double roofKips)
        {
            double ux = alongLength ? sign : 0, uy = alongLength ? 0 : sign;
            yield return new ForcePart { AlongLength = alongLength, Share = PartShare.WallArea, Vx = ux * wallKips, Vy = uy * wallKips };
            yield return new ForcePart { AlongLength = alongLength, Share = PartShare.Top, Vx = ux * roofKips, Vy = uy * roofKips };
        }

        /// <summary>
        /// The wind calculator's wind cases for the procedure its setup chose: Chapter 27 (<see cref="Build"/>), or Chapter 28
        /// (<see cref="BuildEnvelope"/>) when chosen and it applies (otherwise Chapter 27, as <see cref="WindLateralLoads.Compute(WindAnalysisResult, bool)"/>).
        /// The one list of wind for every app: the shear walls and the load path take these cases, by name.
        /// </summary>
        /// <param name="onlyCases1And3">Fig. 27.3-8 exception (Cases 1 and 3 only); for Chapter 28 no torsional cases (28.3.2.1).</param>
        public static List<WindCase> For(WindAnalysisResult analysis, bool onlyCases1And3 = false) =>
            ProcedureOf(analysis) == LateralProcedure.Envelope ? BuildEnvelope(analysis, onlyCases1And3) : Build(analysis, onlyCases1And3);

        /// <summary>The procedure <see cref="For"/> uses: Chapter 28 only when chosen and it applies.</summary>
        public static LateralProcedure ProcedureOf(WindAnalysisResult analysis)
        {
            var b = analysis?.MwfrsAlongLength?.buildingData;
            bool chosen = analysis?.MwfrsAlongLength?.Parameters?.LateralProcedure == LateralProcedure.Envelope;
            return chosen && b != null && EnvelopeLateralLoads.Applicability(b).Count == 0 ? LateralProcedure.Envelope : LateralProcedure.Directional;
        }

        /// <summary>Horizontal force of the roof pieces (kips, plan axes), external pressures: F = −p A n.</summary>
        private static (double x, double y) RoofForce(IEnumerable<Piece> pieces)
        {
            double fx = 0, fy = 0;
            foreach (var p in pieces.Where(q => q.Surface.Kind == SurfaceKind.Roof))
            {
                var f = ToZone(p, 0).ForceKips;
                fx += f.X; fy += f.Y;
            }
            return (fx, fy);
        }

        // ---- one wind -----------------------------------------------------------------------------------------------

        private static Wind MakeWind(string from, bool alongLength, int sign, WindLoadCalculator_MWFRS calc, WindGeometry g, Func<Vec2, Vec2> toPlan)
        {
            var w = new Wind { From = from, AlongLength = alongLength, Sign = sign, Calc = calc };
            BuildingFace F(string face) => (BuildingFace)Enum.Parse(typeof(BuildingFace), face + "Wall");
            var opposite = new Dictionary<string, string> { ["W"] = "East", ["E"] = "West", ["S"] = "North", ["N"] = "South" };
            var own = new Dictionary<string, string> { ["W"] = "West", ["E"] = "East", ["S"] = "South", ["N"] = "North" };
            w.WindwardFace = F(own[from]);
            var faces = new Dictionary<string, BuildingFace[]>
            {
                ["ZoneWW"] = new[] { F(own[from]) },
                ["ZoneLW"] = new[] { F(opposite[from]) },
                ["ZoneSW"] = alongLength ? new[] { BuildingFace.SouthWall, BuildingFace.NorthWall } : new[] { BuildingFace.WestWall, BuildingFace.EastWall },
            };

            var walls = WallPieces(calc, g, faces);
            var force = WindLateralLoads.DirectionalForce(calc, alongLength ? "along length" : "along width");
            var model = new WindGeometry(calc.buildingData);
            w.WallsAndParapets = force.WallsKips + force.ParapetKips;
            w.Walls = force.WallsKips;
            w.MinimumKips = force.MinimumKips;
            w.MinimumWallKips = WindLateralLoads.MinimumWallPressure * force.ProjectedWallArea / 1000.0;
            w.MinimumRoofKips = force.MinimumKips - w.MinimumWallKips;
            foreach (var v in new[] { RoofValue.CaseA, RoofValue.CaseB })
            {
                var set = v == RoofValue.CaseA ? calc.windPressureRoof_Neg_External : calc.windPressureRoof_Pos_External;
                var pieces = RoofPieces(calc, g, set, toPlan, from);
                pieces.AddRange(walls);
                w.Pieces[v] = pieces;
                // the calculated force; the 27.1.5 minimum is its own case (C27.1.5) and does not raise this one
                w.Design[v] = force.WallsKips + WindLateralLoads.RoofHorizontalKips(calc, model, set) + force.ParapetKips;
            }
            return w;
        }

        private static List<Piece> RoofPieces(WindLoadCalculator_Base calc, WindGeometry g, Dictionary<int, PressureData> set,
                                               Func<Vec2, Vec2> toPlan, string from)
        {
            var list = new List<Piece>();
            if (calc.RoofAreaCalculator == null) return list;
            foreach (var kv in calc.RoofAreaCalculator.effWindAreas)
            {
                if (!set.TryGetValue(kv.Key, out var p)) continue;
                var area = kv.Value;
                // a zone with holes goes in as triangles (each convex); the rest whole
                var polys = new List<List<Vec2>>();
                if (area.Holes.Count == 0) polys.Add(area.OuterBoundary.Select(toPlan).ToList());
                else
                {
                    var tris = PolygonOps.Triangulate(area.OuterBoundary.Select(toPlan).ToList(),
                        area.Holes.Select(h => (IReadOnlyList<Vec2>)h.Select(toPlan).ToList()));
                    for (int i = 0; i + 2 < tris.Count; i += 3) polys.Add(new List<Vec2> { tris[i], tris[i + 1], tris[i + 2] });
                }
                foreach (var poly in polys)
                {
                    var ccw = PolygonOps.CounterClockwise(poly);
                    foreach (var roof in g.Roofs)
                    {
                        var piece = PolygonOps.ClipToConvex(ccw, roof.PlanRegion);
                        if (piece.Count < 3 || Math.Abs(PolygonOps.SignedArea(piece)) < 1e-6) continue;
                        list.Add(new Piece { Surface = roof, Poly = piece, Ext = p.ExternalPressure, Label = $"{area.Label_Full} ({from})" });
                    }
                }
            }
            return list;
        }

        private static List<Piece> WallPieces(WindLoadCalculator_MWFRS calc, WindGeometry g, Dictionary<string, BuildingFace[]> faces)
        {
            var list = new List<Piece>();
            var zones = calc.WallAreaCalculator_BldgLength;
            if (zones == null) return list;
            string from = faces["ZoneWW"][0].ToString().Replace("Wall", "").Substring(0, 1);
            foreach (var kv in zones.effWindAreas)
            {
                var area = kv.Value;
                if (!faces.TryGetValue(area.Label_Full, out var onto)) continue;
                var outline = PolygonOps.CounterClockwise(area.OuterBoundary);
                foreach (var face in onto)
                {
                    var wall = g.Surface(face);
                    if (wall == null) continue;
                    if (area.Label_Full == "ZoneWW")
                    {
                        // qz grows with height: bands (0-15 ft, then every 5 ft), each with its average external pressure
                        foreach (var band in Bands(outline))
                        {
                            double a = Math.Abs(PolygonOps.SignedArea(band));
                            double avg = MwfrsLateralLoad.IntegrateOverHeight(band, calc.WindwardWallExternalPressureAt) / a;
                            list.Add(new Piece { Surface = wall, Poly = band, Ext = avg, Label = $"ZoneWW ({from}), {band.Min(q => q.Y):0.#}-{band.Max(q => q.Y):0.#} ft" });
                        }
                    }
                    else if (calc.windPressureWall_Pos_External_MWFRS.TryGetValue(kv.Key, out var p))
                        list.Add(new Piece { Surface = wall, Poly = outline, Ext = p.ExternalPressure, Label = $"{area.Label_Full} ({from})" });
                }
            }
            return list;
        }

        private static IEnumerable<List<Vec2>> Bands(List<Vec2> outline)
        {
            double top = outline.Max(p => p.Y), bottom = outline.Min(p => p.Y), left = outline.Min(p => p.X) - 1, right = outline.Max(p => p.X) + 1;
            var levels = new List<double> { bottom };
            if (top > 15) levels.Add(15);
            for (double z = 15 + BandHeight; z < top - 1e-6; z += BandHeight) levels.Add(z);
            levels.Add(top);
            for (int i = 0; i + 1 < levels.Count; i++)
            {
                if (levels[i + 1] - levels[i] < 1e-6) continue;
                var box = new List<Vec2> { new Vec2(left, levels[i]), new Vec2(right, levels[i]), new Vec2(right, levels[i + 1]), new Vec2(left, levels[i + 1]) };
                var piece = PolygonOps.ClipToConvex(outline, box);
                if (piece.Count >= 3 && Math.Abs(PolygonOps.SignedArea(piece)) > 1e-6) yield return piece;
            }
        }

        // ---- combining --------------------------------------------------------------------------------------------

        private static IEnumerable<Piece> Scaled(IEnumerable<Piece> pieces, double f) =>
            pieces.Select(p => new Piece { Surface = p.Surface, Poly = p.Poly, Ext = f * p.Ext, Label = p.Label });

        /// <summary>
        /// The two winds of a quartering direction at once: every overlap of a piece of each wind on the same surface, with
        /// fa × the first wind's pressure + fb × the second's. With <paramref name="roofLarger"/> (ASCE 7-22 Fig. 27.3-8 note 2)
        /// a roof piece instead takes roofLarger × the larger (in size) of the two winds' roof pressures there.
        /// </summary>
        private static List<Piece> Overlay(List<Piece> a, double fa, List<Piece> b, double fb, double? roofLarger = null)
        {
            var list = new List<Piece>();
            foreach (var pa in a)
                foreach (var pb in b.Where(q => q.Surface == pa.Surface))
                    foreach (var convex in ConvexParts(pb.Poly))
                    {
                        var piece = PolygonOps.ClipToConvex(pa.Poly, convex);
                        if (piece.Count < 3 || Math.Abs(PolygonOps.SignedArea(piece)) < 1e-6) continue;
                        if (roofLarger.HasValue && pa.Surface.Kind == SurfaceKind.Roof)
                        {
                            var big = Math.Abs(pa.Ext) >= Math.Abs(pb.Ext) ? pa : pb;
                            list.Add(new Piece { Surface = pa.Surface, Poly = piece, Ext = roofLarger.Value * big.Ext,
                                                 Label = $"{big.Label} (larger of {pa.Label.Split(' ')[0]} / {pb.Label.Split(' ')[0]}, 27.3-8 note 2)" });
                        }
                        else
                            list.Add(new Piece { Surface = pa.Surface, Poly = piece, Ext = fa * pa.Ext + fb * pb.Ext, Label = pa.Label + " + " + pb.Label });
                    }
            return list;
        }

        private static IEnumerable<List<Vec2>> ConvexParts(List<Vec2> poly)
        {
            if (IsConvex(poly)) { yield return poly; yield break; }
            var tris = PolygonOps.Triangulate(poly);
            for (int i = 0; i + 2 < tris.Count; i += 3) yield return PolygonOps.CounterClockwise(new List<Vec2> { tris[i], tris[i + 1], tris[i + 2] });
        }

        private static bool IsConvex(List<Vec2> p)
        {
            for (int i = 0, n = p.Count; i < n; i++)
            {
                Vec2 a = p[i], b = p[(i + 1) % n], c = p[(i + 2) % n];
                if ((b.X - a.X) * (c.Y - b.Y) - (b.Y - a.Y) * (c.X - b.X) < -1e-9) return false;
            }
            return true;
        }

        private static WindCaseZone ToZone(Piece p, double internalPsf)
        {
            var s = p.Surface;
            bool roof = s.Kind == SurfaceKind.Roof;
            Func<Vec2, Vec3> to3 = roof ? (Func<Vec2, Vec3>)s.LiftToRoof : s.FromWallFrame;
            double plan = Math.Abs(PolygonOps.SignedArea(p.Poly));
            double area = roof ? plan * Math.Sqrt(1 + s.Zx * s.Zx + s.Zy * s.Zy) : plan;
            return new WindCaseZone
            {
                Label = p.Label, Face = s.Face, Kind = s.Kind, SurfaceName = s.Name, Surface = s, Outline = p.Poly,
                Corners = p.Poly.Select(to3).ToList(), Area = area, Normal = s.Normal, Centroid = to3(Centroid(p.Poly)),
                ExternalPsf = p.Ext, InternalPsf = internalPsf,
            };
        }

        private static Vec2 Centroid(IReadOnlyList<Vec2> p)
        {
            double a = 0, cx = 0, cy = 0;
            for (int i = 0, n = p.Count; i < n; i++)
            {
                var u = p[i]; var v = p[(i + 1) % n];
                double k = u.X * v.Y - v.X * u.Y;
                a += k; cx += (u.X + v.X) * k; cy += (u.Y + v.Y) * k;
            }
            return Math.Abs(a) < 1e-12 ? p[0] : new Vec2(cx / (3 * a), cy / (3 * a));
        }
    }
}
