using System.Linq;
using ASCE7WindLoadCalculator;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ASCE7Wind.Tests
{
    /// <summary>
    /// C&amp;C parapets, Section 30.6 / Fig. 30.6-1, Eq. 30.6-1 p = qp [(GCp) - (GCpi)] applied to each face separately.
    /// Case A (windward parapet): front = positive wall GCp, back = negative roof edge/corner GCp.
    /// Case B (leeward parapet): back = positive wall GCp, front = negative wall GCp.
    /// </summary>
    [TestClass]
    public class ParapetCladdingTests
    {
        private static WindLoadCalculator_CC Cc(ASCE7_Versions v, double parapet)
        {
            var b = Bldg.Make(60, 40, 15);
            b.ParapetHeight = parapet;
            return (WindLoadCalculator_CC)Bldg.Run(v, WindLoadCalculationTypes.COMPONENT_AND_CLADDING, b);
        }

        private static ParapetCladdingCase Get(WindLoadCalculator_CC c, string kase, string location) =>
            c.ParapetCases.Single(p => p.Case == kase && p.Location == location);

        [TestMethod]
        public void HandCalculation_7_22_Enclosed_10sqft()
        {
            // qp at 18 ft = 29.91 psf (7-22, Kd outside q); qp Kd = 25.42 psf; GCpi = +/-0.18; A = 10 sq ft.
            // Wall Fig. 30.3-1: Zones 4 and 5 +1.0; Zone 4 -1.1, Zone 5 -1.4. Roof Fig. 30.3-2A: Zone 2 -2.3, Zone 3 -3.2 (-2.3 with a parapet of 3 ft or more, note 5).
            var c = Cc(ASCE7_Versions.ASCE_VER_7_22, 3);
            double qk = 29.91 * 0.85;

            var aCorner = Get(c, "A", "corner");
            Assert.AreEqual(29.91, aCorner.Qp, 0.01);
            Assert.AreEqual(1.0, aCorner.GCpFront, 1e-9);
            Assert.AreEqual(-2.3, aCorner.GCpBack, 1e-9);              // Zone 3 = Zone 2 by Fig. 30.3-2A note 5 (parapet >= 3 ft)
            Assert.AreEqual(qk * 1.18, aCorner.FrontPressure, 0.05);    // 30.0 psf
            Assert.AreEqual(-qk * 2.48, aCorner.BackPressure, 0.05);    // -63.0 psf
            Assert.AreEqual(qk * 3.3, aCorner.NetPressure, 0.05);       // 83.9 psf: internal pressure cancels

            var aEdge = Get(c, "A", "edge");
            Assert.AreEqual(-2.3, aEdge.GCpBack, 1e-9);
            Assert.AreEqual(qk * 3.3, aEdge.NetPressure, 0.05);         // 83.9 psf

            var bCorner = Get(c, "B", "corner");
            Assert.AreEqual(-1.4, bCorner.GCpFront, 1e-9);
            Assert.AreEqual(1.0, bCorner.GCpBack, 1e-9);
            Assert.AreEqual(-qk * 1.58, bCorner.FrontPressure, 0.05);   // -40.2 psf
            Assert.AreEqual(qk * 1.18, bCorner.BackPressure, 0.05);
            Assert.AreEqual(qk * 2.4, bCorner.NetPressure, 0.05);       // 61.0 psf

            Assert.AreEqual(qk * 2.1, Get(c, "B", "edge").NetPressure, 0.05); // 53.4 psf
        }

        [TestMethod]
        public void SameVelocityPressureAsTheMwfrsParapet()
        {
            var b = Bldg.Make(60, 40, 15);
            b.ParapetHeight = 4;
            var mw = (WindLoadCalculator_MWFRS)Bldg.Run(ASCE7_Versions.ASCE_VER_7_16, WindLoadCalculationTypes.MWFRS, b);
            var cc = (WindLoadCalculator_CC)Bldg.Run(ASCE7_Versions.ASCE_VER_7_16, WindLoadCalculationTypes.COMPONENT_AND_CLADDING, b);
            Assert.AreEqual(4, cc.ParapetCases.Count);
            foreach (var p in cc.ParapetCases) Assert.AreEqual(mw.ParapetVelocityPressure, p.Qp, 1e-9);
        }

        [TestMethod]
        public void NoParapet_NoCases()
        {
            Assert.AreEqual(0, Cc(ASCE7_Versions.ASCE_VER_7_22, 0).ParapetCases.Count);
        }

        // Fig. 30.3-2A note 5: parapet >= 3 ft (0.9 m) around a roof <= 7 deg -> roof Zone 3 negative = Zone 2,
        // roof Zones 2 and 3 positive = wall Zones 4 and 5.
        [DataTestMethod]
        [DataRow(ASCE7_Versions.ASCE_VER_7_16)]
        [DataRow(ASCE7_Versions.ASCE_VER_7_22)]
        public void Note5_ParapetAtLeast3ft_RoofZonesAdjusted(ASCE7_Versions v)
        {
            var c = Cc(v, 3);
            Assert.IsTrue(c.ParapetRoofZoneNoteApplied);
            var roof = c.extGCpCurve_Roof; var wall = c.extGCpCurve_Wall;
            foreach (double a in new[] { 10.0, 50.0, 200.0 })
            {
                Assert.AreEqual(roof.RoofCurves_Neg["Zone2"].Evaluate(a), roof.RoofCurves_Neg["Zone3"].Evaluate(a), 1e-9);
                Assert.AreEqual(wall.WallCurves_Pos["Zone4"].Evaluate(a), roof.RoofCurves_Pos["Zone2"].Evaluate(a), 1e-9);
                Assert.AreEqual(wall.WallCurves_Pos["Zone5"].Evaluate(a), roof.RoofCurves_Pos["Zone3"].Evaluate(a), 1e-9);
            }
            Assert.AreEqual(-2.3, roof.RoofCurves_Neg["Zone3"].Evaluate(10), 1e-9);
            Assert.AreEqual(1.0, roof.RoofCurves_Pos["Zone2"].Evaluate(10), 1e-9);
            // the roof pressure table follows the adjusted curves
            foreach (var z in c.RoofAreaCalculator.effWindAreas.Where(z => z.Value.Label_Full == "Zone3"))
                Assert.AreEqual(-2.3, c.windPressureRoof_Neg_External[z.Key].GCp, 1e-9);
        }

        [TestMethod]
        public void Note5_ParapetUnder3ft_NotApplied()
        {
            var c = Cc(ASCE7_Versions.ASCE_VER_7_22, 2.5);
            Assert.IsFalse(c.ParapetRoofZoneNoteApplied);
            Assert.AreEqual(-3.2, c.extGCpCurve_Roof.RoofCurves_Neg["Zone3"].Evaluate(10), 1e-9);
            Assert.AreEqual(-3.2, Get(c, "A", "corner").GCpBack, 1e-9);
        }
    }
}
