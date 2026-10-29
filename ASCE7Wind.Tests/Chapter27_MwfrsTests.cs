using System;
using System.Linq;
using ASCE7WindLoadCalculator;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ASCE7Wind.Tests
{
    /// <summary>
    /// ASCE 7 Chapter 27 Part 1 (directional procedure), Fig. 27.3-1 external pressure coefficients,
    /// Eq. 27.3-1 design pressure, and the zone geometry it implies.
    /// </summary>
    [TestClass]
    public class Chapter27_MwfrsTests
    {
        private static WindLoadCalculator_MWFRS Mwfrs(ASCE7_Versions v, BuildingData b) =>
            (WindLoadCalculator_MWFRS)Bldg.Run(v, WindLoadCalculationTypes.MWFRS, b);

        // ---- Fig. 27.3-1 wall Cp -------------------------------------------------------------------

        [TestMethod]
        public void WallCp_WindwardAndSide()
        {
            var c = Mwfrs(ASCE7_Versions.ASCE_VER_7_16, Bldg.Make(60, 40, 15));
            Assert.AreEqual(0.8, c.Cp_WW, Tol.Tight);
            Assert.AreEqual(-0.7, c.Cp_SW, Tol.Tight);
        }

        /// <summary>Leeward wall Cp: L/B 0-1: -0.5, 2: -0.3, >=4: -0.2, linear between table points.</summary>
        [DataTestMethod]
        [DataRow(0.5, -0.5)]
        [DataRow(1.0, -0.5)]
        [DataRow(1.5, -0.4)]
        [DataRow(2.0, -0.3)]
        [DataRow(3.0, -0.25)]
        [DataRow(4.0, -0.2)]
        [DataRow(6.0, -0.2)]
        public void WallCp_Leeward_Table(double LoverB, double expected)
        {
            var c = Mwfrs(ASCE7_Versions.ASCE_VER_7_16, Bldg.Make(40 * LoverB, 40, 15));
            Assert.AreEqual(expected, c.Cp_LW, Tol.Tight);
        }

        // ---- Fig. 27.3-1 roof Cp, wind parallel to ridge (and flat roofs) ---------------------------

        [TestMethod]
        public void RoofCp_ParallelToRidge_LowRatio()
        {
            // h/L <= 0.5: 0 to h/2: -0.9, h/2 to h: -0.9, h to 2h: -0.5, > 2h: -0.3 ; alternate -0.18
            var fig = Chapter27RoofFigureFactory.CreateRoofFigure(Bldg.Make(120, 40, 15));
            Assert.AreEqual(-0.9, fig.RoofCurves_Neg["Zone4"].Evaluate(50), Tol.Tight);
            Assert.AreEqual(-0.9, fig.RoofCurves_Neg["Zone3"].Evaluate(50), Tol.Tight);
            Assert.AreEqual(-0.5, fig.RoofCurves_Neg["Zone2"].Evaluate(50), Tol.Tight);
            Assert.AreEqual(-0.3, fig.RoofCurves_Neg["Zone1"].Evaluate(50), Tol.Tight);
            foreach (var z in new[] { "Zone1", "Zone2", "Zone3", "Zone4" })
                Assert.AreEqual(-0.18, fig.RoofCurves_Pos[z].Evaluate(50), Tol.Tight);
        }

        [TestMethod]
        public void RoofCp_ParallelToRidge_HighRatio_WithAreaReduction()
        {
            // h/L >= 1.0: 0 to h/2: -1.3 (x area reduction), > h/2: -0.7
            var b = Bldg.Make(30, 30, 30); // h/L = 1
            var fig = Chapter27RoofFigureFactory.CreateRoofFigure(b);
            Assert.AreEqual(-1.3, fig.RoofCurves_Neg["Zone4"].Evaluate(50), Tol.Tight);
            Assert.AreEqual(-0.7, fig.RoofCurves_Neg["Zone3"].Evaluate(50), Tol.Tight);
        }

        [TestMethod]
        public void RoofCp_ParallelToRidge_InterpolatesOnHoverL()
        {
            // h/L = 0.75 is midway between the 0.5 and 1.0 rows.
            var fig = Chapter27RoofFigureFactory.CreateRoofFigure(Bldg.Make(20, 40, 15));
            Assert.AreEqual(-1.1, fig.RoofCurves_Neg["Zone4"].Evaluate(50), 1e-9);
            Assert.AreEqual(-0.6, fig.RoofCurves_Neg["Zone2"].Evaluate(50), 1e-9);
        }

        // ---- Fig. 27.3-1 roof Cp, wind normal to ridge, theta >= 10 deg ----------------------------

        [DataTestMethod]
        //        slope   WW neg  WW pos  LW
        [DataRow(10.0, -0.7, -0.18, -0.3)]
        [DataRow(15.0, -0.5, 0.0, -0.5)]
        [DataRow(20.0, -0.3, 0.2, -0.6)]
        [DataRow(25.0, -0.2, 0.3, -0.6)]
        [DataRow(30.0, -0.2, 0.3, -0.6)]
        [DataRow(35.0, 0.0, 0.4, -0.6)]
        [DataRow(45.0, 0.0, 0.4, -0.6)]
        public void RoofCp_NormalToRidge_LowRatio(double slope, double wwNeg, double wwPos, double lw)
        {
            // h/L <= 0.25 row of Fig. 27.3-1
            var fig = new NormToRidge_LargeSlope_Roof_Low(slope);
            Assert.AreEqual(wwNeg, fig.RoofCurves_Neg["ZoneWWR"].Evaluate(slope), Tol.Tight);
            Assert.AreEqual(wwPos, fig.RoofCurves_Pos["ZoneWWR"].Evaluate(slope), Tol.Tight);
            Assert.AreEqual(lw, fig.RoofCurves_Neg["ZoneLWR"].Evaluate(slope), Tol.Tight);
        }

        [DataTestMethod]
        [DataRow(60.0, 0.6)]
        [DataRow(70.0, 0.7)]
        [DataRow(80.0, 0.8)]
        public void RoofCp_NormalToRidge_SteepRoof_Is001Theta(double slope, double expected)
        {
            var fig = new NormToRidge_LargeSlope_Roof_Low(slope);
            Assert.AreEqual(expected, fig.RoofCurves_Pos["ZoneWWR"].Evaluate(slope), 1e-9);
        }

        /// <summary>Every h/L, including the exact table rows 0.25, 0.5 and 1.0, must yield a figure.</summary>
        [DataTestMethod]
        [DataRow(0.20)]
        [DataRow(0.25)]
        [DataRow(0.40)]
        [DataRow(0.50)]
        [DataRow(0.75)]
        [DataRow(1.00)]
        [DataRow(1.50)]
        public void RoofFigure_NormalToRidge_ExistsForAnyHoverL(double hOverL)
        {
            // Pick eave height so that the mean roof height gives exactly the requested h/L.
            double L = 40, slope = 20, W = 40;
            double rise = Math.Tan(slope * Math.PI / 180) * L / 2;
            double eave = hOverL * L - rise / 2;
            var b = Bldg.Make(L, W, eave, RoofTypes.ROOF_TYPE_GABLE, slope, RidgeDirections.RIDGE_DIR_PERP_TO_BLDGLENGTH);
            Assert.AreEqual(hOverL, b.h_Over_L, 1e-12);
            Assert.IsNotNull(Chapter27RoofFigureFactory.CreateRoofFigure(b), $"no Fig. 27.3-1 curves for h/L = {hOverL}");
        }

        // ---- Eq. 27.3-1: p = q Kd G Cp - qi Kd GCpi (Kd inside q for 7-16) --------------------------

        [DataTestMethod]
        [DataRow(ASCE7_Versions.ASCE_VER_7_16)]
        [DataRow(ASCE7_Versions.ASCE_VER_7_22)]
        public void WallPressure_AppliesKdExactlyOnce_AndGustFactor(ASCE7_Versions v)
        {
            var c = Mwfrs(v, Bldg.Make(60, 40, 15));
            double kz = c.GetKz(c.buildingData.MeanRoofHeight, WindExposureCategories.WIND_EXP_CAT_C);
            double qhWithKd = 0.00256 * kz * 1.0 * 0.85 * 115 * 115;
            var ww = c.WallAreaCalculator_BldgLength.effWindAreas.Single(z => z.Value.Label_Full == "ZoneWW");
            Assert.AreEqual(qhWithKd * 0.85 * 0.8, c.windPressureWall_Pos_External_MWFRS[ww.Key].ExternalPressure, 1e-9);
            Assert.AreEqual(qhWithKd * 0.18, c.windPressureWall_Pos_External_MWFRS[ww.Key].InternalPressure, 1e-9);
        }

        [TestMethod]
        public void GustFactor_ComesFromParameters()
        {
            var b = Bldg.Make(60, 40, 15);
            var p = Bldg.Params(WindLoadCalculationTypes.MWFRS);
            p.GustFactor = 1.0;
            var c = (WindLoadCalculator_MWFRS)WindLoadCalculatorFactory.Create(ASCE7_Versions.ASCE_VER_7_16, WindLoadCalculationTypes.MWFRS, p, b);
            c.Initialize();
            var ww = c.WallAreaCalculator_BldgLength.effWindAreas.Single(z => z.Value.Label_Full == "ZoneWW");
            double qh = c.CalculateDynamicWindPressure(b.MeanRoofHeight);
            Assert.AreEqual(qh * 1.0 * 0.8, c.windPressureWall_Pos_External_MWFRS[ww.Key].ExternalPressure, 1e-9);
        }

        // ---- MWFRS wall surfaces (wind along BuildingLength) ----------------------------------------

        [TestMethod]
        public void WallZones_FlatRoof_Areas()
        {
            var c = Mwfrs(ASCE7_Versions.ASCE_VER_7_16, Bldg.Make(60, 40, 15));
            var w = c.WallAreaCalculator_BldgLength;
            Assert.AreEqual(40 * 15, Bldg.Zones(w, "ZoneWW").Single().Area, 1e-9);
            Assert.AreEqual(40 * 15, Bldg.Zones(w, "ZoneLW").Single().Area, 1e-9);
            Assert.AreEqual(60 * 15, Bldg.Zones(w, "ZoneSW").Single().Area, 1e-9);
        }

        /// <summary>Ridge parallel to the length: windward/leeward faces are gable ends spanning the width.</summary>
        [TestMethod]
        public void WallZones_GableEnd_RidgeParallelToLength_UsesWidthForGableHeight()
        {
            double L = 60, W = 40, H = 15, slope = 15;
            var c = Mwfrs(ASCE7_Versions.ASCE_VER_7_16, Bldg.Make(L, W, H, RoofTypes.ROOF_TYPE_GABLE, slope, RidgeDirections.RIDGE_DIR_PARALLEL_TO_BLDGLENGTH));
            double gable = 0.5 * W * (W / 2 * Math.Tan(slope * Math.PI / 180));
            Assert.AreEqual(W * H + gable, Bldg.Zones(c.WallAreaCalculator_BldgLength, "ZoneWW").Single().Area, 1e-9);
            Assert.AreEqual(L * H, Bldg.Zones(c.WallAreaCalculator_BldgLength, "ZoneSW").Single().Area, 1e-9);
        }

        [TestMethod]
        public void WallZones_GableEnd_RidgePerpToLength_GableOnSideWall()
        {
            double L = 60, W = 40, H = 15, slope = 15;
            var c = Mwfrs(ASCE7_Versions.ASCE_VER_7_16, Bldg.Make(L, W, H, RoofTypes.ROOF_TYPE_GABLE, slope, RidgeDirections.RIDGE_DIR_PERP_TO_BLDGLENGTH));
            double gable = 0.5 * L * (L / 2 * Math.Tan(slope * Math.PI / 180));
            Assert.AreEqual(W * H, Bldg.Zones(c.WallAreaCalculator_BldgLength, "ZoneWW").Single().Area, 1e-9);
            Assert.AreEqual(L * H + gable, Bldg.Zones(c.WallAreaCalculator_BldgLength, "ZoneSW").Single().Area, 1e-9);
        }

        // ---- MWFRS roof zones -------------------------------------------------------------------------

        [DataTestMethod]
        [DataRow(RoofTypes.ROOF_TYPE_FLAT, 0.0, RidgeDirections.RIDGE_DIR_NONE)]
        [DataRow(RoofTypes.ROOF_TYPE_GABLE, 15.0, RidgeDirections.RIDGE_DIR_PARALLEL_TO_BLDGLENGTH)]
        [DataRow(RoofTypes.ROOF_TYPE_GABLE, 15.0, RidgeDirections.RIDGE_DIR_PERP_TO_BLDGLENGTH)]
        [DataRow(RoofTypes.ROOF_TYPE_HIP, 15.0, RidgeDirections.RIDGE_DIR_PARALLEL_TO_BLDGLENGTH)]
        public void RoofZones_CoverPlanArea_AndAllHavePressures(RoofTypes roof, double slope, RidgeDirections ridge)
        {
            foreach (ASCE7_Versions v in Enum.GetValues(typeof(ASCE7_Versions)))
            {
                var c = Mwfrs(v, Bldg.Make(60, 40, 15, roof, slope, ridge));
                Assert.AreEqual(60 * 40, Bldg.SumArea(c.RoofAreaCalculator), 1e-6, $"{v} roof zones must tile the plan");
                foreach (var id in c.RoofAreaCalculator.effWindAreas.Keys)
                {
                    Assert.IsTrue(c.windPressureRoof_Neg_External.ContainsKey(id), $"{v} roof zone {id} has no negative pressure");
                    Assert.IsTrue(c.windPressureRoof_Pos_External.ContainsKey(id), $"{v} roof zone {id} has no positive pressure");
                }
            }
        }
    }
}
