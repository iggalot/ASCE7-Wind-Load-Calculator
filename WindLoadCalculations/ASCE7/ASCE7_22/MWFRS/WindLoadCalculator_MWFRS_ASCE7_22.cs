using System;
using System.Collections.Generic;

namespace ASCE7WindLoadCalculator
{
    public class WindLoadCalculator_MWFRS_ASCE7_22 : WindLoadCalculator_ASCE7_22_Base
    {
        // Figure 27.3.1 coefficients
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

            double interpolatedValue = y0 + (x - x0) * ((y1 - y0) / (x1 - x0));
            return interpolatedValue;
        }

        public WindLoadCalculator_MWFRS_ASCE7_22(WindParameters_Base p, BuildingData bldg_data)
        {
            Parameters = p;
            buildingData = bldg_data;

            CreateExtGcpCurves();
        }

        public void CreateExtGcpCurves()
        {
            switch (ASCEVersion)
            {
                case ASCE7_Versions.ASCE_VER_7_16:
                    extGCpCurve_Roof = Chapter27RoofFigureFactory_ASCE7_16.CreateRoofFigure_ASCE7_16(buildingData);
                    break;
                case ASCE7_Versions.ASCE_VER_7_22:
                    extGCpCurve_Roof = Chapter27RoofFigureFactory_ASCE7_22.CreateRoofFigure_ASCE7_22(buildingData);
                    break;
                default:
                    throw new Exception("ERROR: Invalid ASCE Version: " + ASCEVersion + " in WindLoadCalculator_Base constructor.");
            }
        }

        /// <summary>
        /// In ASCE7-22, the calculations for pressure are multiplied by Kd after qh is calculated.
        /// In ASCE7-16 they are included in the dynamic wind pressure calculation for qh.
        /// </summary>
        /// <param name="tryGetGcp"></param>
        /// <param name="targetDict"></param>
        private void CalculateRoofPressures(
            TryGetGcpDelegate tryGetGcp,
            Dictionary<int, PressureData> targetDict
            )
        {
            foreach (var areaEntry in RoofAreaCalculator.effWindAreas)
            {
                int id = areaEntry.Key;

                double cp;  // in ASCE 7-16, the Cp value is shown in the tables, not GCp...so we need to remember to multiply it back in 
                if (!tryGetGcp(id, out cp))
                    continue;

                var G = 0.85; // gust factor 26.11.1
                var q_h = CalculateDynamicWindPressure(buildingData.MeanRoofHeight);
                var kd = GetKd(Parameters.AnalysisType);
                var _gcpi = GetGCpi();

                targetDict.Add(id, new PressureData()
                {
                    AreaID = id,
                    qh = q_h,
                    GCp = cp,
                    ExternalPressure = q_h * cp * G * kd,
                    GCpi = _gcpi,
                    InternalPressure = q_h * _gcpi * kd,
                });
            }
        }

        private void CalculateWallPressures(
            TryGetGcpDelegate tryGetGcp,
            Dictionary<int, PressureData> targetDict
            )
        {
            foreach (var areaEntry in WallAreaCalculator_BldgLength.effWindAreas)
            {
                int id = areaEntry.Key;

                double cp;  // in ASCE 7-16, the Cp value is shown in the tables, not GCp...so we need to remember to multiply it back in 

                var G = 0.85; // gust factor 26.11.1
                var q_h = CalculateDynamicWindPressure(buildingData.MeanRoofHeight);
                var kd = GetKd(Parameters.AnalysisType);
                var _gcpi = GetGCpi();

                // Windward wall
                if (areaEntry.Value.Label_Full == "ZoneWW")
                {
                    cp = Cp_WW;
                }
                // Leeward Wall
                else if (areaEntry.Value.Label_Full == "ZoneLW")
                {
                    cp = Cp_LW;
                }
                // SideWalls
                else if (areaEntry.Value.Label_Full == "ZoneSW")
                {
                    cp = Cp_SW;
                }
                else
                {
                    throw new NotImplementedException("ERROR: In CalculateWallPressures_BuildingLength, Unkown wall zone: " + areaEntry.Value.Label_Full);
                }

                targetDict.Add(id, new PressureData()
                {
                    AreaID = id,
                    qh = q_h,
                    GCp = cp,
                    ExternalPressure = q_h * cp * G * kd,
                    GCpi = _gcpi,
                    InternalPressure = q_h * _gcpi * kd,
                });
            }
        }

        public override void CalculateExternalPressures()
        {
            // Calculate the pressures and store them in the appropriate dictionaries.
            CalculateRoofPressures(TryGetGCp_Pos_Roof_ByAreaID_MWFRS, windPressureRoof_Pos_External);
            CalculateRoofPressures(TryGetGCp_Neg_Roof_ByAreaID_MWFRS, windPressureRoof_Neg_External);

            CalculateWallPressures(TryGetGCp_Pos_BuildingLengthWall_ByAreaID, windPressureWall_Pos_External_MWFRS);
            CalculateWallPressures(TryGetGCp_Neg_BuildingLengthWall_ByAreaID, windPressureWall_Neg_External_MWFRS);
        }
    }
}
