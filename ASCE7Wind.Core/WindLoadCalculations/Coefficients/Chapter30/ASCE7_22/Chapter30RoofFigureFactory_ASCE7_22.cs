using System;

namespace ASCE7WindLoadCalculator
{
    /// <summary>
    /// ASCE 7-22 Chapter 30 Part 1 roof figure selection.
    /// Gable: 2A (theta &lt;= 7), 2B (7-20), 2C (20-27), 2D (27-45).
    /// Hip:   2A (theta &lt;= 7), 2E (7-20), 2F (20-27), 2F/2G interpolated (27-45), 2G (45).
    /// Slope ranges are steps, except hip roofs between 27° and 45°, which interpolate between 2F and 2G (Fig. 30.3-2G note 6).
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
                    if (slope >= 45) return new Figure30_3_2G_ASCE7_22();
                    // Fig. 30.3-2G note 6: for 27° < θ < 45° interpolate between 2F and 2G, zone by zone:
                    // GCp = GCp(2F) + [GCp(2G) − GCp(2F)] (θ − 27°) / (45° − 27°)
                    return new BlendedGCpFigure(new Figure30_3_2F_ASCE7_22(), new Figure30_3_2G_ASCE7_22(), (slope - 27) / 18.0,
                        "ASCE 7-22 Figures 30.3-2F and 30.3-2G - Hip Roofs (interpolated)",
                        $"h <= 60ft, slope = {slope:0.#}deg: 2F + (2G - 2F) x (slope - 27)/18 (Fig. 30.3-2G note 6)");
                default:
                    throw new ArgumentException("Invalid roof type: " + buildingData.RoofType);
            }
        }
    }
}
