using System;

namespace ASCE7WindLoadCalculator
{
    /// <summary>The three calculations a building needs: cladding, and whole-building wind in each plan direction.</summary>
    public sealed class WindAnalysisResult
    {
        public WindLoadCalculator_Base Cladding { get; internal set; }

        /// <summary>MWFRS with the wind along the BuildingLength.</summary>
        public WindLoadCalculator_Base MwfrsAlongLength { get; internal set; }

        /// <summary>MWFRS with the wind along the BuildingWidth (analysed on the building turned 90 degrees).</summary>
        public WindLoadCalculator_Base MwfrsAlongWidth { get; internal set; }
    }

    /// <summary>Single entry point that runs every wind calculation for a building.</summary>
    public static class WindAnalysis
    {
        /// <exception cref="ArgumentException">The building dimensions are not usable (zero or negative).</exception>
        public static WindAnalysisResult Run(ASCE7_Versions version, WindParameters_Base parameters, BuildingData building)
        {
            if (building == null) throw new ArgumentNullException(nameof(building));
            if (!(building.BuildingLength > 0) || !(building.BuildingWidth > 0) || !(building.BuildingHeight > 0))
                throw new ArgumentException("Building length, width and wall height must all be greater than zero.");
            if (building.RoofTypeIsSloped() && !(building.RoofPitch > 0 && building.RoofPitch < 90))
                throw new ArgumentException("Roof pitch must be between 0 and 90 degrees.");

            parameters = parameters ?? new WindParameters_Base();
            var rotated = building.Clone();
            rotated.RotateBuilding();

            return new WindAnalysisResult
            {
                Cladding = Calculate(version, WindLoadCalculationTypes.COMPONENT_AND_CLADDING, parameters, building),
                MwfrsAlongLength = Calculate(version, WindLoadCalculationTypes.MWFRS, parameters, building),
                MwfrsAlongWidth = Calculate(version, WindLoadCalculationTypes.MWFRS, parameters, rotated),
            };
        }

        private static WindLoadCalculator_Base Calculate(ASCE7_Versions version, WindLoadCalculationTypes type, WindParameters_Base parameters, BuildingData building)
        {
            var p = parameters.Clone();
            p.AnalysisType = type;
            var calc = WindLoadCalculatorFactory.Create(version, type, p, building);
            calc.Initialize();
            return calc;
        }
    }
}
