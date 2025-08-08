using System;

namespace ASCE7WindLoadCalculator
{
    public static class Chapter27RoofFigureFactory_ASCE7_22
    {
        public static Chapter27and30_GCpCurveBase CreateRoofFigure_ASCE7_22(BuildingData buildingData, double area = 50)
        {

            RidgeDirections ridgeDirection = buildingData.RidgeDirection;
            RoofTypes roofType = buildingData.RoofType;
            double roofSlope = buildingData.RoofPitch;
            double h_over_L = buildingData.h_Over_L;

            switch (ridgeDirection)
            {
                case RidgeDirections.RIDGE_DIR_PARALLEL_TO_BLDGLENGTH: // parallel to ridge case
                    {
                        if (h_over_L <= 0.5)
                        {
                            return new ParallelToRidge_LowSlope_Roof_Low_ASCE7_22();
                        }
                        else if (h_over_L >= 1.0)
                        {
                            return new ParallelToRidge_LowSlope_Roof_High_ASCE7_22();
                        }
                        else if (h_over_L > 0.5 && h_over_L < 1.0)
                        {
                            var low = new ParallelToRidge_LowSlope_Roof_Low_ASCE7_22();
                            var high = new ParallelToRidge_LowSlope_Roof_High_ASCE7_22();

                            return new InterpolatedCpCurve(h_over_L, roofSlope, 0.5, 1.0, low, high);
                        }
                    }
                    break;
                case RidgeDirections.RIDGE_DIR_PERP_TO_BLDGLENGTH:  // normal to ridge case
                    if (roofSlope >= 10)
                    {
                        if (h_over_L <= 0.25)
                        {
                            return new NormToRidge_LargeSlope_Roof_Low_ASCE7_22(roofSlope);
                        }
                        else if (h_over_L > 0.25 && h_over_L < 0.5)
                        {
                            // interpolate
                            var low = new NormToRidge_LargeSlope_Roof_Low_ASCE7_22(roofSlope);
                            var mid = new NormToRidge_LargeSlope_Roof_Mid_ASCE7_22(roofSlope);

                            return new InterpolatedCpCurve(h_over_L, roofSlope, 0.25, 0.5, low, mid);

                        }
                        else if (h_over_L > 0.5 && h_over_L < 1.0)
                        {
                            var mid = new NormToRidge_LargeSlope_Roof_Mid_ASCE7_22(roofSlope);
                            var high = new NormToRidge_LargeSlope_Roof_High_ASCE7_22(roofSlope);

                            return new InterpolatedCpCurve(h_over_L, roofSlope, 0.5, 1.0, mid, high);
                        }
                        else if (h_over_L >= 1.0)
                        {
                            return new NormToRidge_LargeSlope_Roof_High_ASCE7_22(roofSlope);
                        }
                    }
                    else
                    {
                        if (h_over_L <= 0.5)
                        {
                            return new ParallelToRidge_LowSlope_Roof_Low_ASCE7_22();
                        }
                        else if (h_over_L >= 1.0)
                        {
                            return new ParallelToRidge_LowSlope_Roof_High_ASCE7_22();
                        }
                        else if (h_over_L > 0.5 && h_over_L < 1.0)
                        {
                            var low = new ParallelToRidge_LowSlope_Roof_Low_ASCE7_22();
                            var high = new ParallelToRidge_LowSlope_Roof_High_ASCE7_22();

                            return new InterpolatedCpCurve(h_over_L, roofSlope, 0.5, 1.0, low, high);
                        }
                    }
                    break;
                case RidgeDirections.RIDGE_DIR_NONE:
                    {
                        if (h_over_L <= 0.5)
                        {
                            return new ParallelToRidge_LowSlope_Roof_Low_ASCE7_22();
                        }
                        else if (h_over_L >= 1.0)
                        {
                            return new ParallelToRidge_LowSlope_Roof_High_ASCE7_22();
                        }
                        else if (h_over_L > 0.5 && h_over_L < 1.0)
                        {
                            var low = new ParallelToRidge_LowSlope_Roof_Low_ASCE7_22();
                            var high = new ParallelToRidge_LowSlope_Roof_High_ASCE7_22();

                            return new InterpolatedCpCurve(h_over_L, roofSlope, 0.5, 1.0, low, high);
                        }
                    }
                    break;

                default:
                    throw new ArgumentException("ERROR: Invalid roof type" + roofType + " in Chapter30FigureFactory");
            }

            return null;
        }
    }

}
