using System;

namespace ASCE7WindLoadCalculator
{
    /// <summary>Site and analysis inputs for a wind calculation.</summary>
    public class WindParameters_Base
    {
        /// <summary>Risk category (I-IV). Informational: it selects which wind-speed map V comes from.</summary>
        public string RiskCategory { get; set; }

        /// <summary>Basic wind speed V (mph), from the map for the building's risk category.</summary>
        public double WindSpeed { get; set; } = 115.0;

        public WindExposureCategories ExposureCategory { get; set; }

        /// <summary>Topographic factor Kzt (26.8); 1.0 on flat terrain.</summary>
        public double Kzt { get; set; } = 1.0;

        /// <summary>Gust-effect factor G for MWFRS (26.11); 0.85 for rigid buildings.</summary>
        public double GustFactor { get; set; } = 0.85;

        /// <summary>
        /// Not used. ASCE 7-16 and 7-22 have no wind importance factor (risk category is built into the
        /// wind-speed maps). Kept so older saved inputs still load.
        /// </summary>
        [Obsolete("ASCE 7-16/7-22 have no wind importance factor; this value is ignored.")]
        public double ImportanceFactor { get; set; } = 1.0;

        /// <summary>Not used; enclosure comes from <see cref="BuildingData.EnclosureType"/>. Kept so older saved inputs still load.</summary>
        [Obsolete("Use BuildingData.EnclosureType; this value is ignored.")]
        public BuildingEnclosures EnclosureClassification { get; set; }

        public WindLoadCalculationTypes AnalysisType { get; set; } = WindLoadCalculationTypes.COMPONENT_AND_CLADDING;

        public WindParameters_Base Clone() => (WindParameters_Base)MemberwiseClone();
    }
}
