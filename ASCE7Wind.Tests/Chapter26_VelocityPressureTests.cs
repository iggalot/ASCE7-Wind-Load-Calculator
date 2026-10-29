using System;
using ASCE7WindLoadCalculator;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ASCE7Wind.Tests
{
    /// <summary>
    /// ASCE 7 Chapter 26: directionality factor Kd (26.6), velocity pressure exposure coefficient Kz
    /// and velocity pressure qz (26.10), terrain constants (Table 26.11-1), internal pressure GCpi (26.13).
    /// </summary>
    [TestClass]
    public class Chapter26_VelocityPressureTests
    {
        private static WindLoadCalculator_Base Calc(ASCE7_Versions v, WindLoadCalculationTypes t = WindLoadCalculationTypes.MWFRS,
            BuildingEnclosures enc = BuildingEnclosures.BLDG_ENCLOSED, double V = 115, double kzt = 1.0,
            WindExposureCategories exp = WindExposureCategories.WIND_EXP_CAT_C)
        {
            var b = Bldg.Make(60, 40, 15, enclosure: enc);
            return WindLoadCalculatorFactory.Create(v, t, Bldg.Params(t, exp, V, kzt), b);
        }

        // ---- Table 26.10-1 (ASCE 7-16), tabulated Kz -------------------------------------------

        [DataTestMethod]
        [DataRow(WindExposureCategories.WIND_EXP_CAT_B, 15.0, 0.57)]
        [DataRow(WindExposureCategories.WIND_EXP_CAT_B, 20.0, 0.62)]
        [DataRow(WindExposureCategories.WIND_EXP_CAT_B, 25.0, 0.66)]
        [DataRow(WindExposureCategories.WIND_EXP_CAT_B, 30.0, 0.70)]
        [DataRow(WindExposureCategories.WIND_EXP_CAT_B, 40.0, 0.76)]
        [DataRow(WindExposureCategories.WIND_EXP_CAT_B, 50.0, 0.81)]
        [DataRow(WindExposureCategories.WIND_EXP_CAT_B, 60.0, 0.85)]
        [DataRow(WindExposureCategories.WIND_EXP_CAT_C, 15.0, 0.85)]
        [DataRow(WindExposureCategories.WIND_EXP_CAT_C, 20.0, 0.90)]
        [DataRow(WindExposureCategories.WIND_EXP_CAT_C, 25.0, 0.94)]
        [DataRow(WindExposureCategories.WIND_EXP_CAT_C, 30.0, 0.98)]
        [DataRow(WindExposureCategories.WIND_EXP_CAT_C, 40.0, 1.04)]
        [DataRow(WindExposureCategories.WIND_EXP_CAT_C, 50.0, 1.09)]
        [DataRow(WindExposureCategories.WIND_EXP_CAT_C, 60.0, 1.13)]
        [DataRow(WindExposureCategories.WIND_EXP_CAT_D, 15.0, 1.03)]
        [DataRow(WindExposureCategories.WIND_EXP_CAT_D, 20.0, 1.08)]
        [DataRow(WindExposureCategories.WIND_EXP_CAT_D, 25.0, 1.12)]
        [DataRow(WindExposureCategories.WIND_EXP_CAT_D, 30.0, 1.16)]
        [DataRow(WindExposureCategories.WIND_EXP_CAT_D, 40.0, 1.22)]
        [DataRow(WindExposureCategories.WIND_EXP_CAT_D, 50.0, 1.27)]
        [DataRow(WindExposureCategories.WIND_EXP_CAT_D, 60.0, 1.31)]
        public void Kz_ASCE7_16_MatchesTable26_10_1(WindExposureCategories exp, double z, double expected)
        {
            Assert.AreEqual(expected, Calc(ASCE7_Versions.ASCE_VER_7_16).GetKz(z, exp), Tol.Table);
        }

        [DataTestMethod]
        [DataRow(ASCE7_Versions.ASCE_VER_7_16)]
        [DataRow(ASCE7_Versions.ASCE_VER_7_22)]
        public void Kz_BelowFifteenFeet_UsesFifteenFeet(ASCE7_Versions v)
        {
            var c = Calc(v);
            foreach (WindExposureCategories e in Enum.GetValues(typeof(WindExposureCategories)))
                Assert.AreEqual(c.GetKz(15, e), c.GetKz(4, e), Tol.Tight, e.ToString());
        }

        // ---- ASCE 7-16 Table 26.11-1 terrain constants: Kz = 2.01 (z/zg)^(2/alpha) ------------

        [DataTestMethod]
        [DataRow(WindExposureCategories.WIND_EXP_CAT_B, 7.0, 1200.0)]
        [DataRow(WindExposureCategories.WIND_EXP_CAT_C, 9.5, 900.0)]
        [DataRow(WindExposureCategories.WIND_EXP_CAT_D, 11.5, 700.0)]
        public void Kz_ASCE7_16_PowerLaw(WindExposureCategories exp, double alpha, double zg)
        {
            foreach (double z in new[] { 15.0, 33.0, 60.0, 200.0 })
                Assert.AreEqual(2.01 * Math.Pow(z / zg, 2.0 / alpha), Calc(ASCE7_Versions.ASCE_VER_7_16).GetKz(z, exp), Tol.Tight);
        }

        // ---- ASCE 7-22 Table 26.11-1 terrain constants: Kz = 2.41 (z/zg)^(2/alpha) ------------
        // 7-22 revised alpha and zg (gradient heights) and the leading coefficient.

        [DataTestMethod]
        [DataRow(WindExposureCategories.WIND_EXP_CAT_B, 7.5, 3280.0)]
        [DataRow(WindExposureCategories.WIND_EXP_CAT_C, 9.8, 2460.0)]
        [DataRow(WindExposureCategories.WIND_EXP_CAT_D, 11.5, 1935.0)]
        public void Kz_ASCE7_22_PowerLaw(WindExposureCategories exp, double alpha, double zg)
        {
            foreach (double z in new[] { 15.0, 33.0, 60.0, 200.0 })
                Assert.AreEqual(2.41 * Math.Pow(z / zg, 2.0 / alpha), Calc(ASCE7_Versions.ASCE_VER_7_22).GetKz(z, exp), Tol.Tight);
        }

        /// <summary>Both editions give the same tabulated Kz at the 15 ft floor (0.57 / 0.85 / 1.03).</summary>
        [DataTestMethod]
        [DataRow(WindExposureCategories.WIND_EXP_CAT_B, 0.57)]
        [DataRow(WindExposureCategories.WIND_EXP_CAT_C, 0.85)]
        [DataRow(WindExposureCategories.WIND_EXP_CAT_D, 1.03)]
        public void Kz_ASCE7_22_AtFifteenFeet_MatchesTable(WindExposureCategories exp, double expected)
        {
            Assert.AreEqual(expected, Calc(ASCE7_Versions.ASCE_VER_7_22).GetKz(15, exp), Tol.Table);
        }

        // ---- 26.6 Kd ---------------------------------------------------------------------------

        [DataTestMethod]
        [DataRow(ASCE7_Versions.ASCE_VER_7_16, WindLoadCalculationTypes.MWFRS)]
        [DataRow(ASCE7_Versions.ASCE_VER_7_16, WindLoadCalculationTypes.COMPONENT_AND_CLADDING)]
        [DataRow(ASCE7_Versions.ASCE_VER_7_22, WindLoadCalculationTypes.MWFRS)]
        [DataRow(ASCE7_Versions.ASCE_VER_7_22, WindLoadCalculationTypes.COMPONENT_AND_CLADDING)]
        public void Kd_Buildings_Is085(ASCE7_Versions v, WindLoadCalculationTypes t)
        {
            Assert.AreEqual(0.85, Calc(v, t).GetKd(t), Tol.Tight);
        }

        // ---- 26.10 velocity pressure -------------------------------------------------------------

        /// <summary>ASCE 7-16 Eq. 26.10-1: qz = 0.00256 Kz Kzt Kd Ke V^2 (Ke = 1). V = 115 mph, Exp C, z = 15 ft.</summary>
        [TestMethod]
        public void Qz_ASCE7_16_IncludesKd()
        {
            var c = Calc(ASCE7_Versions.ASCE_VER_7_16);
            double kz = c.GetKz(15, WindExposureCategories.WIND_EXP_CAT_C);
            Assert.AreEqual(0.00256 * kz * 1.0 * 0.85 * 115 * 115, c.CalculateDynamicWindPressure(15), 1e-9);
            Assert.AreEqual(24.43, c.CalculateDynamicWindPressure(15), 0.01);
        }

        /// <summary>ASCE 7-22 Eq. 26.10-1: qz = 0.00256 Kz Kzt Ke V^2 (Kd moves to the pressure equations).</summary>
        [TestMethod]
        public void Qz_ASCE7_22_ExcludesKd()
        {
            var c = Calc(ASCE7_Versions.ASCE_VER_7_22);
            double kz = c.GetKz(15, WindExposureCategories.WIND_EXP_CAT_C);
            Assert.AreEqual(0.00256 * kz * 1.0 * 115 * 115, c.CalculateDynamicWindPressure(15), 1e-9);
            Assert.AreEqual(28.81, c.CalculateDynamicWindPressure(15), 0.01);
        }

        [DataTestMethod]
        [DataRow(ASCE7_Versions.ASCE_VER_7_16)]
        [DataRow(ASCE7_Versions.ASCE_VER_7_22)]
        public void Qz_ScalesWithKztAndVSquared(ASCE7_Versions v)
        {
            double q0 = Calc(v, V: 100, kzt: 1.0).CalculateDynamicWindPressure(20);
            Assert.AreEqual(1.3 * q0, Calc(v, V: 100, kzt: 1.3).CalculateDynamicWindPressure(20), 1e-9);
            Assert.AreEqual(4.0 * q0, Calc(v, V: 200, kzt: 1.0).CalculateDynamicWindPressure(20), 1e-9);
        }

        /// <summary>There is no wind importance factor in ASCE 7-16/7-22 (risk category is in the wind-speed maps).</summary>
        [DataTestMethod]
        [DataRow(ASCE7_Versions.ASCE_VER_7_16)]
        [DataRow(ASCE7_Versions.ASCE_VER_7_22)]
        public void Qz_IgnoresLegacyImportanceFactor(ASCE7_Versions v)
        {
            var c = Calc(v);
            double q = c.CalculateDynamicWindPressure(20);
#pragma warning disable CS0618 // legacy property kept only for file/UI compatibility
            c.Parameters.ImportanceFactor = 1.15;
#pragma warning restore CS0618
            Assert.AreEqual(q, c.CalculateDynamicWindPressure(20), Tol.Tight);
        }

        // ---- Table 26.13-1 GCpi ------------------------------------------------------------------

        [DataTestMethod]
        [DataRow(BuildingEnclosures.BLDG_ENCLOSED, 0.18)]
        [DataRow(BuildingEnclosures.BLDG_PARTIALLY_ENCLOSED, 0.55)]
        [DataRow(BuildingEnclosures.BLDG_PARTIALLY_OPEN, 0.18)]
        [DataRow(BuildingEnclosures.BLDG_OPEN, 0.0)]
        public void GCpi_MatchesTable26_13_1(BuildingEnclosures enc, double expected)
        {
            Assert.AreEqual(expected, Calc(ASCE7_Versions.ASCE_VER_7_16, enc: enc).GetGCpi(), Tol.Tight);
            Assert.AreEqual(expected, Calc(ASCE7_Versions.ASCE_VER_7_22, enc: enc).GetGCpi(), Tol.Tight);
        }
    }
}
