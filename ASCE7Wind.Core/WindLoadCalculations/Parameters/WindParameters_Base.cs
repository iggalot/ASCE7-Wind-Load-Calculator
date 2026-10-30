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

        /// <summary>Effective wind area used when none is given (sq ft): the small-area end of the C&amp;C curves.</summary>
        public const double DefaultComponentArea = 10.0;

        /// <summary>
        /// Effective wind area (sq ft, 26.2) of the roof component being designed: span × effective width, the
        /// width need not be less than span/3; for fasteners not more than the area tributary to one fastener.
        /// C&amp;C roof GCp is read at this area in every zone. 10 sq ft (sheathing, fasteners) gives the largest values.
        /// </summary>
        public double RoofComponentArea { get; set; } = DefaultComponentArea;

        /// <summary>Effective wind area (sq ft, 26.2) of the wall component being designed (see <see cref="RoofComponentArea"/>).</summary>
        public double WallComponentArea { get; set; } = DefaultComponentArea;

        public WindLoadCalculationTypes AnalysisType { get; set; } = WindLoadCalculationTypes.COMPONENT_AND_CLADDING;

        public WindParameters_Base Clone() => (WindParameters_Base)MemberwiseClone();
    }
}
