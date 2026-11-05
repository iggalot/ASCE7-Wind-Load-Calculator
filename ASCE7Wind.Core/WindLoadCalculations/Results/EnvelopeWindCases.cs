using System;
using System.Collections.Generic;
using System.Linq;

namespace ASCE7WindLoadCalculator
{
    /// <summary>
    /// The Chapter 28 (envelope, low-rise) wind cases, each with its own zones: Figs. 28.3-1 and 28.3-2, the 7-22 Load Cases
    /// 1 to 4 (1 transverse, Case A; 2 longitudinal, Case B; 3 and 4 their torsional cases), each building corner in turn as
    /// the reference corner, each sign of GCpi as its own case. p = qh Kd [(GCpf) − (GCpi)].
    /// The zone rows are those of <see cref="EnvelopeLateralLoads.Direction"/> (calculation axes: wind along +x, reference
    /// corner at the origin), moved onto the building's surfaces for each corner. A corner is the calculation building
    /// mirrored or turned (<see cref="EnvelopeLateralLoads.Corners"/>), so each case's Vx, Vy and Mt are those of the rows.
    /// The walls parallel to the wind take GCpf = 0 in Case A (Fig. 28.3-1): their zone carries the internal pressure alone.
    /// Not in the zones: parapets (their force is in Vx) and the overhang soffits.
    /// </summary>
    public static partial class WindCases
    {
        /// <summary>Label of a Case A wall parallel to the wind (GCpf = 0).</summary>
        public const string ZeroSideWall = "Zone0 (side wall, GCpf 0)";

        /// <summary>28.3.3: the positive pressure on the soffit of a windward roof overhang, GCp = 0.7.</summary>
        public const double EnvelopeSoffitGCp = 0.7;

        /// <param name="torsionalExempt">28.3.2.1 exceptions: no torsional cases (3 and 4).</param>
        public static List<WindCase> BuildEnvelope(WindAnalysisResult analysis, bool torsionalExempt = false)
        {
            if (!(analysis?.MwfrsAlongLength is WindLoadCalculator_MWFRS mx) || !(analysis.MwfrsAlongWidth is WindLoadCalculator_MWFRS my))
                throw new ArgumentException("Both MWFRS directions are needed.", nameof(analysis));
            var b = mx.buildingData;
            var g = new WindGeometry(b);
            double L = b.BuildingLength, B = b.BuildingWidth;
            double gcpi = mx.GetGCpi();
            var signs = gcpi > 0 ? new[] { +1, -1 } : new[] { 0 };
            string Gcpi(int s) => s > 0 ? "+GCpi" : s < 0 ? "−GCpi" : "GCpi 0";
            var cases = new List<WindCase>();

            // each axis with each coefficient set it takes (a flat roof: Case A and Case B each way)
            var runs = new[] { (mx, true), (my, false) }
                .SelectMany(t => EnvelopeLateralLoads.CoefficientCases(t.Item1.buildingData).Select(set => (calc: t.Item1, alongLength: t.Item2, set)));
            foreach (var (calc, alongLength, set) in runs)
            {
                var e = EnvelopeLateralLoads.Direction(calc, alongLength ? "along length" : "along width", set);
                double D = e.DepthAlongWind, W = e.WidthNormalToWind;
                double qi = e.Qh * e.Kd * gcpi;

                for (int k = 0; k < 4; k++)
                {
                    // calculation axes of this corner (mirror / turn), then to the building plan (along the width: turned 90°)
                    int kk = k;
                    Func<Vec2, Vec2> mirror = p => new Vec2(kk >= 2 ? D - p.X : p.X, kk == 1 || kk == 2 ? W - p.Y : p.Y);
                    Func<Vec2, Vec2> toPlan = p => { var m = mirror(p); return alongLength ? m : new Vec2(L - m.Y, m.X); };
                    var refPt = toPlan(new Vec2(0, 0));
                    string corner = (refPt.Y < B / 2 ? "S" : "N") + (refPt.X < L / 2 ? "W" : "E");
                    var dir = toPlan(new Vec2(1, 0)) - toPlan(new Vec2(0, 0));
                    string from = alongLength ? (dir.X > 0 ? "W" : "E") : (dir.Y > 0 ? "S" : "N");

                    foreach (bool torsional in torsionalExempt ? new[] { false } : new[] { false, true })
                    {
                        int lc = (e.LoadCase == "B" ? 2 : 1) + (torsional ? 2 : 0);
                        var cr = EnvelopeLateralLoads.Corners(
                            torsional ? e.TorsionalKips : e.BasicKips,
                            torsional ? e.TorsionalCrossKips : e.BasicCrossKips,
                            torsional ? e.TorsionalTorsion : e.BasicTorsion).ElementAt(k);
                        var (vx, vy, mt) = EnvelopeLateralLoads.ToBuilding(alongLength, cr.vx, cr.vy, cr.mz);
                        bool neglected = torsional ? e.TorsionalRoofNeglected : e.RoofNeglected;

                        var pieces = new List<Piece>();
                        foreach (var row in e.Zones.Where(z => !z.IsParapet && z.Pressure.HasValue))
                        {
                            double ext = row.Pressure.Value * (torsional ? row.TorsionalFactor : 1);
                            string label = "Zone" + (torsional && row.TorsionalGCpf.HasValue ? row.Zone.Replace("E", "") + "T" : row.Zone);
                            pieces.AddRange(row.IsRoof ? RoofPieces(g, row, toPlan, ext, label) : WallPieces(g, row, D, W, toPlan, ext, label));
                        }
                        // Case A: the walls parallel to the wind take GCpf = 0 (Fig. 28.3-1); the zone is still there, its net
                        // pressure the internal pressure alone
                        if (e.LoadCase == "A")
                            foreach (int side in new[] { 2, 3 })
                                pieces.AddRange(WallPieces(g, new EnvelopeZoneForce { Side = side, S0 = 0, S1 = D }, D, W, toPlan, 0.0, ZeroSideWall));

                        foreach (int s in signs)
                        {
                            var c = new WindCase
                            {
                                Name = $"Ch 28 LC{lc} (Case {e.LoadCase}{(torsional ? ", torsional" : "")}), from {from}, ref. corner {corner}, {Gcpi(s)}",
                                FigureCase = lc, Procedure = LateralProcedure.Envelope, ReferenceCorner = corner, CoefficientCase = e.LoadCase,
                                From = from, InternalSign = s,
                                FactorAlongLength = alongLength ? 1 : 0, FactorAlongWidth = alongLength ? 0 : 1,
                                Vx = vx + 0.0, Vy = vy + 0.0, Mt = mt + 0.0,
                                RoofNeglected = neglected,
                                // 28.3.3: GCp 0.7 on the windward overhang's soffit
                                SoffitPsfAlongLength = alongLength ? EnvelopeSoffitGCp * e.Qh * e.Kd : 0,
                                SoffitPsfAlongWidth = alongLength ? 0 : EnvelopeSoffitGCp * e.Qh * e.Kd,
                            };
                            c.Parts.Add(new ForcePart { AlongLength = alongLength, Share = PartShare.Direction, Vx = c.Vx, Vy = c.Vy, Mt = c.Mt });
                            foreach (var p in pieces) c.Zones.Add(ToZone(p, s * qi));
                            cases.Add(c);
                        }
                    }
                }
            }

            // the Chapter 28 minimum (28.3.4 in 7-16, 28.3.6 in 7-22), a separate load case in addition to the others
            // (C28.3.4 / C28.3.6), one per wind: 16 psf on the windward wall, 8 psf on the roof projected normal to the wind
            // (in the force, not drawn as a zone), no internal pressure, no torsion
            string section = mx.Edition.EnvelopeMinimumSection;
            foreach (var (calc, alongLength) in new[] { (mx, true), (my, false) })
            {
                var d = EnvelopeLateralLoads.Direction(calc, alongLength ? "along length" : "along width");
                double min = d.MinimumKips;
                double minWall = WindLateralLoads.MinimumWallPressure * d.ProjectedWallArea / 1000.0;
                foreach (int sgn in new[] { +1, -1 })
                {
                    string from = alongLength ? (sgn > 0 ? "W" : "E") : (sgn > 0 ? "S" : "N");
                    var face = alongLength ? (sgn > 0 ? BuildingFace.WestWall : BuildingFace.EastWall) : (sgn > 0 ? BuildingFace.SouthWall : BuildingFace.NorthWall);
                    var c = new WindCase
                    {
                        Name = $"Ch 28 minimum ({section}), from {from}: 16 psf walls + 8 psf roof (projected)",
                        FigureCase = WindLateralLoads.MinimumCaseNumber, Procedure = LateralProcedure.Envelope, From = from, InternalSign = 0,
                        FactorAlongLength = alongLength ? 1 : 0, FactorAlongWidth = alongLength ? 0 : 1,
                        Vx = (alongLength ? sgn * min : 0) + 0.0, Vy = (alongLength ? 0 : sgn * min) + 0.0, Mt = 0,
                    };
                    c.Parts.AddRange(MinimumParts(alongLength, sgn, minWall, min - minWall));
                    var wall = g.Surface(face);
                    if (wall != null)
                        c.Zones.Add(ToZone(new Piece
                        {
                            Surface = wall, Ext = WindLateralLoads.MinimumWallPressure, Label = $"Minimum wall 16 psf ({from})",
                            Poly = PolygonOps.CounterClockwise(wall.Outline.Select(p => new Vec2((p - wall.Origin).Dot(wall.U), (p - wall.Origin).Dot(wall.V))).ToList()),
                        }, 0));
                    cases.Add(c);
                }
            }
            return cases;
        }

        /// <summary>A wall row on the building: its stretch along the side, moved to the plan, cut out of the wall it lies on
        /// (the full height of the wall there, gable ends included).</summary>
        private static IEnumerable<Piece> WallPieces(WindGeometry g, EnvelopeZoneForce row, double D, double W,
            Func<Vec2, Vec2> toPlan, double ext, string label)
        {
            Vec2 At(double s) => row.Side == 0 ? new Vec2(0, s) : row.Side == 1 ? new Vec2(D, s) : row.Side == 2 ? new Vec2(s, 0) : new Vec2(s, W);
            Vec2 a = toPlan(At(row.S0)), c = toPlan(At(row.S1));
            var wall = g.Walls.OrderBy(w => DistanceToWallLine(w, a) + DistanceToWallLine(w, c)).First();
            double u0 = Along(wall, a), u1 = Along(wall, c);
            if (u1 < u0) { var t = u0; u0 = u1; u1 = t; }
            if (u1 - u0 < 1e-9) yield break;

            var outline = PolygonOps.CounterClockwise(wall.Outline.Select(p => new Vec2((p - wall.Origin).Dot(wall.U), (p - wall.Origin).Dot(wall.V))).ToList());
            double lo = outline.Min(p => p.Y) - 1, hi = outline.Max(p => p.Y) + 1;
            var band = new List<Vec2> { new Vec2(u0, lo), new Vec2(u1, lo), new Vec2(u1, hi), new Vec2(u0, hi) };
            var piece = PolygonOps.ClipToConvex(outline, band);
            if (piece.Count >= 3 && Math.Abs(PolygonOps.SignedArea(piece)) > 1e-9)
                yield return new Piece { Surface = wall, Poly = PolygonOps.CounterClockwise(piece), Ext = ext, Label = label };
        }

        /// <summary>A roof row on the building: its outline on one roof plane (<see cref="EnvelopeZoneForce.PlanPolygon"/>,
        /// calculation axes) moved to the plan and put on the building's roof plane it lies on (the one it overlaps most).</summary>
        private static IEnumerable<Piece> RoofPieces(WindGeometry g, EnvelopeZoneForce row, Func<Vec2, Vec2> toPlan, double ext, string label)
        {
            var poly = PolygonOps.CounterClockwise((row.PlanPolygon ?? new List<Vec2>
            {
                new Vec2(row.S0, row.T0), new Vec2(row.S1, row.T0), new Vec2(row.S1, row.T1), new Vec2(row.S0, row.T1),
            }).Select(toPlan).ToList());
            List<Vec2> best = null; BuildingSurface on = null; double most = 1e-6;
            foreach (var roof in g.Roofs)
            {
                var piece = PolygonOps.ClipToConvex(poly, PolygonOps.CounterClockwise(roof.PlanRegion.ToList()));
                double a = piece.Count >= 3 ? Math.Abs(PolygonOps.SignedArea(piece)) : 0;
                if (a > most) { most = a; best = piece; on = roof; }
            }
            if (on != null)
                yield return new Piece { Surface = on, Poly = PolygonOps.CounterClockwise(best), Ext = ext, Label = label };
        }

        private static double Along(BuildingSurface wall, Vec2 p) => (new Vec3(p.X, p.Y, 0) - wall.Origin).Dot(wall.U);

        /// <summary>Distance in plan from <paramref name="p"/> to the line of the wall's base.</summary>
        private static double DistanceToWallLine(BuildingSurface wall, Vec2 p)
        {
            var n = wall.Normal;
            var d = new Vec3(p.X, p.Y, 0) - wall.Origin;
            return Math.Abs(d.X * n.X + d.Y * n.Y);
        }
    }
}
