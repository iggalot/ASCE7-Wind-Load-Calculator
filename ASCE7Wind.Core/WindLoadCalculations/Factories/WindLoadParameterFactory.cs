using System;

namespace ASCE7WindLoadCalculator
{
    public static class WindLoadParametersFactory
    {
        public static WindParameters_Base Create(
            RoofTypes roofType,
            string riskCategory,
            double windSpeed,
            WindExposureCategories exposureCategory,
            double kzt,
            double importanceFactor
            //,
            //WindLoadCalculationTypes analysisType
            )
        {
            WindParameters_Base parameters;

            if (roofType == RoofTypes.ROOF_TYPE_FLAT)
            {
                parameters = new FlatRoofWindLoadParameters();
            }
            else if (roofType == RoofTypes.ROOF_TYPE_GABLE)
            {
                parameters = new GableRoofWindLoadParameters();
            }
            else if (roofType == RoofTypes.ROOF_TYPE_HIP)
            {
                parameters = new HipRoofWindLoadParameters();
            }
            else
            {
                throw new NotSupportedException("Unsupported roof type: " + roofType);
            }

            parameters.RiskCategory = riskCategory;
            parameters.WindSpeed = windSpeed;
            parameters.ExposureCategory = exposureCategory;
            parameters.Kzt = kzt;
            parameters.ImportanceFactor = importanceFactor;
            //parameters.AnalysisType = analysisType;

            return parameters;
        }
    }
}