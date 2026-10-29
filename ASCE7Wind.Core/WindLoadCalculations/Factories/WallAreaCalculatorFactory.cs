using System;

namespace ASCE7WindLoadCalculator
{
    public static class WallAreaCalculatorFactory
    {
        public static AreaCalculator_Base Create(BuildingData bldg_data, WindParameters_Base parameters, Asce7Edition edition, bool isGable, double wallLength)
        {
            if (bldg_data == null)
                return null;

            switch (parameters.AnalysisType)
            {
                case WindLoadCalculationTypes.COMPONENT_AND_CLADDING:
                    return edition.CreateCCWallZones(bldg_data, isGable, wallLength);
                case WindLoadCalculationTypes.MWFRS:
                    return new WallAreaCalculator_MWFRS(bldg_data);
                default:
                    throw new NotSupportedException("Invalid analysis type: " + parameters.AnalysisType);
            }
        }
    }
}
