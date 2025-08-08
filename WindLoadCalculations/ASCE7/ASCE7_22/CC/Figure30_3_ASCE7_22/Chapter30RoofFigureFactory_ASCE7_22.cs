using System;

namespace ASCE7WindLoadCalculator
{
    public static class Chapter30RoofFigureFactory_ASCE7_22
    {
        public static Chapter27and30_GCpCurveBase CreateRoofFigure_ASCE7_22(BuildingData buildingData)
        {
            RidgeDirections ridgeDirection = buildingData.RidgeDirection;
            RoofTypes roofType = buildingData.RoofType;
            double roofSlope = buildingData.RoofPitch;

            switch (roofType)
            {
                case RoofTypes.ROOF_TYPE_FLAT:
                    // return flat roof figure instance(s), maybe based on slope or h, b?
                    return new Figure30_3_2A_ASCE7_22(); // example class

                case RoofTypes.ROOF_TYPE_GABLE:
                    if (roofSlope <= 7)
                        return new Figure30_3_2A_ASCE7_22();
                    else if (roofSlope <= 20)
                        return new Figure30_3_2B_ASCE7_22();
                    else if (roofSlope <= 27)
                        return new Figure30_3_2C_ASCE7_22();
                    else
                        return new Figure30_3_2D_ASCE7_22();

                case RoofTypes.ROOF_TYPE_HIP:
                    if (roofSlope <= 20)
                        return new Figure30_3_2E_ASCE7_22();
                    else if (roofSlope <= 27)
                        return new Figure30_3_2F_ASCE7_22();
                    else if (roofSlope >= 45)
                        return new Figure30_3_2G_ASCE7_22();
                    else
                        return new Figure30_3_2F_2G_INTERPOLATED_ASCE7_22(new Figure30_3_2F_ASCE7_22(), 27, new Figure30_3_2G_ASCE7_22(), 45, roofSlope);

                default:
                    throw new ArgumentException("ERROR: Invalid roof type" + roofType + " in Chapter30FigureFactory");
            }
        }
    }

}
