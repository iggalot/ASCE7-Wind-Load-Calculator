using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;

namespace ASCE7WindLoadCalculator
{
    public enum ASCE7_Versions
    {
        ASCE_VER_7_16 = 0,  // this must start at index 0
        ASCE_VER_7_22 = 1
    }

    public enum WindLoadCalculationTypes
    {
        COMPONENT_AND_CLADDING = 0,  // Components and cladding  -- Chapter 30
        MWFRS = 1 // MWFRS
    }

    public enum WindExposureCategories
    {
        WIND_EXP_CAT_B = 0,
        WIND_EXP_CAT_C = 1,
        WIND_EXP_CAT_D = 2
    }

    public enum WindLoadCases
    {
        WLC_BaseA,
        WLC_BaseB,
        WLC_Balloon1,
        WLC_Balloon2,
        WLC_Suction1,
        WLC_Suction2
    }

    public enum WindZones_Walls_MWFRS
    {
        [Description("Windward Wall - z=0ft")]
        MWFRS_WW_0 = 0,
        [Description("Windward Wall - z=15ft")]
        MWFRS_WW_15 = 1,
        [Description("Windward Wall - z=h")]
        MWFRS_WW_h = 2,
        [Description("Leeward Wall")]
        MWFRS_LW_h = 3,
        [Description("Sidewall")]
        MWFRS_SW_h = 4
    }

    public enum WindZones_Roof_MWFRS
    {
        [Description("Windward Roof 0->h/2")]
        MWFRS_WR_0_h2 = 0,
        [Description("Windward Roof h/2->h")]
        MWFRS_WR_h2_h = 1,
        [Description("Windward Roof h->2h")]
        MWFRS_WR_h_2h = 2,
        [Description("Windward Roof > 2h")]
        MWFRS_WR_2h_L = 3,
        [Description("Windward Roof Full")]
        MWFRS_WR_Full = 4,
        [Description("Leeward Roof Full")]
        MWFRS_LR_Full = 5
    }

    public abstract class WindLoadCalculator_Base : IWindLoadCalculator_CC_Base, IWindLoadCalculator_MWFRS_Base
    {
        public delegate bool TryGetGcpDelegate(int id, out double gcp);

        public abstract ASCE7_Versions ASCEVersion { get; }
        public abstract WindParameters_Base Parameters { get; set; }
        public abstract BuildingData buildingData { get; set; }

        /// <summary>
        /// Contains the calculator that will be used to calculate the effective wind areas on the roof
        /// </summary>
        public abstract AreaCalculator_Base RoofAreaCalculator { get; set; }

        /// <summary>
        /// Contains the calculator for the wall loads acting on the BuildingLength dimension
        /// </summary>
        public abstract AreaCalculator_Base WallAreaCalculator_BldgLength { get; set; }
        /// <summary>
        /// Contains the calculator for the wall loads acting on the BuildingWidth dimension
        /// </summary>
        public abstract AreaCalculator_Base WallAreaCalculator_BldgWidth { get; set; }


        /// <summary>
        /// Gcp and Cp curves for the roofs as defined in the appropriate ASCE7 volume
        /// </summary>
        public abstract Chapter27and30_GCpCurveBase extGCpCurve_Roof { get; set; }
        public abstract Chapter27and30_GCpCurveBase extGCpCurve_Wall { get; set; }



        public Dictionary<int, PressureData> windPressureRoof_Pos_External { get; set; } = new Dictionary<int, PressureData>();
        public Dictionary<int, PressureData> windPressureRoof_Neg_External { get; set; } = new Dictionary<int, PressureData>();
        public Dictionary<int, PressureData> windPressureBuildingLengthWall_Pos_External { get; set; } = new Dictionary<int, PressureData>();
        public Dictionary<int, PressureData> windPressureBuildingLengthWall_Neg_External { get; set; } = new Dictionary<int, PressureData>();
        public Dictionary<int, PressureData> windPressureBuildingWidthWall_Pos_External { get; set; } = new Dictionary<int, PressureData>();
        public Dictionary<int, PressureData> windPressureBuildingWidthWall_Neg_External { get; set; } = new Dictionary<int, PressureData>();
        public Dictionary<int, PressureData> windPressureOverhang_External { get; set; } = new Dictionary<int, PressureData>();

        /// <summary>
        /// MWFRS pressures
        /// </summary>
        public Dictionary<int, PressureData> windPressureWall_Pos_External_MWFRS { get; set; } = new Dictionary<int, PressureData>();
        public Dictionary<int, PressureData> windPressureWall_Neg_External_MWFRS { get; set; } = new Dictionary<int, PressureData>();



        public Dictionary<int, PressureData> windPressureRoof_Pos_Net { get; set; } = new Dictionary<int, PressureData>();
        public Dictionary<int, PressureData> windPressureRoof_Neg_Net { get; set; } = new Dictionary<int, PressureData>();
        public Dictionary<int, PressureData> windPressureBuildingLength_Pos_Net_CC { get; set; } = new Dictionary<int, PressureData>();
        public Dictionary<int, PressureData> windPressureBuildingLengthWall_Neg_Net_CC { get; set; } = new Dictionary<int, PressureData>();
        public Dictionary<int, PressureData> windPressureBuildingWidthWall_Pos_Net_CC { get; set; } = new Dictionary<int, PressureData>();
        public Dictionary<int, PressureData> windPressureBuildingWidthWall_Neg_Net_CC { get; set; } = new Dictionary<int, PressureData>();


        /// <summary>
        /// Calculates the dyanmic wind pressure q at a specified height z per ASCE7_16 and ASCE7_22
        /// </summary>
        /// <param name="p"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        public abstract double CalculateDynamicWindPressure(double z);
        // Get Kz approximation based on building height and exposure category
        public abstract double GetKz(double z, WindExposureCategories exposure);

        /// <summary>
        /// Compute Kd coefficient for the specific version of ASCE7
        /// </summary>
        /// <param name="calc_type"></param>
        /// <param name="version"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public abstract double GetKd(WindLoadCalculationTypes calc_type);

        /// <summary>
        /// The +/- coefficient for internal pressure coefficient GCpi from ASCE7_16 & ASCE7-22 Table 26.13-1
        /// </summary>
        /// <returns></returns>
        public abstract double GetGCpi();


        public void Initialize()
        {
            CreateAreaCalculators();
            CalculateExternalPressures();
        }

        private void CreateAreaCalculators()
        {
            RoofAreaCalculator = RoofAreaCalculatorFactory.Create(buildingData, Parameters, ASCEVersion);

            if(RoofAreaCalculator != null)
            {
                RoofAreaCalculator.ComputeEffectiveWindAreas();
            }

            bool length_is_gable = false;
            bool width_is_gable = false;
            if(buildingData.BuildingLength > buildingData.BuildingWidth)
            {
                width_is_gable = true;
            } else if (buildingData.BuildingLength < buildingData.BuildingWidth)
            {
                length_is_gable = true;
            } else
            {
                // if it's a square building we arbitrarily assign the gable to the building width
                width_is_gable = true;
            }

            Dictionary<string, double> bldg_length_wall_param = new Dictionary<string, double>();
            bldg_length_wall_param.Add("WallLength", buildingData.BuildingLength);
            WallAreaCalculator_BldgLength = WallAreaCalculatorFactory.Create(buildingData, Parameters, ASCEVersion, length_is_gable);
            if(WallAreaCalculator_BldgLength != null)
            {
                WallAreaCalculator_BldgLength.ComputeEffectiveWindAreas();
            }

            Dictionary<string, double> bldg_width_wall_param = new Dictionary<string, double>();
            bldg_width_wall_param.Add("WallLength", buildingData.BuildingWidth);
            WallAreaCalculator_BldgWidth = WallAreaCalculatorFactory.Create(buildingData, Parameters, ASCEVersion, width_is_gable);
            if(WallAreaCalculator_BldgWidth != null)
            {
                WallAreaCalculator_BldgWidth.ComputeEffectiveWindAreas();
            }
        }

        /// <summary>
        /// Calculates the qh * GCP for external pressures
        /// </summary>
        public abstract void CalculateExternalPressures();

        public string DisplayExternalPressures()
        {
            string str = "External Pressures GCp * qh:\n";

            str += "-- Positive roof pressures\n";
            foreach (var item in windPressureRoof_Pos_External)
            {
                str += "----" + item.Key + ": " + item.Value + "\n";
            }

            str += "-- Negative roof pressures\n";
            foreach (var item in windPressureRoof_Neg_External)
            {
                str += "----" + item.Key + ": " + item.Value + "\n";
            }

            str += "-- Positive end wall pressures\n";
            foreach (var item in windPressureBuildingWidthWall_Pos_External)
            {
                str += "----" + item.Key + ": " + item.Value + "\n";
            }

            str += "-- Negative end wall pressures\n";
            foreach (var item in windPressureBuildingWidthWall_Neg_External)
            {
                str += "----" + item.Key + ": " + item.Value + "\n";
            }

            str += "-- Positive side wall pressures\n";
            foreach (var item in windPressureBuildingLengthWall_Pos_External)
            {
                str += "----" + item.Key + ": " + item.Value + "\n";
            }

            str += "-- Negative side wall pressures\n";
            foreach (var item in windPressureBuildingLengthWall_Neg_External)
            {
                str += "----" + item.Key + ": " + item.Value + "\n";
            }

            str += "-- Overhang pressures\n";
            foreach (var item in windPressureOverhang_External)
            {
                str += "----" + item.Key + ": " + item.Value + "\n";
            }

            return str;
        }

        public string DisplayNetPressures()
        {
            string str = "Net Pressures GCp * qh - GCpi * qh:\n";

            str += "-- Positive roof pressures\n";
            foreach (var item in windPressureRoof_Pos_Net)
            {
                str += "----" + item.Key + ": " + item.Value + "\n";
            }

            str += "-- Negative roof pressures\n";
            foreach (var item in windPressureRoof_Neg_Net)
            {
                str += "----" + item.Key + ": " + item.Value + "\n";
            }

            str += "-- Positive BuildingWidth pressures\n";
            foreach (var item in windPressureBuildingWidthWall_Pos_Net_CC)
            {
                str += "----" + item.Key + ": " + item.Value + "\n";
            }

            str += "-- Negative BuildingWidth pressures\n";
            foreach (var item in windPressureBuildingWidthWall_Neg_Net_CC)
            {
                str += "----" + item.Key + ": " + item.Value + "\n";
            }

            str += "-- Positive BuildingLength pressures\n";
            foreach (var item in windPressureBuildingLength_Pos_Net_CC)
            {
                str += "----" + item.Key + ": " + item.Value + "\n";
            }

            str += "-- Negative BuildingLength pressures\n";
            foreach (var item in windPressureBuildingLengthWall_Neg_Net_CC)
            {
                str += "----" + item.Key + ": " + item.Value + "\n";
            }

            str += "-- Overhang pressures\n";
            foreach (var item in windPressureOverhang_External)
            {
                str += "----" + item.Key + ": " + item.Value + "\n";
            }

            return str;
        }

        /// <summary>
        /// Determines the number of roof zones a building will have for MWFRS pressures
        /// </summary>
        /// <returns></returns>
        public static int GetNumberRoofZones_MWFRS(double mean_roof_ht, double length)
        {
            double offset1 = 0.5 * mean_roof_ht;
            double offset2 = 1.0 * mean_roof_ht;
            double offset3 = 2.0 * mean_roof_ht;

            if (length > offset1 && length <= offset2)
                return 2;
            else if (length > offset2 && length <= offset3)
                return 3;
            else if (length > offset3)
                return 4;
            else
                return 1;
        }

        /// <summary>
        /// Returns the roof zone number that a point is in with 4 being closest to WW wall and 1
        /// being farthest, based on 0.5h, h, and 2h offsets from ASCE7-16 and ASCE7-22
        /// -- points on an offset line belong to the zone number to the left of the line
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public static int GetRoofZoneNumber(Point point, double mean_roof_ht, double length)
        {
            double offset1 = 0.5 * mean_roof_ht;
            double offset2 = 1.0 * mean_roof_ht;
            double offset3 = 2.0 * mean_roof_ht;

            if (point.X <= offset1) return 4;
            else if (point.X <= offset2) return 3;
            else if (point.X <= offset3) return 2;
            else return 1;
        }

        /// <summary>
        /// Function to retrieve a GCp value by area for a CC curve..  If successfull returns true
        /// </summary>
        /// <param name="id">id of the area</param>
        /// <param name="gcp">thereturn GCP value</param>
        /// <returns></returns>
        public bool TryGetGCp_Pos_Roof_ByAreaID_CC(int id, out double gcp)
        {
            gcp = 0.0;


            if (!RoofAreaCalculator.effWindAreas.TryGetValue(id, out var area))
                return false;

            foreach (var gcp_curve in this.extGCpCurve_Roof.RoofCurves_Pos)
            {
                if (gcp_curve.Key == area.Label_Full)
                {
                    gcp = gcp_curve.Value.Evaluate(area.Area);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Function to retrieve a GCp value by area for a CC curve.  If successfull returns true
        /// </summary>
        /// <param name="id">id of the area</param>
        /// <param name="gcp">thereturn GCP value</param>
        /// <returns></returns>
        public bool TryGetGCp_Neg_Roof_ByAreaID_CC(int id, out double gcp)
        {
            gcp = 0.0;


            if (!RoofAreaCalculator.effWindAreas.TryGetValue(id, out var area))
                return false;

            foreach (var gcp_curve in this.extGCpCurve_Roof.RoofCurves_Neg)
            {
                if (gcp_curve.Key == area.Label_Full)
                {
                    gcp = gcp_curve.Value.Evaluate(area.Area);
                    return true;
                }
            }

            return false;
        }


        /// <summary>
        /// Function to retrieve a GCp value by area for a CC curve..  If successfull returns true
        /// </summary>
        /// <param name="id">id of the area</param>
        /// <param name="gcp">thereturn GCP value</param>
        /// <returns></returns>
        public bool TryGetGCp_Pos_Roof_ByAreaID_MWFRS(int id, out double gcp)
        {
            gcp = 0.0;


            if (!RoofAreaCalculator.effWindAreas.TryGetValue(id, out var area))
                return false;

            foreach (var gcp_curve in this.extGCpCurve_Roof.RoofCurves_Pos)
            {
                if (gcp_curve.Key == area.Label_Full)
                {
                    // if we are perpendicular to the ridge, use the slope to retrieve the roof Cp value
                    if (buildingData.RidgeDirection == RidgeDirections.RIDGE_DIR_PERP_TO_BLDGLENGTH)
                    {
                        gcp = gcp_curve.Value.Evaluate(buildingData.RoofPitch);
                    }
                    else
                    {
                        gcp = gcp_curve.Value.Evaluate(area.Area);
                    }

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Function to retrieve a GCp value by area for a CC curve.  If successfull returns true
        /// </summary>
        /// <param name="id">id of the area</param>
        /// <param name="gcp">thereturn GCP value</param>
        /// <returns></returns>
        public bool TryGetGCp_Neg_Roof_ByAreaID_MWFRS(int id, out double gcp)
        {
            gcp = 0.0;


            if (!RoofAreaCalculator.effWindAreas.TryGetValue(id, out var area))
                return false;

            foreach (var gcp_curve in this.extGCpCurve_Roof.RoofCurves_Neg)
            {
                if (gcp_curve.Key == area.Label_Full)
                {
                    // if we are perpendicular to the ridge, use the slope to retrieve the roof Cp value
                    if(buildingData.RidgeDirection == RidgeDirections.RIDGE_DIR_PERP_TO_BLDGLENGTH)
                    {
                        gcp = gcp_curve.Value.Evaluate(buildingData.RoofPitch);
                    } else
                    {
                        gcp = gcp_curve.Value.Evaluate(area.Area);
                    }

                    return true;
                }
            }

            return false;
        }






        /// <summary>
        /// Function to retrieve a GCp value by area.  If successfull returns true
        /// </summary>
        /// <param name="id">id of the area</param>
        /// <param name="gcp">thereturn GCP value</param>
        /// <returns></returns>
        public bool TryGetGCp_Pos_BuildingLengthWall_ByAreaID(int id, out double gcp)
        {
            gcp = 0.0;


            if (!WallAreaCalculator_BldgLength.effWindAreas.TryGetValue(id, out var area))
                return false;

            foreach (var gcp_curve in this.extGCpCurve_Wall.WallCurves_Pos)
            {
                if (gcp_curve.Key == area.Label_Full)
                {
                    gcp = gcp_curve.Value.Evaluate(area.Area);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Function to retrieve a GCp value by area.  If successfull returns true
        /// </summary>
        /// <param name="id">id of the area</param>
        /// <param name="gcp">thereturn GCP value</param>
        /// <returns></returns>
        public bool TryGetGCp_Neg_BuildingLengthWall_ByAreaID(int id, out double gcp)
        {
            gcp = 0.0;


            if (!WallAreaCalculator_BldgLength.effWindAreas.TryGetValue(id, out var area))
                return false;

            foreach (var gcp_curve in this.extGCpCurve_Wall.WallCurves_Neg)
            {
                if (gcp_curve.Key == area.Label_Full)
                {
                    gcp = gcp_curve.Value.Evaluate(area.Area);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Function to retrieve a GCp value by area.  If successfull returns true
        /// </summary>
        /// <param name="id">id of the area</param>
        /// <param name="gcp">thereturn GCP value</param>
        /// <returns></returns>
        public bool TryGetGCp_Pos_BuildingWidthWall_ByAreaID(int id, out double gcp)
        {
            gcp = 0.0;


            if (!WallAreaCalculator_BldgWidth.effWindAreas.TryGetValue(id, out var area))
                return false;

            foreach (var gcp_curve in this.extGCpCurve_Wall.WallCurves_Pos)
            {
                if (gcp_curve.Key == area.Label_Full)
                {
                    gcp = gcp_curve.Value.Evaluate(area.Area);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Function to retrieve a GCp value by area.  If successfull returns true
        /// </summary>
        /// <param name="id">id of the area</param>
        /// <param name="gcp">thereturn GCP value</param>
        /// <returns></returns>
        public bool TryGetGCp_Neg_BuildingWidthWall_ByAreaID(int id, out double gcp)
        {
            gcp = 0.0;


            if (!WallAreaCalculator_BldgWidth.effWindAreas.TryGetValue(id, out var area))
                return false;

            foreach (var gcp_curve in this.extGCpCurve_Wall.WallCurves_Neg)
            {
                if (gcp_curve.Key == area.Label_Full)
                {
                    gcp = gcp_curve.Value.Evaluate(area.Area);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Function to retrieve a GCp value by area.  If successfull returns true
        /// </summary>
        /// <param name="id">id of the area</param>
        /// <param name="gcp">thereturn GCP value</param>
        /// <returns></returns>
        public bool TryGetGCp_Overhang_ByArea(int id, out double gcp)
        {
            gcp = 0.0;


            if (!RoofAreaCalculator.effWindAreas.TryGetValue(id, out var area))
                return false;

            foreach (var gcp_curve in this.extGCpCurve_Roof.OverhangCurves)
            {
                if (gcp_curve.Key == area.Label_Full)
                {
                    gcp = gcp_curve.Value.Evaluate(area.Area);
                    return true;
                }
            }

            return false;
        }

    }
}
