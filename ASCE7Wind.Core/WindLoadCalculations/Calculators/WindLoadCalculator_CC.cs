using System;
using System.Collections.Generic;

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

        /// <summary>Minimum C&amp;C net design pressure (psf), either direction (30.2.2).</summary>
        public const double MinimumNetPressure = 16.0;

        /// <summary>Effective wind area (sq ft) the roof GCp is read at.</summary>
        public double RoofComponentArea => Parameters.RoofComponentArea > 0 ? Parameters.RoofComponentArea : WindParameters_Base.DefaultComponentArea;

        /// <summary>Effective wind area (sq ft) the wall GCp is read at.</summary>
        public double WallComponentArea => Parameters.WallComponentArea > 0 ? Parameters.WallComponentArea : WindParameters_Base.DefaultComponentArea;
    }
}
