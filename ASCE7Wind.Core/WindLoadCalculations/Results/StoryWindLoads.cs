using System;
using System.Collections.Generic;
using System.Linq;

namespace ASCE7WindLoadCalculator
{
    /// <summary>The wind on one floor diaphragm (or the roof), each direction (kips).</summary>
    public sealed class StoryForce
    {
        /// <summary>1 = the floor over the first story ... <see cref="BuildingData.NumberOfStories"/> = the roof.</summary>
        public int Level { get; internal set; }

        /// <summary>Elevation of this diaphragm (ft): the top of the walls of story <see cref="Level"/>.</summary>
        public double Elevation { get; internal set; }

        /// <summary>The horizontal strip of wall whose wind this diaphragm takes (ft). The roof's strip runs to the top of the
        /// walls (gable included) and also takes the roof and parapet forces; level 1's strip starts at the ground.</summary>
        public double StripBottom { get; internal set; }
        public double StripTop { get; internal set; }

        /// <summary>Calculated force (walls in the strip, plus roof and parapets at the top level).</summary>
        public double AlongLengthKips { get; internal set; }
        public double AlongWidthKips { get; internal set; }

        /// <summary>The 27.1.5 / 28.3.4 minimum on the strip (16 psf walls, 8 psf roof at the top level).</summary>
        public double AlongLengthMinimumKips { get; internal set; }
        public double AlongWidthMinimumKips { get; internal set; }

        /// <summary>Force used for design: the calculated force, or the minimum when the minimum governs the whole building
        /// in that direction (so the floors add up to the building's design force).</summary>
        public double AlongLengthDesignKips { get; internal set; }
        public double AlongWidthDesignKips { get; internal set; }

        /// <summary>The load cases for this diaphragm: the same cases (names) as the whole building, with this floor's forces.</summary>
        public List<WindLoadCase> LoadCases { get; } = new List<WindLoadCase>();
    }

    public sealed class StoryWindResult
    {
        /// <summary>The whole building (as for one story).</summary>
        public WindLateralResult Building { get; internal set; }

        /// <summary>Bottom to top: level 1 first, the roof last.</summary>
        public List<StoryForce> Levels { get; } = new List<StoryForce>();

        public double StoryHeight { get; internal set; }
    }

    /// <summary>
    /// The wind on each diaphragm of a building of 1 to 3 stories (each story its own plate height; the eave height is their sum).
    /// Each diaphragm takes the walls' wind from the mid-height of the story below it to the mid-height of the story above;
    /// the roof takes from the mid-height of the top story to the top of the walls plus the roof and parapet forces. Level 1
    /// also takes the bottom half of the first story (which really goes straight into the foundation): conservative, and the
    /// levels then add up exactly to the building's base shear, so one story gives the same loads as before.
    /// <para>Chapter 27: the windward wall uses qz over its strip, the leeward wall qh. Every Fig. 27.3-8 case is formed for each
    /// floor from that floor's forces (eccentricity 0.15 B of the building). Chapter 28: the zone pressures do not change with
    /// height, so each case is split between the floors by wall area, the roof and parapet parts going to the roof.</para>
    /// </summary>
    public static class StoryWindLoads
    {
        /// <summary>The wall strip of each level, bottom to top (ft); the last one is open to the top of the walls.</summary>
        public static List<(double bottom, double top, double elevation)> Strips(double eaveHeight, int stories)
        {
            stories = Math.Max(1, stories);
            return Strips(Enumerable.Repeat(eaveHeight / stories, stories).ToArray());
        }

        /// <summary>
        /// The wall strip of each level for stories of these plate heights (bottom first, ft): from the mid-height of the story
        /// below the diaphragm to the mid-height of the story above it (level 1 from the ground; the roof open to the top).
        /// </summary>
        public static List<(double bottom, double top, double elevation)> Strips(IList<double> plateHeights)
        {
            var h = plateHeights == null || plateHeights.Count == 0 ? new List<double> { 0 } : plateHeights;
            var list = new List<(double, double, double)>();
            double z = 0;
            for (int i = 0; i < h.Count; i++)
            {
                double floor = z;
                z += h[i];
                double bottom = i == 0 ? 0 : floor + h[i] / 2;
                double top = i == h.Count - 1 ? double.PositiveInfinity : z + h[i + 1] / 2;
                list.Add((bottom, top, z));
            }
            return list;
        }

        public static StoryWindResult Compute(WindAnalysisResult analysis, bool onlyCases1And3, int stories)
        {
            if (!(analysis?.MwfrsAlongLength is WindLoadCalculator_MWFRS mx) || !(analysis.MwfrsAlongWidth is WindLoadCalculator_MWFRS my))
                throw new ArgumentException("Both MWFRS directions are needed.", nameof(analysis));

            var whole = WindLateralLoads.Compute(analysis, onlyCases1And3);
            var b = mx.buildingData;
            // The building's own plate heights when it has this many stories; otherwise the eave height split equally.
            var strips = Math.Max(1, stories) == b.NumberOfStories ? Strips(b.StoryPlateHeights) : Strips(b.BuildingHeight, stories);
            var r = new StoryWindResult { Building = whole, StoryHeight = b.BuildingHeight / Math.Max(1, stories) };

            var dx = whole.AlongLength;
            var dy = whole.AlongWidth;
            for (int i = 0; i < strips.Count; i++)
            {
                var (bottom, top, elevation) = strips[i];
                bool roof = i == strips.Count - 1;
                var f = new StoryForce { Level = i + 1, Elevation = elevation, StripBottom = bottom, StripTop = top };

                f.AlongLengthKips = LevelForce(whole, mx, dx, true, bottom, top, roof);
                f.AlongWidthKips = LevelForce(whole, my, dy, false, bottom, top, roof);
                f.AlongLengthMinimumKips = LevelMinimum(dx, b.BuildingHeight, bottom, top, roof, whole.Procedure);
                f.AlongWidthMinimumKips = LevelMinimum(dy, b.BuildingHeight, bottom, top, roof, whole.Procedure);
                f.AlongLengthDesignKips = dx.MinimumGoverns ? f.AlongLengthMinimumKips : f.AlongLengthKips;
                f.AlongWidthDesignKips = dy.MinimumGoverns ? f.AlongWidthMinimumKips : f.AlongWidthKips;
                r.Levels.Add(f);
            }

            if (whole.Procedure == LateralProcedure.Envelope)
                SplitEnvelopeCases(whole, r, b.BuildingHeight);
            else
                foreach (var f in r.Levels)
                    WindLateralLoads.AddDirectionalCases(f.LoadCases, f.AlongLengthDesignKips, f.AlongWidthDesignKips,
                        dx.WidthNormalToWind, dy.WidthNormalToWind, onlyCases1And3);
            return r;
        }

        /// <summary>Calculated force on one strip, one direction.</summary>
        private static double LevelForce(WindLateralResult whole, WindLoadCalculator_MWFRS calc, DirectionalWindForce d, bool alongLength,
                                         double bottom, double top, bool roof)
        {
            double above = roof ? d.RoofKips + d.ParapetKips : 0;
            if (whole.Procedure == LateralProcedure.Envelope)
                return d.WallsKips * WallShare(d, calc.buildingData.BuildingHeight, bottom, top, roof) + above;
            return MwfrsLateralLoad.WallShearKipsBetween(calc, bottom, top) + above;
        }

        /// <summary>The fraction of the projected wall area (gable and parapet at the top) in a strip.</summary>
        private static double WallShare(DirectionalWindForce d, double eave, double bottom, double top, bool roof)
        {
            if (!(d.ProjectedWallArea > 0)) return roof ? 1 : 0;
            double area = roof ? d.ProjectedWallArea - d.WidthNormalToWind * bottom : d.WidthNormalToWind * (Math.Min(top, eave) - bottom);
            return Math.Max(0, area) / d.ProjectedWallArea;
        }

        private static double LevelMinimum(DirectionalWindForce d, double eave, double bottom, double top, bool roof, LateralProcedure procedure)
        {
            double wall = WallShare(d, eave, bottom, top, roof) * d.ProjectedWallArea;
            double wallPsf = WindLateralLoads.MinimumWallPressure, roofPsf = WindLateralLoads.MinimumRoofPressure;
            double min = (wallPsf * wall + (roof ? roofPsf * d.ProjectedRoofArea : 0)) / 1000.0;
            // Chapter 28's minimum (28.3.4) is formed by the envelope code; split it the same way as its areas.
            if (procedure == LateralProcedure.Envelope && d.MinimumKips > 0)
            {
                double total = (wallPsf * d.ProjectedWallArea + roofPsf * d.ProjectedRoofArea) / 1000.0;
                return total > 0 ? d.MinimumKips * min / total : 0;
            }
            return min;
        }

        /// <summary>Chapter 28: each building case is shared between the floors in the proportion of that direction's forces.</summary>
        private static void SplitEnvelopeCases(WindLateralResult whole, StoryWindResult r, double eave)
        {
            double totalX = r.Levels.Sum(l => l.AlongLengthKips), totalY = r.Levels.Sum(l => l.AlongWidthKips);
            double minX = r.Levels.Sum(l => l.AlongLengthMinimumKips), minY = r.Levels.Sum(l => l.AlongWidthMinimumKips);
            foreach (var c in whole.LoadCases)
            {
                bool x = c.Name.Contains("along length");
                bool minimum = c.Name.Contains("minimum");
                foreach (var l in r.Levels)
                {
                    double share = minimum
                        ? (x ? Ratio(l.AlongLengthMinimumKips, minX) : Ratio(l.AlongWidthMinimumKips, minY))
                        : (x ? Ratio(l.AlongLengthKips, totalX) : Ratio(l.AlongWidthKips, totalY));
                    l.LoadCases.Add(new WindLoadCase { CaseNumber = c.CaseNumber, Name = c.Name, Vx = c.Vx * share, Vy = c.Vy * share, Mt = c.Mt * share });
                }
            }
        }

        private static double Ratio(double part, double whole) => whole > 0 ? part / whole : 0;
    }
}
