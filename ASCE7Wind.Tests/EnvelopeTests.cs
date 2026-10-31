using System;
using System.Linq;
using ASCE7WindLoadCalculator;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ASCE7Wind.Tests
{
    /// <summary>
    /// Chapter 28 Part 1 envelope procedure (Fig. 28.3-1), horizontal loads for the shear walls, against hand calculations.
    /// p = qh Kd (GCpf); internal pressure cancels. a = min(0.1 least, 0.4h) &gt;= max(0.04 least, 3 ft); end zones 2a.
    /// </summary>
    [TestClass]
    public class EnvelopeTests
    {
        private static WindAnalysisResult Analysis(BuildingData b, ASCE7_Versions v = ASCE7_Versions.ASCE_VER_7_22) =>
            WindAnalysis.Run(v, new WindParameters_Base { WindSpeed = 115, ExposureCategory = WindExposureCategories.WIND_EXP_CAT_C }, b);

        private static double Tan(double d) => Math.Tan(d * Math.PI / 180);

        [TestMethod]
        public void CaseA_Coefficients_TableRowsAndInterpolation()
        {
            CollectionAssert.AreEqual(new[] { 0.40, -0.69, -0.37, -0.29, 0.61, -1.07, -0.53, -0.43 }, EnvelopeLateralLoads.CaseACoefficients(0));
            CollectionAssert.AreEqual(new[] { 0.53, -0.69, -0.48, -0.43, 0.80, -1.07, -0.69, -0.64 }, EnvelopeLateralLoads.CaseACoefficients(20));
            CollectionAssert.AreEqual(new[] { 0.56, 0.21, -0.43, -0.37, 0.69, 0.27, -0.53, -0.48 }, EnvelopeLateralLoads.CaseACoefficients(37));
            var mid = EnvelopeLateralLoads.CaseACoefficients(12.5);  // halfway between 5 and 20 deg
            double[] expected = { 0.465, -0.69, -0.425, -0.36, 0.705, -1.07, -0.61, -0.535 };
            for (int i = 0; i < 8; i++) Assert.AreEqual(expected[i], mid[i], 1e-9);
        }

        [TestMethod]
        public void ZoneWidthA_Note9()
        {
            Assert.AreEqual(4.0, EnvelopeLateralLoads.ZoneWidthA(Bldg.Make(60, 40, 15)), 1e-9);    // 0.1 x 40 < 0.4 x 15
            Assert.AreEqual(3.0, EnvelopeLateralLoads.ZoneWidthA(Bldg.Make(25, 20, 10)), 1e-9);    // 2.0 raised to 3 ft
            Assert.AreEqual(4.8, EnvelopeLateralLoads.ZoneWidthA(Bldg.Make(100, 80, 12)), 1e-9);   // 0.4h governs
        }

        [TestMethod]
        public void FlatRoof_HandCalculation()
        {
            // 60 x 40 x 15 ft flat, wind along the length: windward wall 40 ft wide, a = 4, end zone 8 ft.
            // Walls: q [ (0.40 + 0.29) 15 x 32 + (0.61 + 0.43) 15 x 8 ] = q (331.2 + 124.8) = 456 q.
            var r = WindLateralLoads.Compute(Analysis(Bldg.Make(60, 40, 15)), false, LateralProcedure.Envelope);
            Assert.AreEqual(LateralProcedure.Envelope, r.Procedure);
            var e = r.EnvelopeAlongLength;
            double q = e.Qh * e.Kd;
            Assert.AreEqual(0.85, e.Kd, 1e-9);
            Assert.AreEqual("A", e.LoadCase);
            Assert.AreEqual(456 * q / 1000, e.WallsKips, 0.01);
            Assert.AreEqual(0, e.RoofKips, 1e-9);
            Assert.AreEqual(e.WallsKips, e.BasicKips, 1e-9);

            // torsion: the extra end-zone pressure (1.04 - 0.69) x 15 x 8 = 42 q acts 16 ft from the center
            Assert.AreEqual(42 * 16 * q / 1000, e.BasicTorsion, 0.01);

            // torsional case: the half beyond the center at 25%:
            // near half 124.8 + 0.69 x 15 x 12 = 249.0 q; far half 0.25 x 0.69 x 15 x 20 = 51.75 q
            Assert.AreEqual(300.75 * q / 1000, e.TorsionalKips, 0.01);
            Assert.AreEqual((124.8 * 16 + 124.2 * 6 - 51.75 * 10) * q / 1000, e.TorsionalTorsion, 0.01);

            // 28.3.4 minimum: 16 psf on 40 x 15 ft
            Assert.AreEqual(9.6, e.MinimumKips, 1e-6);

            // zone table sums to the wall force
            Assert.AreEqual(e.WallsKips, e.Zones.Sum(z => z.Kips), 1e-6);
            Assert.AreEqual(8 * 15, e.Zones.Single(z => z.Zone == "1E").ProjectedArea, 1e-6);
        }

        [TestMethod]
        public void Gable30_Transverse_RoofIncluded_Longitudinal_CaseB()
        {
            // 60 x 40 ft, 10 ft eave, 30 deg gable, ridge along the 60 ft length. a = 4 ft (0.1 x 40 < 0.4 x 15.8).
            var b = Bldg.Make(60, 40, 10, RoofTypes.ROOF_TYPE_GABLE, 30, RidgeDirections.RIDGE_DIR_PARALLEL_TO_BLDGLENGTH);
            var r = WindLateralLoads.Compute(Analysis(b), false, LateralProcedure.Envelope);

            // wind along the width: normal to the ridge, Case A at 30 deg; windward wall 60 ft, end zone 8 ft
            var t = r.EnvelopeAlongWidth;
            double q = t.Qh * t.Kd;
            Assert.IsTrue(t.Transverse);
            Assert.AreEqual("A", t.LoadCase);
            Assert.AreEqual(10 * ((0.56 + 0.37) * 52 + (0.69 + 0.48) * 8) * q / 1000, t.WallsKips, 0.01);   // 577.2 q
            // roof: Zone 2 positive (0.21) so it covers the whole windward slope; rise 20 tan 30
            Assert.IsFalse(t.RoofNeglected);
            Assert.AreEqual(20 * Tan(30) * ((0.21 + 0.43) * 52 + (0.27 + 0.53) * 8) * q / 1000, t.RoofKips, 0.01); // 458.2 q

            // wind along the length: parallel to the ridge, Case B (zones 5, 6), gable-end wall with the triangle
            var l = r.EnvelopeAlongLength;
            Assert.IsFalse(l.Transverse);
            Assert.AreEqual("B", l.LoadCase);
            double endArea = 4 * (10 + 0.5 * 4 * Tan(30));                       // strip 0-4 ft under the rake: 5E/6E are a wide
            double wallArea = 40 * 10 + 0.5 * 40 * 20 * Tan(30);
            double ql = l.Qh * l.Kd;
            Assert.AreEqual(((0.61 + 0.43) * endArea + (0.40 + 0.29) * (wallArea - endArea)) * ql / 1000, l.WallsKips, 0.01);
            Assert.AreEqual(0, l.RoofKips, 1e-9);
        }

        [TestMethod]
        public void Gable20_RoofReducesShear_Neglected()
        {
            // 20 deg: Zone 2 -0.69, Zone 3 -0.48: the roof pushes upwind, so it is neglected (shear not less than without the roof).
            var b = Bldg.Make(60, 40, 10, RoofTypes.ROOF_TYPE_GABLE, 20, RidgeDirections.RIDGE_DIR_PARALLEL_TO_BLDGLENGTH);
            var t = WindLateralLoads.Compute(Analysis(b), false, LateralProcedure.Envelope).EnvelopeAlongWidth;
            Assert.IsTrue(t.RoofNeglected);
            Assert.AreEqual(0, t.RoofKips, 1e-9);
            Assert.AreEqual(t.WallsKips + t.ParapetKips, t.BasicKips, 1e-9);
        }

        [TestMethod]
        public void Case4_AlongRidge_EndZoneA_BothEditions()
        {
            // ASCE 7-22 Fig. 28.3-2 Case 4 (torsional, wind along the ridge): 5E/6E are a wide, 5/6 run 0.5B - a, beyond the
            // center 5T/6T at 25%. 60 x 40 ft, 10 ft eave, 30 deg gable, ridge along the length; a = 4 ft. Gable-end wall
            // height 10 + tan30 x min(y, 40 - y): areas 0-4 ft 44.62, 4-20 ft 270.85, 20-40 ft 315.47 sq ft.
            var b = Bldg.Make(60, 40, 10, RoofTypes.ROOF_TYPE_GABLE, 30, RidgeDirections.RIDGE_DIR_PARALLEL_TO_BLDGLENGTH);
            var l = WindLateralLoads.Compute(Analysis(b), false, LateralProcedure.Envelope).EnvelopeAlongLength;
            double q = l.Qh * l.Kd, t = Tan(30);
            double a04 = 40 + t * 8, a420 = 160 + t * (200 - 8), a2040 = 200 + t * 200;
            Assert.AreEqual("B", l.LoadCase);
            Assert.AreEqual(4.0, l.EndZoneWidth, 1e-9);
            Assert.AreEqual(q * (1.04 * a04 + 0.69 * a420 + 0.25 * 0.69 * a2040) / 1000, l.TorsionalKips, 0.01);

            // 7-16: the same 5/5E/5T and 6/6E/6T layout (user)
            var l16 = WindLateralLoads.Compute(Analysis(b, ASCE7_Versions.ASCE_VER_7_16), false, LateralProcedure.Envelope).EnvelopeAlongLength;
            Assert.AreEqual(4.0, l16.EndZoneWidth, 1e-9);
        }

        [TestMethod]
        public void Zone2_EaveToRidge_BothEditions()
        {
            // Zone 2 (windward slope) and Zone 3 (leeward slope) run from the eave to the ridge in 7-16 and 7-22 (user).
            // 100 ft deep across the ridge, 8 ft eave, 10 deg: rise 50 tan 10 over the 60 ft wall less the 2a end zone.
            var b = Bldg.Make(100, 60, 8, RoofTypes.ROOF_TYPE_GABLE, 10, RidgeDirections.RIDGE_DIR_PERP_TO_BLDGLENGTH);
            foreach (var v in new[] { ASCE7_Versions.ASCE_VER_7_16, ASCE7_Versions.ASCE_VER_7_22 })
            {
                var e = WindLateralLoads.Compute(Analysis(b, v), false, LateralProcedure.Envelope).EnvelopeAlongLength;
                Assert.IsTrue(e.Transverse);
                Assert.AreEqual(50 * Tan(10) * (60 - 2 * e.A), e.Zones.Single(z => z.Zone == "2").ProjectedArea, 0.5);   // strip rounding at the end-zone edge
                Assert.AreEqual(50 * Tan(10) * (60 - 2 * e.A), e.Zones.Single(z => z.Zone == "3").ProjectedArea, 0.5);
            }
        }

        [TestMethod]
        public void LoadCases_BasicTorsionalMinimum_AndExemption()
        {
            var a = Analysis(Bldg.Make(60, 40, 15));
            var full = WindLateralLoads.Compute(a, false, LateralProcedure.Envelope);
            Assert.AreEqual(10, full.LoadCases.Count);           // per direction: 2 basic, 2 torsional, 1 minimum
            var basic = full.LoadCases.Where(c => c.CaseNumber == 1 && c.Vx > 0).ToList();
            Assert.AreEqual(2, basic.Count);
            Assert.AreEqual(-basic[0].Mt, basic[1].Mt, 1e-9);    // reference corner at either end

            var exempt = WindLateralLoads.Compute(a, true, LateralProcedure.Envelope);
            Assert.AreEqual(6, exempt.LoadCases.Count);
            Assert.IsFalse(exempt.LoadCases.Any(c => c.CaseNumber == 2));
        }

        [TestMethod]
        public void Parapet_AddedPer28_3_2()
        {
            var b = Bldg.Make(60, 40, 15);
            b.ParapetHeight = 3;
            var a = Analysis(b);
            var e = WindLateralLoads.Compute(a, false, LateralProcedure.Envelope).EnvelopeAlongLength;
            Assert.AreEqual(((WindLoadCalculator_MWFRS)a.MwfrsAlongLength).ParapetKips, e.ParapetKips, 1e-9);
            Assert.AreEqual(16 * 40 * 18 / 1000.0, e.MinimumKips, 1e-6);   // parapet counts as wall area
        }

        [TestMethod]
        public void NotApplicable_FallsBackToChapter27()
        {
            var hip = Bldg.Make(60, 40, 15, RoofTypes.ROOF_TYPE_HIP, 20);
            var r = WindLateralLoads.Compute(Analysis(hip), false, LateralProcedure.Envelope);
            Assert.AreEqual(LateralProcedure.Directional, r.Procedure);
            StringAssert.Contains(r.ProcedureNote, "hip");

            var tall = Bldg.Make(40, 30, 35);       // h 35 ft > least dimension 30 ft
            Assert.IsNotNull(WindLateralLoads.Compute(Analysis(tall), false, LateralProcedure.Envelope).ProcedureNote);
        }

        [TestMethod]
        public void Edition_7_16_KdInsideQ()
        {
            var e = WindLateralLoads.Compute(Analysis(Bldg.Make(60, 40, 15), ASCE7_Versions.ASCE_VER_7_16), false, LateralProcedure.Envelope).EnvelopeAlongLength;
            Assert.AreEqual(1.0, e.Kd, 1e-9);
            Assert.AreEqual(456 * e.Qh / 1000, e.WallsKips, 0.01);
        }

        [TestMethod]
        public void LowerThanChapter27_ForTypicalLowRise()
        {
            var a = Analysis(Bldg.Make(60, 40, 15));
            var c27 = WindLateralLoads.Compute(a, false, LateralProcedure.Directional);
            var c28 = WindLateralLoads.Compute(a, false, LateralProcedure.Envelope);
            Assert.IsTrue(c28.EnvelopeAlongLength.BasicKips < c27.AlongLength.TotalKips);
        }
    }
}
