using System;

namespace ASCE7WindLoadCalculator
{
    /// <summary>
    /// ASCE 7-16 Chapter 30 Part 1 roof figure selection.
    /// Gable: 2A (theta &lt;= 7), 2B (7-20), 2C (20-27), 2D (27-45).
    /// Hip:   2A (theta &lt;= 7), 2E/2F (7-20, by h/B), 2G (20-27), 2H/2I (27-45).
    /// </summary>
    public static class Chapter30RoofFigureFactory_ASCE7_16
    {
        public static Chapter27and30_GCpCurveBase CreateRoofFigure_ASCE7_16(BuildingData buildingData)
        {
            double slope = buildingData.RoofPitch;

            switch (buildingData.RoofType)
            {
                case RoofTypes.ROOF_TYPE_FLAT:
                    return new Figure30_3_2A_ASCE7_16();
                case RoofTypes.ROOF_TYPE_GABLE:
                    if (slope <= 7) return new Figure30_3_2A_ASCE7_16();
                    if (slope <= 20) return new Figure30_3_2B_ASCE7_16();
                    if (slope <= 27) return new Figure30_3_2C_ASCE7_16();
                    return new Figure30_3_2D_ASCE7_16();
                case RoofTypes.ROOF_TYPE_HIP:
                    if (slope <= 7) return new Figure30_3_2A_ASCE7_16();
                    if (slope <= 20) return new Figure30_3_2E_2F_ASCE7_16(buildingData.HipCladdingHOverB); // note 7: B for Zone 3 the least dimension
                    if (slope <= 27) return new Figure30_3_2G_ASCE7_16();
                    return new Figure30_3_2H_2I_ASCE7_16(slope);
                default:
                    throw new ArgumentException("Invalid roof type: " + buildingData.RoofType);
            }
        }
    }
}
