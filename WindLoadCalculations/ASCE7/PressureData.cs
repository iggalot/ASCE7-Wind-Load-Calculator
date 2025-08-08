namespace ASCE7WindLoadCalculator
{
    public class PressureData
    {
        public  int AreaID { get; set; }
        public double qh { get; set; }
        public double GCp { get; set; }
        public double GCpi { get; set; }
        public double ExternalPressure;
        public double InternalPressure; 
        
        public double NetPressure
        { get
            {
            if(ExternalPressure < 0)
                {
                    return ExternalPressure - InternalPressure;
                }
                else
                {
                    return ExternalPressure + InternalPressure;
                }
            } 
        }
    }
}
