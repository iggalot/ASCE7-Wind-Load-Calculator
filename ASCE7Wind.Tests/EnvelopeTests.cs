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

            // torsion about the center of mass, CCW +: the extra end-zone force (1.04 - 0.69) x 15 x 8 = 42 q (+x) acts at
            // ry = 4 - 20 = -16 ft, Mz = -ry Fx = +16 x 42 q (the +x push below the center turns the plan CCW)
            Assert.AreEqual(42 * 16 * q / 1000, e.BasicTorsion, 0.01);

            // torsional case: the half beyond the center at 25%:
            // near half 124.8 + 0.69 x 15 x 12 = 249.0 q; far half 0.25 x 0.69 x 15 x 20 = 51.75 q
            Assert.AreEqual(300.75 * q / 1000, e.TorsionalKips, 0.01);
            Assert.AreEqual((124.8 * 16 + 124.2 * 6 - 51.75 * 10) * q / 1000, e.TorsionalTorsion, 0.01);

            // 28.3.4 minimum: 16 psf on 40 x 15 ft
            Assert.AreEqual(9.6, e.MinimumKips, 1e-6);

            // zone table sums to the wall force
            Assert.AreEqual(e.WallsKips, e.Zones.Where(z => !z.IsRoof).Sum(z => z.Fx), 1e-6);
            Assert.AreEqual(0, e.BasicCrossKips, 1e-9);
            Assert.AreEqual(8 * 15, e.Zones.Single(z => z.Zone == "1E").Area, 1e-6);

            // signs, row by row: windward push (+0.40) and leeward suction (-0.29) both give +x on the building
            var w1 = e.Zones.Single(z => z.Zone == "1" && z.TorsionalFactor == 1);          // y = 8 to 20 ft
            Assert.AreEqual(0.40 * q * 15 * 12 / 1000, w1.Fx, 1e-9);
            Assert.AreEqual(-6.0, w1.Ry, 1e-9);
            Assert.AreEqual(6.0 * w1.Fx, w1.Mz, 1e-9);
            var l4 = e.Zones.Single(z => z.Zone == "4" && z.TorsionalFactor == 1);
            Assert.AreEqual(-0.29 * q, l4.Pressure.Value, 1e-9);
            Assert.AreEqual(0.29 * q * 15 * 12 / 1000, l4.Fx, 1e-9);
            // roof suction lifts: Fz = +0.69 q A on Zone 2
            var r2 = e.Zones.Single(z => z.Zone == "2" && z.TorsionalFactor == 1);
            Assert.AreEqual(0.69 * q * r2.Area / 1000, r2.Fz, 1e-9);
            Assert.AreEqual(0, r2.Fx, 1e-9);
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
            Assert.AreEqual(((0.61 + 0.43) * endArea + (0.40 + 0.29) * (wallArea - endArea)) * ql / 1000, l.WallsKips, 1e-6);
            Assert.AreEqual(0, l.RoofKips, 1e-9);

            // Case B side walls: suction on both (-0.45, -0.48) pulls each outward; equal and opposite, they cancel in ΣFy
            var s1 = l.Zones.Single(z => z.Zone == "1");
            var s4 = l.Zones.Single(z => z.Zone == "4");
            Assert.AreEqual(-0.45 * ql * 10 * 52 / 1000, s1.Fy, 1e-9);       // wall at y = 0: outward is -y
            Assert.AreEqual(-s1.Fy, s4.Fy, 1e-9);                             // wall at y = W: outward is +y
            // the roof halves do not cancel: Zone 2 (next to wall 1, -0.69 / -1.07) out-pulls Zone 3 (-0.37 / -0.53), toward -y:
            // Fy = q tan30 x 20 [(-1.07 + 0.53) 8 + (-0.69 + 0.37) 52] = -20.96 x 20 tan30 q
            Assert.AreEqual(-20.96 * 20 * Tan(30) * ql / 1000, l.BasicCrossKips, 1e-6);
        }

        /// <summary>
        /// The sign convention, row by row, for every roof form: F = −p A n with n the outward unit normal, r from the center
        /// of mass to the zone centroid, Mz = rx Fy − ry Fx; the case totals are the sums of the rows.
        /// </summary>
        [DataTestMethod]
        [DataRow(RoofTypes.ROOF_TYPE_FLAT, 0.0, RidgeDirections.RIDGE_DIR_PARALLEL_TO_BLDGLENGTH)]
        [DataRow(RoofTypes.ROOF_TYPE_GABLE, 30.0, RidgeDirections.RIDGE_DIR_PARALLEL_TO_BLDGLENGTH)]
        [DataRow(RoofTypes.ROOF_TYPE_GABLE, 20.0, RidgeDirections.RIDGE_DIR_PERP_TO_BLDGLENGTH)]
        public void EveryRow_FollowsTheSignConvention_AndTotalsAreTheSums(RoofTypes roof, double pitch, RidgeDirections ridge)
        {
            var b = Bldg.Make(60, 40, 10, roof, pitch, ridge);
            b.ParapetHeight = 2;
            var r = WindLateralLoads.Compute(Analysis(b), false, LateralProcedure.Envelope);
            foreach (var e in new[] { r.EnvelopeAlongLength, r.EnvelopeAlongWidth })
            {
                foreach (var z in e.Zones.Where(z => !z.IsParapet))
                {
                    double f = -z.Pressure.Value * z.Area / 1000;
                    Assert.AreEqual(f * z.Nx, z.Fx, 1e-9, z.Zone);
                    Assert.AreEqual(f * z.Ny, z.Fy, 1e-9, z.Zone);
                    Assert.AreEqual(f * z.Nz, z.Fz, 1e-9, z.Zone);
                    Assert.AreEqual(1.0, z.Nx * z.Nx + z.Ny * z.Ny + z.Nz * z.Nz, 1e-9, "unit normal");
                    Assert.AreEqual(z.Xc - e.ComX, z.Rx, 1e-9);
                    Assert.AreEqual(z.Yc - e.ComY, z.Ry, 1e-9);
                    Assert.AreEqual(z.Rx * z.Fy - z.Ry * z.Fx, z.Mz, 1e-9, z.Zone);
                }
                var used = e.Zones.Where(z => !(z.IsRoof && e.RoofNeglected)).ToList();
                Assert.AreEqual(used.Sum(z => z.Fx), e.BasicKips, 1e-9);
                Assert.AreEqual(used.Sum(z => z.Fy), e.BasicCrossKips, 1e-9);
                Assert.AreEqual(used.Sum(z => z.Mz), e.BasicTorsion, 1e-9);
                var usedT = e.Zones.Where(z => !(z.IsRoof && e.TorsionalRoofNeglected)).ToList();
                Assert.AreEqual(usedT.Sum(z => z.Fx * z.TorsionalFactor), e.TorsionalKips, 1e-9);
                Assert.AreEqual(usedT.Sum(z => z.Mz * z.TorsionalFactor), e.TorsionalTorsion, 1e-9);
                // the roof rows cover the whole plan
                Assert.AreEqual(e.WidthNormalToWind * e.DepthAlongWind, e.Zones.Where(z => z.IsRoof).Sum(z => z.Area * z.Nz), 1e-6);
            }
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
                // projected on the vertical plane: sloped area × sin θ = plan × tan θ
                double sin = Math.Sin(10 * Math.PI / 180);
                Assert.AreEqual(50 * Tan(10) * (60 - 2 * e.A), e.Zones.Where(z => z.Zone == "2").Sum(z => z.Area) * sin, 1e-6);
                Assert.AreEqual(50 * Tan(10) * (60 - 2 * e.A), e.Zones.Where(z => z.Zone == "3").Sum(z => z.Area) * sin, 1e-6);
            }
        }

        [TestMethod]
        public void LoadCases_FourCornersEachAxis_BasicTorsionalMinimum_AndExemption()
        {
            var a = Analysis(Bldg.Make(60, 40, 15));
            var full = WindLateralLoads.Compute(a, false, LateralProcedure.Envelope);
            Assert.AreEqual(20, full.LoadCases.Count);           // per axis: 4 corners basic, 4 corners torsional, minimum ±
            var e = full.EnvelopeAlongLength;
            var basic = full.LoadCases.Where(c => c.CaseNumber == 1 && c.Name.Contains("along length")).ToList();
            Assert.AreEqual(4, basic.Count);
            double fx = e.BasicKips, fy = e.BasicCrossKips, m = e.BasicTorsion;
            var expected = new[] { (fx, fy, m), (fx, -fy, -m), (-fx, -fy, m), (-fx, fy, -m) };
            for (int i = 0; i < 4; i++)
            {
                Assert.AreEqual(expected[i].Item1, basic[i].Vx, 1e-9);
                Assert.AreEqual(expected[i].Item2, basic[i].Vy, 1e-9);
                Assert.AreEqual(expected[i].Item3, basic[i].Mt, 1e-9);
            }
            Assert.AreEqual(2, full.LoadCases.Count(c => c.CaseNumber == 3 && c.Name.Contains("along length")));

            var exempt = WindLateralLoads.Compute(a, true, LateralProcedure.Envelope);
            Assert.AreEqual(12, exempt.LoadCases.Count);
            Assert.IsFalse(exempt.LoadCases.Any(c => c.CaseNumber == 2));
        }

        /// <summary>
        /// Wind along the width: the calculation axes are the building axes turned 90° CCW, so (Fx, Fy, Mz) → (−Fy, Fx, Mz).
        /// Case B there (ridge across the length) has a cross-wind force from the roof halves, so the mapping is visible.
        /// </summary>
        [TestMethod]
        public void AlongWidth_MapsToBuildingAxes_WithTheCrossWindForce()
        {
            var b = Bldg.Make(60, 40, 10, RoofTypes.ROOF_TYPE_GABLE, 30, RidgeDirections.RIDGE_DIR_PERP_TO_BLDGLENGTH);
            var r = WindLateralLoads.Compute(Analysis(b), false, LateralProcedure.Envelope);
            var e = r.EnvelopeAlongWidth;
            Assert.AreEqual("B", e.LoadCase);
            Assert.IsTrue(Math.Abs(e.BasicCrossKips) > 0.01);
            var first = r.LoadCases.First(c => c.CaseNumber == 1 && c.Name.Contains("along width"));
            Assert.AreEqual(-e.BasicCrossKips, first.Vx, 1e-9);
            Assert.AreEqual(e.BasicKips, first.Vy, 1e-9);
            Assert.AreEqual(e.BasicTorsion, first.Mt, 1e-9);
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
