using System;
using System.Collections.Generic;

namespace ASCE7WindLoadCalculator
{
    /// <summary>
    /// MWFRS pressures, Chapter 27 Part 1 (directional procedure), Fig. 27.3-1.
    /// p = q G Cp - qi (GCpi), with Kd applied per the edition.
    /// </summary>
    public class WindLoadCalculator_MWFRS : WindLoadCalculator_Base
    {
        // Figure 27.3-1 wall coefficients
        public double Cp_WW { get; set; } = 0.8;  // windward wall
        public double Cp_SW { get; set; } = -0.7; // sidewalls
        public double Cp_LW { get => GetLWCpValues(); } // leeward walls

        private double GetLWCpValues()
        {
            double x = buildingData.L_Over_B;

            if (x < 1) return -0.5;
            if (x >= 4) return -0.2;

            // Linear interpolation between (1, -0.5) and (4, -0.2)
            double x0 = 1.0, y0 = -0.5;
            double x1 = 4.0, y1 = -0.2;

            return y0 + (x - x0) * ((y1 - y0) / (x1 - x0));
        }

        public WindLoadCalculator_MWFRS(Asce7Edition edition, WindParameters_Base p, BuildingData bldg_data)
            : base(edition, p, bldg_data)
        {
            CreateExtGcpCurves();
        }

        public void CreateExtGcpCurves()
        {
            extGCpCurve_Roof = Chapter27RoofFigureFactory.CreateRoofFigure(buildingData);
            if (extGCpCurve_Roof != null)
                extGCpCurve_Roof.ChartTitle = Edition.Name + " Figure 27.3-1";
        }

        public override void CalculateExternalPressures()
        {
            const double G = 0.85; // gust-effect factor, rigid building (26.11.1)
            double qh = CalculateDynamicWindPressure(buildingData.MeanRoofHeight);

            // Normal to the ridge, roof Cp is read against roof slope; otherwise against area (area reduction).
            bool bySlope = buildingData.RidgeDirection == RidgeDirections.RIDGE_DIR_PERP_TO_BLDGLENGTH;
            Func<EffectiveWindArea, double> curveX = a => bySlope ? buildingData.RoofPitch : a.Area;

            AddPressures(RoofAreaCalculator, extGCpCurve_Roof.RoofCurves_Pos, curveX, qh, G, windPressureRoof_Pos_External);
            AddPressures(RoofAreaCalculator, extGCpCurve_Roof.RoofCurves_Neg, curveX, qh, G, windPressureRoof_Neg_External);
            AddWallPressures(qh, G, windPressureWall_Pos_External_MWFRS);
            AddWallPressures(qh, G, windPressureWall_Neg_External_MWFRS);
        }

        private void AddWallPressures(double qh, double G, Dictionary<int, PressureData> target)
        {
            double kd = PressureKd;
            double gcpi = GetGCpi();

            foreach (var areaEntry in WallAreaCalculator_BldgLength.effWindAreas)
            {
                double cp;
                switch (areaEntry.Value.Label_Full)
                {
                    case "ZoneWW": cp = Cp_WW; break;
                    case "ZoneLW": cp = Cp_LW; break;
                    case "ZoneSW": cp = Cp_SW; break;
                    default:
                        throw new NotSupportedException("Unknown MWFRS wall zone: " + areaEntry.Value.Label_Full);
                }

                target.Add(areaEntry.Key, new PressureData()
                {
                    AreaID = areaEntry.Key,
                    qh = qh,
                    GCp = cp,
                    ExternalPressure = qh * cp * G * kd,
                    GCpi = gcpi,
                    InternalPressure = qh * gcpi * kd,
                });
            }
        }
    }
}
