using System;
using ASCE7WindLoadCalculator;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ASCE7Wind.Tests
{
    /// <summary>
    /// MWFRS parapets, 27.3.4: pp = qp Kd (GCpn), qp at the top of the parapet, GCpn = +1.5 windward, -1.0 leeward.
    /// Both act downwind, so the horizontal force is (1.5 + 1.0) qp Kd x parapet height x width normal to the wind.
    /// </summary>
    [TestClass]
    public class ParapetTests
    {
        private static WindLoadCalculator_MWFRS Mwfrs(ASCE7_Versions v, BuildingData b) =>
            (WindLoadCalculator_MWFRS)Bldg.Run(v, WindLoadCalculationTypes.MWFRS, b);

        private static BuildingData Flat(double parapet)
        {
            var b = Bldg.Make(60, 40, 15);
            b.ParapetHeight = parapet;
            return b;
        }

        [TestMethod]
        public void HandCalculation_7_16_FlatRoof_3ftParapet()
        {
            // Top of parapet z = 18 ft, Exp C: Kz = 2.01 (18/900)^(2/9.5) = 0.882.
            // qp = 0.00256 x 0.882 x 1.0 x 0.85 x 115^2 = 25.39 psf (Kd inside q in 7-16).
            // Windward +1.5 -> 38.08 psf; leeward -1.0 -> -25.39 psf; combined 63.47 psf.
            // Wind along the length: B = 40 ft -> F = 63.47 x 3 x 40 = 7.62 kips.
            var c = Mwfrs(ASCE7_Versions.ASCE_VER_7_16, Flat(3));
            Assert.AreEqual(18.0, c.ParapetTopHeight, 1e-12);
            Assert.AreEqual(25.39, c.ParapetVelocityPressure, 0.02);
            Assert.AreEqual(38.08, c.ParapetPressureWindward, 0.03);
            Assert.AreEqual(-25.39, c.ParapetPressureLeeward, 0.02);
            Assert.AreEqual(7.62, c.ParapetKips, 0.01);
        }

        [DataTestMethod]
        [DataRow(ASCE7_Versions.ASCE_VER_7_16)]
        [DataRow(ASCE7_Versions.ASCE_VER_7_22)]
        public void ParapetAddsToTheShearWallForceAndTheMinimum(ASCE7_Versions v)
        {
            var without = WindLateralLoads.DirectionalForce(Mwfrs(v, Flat(0)), "along length");
            var withP = WindLateralLoads.DirectionalForce(Mwfrs(v, Flat(3)), "along length");
            var c = Mwfrs(v, Flat(3));
            Assert.AreEqual(0, without.ParapetKips);
            Assert.AreEqual(c.ParapetKips, withP.ParapetKips, 1e-9);
            Assert.AreEqual(without.TotalKips + c.ParapetKips, withP.TotalKips, 1e-9, "walls and roof unchanged; the parapet adds");
            Assert.AreEqual(without.MinimumKips + 16.0 * 3 * 40 / 1000.0, withP.MinimumKips, 1e-9, "parapet area counts in the 16 psf minimum");
        }

        [TestMethod]
        public void SevenTwentyTwo_AppliesKdOnTheParapetPressure()
        {
            // In 7-22 qp excludes Kd; Eq. 27.3-3 multiplies by Kd = 0.85, so pp = 1.5 x 0.85 x qp.
            var c = Mwfrs(ASCE7_Versions.ASCE_VER_7_22, Flat(3));
            Assert.AreEqual(1.5 * 0.85 * c.ParapetVelocityPressure, c.ParapetPressureWindward, 1e-9);
        }

        [TestMethod]
        public void SlopedRoof_ParapetIgnoredWithAMessage()
        {
            var b = Bldg.Make(60, 40, 15, RoofTypes.ROOF_TYPE_GABLE, 20, RidgeDirections.RIDGE_DIR_PARALLEL_TO_BLDGLENGTH);
            b.ParapetHeight = 3;
            var c = Mwfrs(ASCE7_Versions.ASCE_VER_7_22, b);
            Assert.AreEqual(0, c.ParapetKips);
            CollectionAssert.Contains(new System.Collections.Generic.List<string>(c.ValidationMessages),
                "Parapets are counted for flat roofs only; the parapet height was ignored for this sloped roof.");
        }
    }
}
