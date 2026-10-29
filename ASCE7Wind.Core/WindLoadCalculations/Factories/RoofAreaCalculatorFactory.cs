using System;

namespace ASCE7WindLoadCalculator
{
    public static class RoofAreaCalculatorFactory
    {
        /// <summary>
        /// Roof zone layout for the analysis type. C&amp;C layouts are edition specific; MWFRS layouts
        /// (Fig. 27.3-1) are the same in ASCE 7-16 and 7-22.
        /// </summary>
        public static AreaCalculator_Base Create(BuildingData bldg_data, WindParameters_Base parameters, Asce7Edition edition)
        {
            if (bldg_data == null)
                return null;

            switch (parameters.AnalysisType)
            {
                case WindLoadCalculationTypes.COMPONENT_AND_CLADDING:
                    return edition.CreateCCRoofZones(bldg_data);
                case WindLoadCalculationTypes.MWFRS:
                    return CreateMwfrs(bldg_data);
                default:
                    throw new NotSupportedException("Invalid analysis type: " + parameters.AnalysisType);
            }
        }

        private static AreaCalculator_Base CreateMwfrs(BuildingData b)
        {
            switch (b.RoofType)
            {
                case RoofTypes.ROOF_TYPE_FLAT:
                    return new FlatRoofAreaCalculator_NoRidge_MWFRS(b);
                case RoofTypes.ROOF_TYPE_GABLE:
                    if (b.RidgeDirection == RidgeDirections.RIDGE_DIR_PERP_TO_BLDGLENGTH)
                        return new GableRoofAreaCalculator_PerpToRidge_MWFRS(b);
                    if (b.RidgeDirection == RidgeDirections.RIDGE_DIR_PARALLEL_TO_BLDGLENGTH)
                        return new GableRoofAreaCalculator_ParallelToRidge_MWFRS(b);
                    return null;
                case RoofTypes.ROOF_TYPE_HIP:
                    if (b.RidgeDirection == RidgeDirections.RIDGE_DIR_PERP_TO_BLDGLENGTH)
                        return new HipRoofAreaCalculator_PerpToRidge_MWFRS(b);
                    if (b.RidgeDirection == RidgeDirections.RIDGE_DIR_PARALLEL_TO_BLDGLENGTH)
                        return new HipRoofAreaCalculator_ParallelToRidge_MWFRS(b);
                    return null;
                default:
                    throw new NotSupportedException("Invalid roof type: " + b.RoofType);
            }
        }
    }
}
