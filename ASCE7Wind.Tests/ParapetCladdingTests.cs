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
            // Wall Fig. 30.3-1: Zones 4 and 5 +1.0; Zone 4 -1.1, Zone 5 -1.4. Roof Fig. 30.3-2A: Zone 2 -2.3, Zone 3 -3.2.
            var c = Cc(ASCE7_Versions.ASCE_VER_7_22, 3);
            double qk = 29.91 * 0.85;

            var aCorner = Get(c, "A", "corner");
            Assert.AreEqual(29.91, aCorner.Qp, 0.01);
            Assert.AreEqual(1.0, aCorner.GCpFront, 1e-9);
            Assert.AreEqual(-3.2, aCorner.GCpBack, 1e-9);
            Assert.AreEqual(qk * 1.18, aCorner.FrontPressure, 0.05);    // 30.0 psf
            Assert.AreEqual(-qk * 3.38, aCorner.BackPressure, 0.05);    // -85.9 psf
            Assert.AreEqual(qk * 4.2, aCorner.NetPressure, 0.05);       // 106.8 psf: internal pressure cancels

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
    }
}
