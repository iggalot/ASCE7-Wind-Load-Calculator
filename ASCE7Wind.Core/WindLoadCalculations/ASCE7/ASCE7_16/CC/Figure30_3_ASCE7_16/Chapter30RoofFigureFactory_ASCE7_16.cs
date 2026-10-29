using System;

namespace ASCE7WindLoadCalculator
{
    public static class Chapter30RoofFigureFactory_ASCE7_16
    {
        public static Chapter27and30_GCpCurveBase CreateRoofFigure_ASCE7_16(BuildingData buildingData)
        {
            RidgeDirections ridgeDirection = buildingData.RidgeDirection;
            RoofTypes roofType = buildingData.RoofType;
            double roofSlope = buildingData.RoofPitch;
            double h = buildingData.MeanRoofHeight;
            double h_over_B = buildingData.h_Over_B;

            switch (roofType)
            {
                case RoofTypes.ROOF_TYPE_FLAT:
                    // return flat roof figure instance(s), maybe based on slope or h, b?
                    return new Figure30_3_2A_ASCE7_16(); // example class

                case RoofTypes.ROOF_TYPE_GABLE:
                    if (roofSlope <= 7)
                        return new Figure30_3_2A_ASCE7_16();
                    else if (roofSlope <= 20)
                        return new Figure30_3_2B_ASCE7_16();
                    else if (roofSlope <= 27)
                        return new Figure30_3_2C_ASCE7_16();
                    else
                        return new Figure30_3_2D_ASCE7_16();

                case RoofTypes.ROOF_TYPE_HIP:
                    if (roofSlope <= 20)
                        return new Figure30_3_2E_2F_ASCE7_16(h_over_B);
                    else if (roofSlope <= 27)
                        return new Figure30_3_2G_ASCE7_16();
                    else
                        return new Figure30_3_2H_2I_ASCE7_16(roofSlope);

                default:
                    throw new ArgumentException("ERROR: Invalid roof type" + roofType + " in Chapter30FigureFactory");
            }
        }
    }

}
