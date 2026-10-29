using System.Collections.Generic;

namespace ASCE7WindLoadCalculator
{
    // Wind Load Parameters class
    public class WindParameters_Base
    {
        public string RiskCategory { get; set; } 
        public double WindSpeed { get; set; } = 115.0;
        public WindExposureCategories ExposureCategory { get; set; }
        public BuildingEnclosures EnclosureClassification { get; set; }
        public double Kzt { get; set; } = 1.0;
        public double GustFactor { get; set; } = 0.85;
        public double ImportanceFactor { get; set; } = 1.0;
        public WindLoadCalculationTypes AnalysisType { get; set; } = WindLoadCalculationTypes.COMPONENT_AND_CLADDING;

        public Dictionary<int, double> GCp_Values { get; set; } = new Dictionary<int, double>();

        public WindParameters_Base Clone()
        {
            return new WindParameters_Base()
            {
                RiskCategory = this.RiskCategory,
                WindSpeed = this.WindSpeed,
                ExposureCategory = this.ExposureCategory,
                EnclosureClassification = this.EnclosureClassification,
                Kzt = this.Kzt,
                GustFactor = this.GustFactor,
                ImportanceFactor = this.ImportanceFactor,
                AnalysisType = this.AnalysisType,
                GCp_Values = new Dictionary<int, double>(this.GCp_Values)
            };
        }
    }



}
