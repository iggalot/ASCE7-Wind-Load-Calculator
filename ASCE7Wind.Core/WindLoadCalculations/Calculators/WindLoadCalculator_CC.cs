using System;
using System.Collections.Generic;
using System.Linq;

namespace ASCE7WindLoadCalculator
{
    /// <summary>
    /// Components and cladding pressures, Chapter 30 Part 1 (low-rise / h &lt;= 60 ft).
    /// p = qh [(GCp) - (+/-GCpi)], with Kd applied per the edition.
    /// </summary>
    public class WindLoadCalculator_CC : WindLoadCalculator_Base
    {
        /// <summary>Upper limit of mean roof height for Chapter 30 Part 1 (ft).</summary>
        public const double MaxMeanRoofHeight = 60.0;

        /// <summary>Steepest roof covered by the Chapter 30 Part 1 roof figures (deg).</summary>
        public const double MaxRoofSlope = 45.0;

        public WindLoadCalculator_CC(Asce7Edition edition, WindParameters_Base p, BuildingData bldg_data)
            : base(edition, p, bldg_data)
        {
            if (buildingData.MeanRoofHeight > MaxMeanRoofHeight)
            {
                AddValidationMessage(
                    $"Mean roof height {buildingData.MeanRoofHeight:0.#} ft is above the {MaxMeanRoofHeight:0} ft limit " +
                    "for the low-rise cladding method (Chapter 30 Part 1); cladding pressures were not calculated.");
                return;
            }

            if (buildingData.RoofTypeIsSloped() && buildingData.RoofPitch > MaxRoofSlope)
            {
                AddValidationMessage(
                    $"Roof slope {buildingData.RoofPitch:0.#} deg is steeper than the {MaxRoofSlope:0} deg covered by the " +
                    "Chapter 30 roof figures; the 27-45 deg values were used and should be checked.");
            }

            CreateExtGcpCurves();
        }

        public void CreateExtGcpCurves()
        {
            extGCpCurve_Roof = Edition.CreateCCRoofFigure(buildingData);
            extGCpCurve_Wall = Edition.CreateCCWallFigure();
            ApplyParapetRoofZoneNote();     // before the wall reduction: the roof zones take the figure's own wall values
            ApplyLowSlopeWallReduction();
        }

        /// <summary>Roof angle (deg) at or below which Fig. 30.3-1 note 5 reduces the wall GCp.</summary>
        public const double WallReductionMaxRoofAngle = 10.0;

        /// <summary>Fig. 30.3-1 note 5 factor on wall GCp for low-slope roofs.</summary>
        public const double LowSlopeWallFactor = 0.9;

        /// <summary>True when the wall curves were reduced by Fig. 30.3-1 note 5 (roof angle 10 deg or less).</summary>
        public bool WallReductionApplied { get; private set; }

        /// <summary>
        /// Fig. 30.3-1 note 5 (7-16 and 7-22): values of (GCp) for walls are reduced by 10% when θ ≤ 10°; a flat roof is 0°.
        /// The wall curves, positive and negative, are replaced by scaled copies (the figure objects belong to this calculator),
        /// so the walls, the parapet faces taken from the wall figure and the reports all use the reduced values.
        /// </summary>
        private void ApplyLowSlopeWallReduction()
        {
            double theta = buildingData.RoofTypeIsSloped() ? buildingData.RoofPitch : 0;
            WallReductionApplied = theta <= WallReductionMaxRoofAngle;
            if (!WallReductionApplied) return;
            foreach (var set in new[] { extGCpCurve_Wall.WallCurves_Pos, extGCpCurve_Wall.WallCurves_Neg })
                foreach (var key in set.Keys.ToList())
                {
                    var c = set[key];
                    set[key] = new ExternalGCpCurve(c.Points.Select(p => (p.X, p.Y * LowSlopeWallFactor)).ToArray(), c.LogX);
                }
        }

        /// <summary>Smallest parapet height (ft) for the Fig. 30.3-2A note 5 roof-zone adjustment (3 ft, 0.9 m).</summary>
        public const double ParapetZoneNoteMinHeight = 3.0;

        /// <summary>True when Fig. 30.3-2A note 5 was applied to the roof curves (parapet of 3 ft or more, roof 7 deg or flatter).</summary>
        public bool ParapetRoofZoneNoteApplied { get; private set; }

        /// <summary>
        /// Fig. 30.3-2A note 5 (7-16 and 7-22): with a parapet 3 ft (0.9 m) or higher around the perimeter of a roof of
        /// 7 deg or less, negative (GCp) in roof Zone 3 equals Zone 2, and positive (GCp) in roof Zones 2 and 3 equal
        /// wall Zones 4 and 5 (Fig. 30.3-1). The parapet is taken to run around the whole perimeter. The figure objects
        /// are created fresh for this calculator, so the curves are replaced in place.
        /// </summary>
        private void ApplyParapetRoofZoneNote()
        {
            ParapetRoofZoneNoteApplied = false;
            if (!buildingData.HasParapet || buildingData.ParapetHeight < ParapetZoneNoteMinHeight) return;
            var roofPos = extGCpCurve_Roof.RoofCurves_Pos; var roofNeg = extGCpCurve_Roof.RoofCurves_Neg; var wallPos = extGCpCurve_Wall.WallCurves_Pos;
            if (!roofNeg.ContainsKey("Zone2") || !roofNeg.ContainsKey("Zone3") || !wallPos.ContainsKey("Zone4") || !wallPos.ContainsKey("Zone5")) return;
            roofNeg["Zone3"] = roofNeg["Zone2"];
            if (roofPos.ContainsKey("Zone2")) roofPos["Zone2"] = wallPos["Zone4"];
            if (roofPos.ContainsKey("Zone3")) roofPos["Zone3"] = wallPos["Zone5"];
            ParapetRoofZoneNoteApplied = true;
        }

        public override void CalculateExternalPressures()
        {
            if (extGCpCurve_Roof == null || extGCpCurve_Wall == null)
                return; // outside the method's limits; see ValidationMessages

            double qh = CalculateDynamicWindPressure(buildingData.MeanRoofHeight);

            // GCp is read at the effective wind area of the component being designed (26.2), not at the area of
            // the zone: the zones only say where each curve applies. (Reading at the zone area gave too small a
            // GCp for every real component, e.g. -0.8 instead of -1.5 in a large roof Zone 1.)
            double roofArea = RoofComponentArea, wallArea = WallComponentArea;
            Func<EffectiveWindArea, double> roof = a => roofArea;
            Func<EffectiveWindArea, double> wall = a => wallArea;

            AddPressures(RoofAreaCalculator, extGCpCurve_Roof.RoofCurves_Pos, roof, qh, 1.0, windPressureRoof_Pos_External, MinimumNetPressure);
            AddPressures(RoofAreaCalculator, extGCpCurve_Roof.RoofCurves_Neg, roof, qh, 1.0, windPressureRoof_Neg_External, MinimumNetPressure);
            AddPressures(WallAreaCalculator_BldgLength, extGCpCurve_Wall.WallCurves_Pos, wall, qh, 1.0, windPressureBuildingLengthWall_Pos_External, MinimumNetPressure);
            AddPressures(WallAreaCalculator_BldgLength, extGCpCurve_Wall.WallCurves_Neg, wall, qh, 1.0, windPressureBuildingLengthWall_Neg_External, MinimumNetPressure);
            AddPressures(WallAreaCalculator_BldgWidth, extGCpCurve_Wall.WallCurves_Pos, wall, qh, 1.0, windPressureBuildingWidthWall_Pos_External, MinimumNetPressure);
            AddPressures(WallAreaCalculator_BldgWidth, extGCpCurve_Wall.WallCurves_Neg, wall, qh, 1.0, windPressureBuildingWidthWall_Neg_External, MinimumNetPressure);
            AddOverhangPressures(qh, roofArea);
            AddParapetCases(wallArea);
        }

        /// <summary>
        /// True for roof zones whose overhang GCp was built as roof + wall (ASCE 7-22, where the figure has no overhang
        /// curve: gable roofs over 7 deg and hip roofs). False where a figure overhang curve was read.
        /// </summary>
        public Dictionary<int, bool> OverhangFromRoofPlusWall { get; } = new Dictionary<int, bool>();

        /// <summary>
        /// Roof overhang pressures for every roof zone, read at the roof component area. The figure overhang curves already
        /// include the top and bottom surfaces, so no internal pressure is applied (the soffit is outside the building).
        /// ASCE 7-22 dropped the overhang curves for gable roofs over 7 deg and for hip roofs: there the overhang GCp is the
        /// roof-zone GCp plus the positive wall GCp below it (Zone 5 under the corner zones 3, Zone 4 elsewhere; TO VERIFY
        /// against the printed standard, see docs/DATA_VERIFICATION.md).
        /// </summary>
        private void AddOverhangPressures(double qh, double area)
        {
            OverhangFromRoofPlusWall.Clear();
            if (RoofAreaCalculator == null) return;
            double kd = PressureKd;
            bool roofPlusWall22 = Edition.Version == ASCE7_Versions.ASCE_VER_7_22;
            foreach (var zone in RoofAreaCalculator.effWindAreas)
            {
                double gcp;
                bool summed = false;
                if (TryEvaluateCurve(extGCpCurve_Roof.OverhangCurves, zone.Value, area, out gcp)) { }
                else if (roofPlusWall22
                         && TryEvaluateCurve(extGCpCurve_Roof.RoofCurves_Neg, zone.Value, area, out double roofGcp)
                         && extGCpCurve_Wall.WallCurves_Pos.TryGetValue(zone.Value.Label_Full.StartsWith("Zone3", StringComparison.Ordinal) ? "Zone5" : "Zone4", out var wallCurve))
                {
                    gcp = roofGcp - wallCurve.Evaluate(area); // suction on top plus the wall pressure pushing up on the soffit
                    summed = true;
                }
                else continue;

                OverhangFromRoofPlusWall[zone.Key] = summed;
                windPressureOverhang_External.Add(zone.Key, new PressureData
                {
                    AreaID = zone.Key, qh = qh, GCp = gcp, ExternalPressure = qh * gcp * kd,
                    GCpi = 0, InternalPressure = 0, MinimumNet = MinimumNetPressure,
                });
            }
        }

        /// <summary>C&amp;C parapet load cases A and B (30.6), corner and edge; empty without a parapet on a flat roof.</summary>
        public List<ParapetCladdingCase> ParapetCases { get; } = new List<ParapetCladdingCase>();

        /// <summary>
        /// Section 30.6, Fig. 30.6-1, qp at the top of the parapet, GCp at the wall component area:
        /// case A (windward parapet): front = positive wall GCp (Zone 5 at corners, 4 elsewhere), back = negative roof GCp
        /// (Zone 3 at corners, 2 elsewhere); case B (leeward parapet): back = positive wall GCp, front = negative wall GCp.
        /// GCpi is from the porosity of the parapet envelope (Table 26.13-1, BuildingData.ParapetEnclosure), and each face
        /// is evaluated under both positive and negative internal pressure.
        /// </summary>
        private void AddParapetCases(double area)
        {
            ParapetCases.Clear();
            if (!buildingData.HasParapet) return;
            var wallPos = extGCpCurve_Wall.WallCurves_Pos; var wallNeg = extGCpCurve_Wall.WallCurves_Neg; var roofNeg = extGCpCurve_Roof.RoofCurves_Neg;
            double qp = CalculateDynamicWindPressure(buildingData.BuildingHeight + buildingData.ParapetHeight);
            double kd = PressureKd, gcpi = GCpiFor(buildingData.ParapetEnclosure);
            double Face(double gcp, double internalCoeff)
            {
                double p = qp * kd * (gcp - internalCoeff);
                return Math.Abs(p) < MinimumNetPressure ? (p >= 0 ? MinimumNetPressure : -MinimumNetPressure) : p;
            }
            double Larger(double x, double y) => Math.Abs(x) >= Math.Abs(y) ? x : y;
            ParapetCladdingCase Make(string kase, string location, string frontSource, double gcpFront, string backSource, double gcpBack)
            {
                double fPos = Face(gcpFront, gcpi), fNeg = Face(gcpFront, -gcpi), bPos = Face(gcpBack, gcpi), bNeg = Face(gcpBack, -gcpi);
                return new ParapetCladdingCase
                {
                    Case = kase, Location = location, Qp = qp, GCpi = gcpi,
                    FrontSource = frontSource, GCpFront = gcpFront, FrontPressurePosGCpi = fPos, FrontPressureNegGCpi = fNeg, FrontPressure = Larger(fPos, fNeg),
                    BackSource = backSource, GCpBack = gcpBack, BackPressurePosGCpi = bPos, BackPressureNegGCpi = bNeg, BackPressure = Larger(bPos, bNeg),
                    NetPressure = Math.Abs(qp * kd * (gcpFront - gcpBack)),
                };
            }
            foreach (var (location, wallZone, roofZone) in new[] { ("corner", "Zone5", "Zone3"), ("edge", "Zone4", "Zone2") })
            {
                if (!wallPos.ContainsKey(wallZone) || !wallNeg.ContainsKey(wallZone) || !roofNeg.ContainsKey(roofZone)) continue;
                double wp = wallPos[wallZone].Evaluate(area), wn = wallNeg[wallZone].Evaluate(area), rn = roofNeg[roofZone].Evaluate(area);
                string roofSource = roofZone + (ParapetRoofZoneNoteApplied && roofZone == "Zone3" ? " (roof, -, = Zone2 by note 5)" : " (roof, -)");
                ParapetCases.Add(Make("A", location, wallZone + " (wall, +)", wp, roofSource, rn));
                ParapetCases.Add(Make("B", location, wallZone + " (wall, -)", wn, wallZone + " (wall, +)", wp));
            }
        }

        /// <summary>Minimum C&amp;C net design pressure (psf), either direction (30.2.2).</summary>
        public const double MinimumNetPressure = 16.0;

        /// <summary>Effective wind area (sq ft) the roof GCp is read at.</summary>
        public double RoofComponentArea => Parameters.RoofComponentArea > 0 ? Parameters.RoofComponentArea : WindParameters_Base.DefaultComponentArea;

        /// <summary>Effective wind area (sq ft) the wall GCp is read at.</summary>
        public double WallComponentArea => Parameters.WallComponentArea > 0 ? Parameters.WallComponentArea : WindParameters_Base.DefaultComponentArea;
    }
}
