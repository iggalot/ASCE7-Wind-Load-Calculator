using System;

namespace ASCE7WindLoadCalculator
{
    public class AreaCalculator_CC_ASCE7_22_Base : AreaCalculator_ASCE7_22_Base
    {
        public override double CritDim_a { get=> ComputeCritDim_a(); }
        public override bool HasCritDim { get; set; } = true;

        public double ComputeCritDim_a()
        {
            return Math.Max(
                Math.Min(0.4 * buildingData.MeanRoofHeight, 0.1 * Math.Min(buildingData.BuildingLength, buildingData.BuildingWidth)),
                Math.Max(0.04 * Math.Min(buildingData.BuildingLength, buildingData.BuildingWidth),
                3)
                );
        }
    }
}
