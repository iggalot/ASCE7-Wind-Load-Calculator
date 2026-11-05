using System;
using System.Linq;
using ASCE7WindLoadCalculator;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ASCE7Wind.Tests
{
    /// <summary>
    /// The Chapter 30 corrections from the 7-16 / 7-22 audit, each against the printed figure: between printed breakpoints
    /// a curve is a straight line on log(area).
    /// </summary>
    [TestClass]
    public class Chapter30_FigureAuditTests
    {
        private static double Line(double a0, double y0, double a1, double y1, double a) =>
            y0 + (y1 - y0) * Math.Log10(a / a0) / Math.Log10(a1 / a0);

        /// <summary>Fig. 30.3-2A (7-16 p. 336, 7-22 p. 319): Zone 1' is -0.9 to 100 sq ft and -0.4 at 1000; the overhang
        /// Zone 2 is -2.3 to 10 sq ft, -1.6 at 100 (where it meets Zone 1) and -1.0 from 500.</summary>
        [DataTestMethod]
        [DataRow(ASCE7_Versions.ASCE_VER_7_16)]
        [DataRow(ASCE7_Versions.ASCE_VER_7_22)]
        public void Fig2A_Zone1Prime_AndOverhangZone2(ASCE7_Versions v)
        {
            var f = Asce7Edition.For(v).CreateCCRoofFigure(Bldg.Make(60, 40, 15, RoofTypes.ROOF_TYPE_FLAT, 0));
            Assert.AreEqual(-0.9, f.RoofCurves_Neg["Zone1'"].Evaluate(100), 1e-9);
            Assert.AreEqual(Line(100, -0.9, 1000, -0.4, 500), f.RoofCurves_Neg["Zone1'"].Evaluate(500), 1e-9, "-0.55 at 500, not -0.4");
            Assert.AreEqual(-0.4, f.RoofCurves_Neg["Zone1'"].Evaluate(1000), 1e-9);
            Assert.AreEqual(-2.3, f.OverhangCurves["Zone2"].Evaluate(10), 1e-9);
            Assert.AreEqual(-1.6, f.OverhangCurves["Zone2"].Evaluate(100), 1e-9);
            Assert.AreEqual(-1.0, f.OverhangCurves["Zone2"].Evaluate(500), 1e-9);
            Assert.AreEqual(-1.1, f.OverhangCurves["Zone3"].Evaluate(500), 1e-9, "only Zone 3 ends at -1.1");
        }

        /// <summary>ASCE 7-16 Fig. 30.3-2C (p. 338): Zone 3r bends at the printed 4 sq ft (roof -3.6 to -1.8 at 50, overhang
        /// -4.7 to -2.4 at 50).</summary>
        [TestMethod]
        public void Fig2C_7_16_Zone3r_FromThePrinted4SqFt()
        {
            var f = Asce7Edition.ASCE7_16.CreateCCRoofFigure(Bldg.Make(60, 40, 15, RoofTypes.ROOF_TYPE_GABLE, 24));
            Assert.AreEqual(Line(4, -3.6, 50, -1.8, 10), f.RoofCurves_Neg["Zone3r"].Evaluate(10), 1e-9);
            Assert.AreEqual(Line(4, -4.7, 50, -2.4, 10), f.OverhangCurves["Zone3r"].Evaluate(10), 1e-9);
            Assert.AreEqual(Line(10, -2.5, 150, -1.2, 50), f.RoofCurves_Neg["Zone2n"].Evaluate(50), 1e-9);
        }

        /// <summary>ASCE 7-16 Fig. 30.3-2G (p. 342): overhang Zone 3 from -3.1 at 7 sq ft to -1.5 at 200.</summary>
        [TestMethod]
        public void Fig2G_7_16_OverhangZone3()
        {
            var f = Asce7Edition.ASCE7_16.CreateCCRoofFigure(Bldg.Make(60, 40, 15, RoofTypes.ROOF_TYPE_HIP, 24));
            Assert.AreEqual(Line(7, -3.1, 200, -1.5, 10), f.OverhangCurves["Zone3"].Evaluate(10), 1e-9);
            Assert.AreEqual(Line(10, 0.7, 100, 0.3, 50), f.RoofCurves_Pos["Zone1"].Evaluate(50), 1e-9);
        }

        /// <summary>ASCE 7-16 Figs. 30.3-2H/2I (pp. 343-344), hip roofs 27° to 45°: the printed equations, Amin = 9 - 0.135θ
        /// (Zone 3) and Amax = 280 - 5θ (Zone 2e), straight lines on log(area) between.</summary>
        [DataTestMethod]
        [DataRow(30.0)]
        [DataRow(45.0)]
        public void Fig2H2I_7_16_EquationsAndLimits(double t)
        {
            var f = Asce7Edition.ASCE7_16.CreateCCRoofFigure(Bldg.Make(60, 40, 15, RoofTypes.ROOF_TYPE_HIP, t));
            double amin = 9 - 0.135 * t, amax = 280 - 5 * t;
            double r3 = 1.25 - 0.108 * t, o3 = 0.45 - 0.108 * t, r2e = 0.2 - 0.067 * t;
            Assert.AreEqual(r3, f.RoofCurves_Neg["Zone3"].Evaluate(1), 1e-9);
            Assert.AreEqual(r3, f.RoofCurves_Neg["Zone3"].Evaluate(amin), 1e-9);
            Assert.AreEqual(Line(amin, r3, 50, -1.0, 10), f.RoofCurves_Neg["Zone3"].Evaluate(10), 1e-9);
            Assert.AreEqual(Line(amin, o3, 50, -1.8, 20), f.OverhangCurves["Zone3"].Evaluate(20), 1e-9);
            Assert.AreEqual(Line(3, r2e, amax, -0.8, 20), f.RoofCurves_Neg["Zone2e"].Evaluate(20), 1e-9);
            Assert.AreEqual(-0.8, f.RoofCurves_Neg["Zone2e"].Evaluate(amax), 1e-9);
            Assert.AreEqual(-0.895 - 0.0135 * t, f.OverhangCurves["Zone1"].Evaluate(500), 1e-9, "Zone 1 overhang, large areas");
            Assert.AreEqual(Line(3, 0.9, 100, 0.3, 10), f.RoofCurves_Pos["Zone1"].Evaluate(10), 1e-9);
        }

        /// <summary>ASCE 7-22 Fig. 30.3-2G note 6 (p. 325): hip roofs 27° &lt; θ &lt; 45° interpolate between 2F and 2G.</summary>
        [TestMethod]
        public void Hip_7_22_Between27And45_InterpolatesFigures2Fand2G()
        {
            var f = Asce7Edition.ASCE7_22.CreateCCRoofFigure(Bldg.Make(60, 40, 15, RoofTypes.ROOF_TYPE_HIP, 36));
            var lo = new Figure30_3_2F_ASCE7_22();
            var hi = new Figure30_3_2G_ASCE7_22();
            foreach (var zone in lo.RoofCurves_Neg.Keys)
                foreach (var A in new[] { 1.0, 10, 35, 100, 400, 1000 })
                {
                    double expected = lo.RoofCurves_Neg[zone].Evaluate(A) + 0.5 * (hi.RoofCurves_Neg[zone].Evaluate(A) - lo.RoofCurves_Neg[zone].Evaluate(A));
                    Assert.AreEqual(expected, f.RoofCurves_Neg[zone].Evaluate(A), 1e-9, $"{zone} at {A}");
                }
            Assert.IsInstanceOfType(Asce7Edition.ASCE7_22.CreateCCRoofFigure(Bldg.Make(60, 40, 15, RoofTypes.ROOF_TYPE_HIP, 45)), typeof(Figure30_3_2G_ASCE7_22));
        }

        /// <summary>ASCE 7-22 Fig. 30.3-2D (p. 322), gable roofs 27° to 45°: Zone 3 an a × a square at each eave corner, Zone 2
        /// an a-wide strip along each rake between them, Zone 1 the rest; no ridge strip.</summary>
        [DataTestMethod]
        [DataRow(RidgeDirections.RIDGE_DIR_PARALLEL_TO_BLDGLENGTH)]
        [DataRow(RidgeDirections.RIDGE_DIR_PERP_TO_BLDGLENGTH)]
        public void Gable_7_22_Over27_Fig2DLayout(RidgeDirections ridge)
        {
            double L = 60, B = 40;
            var c = Bldg.Run(ASCE7_Versions.ASCE_VER_7_22, WindLoadCalculationTypes.COMPONENT_AND_CLADDING,
                             Bldg.Make(L, B, 15, RoofTypes.ROOF_TYPE_GABLE, 35, ridge));
            var zones = (AreaCalculator_CC_Base)c.RoofAreaCalculator;
            Assert.IsInstanceOfType(zones, typeof(GableRoof2DAreaCalculator_CC_ASCE7_22));
            double a = zones.CritDim_a;
            double Sum(string z) => zones.effWindAreas.Values.Where(q => q.Label_Full == z).Sum(q => q.Area);
            bool alongX = ridge == RidgeDirections.RIDGE_DIR_PARALLEL_TO_BLDGLENGTH;
            double rake = alongX ? B : L;   // length of each rake edge
            Assert.AreEqual(4 * a * a, Sum("Zone3"), 1e-6);
            Assert.AreEqual(2 * a * (rake - 2 * a), Sum("Zone2"), 1e-6);
            Assert.AreEqual(L * B - 4 * a * a - 2 * a * (rake - 2 * a), Sum("Zone1"), 1e-6);
            // every Zone 3 piece touches an eave and a rake (a corner)
            foreach (var z in zones.effWindAreas.Values.Where(q => q.Label_Full == "Zone3"))
                Assert.IsTrue(z.OuterBoundary.Any(p => (p.X == 0 || p.X == L) && (p.Y == 0 || p.Y == B)), "Zone 3 at a corner");
        }
    }
}
