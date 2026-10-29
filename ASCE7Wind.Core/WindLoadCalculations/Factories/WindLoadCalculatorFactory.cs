using System;

namespace ASCE7WindLoadCalculator
{
    public static class WindLoadCalculatorFactory
    {
        public static WindLoadCalculator_Base Create(
            ASCE7_Versions version,
            WindLoadCalculationTypes calculationType,
            WindParameters_Base parameters,
            BuildingData buildingData)
        {
            var edition = Asce7Edition.For(version);
            switch (calculationType)
            {
                case WindLoadCalculationTypes.COMPONENT_AND_CLADDING:
                    return new WindLoadCalculator_CC(edition, parameters, buildingData);
                case WindLoadCalculationTypes.MWFRS:
                    return new WindLoadCalculator_MWFRS(edition, parameters, buildingData);
                default:
                    throw new NotSupportedException("Calculation type not supported: " + calculationType);
            }
        }
    }
}
