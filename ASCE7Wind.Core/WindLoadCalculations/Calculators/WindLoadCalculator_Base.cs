using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

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

    /// <summary>
    /// Edition-agnostic wind calculator. Holds the building, parameters, zone layouts and coefficient
    /// figures, and fills the pressure tables. Edition differences come from <see cref="Edition"/>.
    /// </summary>
    public abstract class WindLoadCalculator_Base
    {
        protected WindLoadCalculator_Base(Asce7Edition edition, WindParameters_Base parameters, BuildingData building)
        {
            Edition = edition ?? throw new ArgumentNullException(nameof(edition));
            Parameters = parameters;
            buildingData = building;
        }

        public Asce7Edition Edition { get; }
        public ASCE7_Versions ASCEVersion => Edition.Version;

        private readonly List<string> validationMessages = new List<string>();

        /// <summary>
        /// Plain-language reasons the results are incomplete or outside the method's limits
        /// (for example, a mean roof height above the Chapter 30 Part 1 limit). Empty when valid.
        /// </summary>
        public IReadOnlyList<string> ValidationMessages => validationMessages;

        /// <summary>True when the building is within the limits of the selected procedure.</summary>
        public bool IsValid => validationMessages.Count == 0;

        protected void AddValidationMessage(string message) => validationMessages.Add(message);
        public WindParameters_Base Parameters { get; set; }
        public BuildingData buildingData { get; set; }

        /// <summary>Zone layout for the roof.</summary>
        public AreaCalculator_Base RoofAreaCalculator { get; set; }

        /// <summary>Zone layout for the walls along the BuildingLength dimension.</summary>
        public AreaCalculator_Base WallAreaCalculator_BldgLength { get; set; }

        /// <summary>Zone layout for the walls along the BuildingWidth dimension.</summary>
        public AreaCalculator_Base WallAreaCalculator_BldgWidth { get; set; }

        /// <summary>GCp (Ch. 30) or Cp (Ch. 27) curves for the roof and walls.</summary>
        public Chapter27and30_GCpCurveBase extGCpCurve_Roof { get; set; }
        public Chapter27and30_GCpCurveBase extGCpCurve_Wall { get; set; }

        public Dictionary<int, PressureData> windPressureRoof_Pos_External { get; set; } = new Dictionary<int, PressureData>();
        public Dictionary<int, PressureData> windPressureRoof_Neg_External { get; set; } = new Dictionary<int, PressureData>();
        public Dictionary<int, PressureData> windPressureBuildingLengthWall_Pos_External { get; set; } = new Dictionary<int, PressureData>();
        public Dictionary<int, PressureData> windPressureBuildingLengthWall_Neg_External { get; set; } = new Dictionary<int, PressureData>();
        public Dictionary<int, PressureData> windPressureBuildingWidthWall_Pos_External { get; set; } = new Dictionary<int, PressureData>();
        public Dictionary<int, PressureData> windPressureBuildingWidthWall_Neg_External { get; set; } = new Dictionary<int, PressureData>();
        public Dictionary<int, PressureData> windPressureOverhang_External { get; set; } = new Dictionary<int, PressureData>();

        /// <summary>MWFRS wall pressures.</summary>
        public Dictionary<int, PressureData> windPressureWall_Pos_External_MWFRS { get; set; } = new Dictionary<int, PressureData>();
        public Dictionary<int, PressureData> windPressureWall_Neg_External_MWFRS { get; set; } = new Dictionary<int, PressureData>();

        public Dictionary<int, PressureData> windPressureRoof_Pos_Net { get; set; } = new Dictionary<int, PressureData>();
        public Dictionary<int, PressureData> windPressureRoof_Neg_Net { get; set; } = new Dictionary<int, PressureData>();
        public Dictionary<int, PressureData> windPressureBuildingLength_Pos_Net_CC { get; set; } = new Dictionary<int, PressureData>();
        public Dictionary<int, PressureData> windPressureBuildingLengthWall_Neg_Net_CC { get; set; } = new Dictionary<int, PressureData>();
        public Dictionary<int, PressureData> windPressureBuildingWidthWall_Pos_Net_CC { get; set; } = new Dictionary<int, PressureData>();
        public Dictionary<int, PressureData> windPressureBuildingWidthWall_Neg_Net_CC { get; set; } = new Dictionary<int, PressureData>();

        // ---------------------------------------------------------------------------------------
        // Velocity pressure
        // ---------------------------------------------------------------------------------------

        /// <summary>
        /// Velocity pressure qz (psf) at height z (ft), Eq. 26.10-1: qz = 0.00256 Kz Kzt Ke [Kd] V^2.
        /// Includes Kd only when the edition puts Kd in q (see <see cref="Asce7Edition.KdInVelocityPressure"/>).
        /// There is no importance factor: risk category is built into the ASCE 7-16/7-22 wind-speed maps.
        /// The ground elevation factor Ke is taken as 1.0 (permitted for all elevations, 26.9).
        /// </summary>
        public double CalculateDynamicWindPressure(double z)
        {
            if (Parameters == null)
                throw new InvalidOperationException("Wind parameters are not set.");

            WindParameters_Base p = Parameters;
            double V = p.WindSpeed;
            double Kzt = p.Kzt;
            double Kz = GetKz(z, p.ExposureCategory);

            if (Edition.KdInVelocityPressure)
            {
                double Kd = GetKd(Parameters.AnalysisType);
                return 0.00256 * Kz * Kzt * Kd * V * V;
            }
            return 0.00256 * Kz * Kzt * V * V;
        }

        /// <summary>Velocity pressure exposure coefficient Kz (power-law form, z not less than 15 ft).</summary>
        public double GetKz(double z, WindExposureCategories exposure)
        {
            var (alpha, zg) = Edition.TerrainConstants(exposure);
            z = Math.Max(z, 15);
            return Edition.KzCoefficient * Math.Pow(z / zg, 2.0 / alpha);
        }

        /// <summary>Directionality factor Kd for buildings (Table 26.6-1).</summary>
        public double GetKd(WindLoadCalculationTypes calc_type)
        {
            switch (calc_type)
            {
                case WindLoadCalculationTypes.COMPONENT_AND_CLADDING:
                case WindLoadCalculationTypes.MWFRS:
                    return 0.85;
                default:
                    throw new NotImplementedException("ERROR:  " + calc_type + " not supported. ");
            }
        }

        /// <summary>Internal pressure coefficient magnitude |GCpi| (Table 26.13-1).</summary>
        public double GetGCpi() => GCpiFor(buildingData.EnclosureType);

        /// <summary>|GCpi| for an enclosure classification (Table 26.13-1).</summary>
        public static double GCpiFor(BuildingEnclosures enclosure)
        {
            switch (enclosure)
            {
                case BuildingEnclosures.BLDG_ENCLOSED: return 0.18;
                case BuildingEnclosures.BLDG_PARTIALLY_ENCLOSED: return 0.55;
                case BuildingEnclosures.BLDG_PARTIALLY_OPEN: return 0.18;
                case BuildingEnclosures.BLDG_OPEN: return 0.0;
                default: throw new ArgumentOutOfRangeException(nameof(enclosure), enclosure, "Invalid enclosure type.");
            }
        }

        /// <summary>
        /// Kd multiplier applied in the pressure equations: Kd when the edition keeps Kd out of q, otherwise 1.
        /// </summary>
        protected double PressureKd => Edition.KdInVelocityPressure ? 1.0 : GetKd(Parameters.AnalysisType);

        // ---------------------------------------------------------------------------------------
        // Pipeline
        // ---------------------------------------------------------------------------------------

        public void Initialize()
        {
            CreateAreaCalculators();
            CalculateExternalPressures();
        }

        private void CreateAreaCalculators()
        {
            RoofAreaCalculator = RoofAreaCalculatorFactory.Create(buildingData, Parameters, Edition);
            ComputeZones(RoofAreaCalculator, "roof");

            // Gable-end walls are the walls perpendicular to the ridge.
            bool gable = buildingData.RoofType == RoofTypes.ROOF_TYPE_GABLE;
            bool length_is_gable = gable && buildingData.RidgeDirection == RidgeDirections.RIDGE_DIR_PERP_TO_BLDGLENGTH;
            bool width_is_gable = gable && buildingData.RidgeDirection == RidgeDirections.RIDGE_DIR_PARALLEL_TO_BLDGLENGTH;

            WallAreaCalculator_BldgLength = WallAreaCalculatorFactory.Create(buildingData, Parameters, Edition, length_is_gable, buildingData.BuildingLength);
            ComputeZones(WallAreaCalculator_BldgLength, "length walls");

            WallAreaCalculator_BldgWidth = WallAreaCalculatorFactory.Create(buildingData, Parameters, Edition, width_is_gable, buildingData.BuildingWidth);
            ComputeZones(WallAreaCalculator_BldgWidth, "width walls");
        }

        private void ComputeZones(AreaCalculator_Base zones, string surface)
        {
            if (zones != null && !zones.Compute())
                AddValidationMessage($"The {surface} zone layout could not be completed for a building this size; some zones may be missing.");
        }

        /// <summary>Fills the pressure tables from the zones and coefficient curves.</summary>
        public abstract void CalculateExternalPressures();

        /// <summary>
        /// Evaluates the curve whose key matches the zone label. Returns false when the figure has no
        /// curve for that zone (the zone is then left out of the results).
        /// </summary>
        protected static bool TryEvaluateCurve(
            Dictionary<string, ExternalGCpCurve> curves,
            EffectiveWindArea zone,
            double x,
            out double gcp)
        {
            if (curves != null && curves.TryGetValue(zone.Label_Full, out var curve))
            {
                gcp = curve.Evaluate(x);
                return true;
            }
            gcp = 0.0;
            return false;
        }

        /// <summary>
        /// Adds one PressureData per zone that has a matching curve. <paramref name="minimumNet"/> is the smallest net design pressure (psf), 0 for none.
        /// External = q * coefficient * G * Kd(pressure); internal = q * GCpi * Kd(pressure).
        /// </summary>
        protected void AddPressures(
            AreaCalculator_Base zones,
            Dictionary<string, ExternalGCpCurve> curves,
            Func<EffectiveWindArea, double> curveX,
            double q,
            double G,
            Dictionary<int, PressureData> target,
            double minimumNet = 0)
        {
            if (zones == null) return;

            double kd = PressureKd;
            double gcpi = GetGCpi();

            foreach (var areaEntry in zones.effWindAreas)
            {
                if (!TryEvaluateCurve(curves, areaEntry.Value, curveX(areaEntry.Value), out double gcp))
                    continue;

                target.Add(areaEntry.Key, new PressureData()
                {
                    AreaID = areaEntry.Key,
                    qh = q,
                    GCp = gcp,
                    ExternalPressure = q * gcp * G * kd,
                    GCpi = gcpi,
                    InternalPressure = q * gcpi * kd,
                    MinimumNet = minimumNet,
                });
            }
        }

        // ---------------------------------------------------------------------------------------
        // Reporting
        // ---------------------------------------------------------------------------------------

        public string DisplayExternalPressures()
        {
            var sb = new StringBuilder("External Pressures GCp * qh:\n");
            AppendSection(sb, "-- Positive roof pressures", windPressureRoof_Pos_External);
            AppendSection(sb, "-- Negative roof pressures", windPressureRoof_Neg_External);
            AppendSection(sb, "-- Positive end wall pressures", windPressureBuildingWidthWall_Pos_External);
            AppendSection(sb, "-- Negative end wall pressures", windPressureBuildingWidthWall_Neg_External);
            AppendSection(sb, "-- Positive side wall pressures", windPressureBuildingLengthWall_Pos_External);
            AppendSection(sb, "-- Negative side wall pressures", windPressureBuildingLengthWall_Neg_External);
            AppendSection(sb, "-- Overhang pressures", windPressureOverhang_External);
            return sb.ToString();
        }

        public string DisplayNetPressures()
        {
            var sb = new StringBuilder("Net Pressures GCp * qh - GCpi * qh:\n");
            AppendSection(sb, "-- Positive roof pressures", windPressureRoof_Pos_Net);
            AppendSection(sb, "-- Negative roof pressures", windPressureRoof_Neg_Net);
            AppendSection(sb, "-- Positive BuildingWidth pressures", windPressureBuildingWidthWall_Pos_Net_CC);
            AppendSection(sb, "-- Negative BuildingWidth pressures", windPressureBuildingWidthWall_Neg_Net_CC);
            AppendSection(sb, "-- Positive BuildingLength pressures", windPressureBuildingLength_Pos_Net_CC);
            AppendSection(sb, "-- Negative BuildingLength pressures", windPressureBuildingLengthWall_Neg_Net_CC);
            AppendSection(sb, "-- Overhang pressures", windPressureOverhang_External);
            return sb.ToString();
        }

        private static void AppendSection(StringBuilder sb, string title, Dictionary<int, PressureData> items)
        {
            sb.Append(title).Append('\n');
            foreach (var item in items)
                sb.Append("----").Append(item.Key).Append(": ").Append(item.Value).Append('\n');
        }
    }
}
