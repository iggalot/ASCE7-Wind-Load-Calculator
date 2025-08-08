using System;

namespace ASCE7WindLoadCalculator
{
    public static class WallAreaCalculatorFactory
    {
        public static AreaCalculator_Base Create(
            BuildingData bldg_data,
            WindParameters_Base parameters,
            ASCE7_Versions version,
            bool isGable
            )
        {
            if (bldg_data == null)
            {
                return null;
            }

            if (version == ASCE7_Versions.ASCE_VER_7_16)
            {
                if (parameters.AnalysisType == WindLoadCalculationTypes.COMPONENT_AND_CLADDING)
                {
                    // figure out the wall arrangements
                    if (bldg_data.RoofType == RoofTypes.ROOF_TYPE_FLAT)
                        return new WallAreaCalculator_NonGableEnd_CC_ASCE7_16(bldg_data);
                    else if (bldg_data.RoofType == RoofTypes.ROOF_TYPE_HIP)
                    {
                        return new WallAreaCalculator_NonGableEnd_CC_ASCE7_16(bldg_data);
                    }

                    else if (bldg_data.RoofType == RoofTypes.ROOF_TYPE_GABLE)
                    {
                        return new WallAreaCalculator_NonGableEnd_CC_ASCE7_16(bldg_data);
                    }
                    else
                    {
                        throw new NotSupportedException("Unsupported roof type: " + bldg_data.RoofType);
                    }
                } else if (parameters.AnalysisType == WindLoadCalculationTypes.MWFRS)
                {
                    // figure out the wall arrangements
                    if (bldg_data.RoofType == RoofTypes.ROOF_TYPE_FLAT)
                        return new WallAreaCalculator_MWFRS_ASCE7_16(bldg_data);
                    else if (bldg_data.RoofType == RoofTypes.ROOF_TYPE_HIP)
                    {
                        return new WallAreaCalculator_MWFRS_ASCE7_16(bldg_data);
                    }

                    else if (bldg_data.RoofType == RoofTypes.ROOF_TYPE_GABLE)
                    {
                        return new WallAreaCalculator_MWFRS_ASCE7_16(bldg_data);
                    }
                    else
                    {
                        throw new NotSupportedException("Unsupported roof type: " + bldg_data.RoofType);
                    }
                }
            } else if (version == ASCE7_Versions.ASCE_VER_7_22)
            {
                if (parameters.AnalysisType == WindLoadCalculationTypes.COMPONENT_AND_CLADDING)
                {
                    // figure out the wall arrangements
                    if (bldg_data.RoofType == RoofTypes.ROOF_TYPE_FLAT)
                        return new WallAreaCalculator_NonGableEnd_CC_ASCE7_22(bldg_data);
                    else if (bldg_data.RoofType == RoofTypes.ROOF_TYPE_HIP)
                    {
                        return new WallAreaCalculator_NonGableEnd_CC_ASCE7_22(bldg_data);
                    }

                    else if (bldg_data.RoofType == RoofTypes.ROOF_TYPE_GABLE)
                    {
                        if (isGable is true)
                        {
                            return new WallAreaCalculator_GableEnd_CC_ASCE7_22(bldg_data);
                        }
                        else
                        {
                            return new WallAreaCalculator_NonGableEnd_CC_ASCE7_22(bldg_data);
                        }
                    }
                    else
                    {
                        throw new NotSupportedException("Unsupported roof type: " + bldg_data.RoofType);
                    }
                }
                else if (parameters.AnalysisType == WindLoadCalculationTypes.MWFRS)
                {
                    // figure out the wall arrangements
                    if (bldg_data.RoofType == RoofTypes.ROOF_TYPE_FLAT)
                        return new WallAreaCalculator_MWFRS_ASCE7_22(bldg_data);
                    else if (bldg_data.RoofType == RoofTypes.ROOF_TYPE_HIP)
                    {
                        return new WallAreaCalculator_MWFRS_ASCE7_22(bldg_data);
                    }

                    else if (bldg_data.RoofType == RoofTypes.ROOF_TYPE_GABLE)
                    {
                            return new WallAreaCalculator_MWFRS_ASCE7_22(bldg_data);
                    }
                    else
                    {
                        throw new NotSupportedException("Unsupported roof type: " + bldg_data.RoofType);
                    }
                }
                else
                {
                    throw new NotImplementedException("Wall calculator not implemented for version " + version);
                }
            }

            return null;
        }
    }
}
