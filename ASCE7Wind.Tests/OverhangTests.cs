using System;
using System.Linq;
using ASCE7WindLoadCalculator;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ASCE7Wind.Tests
{
    /// <summary>
    /// Roof overhangs. MWFRS: 27.3.3 (ASCE 7-16 and 7-22), "the positive external pressures on the bottom surface of
    /// windward roof overhangs shall be determined using Cp = 0.8 and combined with the top surface pressures determined
    /// using Figure 27.3-1." C&amp;C: the Fig. 30.3-2 overhang curves (both surfaces), or in ASCE 7-22, where the figure has
    /// none, roof GCp + wall GCp. No internal pressure on overhangs.
    /// </summary>
    [TestClass]
    public class OverhangTests
    {
        private static WindLoadCalculator_MWFRS Mwfrs(ASCE7_Versions v, BuildingData b) =>
            (WindLoadCalculator_MWFRS)Bldg.Run(v, WindLoadCalculationTypes.MWFRS, b);

        private static WindLoadCalculator_CC Cc(ASCE7_Versions v, BuildingData b) =>
            (WindLoadCalculator_CC)Bldg.Run(v, WindLoadCalculationTypes.COMPONENT_AND_CLADDING, b);

        [TestMethod]
        public void Mwfrs_FlatRoof_HandCalculation()
        {
            // ASCE 7-16, V = 115 mph, Exp C, h = 15 ft: Kz = 0.85, Kzt = Kd(0.85 in qh) = Ke = 1 ->
            // qh = 0.00256 x 0.85 x 1 x 0.85 x 115^2 = 24.46 psf.
            // Soffit: qh G Cp = 24.46 x 0.85 x 0.8 = 16.63 psf up.
            // Top, windward distance zone 0 to h/2 (h/L <= 0.5): Cp = -0.9 -> 24.46 x 0.85 x -0.9 = -18.71 psf.
            // Net = -18.71 - 16.63 = -35.35 psf (up); a 2 ft overhang: 70.7 lb per ft of eave.
            var b = Bldg.Make(60, 40, 15);
            b.OverhangWidth = 2;
            var c = Mwfrs(ASCE7_Versions.ASCE_VER_7_16, b);
            Assert.AreEqual(16.63, c.OverhangBottomPressure, 0.05);
            Assert.AreEqual(-0.9, c.WindwardOverhangTop.GCp, 1e-9);
            Assert.AreEqual(-18.71, c.WindwardOverhangTop.ExternalPressure, 0.05);
            Assert.AreEqual(-35.35, c.WindwardOverhangNetPressure, 0.1);
            Assert.AreEqual(70.7, c.WindwardOverhangUpliftPerFoot, 0.2);
        }

        [DataTestMethod]
        [DataRow(ASCE7_Versions.ASCE_VER_7_16)]
        [DataRow(ASCE7_Versions.ASCE_VER_7_22)]
        public void Mwfrs_SoffitIsWindwardWallCpAtRoofHeight(ASCE7_Versions v)
        {
            // Cp = 0.8 on the soffit is the windward-wall coefficient read at qh: the same as the windward wall at z = h.
            var b = Bldg.Make(60, 40, 20, RoofTypes.ROOF_TYPE_GABLE, 25, RidgeDirections.RIDGE_DIR_PARALLEL_TO_BLDGLENGTH);
            var c = Mwfrs(v, b);
            Assert.AreEqual(c.WindwardWallExternalPressureAt(b.MeanRoofHeight), c.OverhangBottomPressure, 1e-9);
            Assert.AreEqual(c.WindwardOverhangTop.ExternalPressure - c.OverhangBottomPressure, c.WindwardOverhangNetPressure, 1e-9);
            Assert.AreEqual(0, c.WindwardOverhangUpliftPerFoot, "no overhang drawn: no force");
        }

        [TestMethod]
        public void CC_7_16_ReadsTheFigureOverhangCurves_NoInternalPressure()
        {
            var b = Bldg.Make(60, 40, 15, RoofTypes.ROOF_TYPE_GABLE, 20, RidgeDirections.RIDGE_DIR_PARALLEL_TO_BLDGLENGTH);
            var c = Cc(ASCE7_Versions.ASCE_VER_7_16, b);
            Assert.IsTrue(c.windPressureOverhang_External.Count > 0);
            var fig = ASCE7WindLoadCalculator.Asce7Edition.ASCE7_16.CreateCCRoofFigure(b);
            foreach (var kv in c.windPressureOverhang_External)
            {
                string label = c.RoofAreaCalculator.effWindAreas[kv.Key].Label_Full;
                Assert.AreEqual(fig.OverhangCurves[label].Evaluate(c.RoofComponentArea), kv.Value.GCp, 1e-9, label);
                Assert.AreEqual(0, kv.Value.GCpi);
                Assert.AreEqual(0, kv.Value.InternalPressure);
                Assert.AreEqual(kv.Value.ExternalPressure, kv.Value.CalculatedNetPressure, 1e-9);
                Assert.IsFalse(c.OverhangFromRoofPlusWall[kv.Key]);
            }
        }

        [TestMethod]
        public void CC_7_22_SteepGable_RoofPlusWall()
        {
            // 7-22 has no overhang curves for gables over 7 deg: GCp = roof GCp + wall GCp. At A = 10 sq ft the positive
            // wall GCp is +1.0 for Zones 4 and 5, so the overhang GCp is the roof GCp - 1.0 (e.g. Zone 2: -2.0 -> -3.0).
            var b = Bldg.Make(60, 40, 15, RoofTypes.ROOF_TYPE_GABLE, 20, RidgeDirections.RIDGE_DIR_PARALLEL_TO_BLDGLENGTH);
            var c = Cc(ASCE7_Versions.ASCE_VER_7_22, b);
            Assert.AreEqual(10.0, c.RoofComponentArea);
            Assert.IsTrue(c.windPressureOverhang_External.Count > 0);
            foreach (var kv in c.windPressureOverhang_External)
            {
                Assert.IsTrue(c.OverhangFromRoofPlusWall[kv.Key]);
                Assert.AreEqual(c.windPressureRoof_Neg_External[kv.Key].GCp - 1.0, kv.Value.GCp, 1e-9);
                Assert.AreEqual(0, kv.Value.InternalPressure);
            }
        }

        [TestMethod]
        public void CC_7_22_LowSlope_StillReadsFigure2A()
        {
            var b = Bldg.Make(60, 40, 15);
            var c = Cc(ASCE7_Versions.ASCE_VER_7_22, b);
            Assert.IsTrue(c.windPressureOverhang_External.Count > 0);
            Assert.IsTrue(c.OverhangFromRoofPlusWall.Values.All(s => !s));
        }

        [TestMethod]
        public void CC_Overhang_IsMoreSevereThanTheRoofZoneBelowIt()
        {
            foreach (var v in new[] { ASCE7_Versions.ASCE_VER_7_16, ASCE7_Versions.ASCE_VER_7_22 })
                foreach (var roof in new[] { (RoofTypes.ROOF_TYPE_FLAT, 0.0), (RoofTypes.ROOF_TYPE_GABLE, 20.0), (RoofTypes.ROOF_TYPE_HIP, 20.0) })
                {
                    var b = Bldg.Make(60, 40, 15, roof.Item1, roof.Item2,
                        roof.Item1 == RoofTypes.ROOF_TYPE_FLAT ? RidgeDirections.RIDGE_DIR_NONE : RidgeDirections.RIDGE_DIR_PARALLEL_TO_BLDGLENGTH);
                    var c = Cc(v, b);
                    Assert.IsTrue(c.windPressureOverhang_External.Count > 0, $"{v} {roof}");
                    foreach (var kv in c.windPressureOverhang_External)
                        if (c.windPressureRoof_Neg_External.TryGetValue(kv.Key, out var top))
                            Assert.IsTrue(kv.Value.GCp <= top.GCp + 1e-9, $"{v} {roof} {c.RoofAreaCalculator.effWindAreas[kv.Key].Label_Full}: {kv.Value.GCp} vs roof {top.GCp}");
                }
        }
    }
}
