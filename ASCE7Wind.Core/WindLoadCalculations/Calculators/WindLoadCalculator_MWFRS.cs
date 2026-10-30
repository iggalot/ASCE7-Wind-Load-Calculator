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

        /// <summary>Leeward wall Cp, Fig. 27.3-1: L/B 0-1: -0.5, 2: -0.3, &gt;= 4: -0.2 (linear between).</summary>
        private double GetLWCpValues()
        {
            double x = buildingData.L_Over_B;
            if (x <= 1) return -0.5;
            if (x <= 2) return -0.5 + (x - 1) * (0.2 / 1.0);
            if (x < 4) return -0.3 + (x - 2) * (0.1 / 2.0);
            return -0.2;
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
            if (extGCpCurve_Roof == null)
            {
                AddValidationMessage("No Fig. 27.3-1 roof coefficients apply to this roof geometry.");
                return;
            }

            double G = Parameters.GustFactor; // gust-effect factor; 0.85 for rigid buildings (26.11.1)
            double qh = CalculateDynamicWindPressure(buildingData.MeanRoofHeight);

            // Roof planes facing the wind (windward/leeward roof) read Cp against roof slope; distance
            // zones 1-4 read against area (the -1.3 area reduction).
            double slope = buildingData.RoofPitch;
            Func<EffectiveWindArea, double> curveX = a =>
                a.Label_Full == "ZoneWWR" || a.Label_Full == "ZoneLWR" ? slope : a.Area;

            AddPressures(RoofAreaCalculator, extGCpCurve_Roof.RoofCurves_Pos, curveX, qh, G, windPressureRoof_Pos_External);
            AddPressures(RoofAreaCalculator, extGCpCurve_Roof.RoofCurves_Neg, curveX, qh, G, windPressureRoof_Neg_External);
            AddWallPressures(qh, G, windPressureWall_Pos_External_MWFRS);
            AddWallPressures(qh, G, windPressureWall_Neg_External_MWFRS);
            overhangQhGKd = qh * G * PressureKd;
        }

        // ---- Roof overhangs, 27.3.3 ----

        /// <summary>Cp on the bottom surface of a windward roof overhang (27.3.3).</summary>
        public const double Cp_OverhangBottom = 0.8;

        private double overhangQhGKd;

        /// <summary>
        /// Pressure (psf) pushing up on the soffit of the windward overhang: qh G (0.8), with Kd per the edition. No internal
        /// pressure: the soffit is outside the building.
        /// </summary>
        public double OverhangBottomPressure => overhangQhGKd * Cp_OverhangBottom;

        /// <summary>
        /// The top-surface pressure at the windward eave (Fig. 27.3-1): the windward roof plane for wind normal to the ridge,
        /// otherwise the windward distance zone (Zone4, 0 to h/2). The most negative value is used. Null when no roof pressures exist.
        /// </summary>
        public PressureData WindwardOverhangTop
        {
            get
            {
                if (RoofAreaCalculator == null) return null;
                PressureData best = null;
                foreach (var zone in RoofAreaCalculator.effWindAreas)
                {
                    string label = zone.Value.Label_Full;
                    if (label != "ZoneWWR" && label != "Zone4") continue; // Zone4 = 0 to h/2 from the windward edge
                    foreach (var set in new[] { windPressureRoof_Neg_External, windPressureRoof_Pos_External })
                        if (set.TryGetValue(zone.Key, out var p) && (best == null || p.ExternalPressure < best.ExternalPressure))
                            best = p;
                }
                return best;
            }
        }

        /// <summary>
        /// Net pressure (psf) on the windward overhang, top plus bottom, negative = upward: the top-surface external pressure
        /// combined with the soffit pressure (27.3.3). NaN when no roof pressures exist.
        /// </summary>
        public double WindwardOverhangNetPressure =>
            WindwardOverhangTop is PressureData top ? top.ExternalPressure - OverhangBottomPressure : double.NaN;

        /// <summary>Upward force per foot of eave (lb/ft) on a windward overhang of the building's overhang width.</summary>
        public double WindwardOverhangUpliftPerFoot => buildingData.OverhangWidth > 0 && !double.IsNaN(WindwardOverhangNetPressure)
            ? -WindwardOverhangNetPressure * buildingData.OverhangWidth : 0;

        /// <summary>
        /// External windward-wall pressure (psf) at height z: qz G Cp, with Kd per the edition. ASCE 7 27.3:
        /// the windward wall uses qz at each height (Kz taken at not less than 15 ft); the leeward wall,
        /// side walls and roof use qh.
        /// </summary>
        public double WindwardWallExternalPressureAt(double z)
        {
            double qz = CalculateDynamicWindPressure(Math.Max(0, z));
            return qz * Cp_WW * Parameters.GustFactor * PressureKd;
        }

        /// <summary>
        /// Net windward-wall pressure (psf) at height z, combined with the internal pressure (based on qh)
        /// that makes it largest: qz G Cp + qh |GCpi|.
        /// </summary>
        public double WindwardWallNetPressureAt(double z)
        {
            double qh = CalculateDynamicWindPressure(buildingData.MeanRoofHeight);
            return WindwardWallExternalPressureAt(z) + qh * GetGCpi() * PressureKd;
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
