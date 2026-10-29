using System;

namespace ASCE7WindLoadCalculator
{
    public class AreaCalculator_MWFRS_ASCE7_22_Base : AreaCalculator_ASCE7_22_Base
    {
        public override BuildingData buildingData { get; set; }
        public override bool HasCritDim { get; set; } = false;
        public override double CritDim_a { get; set; } = 0;
        public override void ComputeEffectiveWindAreas()
        {
            throw new NotImplementedException();
        }
    }
}
