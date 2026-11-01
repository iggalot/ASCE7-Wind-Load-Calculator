using System;

namespace ASCE7WindLoadCalculator
{
    public enum BuildingEnclosures
    {
        BLDG_ENCLOSED = 0,
        BLDG_PARTIALLY_ENCLOSED = 1,
        BLDG_PARTIALLY_OPEN = 2,
        BLDG_OPEN = 3
    }

    public enum RidgeDirections
    {
        RIDGE_DIR_NONE = 0,
        RIDGE_DIR_PERP_TO_BLDGLENGTH = 1,
        RIDGE_DIR_PARALLEL_TO_BLDGLENGTH = 2
    }

    public enum RoofTypes
    {
        ROOF_TYPE_FLAT = 0,
        ROOF_TYPE_GABLE = 1,
        ROOF_TYPE_HIP = 2
    }

    /// <summary>
    /// Rectangular building: plan L x B, eave height, roof type and pitch, enclosure.
    /// Derived quantities (ridge height, mean roof height h, ratios) are always computed from the inputs,
    /// so the object is valid regardless of the order in which properties are set.
    /// </summary>
    public class BuildingData
    {
        private RidgeDirections requestedRidge = RidgeDirections.RIDGE_DIR_NONE;

        /// <summary>Plan dimension L (ft).</summary>
        public double BuildingLength { get; set; } = 60;

        /// <summary>Plan dimension B (ft).</summary>
        public double BuildingWidth { get; set; } = 40;

        /// <summary>Eave height (ft).</summary>
        public double BuildingHeight { get; set; } = 15;

        /// <summary>Number of stories (floors), 1 to <see cref="MaxStories"/>. The eave height is split equally between them; each floor
        /// diaphragm and the roof get the wind on their own horizontal strip of the walls (shear-wall design).</summary>
        public int NumberOfStories
        {
            get => stories;
            set => stories = Math.Max(1, Math.Min(MaxStories, value));
        }
        private int stories = 1;

        /// <summary>The most stories the shear-wall design handles.</summary>
        public const int MaxStories = 3;

        /// <summary>Story (floor-to-floor) height: eave height / stories (ft).</summary>
        public double StoryHeight => BuildingHeight / NumberOfStories;

        /// <summary>Roof slope (degrees).</summary>
        public double RoofPitch { get; set; } = 15;

        /// <summary>Parapet height above the roof (ft), around a flat roof; 0 for none. Sloped roofs ignore it.</summary>
        public double ParapetHeight { get; set; } = 0;

        /// <summary>True when a parapet is counted: a flat roof with a parapet height above 0.</summary>
        public bool HasParapet => ParapetHeight > 0 && RoofTypeIsFlat();

        /// <summary>
        /// Porosity of the parapet envelope for its C&amp;C internal pressure (30.6: GCpi from Table 26.13-1 based on the
        /// parapet envelope, not the building). Enclosed (+/-0.18) suits a parapet sheathed on both faces; Open (0) a solid
        /// or open parapet with no cavity.
        /// </summary>
        public BuildingEnclosures ParapetEnclosure { get; set; } = BuildingEnclosures.BLDG_ENCLOSED;

        /// <summary>Roof overhang (eave) width beyond the wall face (ft); 0 for none.</summary>
        public double OverhangWidth { get; set; } = 0;

        public RoofTypes RoofType { get; set; } = RoofTypes.ROOF_TYPE_FLAT;

        public BuildingEnclosures EnclosureType { get; set; } = BuildingEnclosures.BLDG_ENCLOSED;

        /// <summary>
        /// Ridge direction relative to the BuildingLength side:
        /// flat roof: always NONE; hip roof: along the longer side;
        /// gable roof: the user's choice, defaulting to along the longer side when not chosen.
        /// </summary>
        public RidgeDirections RidgeDirection
        {
            get => EffectiveRidge(requestedRidge);
            set => requestedRidge = value;
        }

        private RidgeDirections AlongLongerSide =>
            BuildingLength >= BuildingWidth ? RidgeDirections.RIDGE_DIR_PARALLEL_TO_BLDGLENGTH : RidgeDirections.RIDGE_DIR_PERP_TO_BLDGLENGTH;

        private RidgeDirections EffectiveRidge(RidgeDirections requested)
        {
            switch (RoofType)
            {
                case RoofTypes.ROOF_TYPE_FLAT: return RidgeDirections.RIDGE_DIR_NONE;
                case RoofTypes.ROOF_TYPE_HIP: return AlongLongerSide;
                case RoofTypes.ROOF_TYPE_GABLE: return requested == RidgeDirections.RIDGE_DIR_NONE ? AlongLongerSide : requested;
                default: throw new NotSupportedException($"Ridge direction not defined for roof type {RoofType}");
            }
        }

        /// <summary>Mean roof height h (ft): average of eave and ridge heights (26.2).</summary>
        public double MeanRoofHeight => 0.5 * (BuildingHeight + RidgeHeight);

        /// <summary>Height of the highest point of the roof (ft).</summary>
        public double RidgeHeight => ComputeRidgeHeight();

        public double h_Over_L => MeanRoofHeight / BuildingLength;
        public double h_Over_B => MeanRoofHeight / BuildingWidth;
        public double L_Over_B => BuildingLength / BuildingWidth;

        /// <summary>Horizontal span measured across the ridge (ft): the roof rises over half of it.</summary>
        public double SpanAcrossRidge
        {
            get
            {
                switch (RoofType)
                {
                    case RoofTypes.ROOF_TYPE_GABLE:
                        return RidgeDirection == RidgeDirections.RIDGE_DIR_PERP_TO_BLDGLENGTH ? BuildingLength : BuildingWidth;
                    case RoofTypes.ROOF_TYPE_HIP:
                        return Math.Min(BuildingLength, BuildingWidth);
                    default:
                        return 0;
                }
            }
        }

        /// <summary>Kept for existing callers: stores the effective ridge direction explicitly.</summary>
        public void ValidateRidgeDirection() => requestedRidge = RidgeDirection;

        public double ComputeRidgeHeight() =>
            BuildingHeight + Math.Tan(RoofPitch * Math.PI / 180.0) * SpanAcrossRidge / 2.0;

        public bool RoofTypeIsSloped() => RoofType == RoofTypes.ROOF_TYPE_HIP || RoofType == RoofTypes.ROOF_TYPE_GABLE;

        public bool RoofTypeIsFlat() => RoofType == RoofTypes.ROOF_TYPE_FLAT;

        /// <summary>Turns the plan by 90 degrees: swaps L and B and flips the ridge direction, so the physical building is unchanged.</summary>
        public void RotateBuilding()
        {
            var ridge = RidgeDirection;
            double temp = BuildingLength;
            BuildingLength = BuildingWidth;
            BuildingWidth = temp;

            if (RoofTypeIsSloped())
            {
                requestedRidge = ridge == RidgeDirections.RIDGE_DIR_PARALLEL_TO_BLDGLENGTH
                    ? RidgeDirections.RIDGE_DIR_PERP_TO_BLDGLENGTH
                    : RidgeDirections.RIDGE_DIR_PARALLEL_TO_BLDGLENGTH;
            }
            else
            {
                requestedRidge = RidgeDirections.RIDGE_DIR_NONE;
            }
        }

        public BuildingData Clone() => (BuildingData)MemberwiseClone();
    }
}
