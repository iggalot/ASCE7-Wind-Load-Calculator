using System;

namespace ASCE7WindLoadCalculator
{
    public abstract class AreaCalculator_CC_Base : AreaCalculator_Base
    {
        /// <summary>
        /// The zone width "a" of the Chapter 30 Part 1 figures: 10% of the least horizontal dimension or 0.4h, whichever is
        /// smaller, but not less than 4% of the least horizontal dimension or 3 ft; h the figure height (the eave height for
        /// θ ≤ 10°, <see cref="BuildingData.CcFigureHeight"/>). The walls figure's exception (<see cref="IsWallFigure"/>):
        /// for θ = 0° to 7° and a least horizontal dimension over 300 ft, a is at most 0.8h (Fig. 30.3-1, 7-16 and 7-22).
        /// </summary>
        public override double CritDim_a { get=> ComputeCritDim_a(); }
        public override bool HasCritDim => true;

        /// <summary>True for the walls' zones (Fig. 30.3-1), whose notation carries the 0.8h exception.</summary>
        protected virtual bool IsWallFigure => false;

        /// <summary>Least horizontal dimension (ft) over which Fig. 30.3-1's exception applies.</summary>
        public const double LargeBuildingLeastDimension = 300.0;

        /// <summary>Steepest roof (deg) for Fig. 30.3-1's exception.</summary>
        public const double LargeBuildingMaxRoofAngle = 7.0;

        public double ComputeCritDim_a()
        {
            double h = buildingData.CcFigureHeight;
            double least = Math.Min(buildingData.BuildingLength, buildingData.BuildingWidth);
            double a = Math.Max(Math.Min(0.4 * h, 0.1 * least), Math.Max(0.04 * least, 3));
            double theta = buildingData.RoofTypeIsSloped() ? buildingData.RoofPitch : 0;
            if (IsWallFigure && theta <= LargeBuildingMaxRoofAngle && least > LargeBuildingLeastDimension)
                a = Math.Min(a, 0.8 * h);
            return a;
        }


    }
}
