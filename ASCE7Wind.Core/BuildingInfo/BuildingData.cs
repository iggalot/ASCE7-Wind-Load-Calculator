using System;
using System.Collections.Generic;
using System.Linq;

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

        /// <summary>
        /// Eave height (ft): the sum of the story plate heights when they are given (<see cref="PlateHeights"/>), otherwise
        /// the height entered. Setting it clears the plate heights (the stories then share it equally).
        /// </summary>
        public double BuildingHeight
        {
            get => plateHeights != null && plateHeights.Count > 0 ? StoryPlateHeights.Sum() : eave;
            set
            {
                eave = value;
                plateHeights = null;
            }
        }
        private double eave = 15;

        /// <summary>Number of stories (floors), 1 to <see cref="MaxStories"/>. Each floor diaphragm and the roof get the wind on
        /// their own horizontal strip of the walls (shear-wall design).</summary>
        public int NumberOfStories
        {
            get => stories;
            set => stories = Math.Max(1, Math.Min(MaxStories, value));
        }
        private int stories = 1;

        /// <summary>The most stories the shear-wall design handles.</summary>
        public const int MaxStories = 3;

        /// <summary>
        /// The plate height of each story, bottom first (ft: floor to the top plate of its walls); the eave height is their sum.
        /// Null = not given: the eave height is split equally. Fewer entries than stories repeat the last one. Declared after
        /// <see cref="BuildingHeight"/> so a saved file sets the eave first and then the plate heights.
        /// </summary>
        public List<double> PlateHeights
        {
            get => plateHeights?.ToList();
            set => plateHeights = value == null || value.Count == 0 ? null : value.ToList();
        }
        private List<double> plateHeights;

        /// <summary>The plate height of story <paramref name="story"/> (1 = the first story), ft.</summary>
        public double StoryPlateHeight(int story)
        {
            if (plateHeights == null || plateHeights.Count == 0) return eave / NumberOfStories;
            return plateHeights[Math.Max(0, Math.Min(plateHeights.Count - 1, story - 1))];
        }

        /// <summary>The plate height of each story in use, bottom first (ft).</summary>
        public double[] StoryPlateHeights => Enumerable.Range(1, NumberOfStories).Select(StoryPlateHeight).ToArray();

        /// <summary>Top of the walls of each story (floor and roof elevations), bottom first (ft).</summary>
        public double[] StoryElevations
        {
            get
            {
                var h = StoryPlateHeights;
                double z = 0;
                return h.Select(x => z += x).ToArray();
            }
        }

        /// <summary>Average story height: eave height / stories (ft).</summary>
        public double StoryHeight => BuildingHeight / NumberOfStories;

        /// <summary>True when the stories have different plate heights.</summary>
        public bool UnequalStories => StoryPlateHeights.Distinct().Count() > 1;

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

        /// <summary>
        /// h of the MWFRS figures (ft): the mean roof height, except that the eave height is used for θ ≤ 10° (notation of
        /// Fig. 27.3-1 and Fig. 28.3-1, ASCE 7-16 and 7-22 alike). Used for qh, h/L, the Fig. 27.3-1 roof distance bands and
        /// the Fig. 28.3-1 dimension a. For a flat roof both are the eave height.
        /// </summary>
        public double MwfrsFigureHeight => RoofType == RoofTypes.ROOF_TYPE_FLAT || RoofPitch <= 10 ? BuildingHeight : MeanRoofHeight;

        /// <summary>
        /// h of the cladding figures (ft), Chapter 30 Part 1 (Figs. 30.3-1 and 30.3-2A to 30.3-2I, 7-16 and 7-22 alike):
        /// "h = Mean roof height, except that eave height shall be used for θ ≤ 10°", the same rule as the MWFRS figures.
        /// Used for the cladding qh and the zone widths (a = 0.4h at most, and the 0.6h and 0.2h of Fig. 30.3-2A).
        /// The 60 ft limit of Part 1 stays on the mean roof height (26.2, the low-rise limit).
        /// </summary>
        public double CcFigureHeight => MwfrsFigureHeight;

        /// <summary>Height of the highest point of the roof (ft).</summary>
        public double RidgeHeight => ComputeRidgeHeight();

        /// <summary>h/L of Fig. 27.3-1, with h the figure height (<see cref="MwfrsFigureHeight"/>) and L the dimension along the wind.</summary>
        public double h_Over_L => MwfrsFigureHeight / BuildingLength;
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

        public BuildingData Clone()
        {
            var c = (BuildingData)MemberwiseClone();
            c.plateHeights = plateHeights?.ToList();
            return c;
        }
    }
}
