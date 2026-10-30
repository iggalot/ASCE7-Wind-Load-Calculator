using System;
using System.Collections.Generic;
using System.Linq;

namespace ASCE7WindLoadCalculator
{
    /// <summary>Horizontal MWFRS wind force for one wind direction (kips, external pressures only).</summary>
    public sealed class DirectionalWindForce
    {
        /// <summary>"along length" (wind from the west, +x) or "along width" (wind from the south, +y).</summary>
        public string Direction { get; internal set; }

        /// <summary>Windward + leeward wall force (windward integrated with qz up the wall).</summary>
        public double WallsKips { get; internal set; }

        /// <summary>Horizontal component of the roof pressures for the governing roof Cp value.</summary>
        public double RoofKips { get; internal set; }

        /// <summary>Windward + leeward parapet force (27.3.4), 0 without a parapet.</summary>
        public double ParapetKips { get; internal set; }

        /// <summary>Which Fig. 27.3-1 roof Cp value governs the total: the more negative or the other one.</summary>
        public MwfrsRoofCase GoverningRoofCase { get; internal set; }

        /// <summary>Walls + roof (governing roof case) + parapets.</summary>
        public double TotalKips => WallsKips + RoofKips + ParapetKips;

        /// <summary>27.1.5 minimum: 16 psf on the wall area and 8 psf on the roof area projected normal to the wind.</summary>
        public double MinimumKips { get; internal set; }

        /// <summary>Force used for design: the larger of the calculated total and the 27.1.5 minimum.</summary>
        public double DesignKips => Math.Max(TotalKips, MinimumKips);

        public bool MinimumGoverns => MinimumKips > TotalKips;

        /// <summary>Wall area projected normal to the wind (ft²), including any gable triangle and the parapet (it counts as wall area for the 27.1.5 minimum).</summary>
        public double ProjectedWallArea { get; internal set; }

        /// <summary>Roof area projected normal to the wind (ft²), above the windward wall.</summary>
        public double ProjectedRoofArea { get; internal set; }

        /// <summary>Plan dimension perpendicular to the wind (ft) - B in Fig. 27.3-8.</summary>
        public double WidthNormalToWind { get; internal set; }
    }

    /// <summary>One Fig. 27.3-8 design wind load case, as forces at the building's plan center.</summary>
    public sealed class WindLoadCase
    {
        /// <summary>1 to 4 per Fig. 27.3-8.</summary>
        public int CaseNumber { get; internal set; }
        public string Name { get; internal set; }

        /// <summary>Force along BuildingLength (+x, kips).</summary>
        public double Vx { get; internal set; }

        /// <summary>Force along BuildingWidth (+y, kips).</summary>
        public double Vy { get; internal set; }

        /// <summary>Torsional moment about the vertical axis, counter-clockwise positive (kip-ft).</summary>
        public double Mt { get; internal set; }
    }

    public sealed class WindLateralResult
    {
        public DirectionalWindForce AlongLength { get; internal set; }
        public DirectionalWindForce AlongWidth { get; internal set; }
        public List<WindLoadCase> LoadCases { get; } = new List<WindLoadCase>();

        /// <summary>True when only Load Cases 1 and 3 were generated (Fig. 27.3-8 exception).</summary>
        public bool OnlyCases1And3 { get; internal set; }
    }

    /// <summary>
    /// Lateral wind loads for the shear-wall design from the MWFRS results (Chapter 27, directional
    /// procedure): directional forces including the roof and the 27.1.5 minimum, and the Fig. 27.3-8 load
    /// cases 1-4 with their 0.15B eccentricities.
    /// </summary>
    public static class WindLateralLoads
    {
        public const double MinimumWallPressure = 16.0; // psf, 27.1.5
        public const double MinimumRoofPressure = 8.0;  // psf, 27.1.5
        public const double CaseEccentricity = 0.15;    // e = 0.15 B, Fig. 27.3-8

        /// <param name="onlyCases1And3">
        /// Fig. 27.3-8 exception: one-story buildings with h &lt;= 30 ft, buildings two stories or less
        /// framed with light-frame construction, and buildings two stories or less designed with flexible
        /// diaphragms need only Load Cases 1 and 3.
        /// </param>
        public static WindLateralResult Compute(WindAnalysisResult analysis, bool onlyCases1And3 = false)
        {
            if (analysis?.MwfrsAlongLength == null || analysis.MwfrsAlongWidth == null)
                throw new ArgumentException("Both MWFRS directions are needed.", nameof(analysis));

            var r = new WindLateralResult
            {
                AlongLength = DirectionalForce(analysis.MwfrsAlongLength, "along length"),
                AlongWidth = DirectionalForce(analysis.MwfrsAlongWidth, "along width"),
                OnlyCases1And3 = onlyCases1And3,
            };

            double vx = r.AlongLength.DesignKips, vy = r.AlongWidth.DesignKips;
            double bx = r.AlongLength.WidthNormalToWind; // building width B (normal to x wind)
            double by = r.AlongWidth.WidthNormalToWind;  // building length L (normal to y wind)
            double ex = CaseEccentricity * bx, ey = CaseEccentricity * by;

            void Add(int n, string name, double x, double y, double mt) =>
                r.LoadCases.Add(new WindLoadCase { CaseNumber = n, Name = name, Vx = x, Vy = y, Mt = mt });

            // Case 1: full pressure in each principal direction separately.
            Add(1, "Case 1, along length", vx, 0, 0);
            Add(1, "Case 1, along width", 0, vy, 0);

            if (!onlyCases1And3)
            {
                // Case 2: 75% in one direction with torsion 0.75 V e, e = +/-0.15 B.
                foreach (int s in new[] { 1, -1 })
                {
                    Add(2, $"Case 2, along length, {Sign(s)} torsion", 0.75 * vx, 0, s * 0.75 * vx * ex);
                    Add(2, $"Case 2, along width, {Sign(s)} torsion", 0, 0.75 * vy, s * 0.75 * vy * ey);
                }
            }

            // Case 3: 75% in both directions at once (both diagonal directions).
            foreach (int sy in new[] { 1, -1 })
                Add(3, $"Case 3, both directions ({Sign(sy)} width)", 0.75 * vx, sy * 0.75 * vy, 0);

            if (!onlyCases1And3)
            {
                // Case 4: 56.3% in both directions with torsion from both eccentricities.
                foreach (int sy in new[] { 1, -1 })
                    foreach (int s1 in new[] { 1, -1 })
                        foreach (int s2 in new[] { 1, -1 })
                            Add(4, $"Case 4, both directions ({Sign(sy)} width), torsion {Sign(s1)}{Sign(s2)}",
                                0.563 * vx, sy * 0.563 * vy, 0.563 * (s1 * vx * ex + s2 * vy * ey));
            }
            return r;
        }

        private static string Sign(int s) => s > 0 ? "+" : "-";

        /// <summary>Directional force from one MWFRS calculator (wind along +x in that calculator's frame).</summary>
        public static DirectionalWindForce DirectionalForce(WindLoadCalculator_Base calculator, string direction)
        {
            if (!(calculator is WindLoadCalculator_MWFRS))
                throw new ArgumentException("An MWFRS calculator is needed.", nameof(calculator));

            var b = calculator.buildingData;
            var model = new BuildingModel(b);

            double walls = MwfrsLateralLoad.WallBaseShearKips(calculator);
            double roofA = RoofHorizontalKips(calculator, model, calculator.windPressureRoof_Neg_External);
            double roofB = RoofHorizontalKips(calculator, model, calculator.windPressureRoof_Pos_External);

            var (wallArea, roofArea) = ProjectedAreas(model);
            var mwfrs = (WindLoadCalculator_MWFRS)calculator;
            if (b.HasParapet) wallArea += b.ParapetHeight * b.BuildingWidth;
            return new DirectionalWindForce
            {
                Direction = direction,
                WallsKips = walls,
                RoofKips = roofA >= roofB ? roofA : roofB,
                ParapetKips = mwfrs.ParapetKips,
                GoverningRoofCase = roofA >= roofB ? MwfrsRoofCase.MaxSuction : MwfrsRoofCase.Alternate,
                ProjectedWallArea = wallArea,
                ProjectedRoofArea = roofArea,
                MinimumKips = (MinimumWallPressure * wallArea + MinimumRoofPressure * roofArea) / 1000.0,
                WidthNormalToWind = b.BuildingWidth,
            };
        }

        /// <summary>
        /// Horizontal force along +x from roof pressures (kips). A pressure p on a plane z = Z0 + Zx x + Zy y
        /// acting over plan area A pushes along x with F = p Zx A (inward pressure positive).
        /// </summary>
        public static double RoofHorizontalKips(WindLoadCalculator_Base calculator, BuildingModel model, Dictionary<int, PressureData> pressures)
        {
            var zones = calculator.RoofAreaCalculator;
            if (zones == null) return 0;
            double pounds = 0;
            foreach (var kv in zones.effWindAreas)
            {
                if (!pressures.TryGetValue(kv.Key, out var p)) continue;
                var outer = PolygonOps.CounterClockwise(kv.Value.OuterBoundary);
                foreach (var plane in model.Roofs)
                {
                    if (Math.Abs(plane.Zx) < 1e-12) continue; // plane parallel to the wind: no x component
                    double planArea = Math.Abs(PolygonOps.SignedArea(PolygonOps.ClipToConvex(outer, plane.PlanRegion)));
                    foreach (var hole in kv.Value.Holes)
                        planArea -= Math.Abs(PolygonOps.SignedArea(PolygonOps.ClipToConvex(PolygonOps.CounterClockwise(hole), plane.PlanRegion)));
                    pounds += p.ExternalPressure * plane.Zx * planArea;
                }
            }
            return pounds / 1000.0;
        }

        /// <summary>
        /// Areas projected on a vertical plane normal to +x: the windward wall (up to its top edge, which is
        /// the rake on a gable end) and the roof silhouette above it.
        /// </summary>
        public static (double wall, double roof) ProjectedAreas(BuildingModel model, int samples = 2000)
        {
            double L = model.L, B = model.B, wall = 0, roof = 0, dy = B / samples;
            for (int i = 0; i < samples; i++)
            {
                double y = (i + 0.5) * dy;
                double wallTop = model.RoofHeightAt(0, y);
                double roofTop = Math.Max(wallTop, RoofTopAlongX(model, y, L));
                wall += wallTop * dy;
                roof += (roofTop - wallTop) * dy;
            }
            return (wall, roof);
        }

        private static double RoofTopAlongX(BuildingModel model, double y, double L)
        {
            double best = double.MinValue;
            foreach (double x in new[] { 0.0, L / 2, L, Math.Min(y, L), Math.Max(0, L - y) }.Concat(Enumerable.Range(0, 65).Select(i => L * i / 64.0)))
                best = Math.Max(best, model.RoofHeightAt(Math.Min(Math.Max(x, 0), L), y));
            return best;
        }
    }
}
