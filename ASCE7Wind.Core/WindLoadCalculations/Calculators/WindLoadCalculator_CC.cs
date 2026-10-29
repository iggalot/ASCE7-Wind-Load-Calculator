using System;

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
            Func<EffectiveWindArea, double> byArea = a => a.Area;

            AddPressures(RoofAreaCalculator, extGCpCurve_Roof.RoofCurves_Pos, byArea, qh, 1.0, windPressureRoof_Pos_External);
            AddPressures(RoofAreaCalculator, extGCpCurve_Roof.RoofCurves_Neg, byArea, qh, 1.0, windPressureRoof_Neg_External);
            AddPressures(WallAreaCalculator_BldgLength, extGCpCurve_Wall.WallCurves_Pos, byArea, qh, 1.0, windPressureBuildingLengthWall_Pos_External);
            AddPressures(WallAreaCalculator_BldgLength, extGCpCurve_Wall.WallCurves_Neg, byArea, qh, 1.0, windPressureBuildingLengthWall_Neg_External);
            AddPressures(WallAreaCalculator_BldgWidth, extGCpCurve_Wall.WallCurves_Pos, byArea, qh, 1.0, windPressureBuildingWidthWall_Pos_External);
            AddPressures(WallAreaCalculator_BldgWidth, extGCpCurve_Wall.WallCurves_Neg, byArea, qh, 1.0, windPressureBuildingWidthWall_Neg_External);
        }
    }
}
