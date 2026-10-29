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

    // <summary>
    /// A class for storing the building data
    /// </summary>
    public class BuildingData
    {
        // defalt values
        public double BuildingLength { get; set; } = 60; // L
        public double BuildingWidth { get; set; } = 40;  // B
        public double BuildingHeight { get; set; } = 15;
        public double RoofPitch { get; set; } = 15;
        public RidgeDirections RidgeDirection { get; set; } = RidgeDirections.RIDGE_DIR_NONE;
        public RoofTypes RoofType { get; set; } = RoofTypes.ROOF_TYPE_FLAT;
        public BuildingEnclosures EnclosureType { get; set; } = BuildingEnclosures.BLDG_ENCLOSED;

        /// <summary>
        /// The mean roof height of the building, h per ASCE7
        /// </summary>
        public double MeanRoofHeight { get => ComputeMeanRoofHeight(); }
        public double RidgeHeight { get => ComputeRidgeHeight(); }

        /// <summary>
        /// Helper calclations
        /// </summary>
        public double h_Over_L { get => MeanRoofHeight / BuildingLength; }
        public double h_Over_B { get => MeanRoofHeight / BuildingWidth; }
        public double L_Over_B { get => BuildingLength / BuildingWidth; }


        public BuildingData()
        {
            ValidateRidgeDirection();
        }

        /// <summary>
        /// Validates the ridge direction.  
        /// -- For flat roof, ridge direction is RIDGE_DIR_NONE
        /// -- For hip roof, the ridge direction is parallel to the longest side, othrewise geometry doesn't work
        /// -- For gable roof, the ridge direction is user defined
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        public void ValidateRidgeDirection()
        {
            switch (RoofType)
            {
                case RoofTypes.ROOF_TYPE_FLAT:
                    RidgeDirection = RidgeDirections.RIDGE_DIR_NONE;
                    break;

                case RoofTypes.ROOF_TYPE_HIP:
                    // Automatically assign based on longest dimension
                    RidgeDirection = (BuildingLength >= BuildingWidth)
                        ? RidgeDirections.RIDGE_DIR_PARALLEL_TO_BLDGLENGTH
                        : RidgeDirections.RIDGE_DIR_PERP_TO_BLDGLENGTH;
                    break;

                case RoofTypes.ROOF_TYPE_GABLE:
                    // Must be user-defined and not NONE
                    if (RidgeDirection == RidgeDirections.RIDGE_DIR_NONE)
                    {
                        // Reset it to a default being parallel to the shortest dimension
                        if(BuildingLength >= BuildingWidth)
                        {
                            RidgeDirection = RidgeDirections.RIDGE_DIR_PARALLEL_TO_BLDGLENGTH;
                        } else
                        {
                            RidgeDirection = RidgeDirections.RIDGE_DIR_PERP_TO_BLDGLENGTH;
                        }
                    }
                    // Otherwise, use as-is
                    break;

                default:
                    throw new NotImplementedException($"Ridge direction validation not implemented for roof type {RoofType}");
            }
        }


        private double ComputeMeanRoofHeight()
        {
            return 0.5 * (BuildingHeight + ComputeRidgeHeight());
        }


        public double ComputeRidgeHeight()
        {
            if(RoofType == RoofTypes.ROOF_TYPE_FLAT) return BuildingHeight;
            if(RoofType == RoofTypes.ROOF_TYPE_GABLE)
            {
                if (RidgeDirection == RidgeDirections.RIDGE_DIR_PERP_TO_BLDGLENGTH)
                {
                    return BuildingHeight + Math.Tan(RoofPitch * Math.PI / 180.0) * BuildingLength / 2.0;
                }
                else if (RidgeDirection == RidgeDirections.RIDGE_DIR_PARALLEL_TO_BLDGLENGTH)
                {
                    return BuildingHeight + Math.Tan(RoofPitch * Math.PI / 180.0) * BuildingWidth / 2.0;
                }
            }
            if(RoofType == RoofTypes.ROOF_TYPE_HIP)
            {
                return BuildingHeight + Math.Tan(RoofPitch * Math.PI / 180.0) * Math.Min(BuildingLength, BuildingWidth) / 2.0;
            }

            return BuildingHeight;

        }

        public bool RoofTypeIsSloped()
        {
            return (RoofType == RoofTypes.ROOF_TYPE_HIP || RoofType == RoofTypes.ROOF_TYPE_GABLE);
        }

        public bool RoofTypeIsFlat()
        {
            return (RoofType == RoofTypes.ROOF_TYPE_FLAT);
        }

        /// <summary>
        /// A routine that flips the plan view of the building by 90 degrees, including the ridge direction if necessary
        /// </summary>
        public void RotateBuilding()
        {
            double temp = BuildingLength;
            BuildingLength = BuildingWidth;
            BuildingWidth = temp;

            // if the roof is sloped, flip the ridge direction
            if (RoofTypeIsSloped())
            {
                RidgeDirection = RidgeDirection == RidgeDirections.RIDGE_DIR_PARALLEL_TO_BLDGLENGTH ? RidgeDirections.RIDGE_DIR_PERP_TO_BLDGLENGTH : RidgeDirections.RIDGE_DIR_PARALLEL_TO_BLDGLENGTH;
            }
            // otherwise we have a non-sloped (flat) roof
            else
            {
                RidgeDirection = RidgeDirections.RIDGE_DIR_NONE;
            }
            ValidateRidgeDirection();
        }

        /// <summary>
        /// Creates a clone of this object
        /// </summary>
        /// <returns></returns>
        public BuildingData Clone()
        {
            return new BuildingData()
            {
                BuildingLength = this.BuildingLength,
                BuildingWidth = this.BuildingWidth,
                BuildingHeight = this.BuildingHeight,
                RoofPitch = this.RoofPitch,
                RidgeDirection = this.RidgeDirection,
                RoofType = this.RoofType,
                EnclosureType = this.EnclosureType
            };
        }
    }
}
