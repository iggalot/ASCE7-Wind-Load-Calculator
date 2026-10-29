using System;

namespace ASCE7WindLoadCalculator
{
    /// <summary>
    /// ASCE 7-22 Chapter 30 Part 1 roof figure selection.
    /// Gable: 2A (theta &lt;= 7), 2B (7-20), 2C (20-27), 2D (27-45).
    /// Hip:   2A (theta &lt;= 7), 2E (7-20), 2F (20-27), 2G (27-45).
    /// Slope ranges are steps: a figure applies across its whole range, with no blending between figures.
    /// </summary>
    public static class Chapter30RoofFigureFactory_ASCE7_22
    {
        public static Chapter27and30_GCpCurveBase CreateRoofFigure_ASCE7_22(BuildingData buildingData)
        {
            double slope = buildingData.RoofPitch;

            switch (buildingData.RoofType)
            {
                case RoofTypes.ROOF_TYPE_FLAT:
                    return new Figure30_3_2A_ASCE7_22();
                case RoofTypes.ROOF_TYPE_GABLE:
                    if (slope <= 7) return new Figure30_3_2A_ASCE7_22();
                    if (slope <= 20) return new Figure30_3_2B_ASCE7_22();
                    if (slope <= 27) return new Figure30_3_2C_ASCE7_22();
                    return new Figure30_3_2D_ASCE7_22();
                case RoofTypes.ROOF_TYPE_HIP:
                    if (slope <= 7) return new Figure30_3_2A_ASCE7_22();
                    if (slope <= 20) return new Figure30_3_2E_ASCE7_22();
                    if (slope <= 27) return new Figure30_3_2F_ASCE7_22();
                    return new Figure30_3_2G_ASCE7_22();
                default:
                    throw new ArgumentException("Invalid roof type: " + buildingData.RoofType);
            }
        }
    }
}
