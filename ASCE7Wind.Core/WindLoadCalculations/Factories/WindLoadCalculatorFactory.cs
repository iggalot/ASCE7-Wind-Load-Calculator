using System;

namespace ASCE7WindLoadCalculator
{
    public static class WindLoadCalculatorFactory
    {
        public static WindLoadCalculator_Base Create(
            ASCE7_Versions version,
            WindLoadCalculationTypes calculationType,
            WindParameters_Base parameters,
            BuildingData buildingData
            )
        {
            if (version == ASCE7_Versions.ASCE_VER_7_16)
            {
                if (calculationType == WindLoadCalculationTypes.COMPONENT_AND_CLADDING)
                {
                    return new WindLoadCalculator_CC_ASCE7_16(parameters, buildingData);
                }
                else if (calculationType == WindLoadCalculationTypes.MWFRS)
                {
                    return new WindLoadCalculator_MWFRS_ASCE7_16(parameters, buildingData);
                }
            }

            else if (version == ASCE7_Versions.ASCE_VER_7_22)
            {
                if (calculationType == WindLoadCalculationTypes.COMPONENT_AND_CLADDING)
                {
                    return new WindLoadCalculator_CC_ASCE7_22(parameters, buildingData);
                }
                else if (calculationType == WindLoadCalculationTypes.MWFRS)
                {
                    return new WindLoadCalculator_MWFRS_ASCE7_22(parameters, buildingData);
                }
            }

            throw new NotImplementedException("Error: ASCE7 version " + version + " is not implemented or " + calculationType + " calculation type is not implemented at this time.");
        }
    }
}
