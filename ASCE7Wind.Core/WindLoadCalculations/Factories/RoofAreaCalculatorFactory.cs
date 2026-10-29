using System;

namespace ASCE7WindLoadCalculator
{
    public static class RoofAreaCalculatorFactory
    {
        public static AreaCalculator_Base Create(
            BuildingData bldg_data,
            WindParameters_Base parameters,
            ASCE7_Versions version
            )
        {
            if(bldg_data == null)
            {
                return null;
            }

            if (version == ASCE7_Versions.ASCE_VER_7_16)
            {
                if (parameters.AnalysisType == WindLoadCalculationTypes.COMPONENT_AND_CLADDING)
                {
                    if (bldg_data.RoofType == RoofTypes.ROOF_TYPE_FLAT)
                    {
                        return new FlatRoofAreaCalculator_CC_ASCE7_16(bldg_data);
                    }
                    else if (bldg_data.RoofType == RoofTypes.ROOF_TYPE_GABLE)
                    {
                        if (bldg_data.RoofPitch < 7) return new FlatRoofAreaCalculator_CC_ASCE7_16(bldg_data);
                        else return new GableRoofAreaCalculator_CC_ASCE7_16(bldg_data);
                    }
                    else if (bldg_data.RoofType == RoofTypes.ROOF_TYPE_HIP)
                    {
                        if (bldg_data.RoofPitch < 7) return new FlatRoofAreaCalculator_CC_ASCE7_16(bldg_data);
                        else return new HipRoofAreaCalculator_CC_ASCE7_16(bldg_data);
                    }
                    else
                    {
                        throw new NotImplementedException("ERROR: Invalid roof type: " + bldg_data.RoofType + " in RoofAreaCalculatorFactory.");
                    }
                }
                else if (parameters.AnalysisType == WindLoadCalculationTypes.MWFRS)
                {

                    if (bldg_data.RoofType == RoofTypes.ROOF_TYPE_FLAT)
                    {
                        return new FlatRoofAreaCalculator_NoRidge_MWFRS_ASCE7_16(bldg_data);
                    }
                    else if (bldg_data.RoofType == RoofTypes.ROOF_TYPE_GABLE)
                    {
                        // normal to ridge for slope >= 10deg
                        if(bldg_data.RoofPitch >= 10 && bldg_data.RidgeDirection == RidgeDirections.RIDGE_DIR_PERP_TO_BLDGLENGTH)
                        {
                            return new GableRoofAreaCalculator_PerpToRidge_MWFRS_ASCE7_16(bldg_data);
                        } 
                        // normal to ridge for slope < 10 deg
                        else if (bldg_data.RoofPitch <= 10 && bldg_data.RidgeDirection == RidgeDirections.RIDGE_DIR_PERP_TO_BLDGLENGTH)
                        {
                            return new GableRoofAreaCalculator_PerpToRidge_MWFRS_ASCE7_16(bldg_data);
                        }

                        // parallel to slope
                        else if (bldg_data.RidgeDirection == RidgeDirections.RIDGE_DIR_PARALLEL_TO_BLDGLENGTH)
                        {
                            return new GableRoofAreaCalculator_ParallelToRidge_MWFRS_ASCE7_16(bldg_data);
                        }
                    }
                    else if (bldg_data.RoofType == RoofTypes.ROOF_TYPE_HIP)
                    {
                        // normal to ridge for slope >= 10deg
                        if (bldg_data.RoofPitch >= 10 && bldg_data.RidgeDirection == RidgeDirections.RIDGE_DIR_PERP_TO_BLDGLENGTH)
                        {
                            return new HipRoofAreaCalculator_PerpToRidge_MWFRS_ASCE7_16(bldg_data);
                        }
                        // normal to ridge for slope < 10 deg
                        else if (bldg_data.RoofPitch <= 10 && bldg_data.RidgeDirection == RidgeDirections.RIDGE_DIR_PERP_TO_BLDGLENGTH)
                        {
                            return new HipRoofAreaCalculator_PerpToRidge_MWFRS_ASCE7_16(bldg_data);
                        }

                        // parallel to slope
                        else if (bldg_data.RidgeDirection == RidgeDirections.RIDGE_DIR_PARALLEL_TO_BLDGLENGTH)
                        {
                            return new HipRoofAreaCalculator_ParallelToRidge_MWFRS_ASCE7_16(bldg_data);
                        }
                    }
                    else
                    {
                        throw new NotImplementedException("ERROR: Invalid roof type: " + bldg_data.RoofType + " in RoofAreaCalculatorFactory.");
                    }
                }
            } else if (version == ASCE7_Versions.ASCE_VER_7_22)
            {
                if (parameters.AnalysisType == WindLoadCalculationTypes.COMPONENT_AND_CLADDING)
                {
                    if (bldg_data.RoofType == RoofTypes.ROOF_TYPE_FLAT)
                    {
                        return new FlatRoofAreaCalculator_CC_ASCE7_22(bldg_data);
                    }
                    else if (bldg_data.RoofType == RoofTypes.ROOF_TYPE_GABLE)
                    {
                        if (bldg_data.RoofPitch < 7) return new FlatRoofAreaCalculator_CC_ASCE7_22(bldg_data);
                        else return new GableRoofAreaCalculator_CC_ASCE7_22(bldg_data);
                    }
                    else if (bldg_data.RoofType == RoofTypes.ROOF_TYPE_HIP)
                    {
                        if (bldg_data.RoofPitch < 7) return new FlatRoofAreaCalculator_CC_ASCE7_22(bldg_data);
                        else return new HipRoofAreaCalculator_CC_ASCE7_22(bldg_data);
                    }
                    else
                    {
                        throw new NotImplementedException("ERROR: Invalid roof type: " + bldg_data.RoofType + " in RoofAreaCalculatorFactory.");
                    }
                }
                else if (parameters.AnalysisType == WindLoadCalculationTypes.MWFRS)
                {

                    if (bldg_data.RoofType == RoofTypes.ROOF_TYPE_FLAT)
                    {
                        return new FlatRoofAreaCalculator_NoRidge_MWFRS_ASCE7_22(bldg_data);
                    }
                    else if (bldg_data.RoofType == RoofTypes.ROOF_TYPE_GABLE)
                    {
                        // normal to ridge for slope >= 10deg
                        if (bldg_data.RoofPitch >= 10 && bldg_data.RidgeDirection == RidgeDirections.RIDGE_DIR_PERP_TO_BLDGLENGTH)
                        {
                            return new GableRoofAreaCalculator_PerpToRidge_MWFRS_ASCE7_22(bldg_data);
                        }
                        // normal to ridge for slope < 10 deg
                        else if (bldg_data.RoofPitch <= 10 && bldg_data.RidgeDirection == RidgeDirections.RIDGE_DIR_PERP_TO_BLDGLENGTH)
                        {
                            return new GableRoofAreaCalculator_PerpToRidge_MWFRS_ASCE7_22(bldg_data);
                        }

                        // parallel to slope
                        else if (bldg_data.RidgeDirection == RidgeDirections.RIDGE_DIR_PARALLEL_TO_BLDGLENGTH)
                        {
                            return new GableRoofAreaCalculator_ParallelToRidge_MWFRS_ASCE7_22(bldg_data);
                        }
                    }
                    else if (bldg_data.RoofType == RoofTypes.ROOF_TYPE_HIP)
                    {
                        // normal to ridge for slope >= 10deg
                        if (bldg_data.RoofPitch >= 10 && bldg_data.RidgeDirection == RidgeDirections.RIDGE_DIR_PERP_TO_BLDGLENGTH)
                        {
                            return new HipRoofAreaCalculator_PerpToRidge_MWFRS_ASCE7_22(bldg_data);
                        }
                        // normal to ridge for slope < 10 deg
                        else if (bldg_data.RoofPitch <= 10 && bldg_data.RidgeDirection == RidgeDirections.RIDGE_DIR_PERP_TO_BLDGLENGTH)
                        {
                            return new HipRoofAreaCalculator_PerpToRidge_MWFRS_ASCE7_22(bldg_data);
                        }

                        // parallel to slope
                        else if (bldg_data.RidgeDirection == RidgeDirections.RIDGE_DIR_PARALLEL_TO_BLDGLENGTH)
                        {
                            return new HipRoofAreaCalculator_ParallelToRidge_MWFRS_ASCE7_22(bldg_data);
                        }
                    }
                    else
                    {
                        throw new NotImplementedException("ERROR: Invalid roof type: " + bldg_data.RoofType + " in RoofAreaCalculatorFactory.");
                    }
                }
                else
                {
                    throw new NotImplementedException("ERROR: Invalid analysis type: " + parameters.AnalysisType + " in RoofAreaCalculatorFactory.");
                }
            } else
            {
                throw new NotImplementedException("ERROR: Invalid ASCE version: " + version + " in RoofAreaCalculatorFactory.");
            }
            return null;
        }
    }
}
