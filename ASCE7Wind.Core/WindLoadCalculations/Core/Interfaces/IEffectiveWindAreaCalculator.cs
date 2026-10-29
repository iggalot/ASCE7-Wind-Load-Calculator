namespace ASCE7WindLoadCalculator
{
    public interface IEffectiveWindAreaCalculator
    {
        /// <summary>
        /// Function to compute the areas of the calculator.  To be overriden by the implementating class
        /// </summary>
        /// <param name="p"></param>
        /// <param name="bldg_data"></param>
        void ComputeEffectiveWindAreas();
    }
}
