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

            // torsional case (ASCE 7-22, printed Fig. 28.3-2 T values at 0-5 deg: 1T 0.10, 4T -0.07):
            // near half 124.8 + 0.69 x 15 x 12 = 249.0 q; far half (0.10 + 0.07) x 15 x 20 = 51.0 q
            Assert.AreEqual(300.0 * q / 1000, e.TorsionalKips, 1e-6);
            Assert.AreEqual((124.8 * 16 + 124.2 * 6 - 51.0 * 10) * q / 1000, e.TorsionalTorsion, 1e-6);
            // ASCE 7-16: 25% of the full pressures, far half 0.25 x 0.69 x 15 x 20 = 51.75 q
            var e16 = WindLateralLoads.Compute(Analysis(Bldg.Make(60, 40, 15), ASCE7_Versions.ASCE_VER_7_16), false, LateralProcedure.Envelope).EnvelopeAlongLength;
            double q16 = e16.Qh * e16.Kd;
            Assert.AreEqual(300.75 * q16 / 1000, e16.TorsionalKips, 1e-6);

            // minimum (28.3.6 in 7-22): 16 psf on 40 x 15 ft
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
        [DataRow(RoofTypes.ROOF_TYPE_HIP, 30.0, RidgeDirections.RIDGE_DIR_PARALLEL_TO_BLDGLENGTH)]
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
            // 7-22: 5T and 6T as printed in Fig. 28.3-2 (0.10, -0.07)
            Assert.AreEqual(q * (1.04 * a04 + 0.69 * a420 + (0.10 + 0.07) * a2040) / 1000, l.TorsionalKips, 1e-6);

            // 7-16: the same 5/5E/5T and 6/6E/6T layout (user), T zones at 25% (Fig. 28.3-1 note 5)
            var l16 = WindLateralLoads.Compute(Analysis(b, ASCE7_Versions.ASCE_VER_7_16), false, LateralProcedure.Envelope).EnvelopeAlongLength;
            Assert.AreEqual(4.0, l16.EndZoneWidth, 1e-9);
            double q16 = l16.Qh * l16.Kd;
            Assert.AreEqual(q16 * (1.04 * a04 + 0.69 * a420 + 0.25 * 0.69 * a2040) / 1000, l16.TorsionalKips, 1e-6);
        }

        [TestMethod]
        public void Zone2_LimitedTo2_5he_BothEditions()
        {
            // Fig. 28.3-1 note: a negative zone 2/2E GCpf applies for min(0.5 D, 2.5 he) from the windward eave; the rest of
            // the windward slope to the ridge takes zone 3/3E. 100 ft deep across the ridge, 8 ft eave, 10 deg: 2.5 he = 20 ft
            // < 50 ft, so zone 2 is 20 ft of plan and zone 3 is the other 30 ft of the windward slope plus the 50 ft leeward
            // slope. Plan areas over the 60 ft wall less the 2a end zone.
            var b = Bldg.Make(100, 60, 8, RoofTypes.ROOF_TYPE_GABLE, 10, RidgeDirections.RIDGE_DIR_PERP_TO_BLDGLENGTH);
            foreach (var v in new[] { ASCE7_Versions.ASCE_VER_7_16, ASCE7_Versions.ASCE_VER_7_22 })
            {
                var e = WindLateralLoads.Compute(Analysis(b, v), false, LateralProcedure.Envelope).EnvelopeAlongLength;
                Assert.IsTrue(e.Transverse);
                Assert.AreEqual(20.0, e.Zone2Reach, 1e-9);
                double cos = Math.Cos(10 * Math.PI / 180), wide = 60 - 2 * e.A;       // plan = sloped area × cos θ
                double Plan(string zone, Func<EnvelopeZoneForce, bool> where) => e.Zones.Where(z => z.Zone == zone && where(z)).Sum(z => z.Area) * cos;
                Assert.AreEqual(20 * wide, Plan("2", z => true), 1e-6);
                Assert.AreEqual(30 * wide, Plan("3", z => z.Xc < 50), 1e-6, "windward slope beyond 2.5 he");
                Assert.AreEqual(50 * wide, Plan("3", z => z.Xc > 50), 1e-6, "leeward slope");
                Assert.AreEqual(20 * 2 * e.A, Plan("2E", z => true), 1e-6);
                Assert.AreEqual(30 * 2 * e.A, Plan("3E", z => z.Xc < 50), 1e-6);
                // the windward remainder carries zone 3's coefficient (10 deg: -0.37 + 5/15 (-0.48 + 0.37) = -0.4067)
                Assert.AreEqual(-0.37 - 0.11 / 3, e.Zones.First(z => z.Zone == "3" && z.Xc < 50).GCpf.Value, 1e-9);
            }
        }

        [TestMethod]
        public void Zone2_ReachesRidge_WhenHalfDepthIsLess()
        {
            // 40 ft deep, 15 ft eave: 0.5 D = 20 < 2.5 he = 37.5, so zone 2 runs eave to ridge (no zone 3 on the windward slope)
            var b = Bldg.Make(40, 60, 15, RoofTypes.ROOF_TYPE_GABLE, 10, RidgeDirections.RIDGE_DIR_PERP_TO_BLDGLENGTH);
            var e = WindLateralLoads.Compute(Analysis(b), false, LateralProcedure.Envelope).EnvelopeAlongLength;
            Assert.AreEqual(20.0, e.Zone2Reach, 1e-9);
            Assert.IsFalse(e.Zones.Any(z => z.IsRoof && (z.Zone == "3" || z.Zone == "3E") && z.Xc < 20));
        }

        [TestMethod]
        public void LoadCases_FourCornersEachAxis_BasicTorsionalMinimum_AndExemption()
        {
            var a = Analysis(Bldg.Make(60, 40, 15));
            var full = WindLateralLoads.Compute(a, false, LateralProcedure.Envelope);
            // a flat roof takes Case A and Case B each way: per axis 2 sets × (4 corners basic + 4 torsional), and the
            // minimum ± as a separate load case (CaseNumber 0)
            Assert.AreEqual(2 * (2 * 8 + 2), full.LoadCases.Count);
            Assert.AreEqual(4, full.EnvelopeDirections.Count);
            CollectionAssert.AreEqual(new[] { "A", "B", "A", "B" }, full.EnvelopeDirections.Select(d => d.LoadCase).ToArray());
            var e = full.EnvelopeAlongLength;
            var basic = full.LoadCases.Where(c => c.CaseNumber == 1 && c.Name.Contains("basic A, along length")).ToList();
            Assert.AreEqual(4, basic.Count);
            double fx = e.BasicKips, fy = e.BasicCrossKips, m = e.BasicTorsion;
            var expected = new[] { (fx, fy, m), (fx, -fy, -m), (-fx, -fy, m), (-fx, fy, -m) };
            for (int i = 0; i < 4; i++)
            {
                Assert.AreEqual(expected[i].Item1, basic[i].Vx, 1e-9);
                Assert.AreEqual(expected[i].Item2, basic[i].Vy, 1e-9);
                Assert.AreEqual(expected[i].Item3, basic[i].Mt, 1e-9);
            }
            Assert.AreEqual(2, full.LoadCases.Count(c => c.CaseNumber == WindLateralLoads.MinimumCaseNumber && c.Name.Contains("along length")));
            Assert.AreEqual(e.MinimumKips, full.LoadCases.First(c => c.CaseNumber == WindLateralLoads.MinimumCaseNumber && c.Name.Contains("along length")).Vx, 1e-9);
            StringAssert.Contains(full.LoadCases.First(c => c.CaseNumber == WindLateralLoads.MinimumCaseNumber).Name, "28.3.6");
            Assert.IsFalse(full.LoadCases.Any(c => c.CaseNumber == 3), "no Load Case 3 in the shear-wall list: the minimum is case 0");

            var exempt = WindLateralLoads.Compute(a, true, LateralProcedure.Envelope);
            Assert.AreEqual(2 * (2 * 4 + 2), exempt.LoadCases.Count);
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
            var hip = Bldg.Make(60, 40, 15, RoofTypes.ROOF_TYPE_HIP, 20);   // hip roofs are covered (Fig. C28.3-3)
            Assert.AreEqual(LateralProcedure.Envelope, WindLateralLoads.Compute(Analysis(hip), false, LateralProcedure.Envelope).Procedure);

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

        /// <summary>Fig. 28.3-1 notation: h is the eave height for θ ≤ 10°, the mean roof height above; a uses that h.</summary>
        [TestMethod]
        public void ZoneWidthA_UsesEaveHeight_AtOrBelow10Degrees()
        {
            // 200 x 150 ft, 10 ft eave, ridge along the 200 ft side (span 150): 0.1 x 150 = 15, floor max(0.04 x 150, 3) = 6.
            // 10 deg: h = eave 10, 0.4h = 4 < 6, so a = 6. 11 deg: h = 10 + tan11 x 75 / 2 = 17.289, a = 0.4h = 6.916.
            var b10 = Bldg.Make(200, 150, 10, RoofTypes.ROOF_TYPE_GABLE, 10, RidgeDirections.RIDGE_DIR_PARALLEL_TO_BLDGLENGTH);
            Assert.AreEqual(10.0, b10.MwfrsFigureHeight, 1e-9);
            Assert.AreEqual(6.0, EnvelopeLateralLoads.ZoneWidthA(b10), 1e-9);
            var b11 = Bldg.Make(200, 150, 10, RoofTypes.ROOF_TYPE_GABLE, 11, RidgeDirections.RIDGE_DIR_PARALLEL_TO_BLDGLENGTH);
            Assert.AreEqual(10 + Tan(11) * 75 / 2, b11.MwfrsFigureHeight, 1e-9);
            Assert.AreEqual(0.4 * (10 + Tan(11) * 75 / 2), EnvelopeLateralLoads.ZoneWidthA(b11), 1e-9);
        }

        /// <summary>Fig. 28.3-1 note 9 exception: θ 0 to 7° and least dimension over 300 ft, a is at most 0.8h.</summary>
        [TestMethod]
        public void ZoneWidthA_LargeLowSlopeException()
        {
            // 400 x 320 ft flat, 10 ft eave: min(32, 4) = 4, floor max(12.8, 3) = 12.8, capped at 0.8 x 10 = 8
            var flat = Bldg.Make(400, 320, 10);
            Assert.IsTrue(EnvelopeLateralLoads.LargeFlatException(flat));
            Assert.AreEqual(8.0, EnvelopeLateralLoads.ZoneWidthA(flat), 1e-9);
            // an 8 deg gable is outside the exception: a = 12.8
            var gable8 = Bldg.Make(400, 320, 10, RoofTypes.ROOF_TYPE_GABLE, 8, RidgeDirections.RIDGE_DIR_PARALLEL_TO_BLDGLENGTH);
            Assert.IsFalse(EnvelopeLateralLoads.LargeFlatException(gable8));
            Assert.AreEqual(12.8, EnvelopeLateralLoads.ZoneWidthA(gable8), 1e-9);
            // least dimension 300 ft exactly is not "greater than 300 ft": a = max(min(30, 4), max(12, 3)) = 12
            Assert.AreEqual(12.0, EnvelopeLateralLoads.ZoneWidthA(Bldg.Make(400, 300, 10)), 1e-9);
        }

        /// <summary>Table 26.10-1 footnote a: use Kz = 0.70 in Chapter 28, Exposure B, z &lt; 30 ft (7-16 and 7-22).</summary>
        [TestMethod]
        public void Kz070_ExposureB_Below30ft()
        {
            foreach (var v in new[] { ASCE7_Versions.ASCE_VER_7_16, ASCE7_Versions.ASCE_VER_7_22 })
            {
                var a = WindAnalysis.Run(v, new WindParameters_Base { WindSpeed = 115, ExposureCategory = WindExposureCategories.WIND_EXP_CAT_B }, Bldg.Make(60, 40, 15));
                var e = WindLateralLoads.Compute(a, false, LateralProcedure.Envelope).EnvelopeAlongLength;
                // qh = 0.00256 x 0.70 x 115² (x Kd 0.85 inside q for 7-16)
                double q = 0.00256 * 0.70 * 115 * 115 * (v == ASCE7_Versions.ASCE_VER_7_16 ? 0.85 : 1.0);
                Assert.AreEqual(0.70, e.Kz, 1e-12, v.ToString());
                Assert.AreEqual(q, e.Qh, 1e-9, v.ToString());
                Assert.IsTrue(e.Notes.Any(n => n.Contains("Table 26.10-1 footnote a")));
                // Chapter 27 keeps the power-law Kz (0.575 at 15 ft in 7-16): lower than 0.70
                Assert.IsTrue(a.MwfrsAlongLength.GetKz(15, WindExposureCategories.WIND_EXP_CAT_B) < 0.70);
            }
            // Exposure C is unchanged (Kz 0.85 at 15 ft)
            var c = WindAnalysis.Run(ASCE7_Versions.ASCE_VER_7_22, new WindParameters_Base { WindSpeed = 115, ExposureCategory = WindExposureCategories.WIND_EXP_CAT_C }, Bldg.Make(60, 40, 15));
            Assert.AreEqual(c.MwfrsAlongLength.GetKz(15, WindExposureCategories.WIND_EXP_CAT_C),
                            WindLateralLoads.Compute(c, false, LateralProcedure.Envelope).EnvelopeAlongLength.Kz, 1e-12);
        }

        /// <summary>ASCE 7-22 Fig. 28.3-2: the printed T coefficients (Load Case 3: 1T-4T; Load Case 4: 5T, 6T), linear in θ.</summary>
        [TestMethod]
        public void PrintedTorsionalTable_7_22()
        {
            CollectionAssert.AreEqual(new[] { 0.10, -0.17, -0.09, -0.07 }, EnvelopeLateralLoads.CaseATCoefficients(0));
            CollectionAssert.AreEqual(new[] { 0.13, -0.17, -0.12, -0.11 }, EnvelopeLateralLoads.CaseATCoefficients(20));
            CollectionAssert.AreEqual(new[] { 0.14, 0.05, -0.11, -0.09 }, EnvelopeLateralLoads.CaseATCoefficients(37));
            CollectionAssert.AreEqual(new[] { 0.14, 0.14, -0.09, -0.09 }, EnvelopeLateralLoads.CaseATCoefficients(90));
            var mid = EnvelopeLateralLoads.CaseATCoefficients(12.5);     // halfway between 5 and 20 deg
            double[] expected = { 0.115, -0.17, -0.105, -0.09 };
            for (int i = 0; i < 4; i++) Assert.AreEqual(expected[i], mid[i], 1e-9);
            Assert.AreEqual(0.10, EnvelopeLateralLoads.GCpf5T, 1e-12);
            Assert.AreEqual(-0.07, EnvelopeLateralLoads.GCpf6T, 1e-12);

            // the T rows carry the printed value; 7-16 rows carry 25% of their own
            var b = Bldg.Make(60, 40, 15);
            var t22 = WindLateralLoads.Compute(Analysis(b), false, LateralProcedure.Envelope).EnvelopeAlongLength.Zones.Single(z => z.Zone == "4" && z.TorsionalGCpf.HasValue);
            Assert.AreEqual(-0.07, t22.TorsionalGCpf.Value, 1e-12);
            Assert.AreEqual(-0.07 / -0.29, t22.TorsionalFactor, 1e-12);
            var t16 = WindLateralLoads.Compute(Analysis(b, ASCE7_Versions.ASCE_VER_7_16), false, LateralProcedure.Envelope).EnvelopeAlongLength.Zones.Single(z => z.Zone == "4" && z.TorsionalGCpf.HasValue);
            Assert.AreEqual(0.25 * -0.29, t16.TorsionalGCpf.Value, 1e-12);
            Assert.AreEqual(0.25, t16.TorsionalFactor, 1e-12);
        }

        /// <summary>
        /// Hip roof, Fig. C28.3-3 (7-22) / C28.3-2 (7-16): walls as Fig. 28.3-1 Load Cases 1 and 2, the roof with the Case A
        /// (Load Case 1) table at the roof angle in both. 60 x 40 ft, 10 ft eave, 30 deg, ridge along the 60 ft side (20 ft
        /// long, hips at 45° in plan); a = 4 ft (0.1 x 40 &lt; 0.4 x 15.77). Case A at 30°: 2 0.21, 2E 0.27, 3 -0.43, 3E -0.53.
        /// </summary>
        [TestMethod]
        public void HipRoof_LoadCases1And2_HandCalculation()
        {
            var b = Bldg.Make(60, 40, 10, RoofTypes.ROOF_TYPE_HIP, 30, RidgeDirections.RIDGE_DIR_PARALLEL_TO_BLDGLENGTH);
            var r = WindLateralLoads.Compute(Analysis(b), false, LateralProcedure.Envelope);
            Assert.IsNull(r.ProcedureNote);
            double t = Tan(30);

            // Load Case 1, wind across the ridge (along the width): D = 40, W = 60. Walls as the gable: 577.2 q.
            // Each main roof plane is a trapezoid of 800 sq ft plan; the 2a end strip (y 0-8) holds 32 of it (x from 0 to y).
            // The hip ends slope across the wind (Fx 0). Fx = q tan30 [0.27 x 32 + 0.21 x 768 + 0.53 x 32 + 0.43 x 768] = 517.12 q tan30
            var e1 = r.EnvelopeAlongWidth;
            double q = e1.Qh * e1.Kd;
            Assert.IsTrue(e1.HipRoof);
            Assert.AreEqual("A", e1.LoadCase);
            Assert.AreEqual(4.0, e1.A, 1e-9);
            Assert.AreEqual(10 * ((0.56 + 0.37) * 52 + (0.69 + 0.48) * 8) * q / 1000, e1.WallsKips, 1e-6);
            Assert.IsFalse(e1.RoofNeglected);
            Assert.AreEqual(517.12 * t * q / 1000, e1.RoofKips, 1e-6);
            Assert.AreEqual(40 * 60, e1.Zones.Where(z => z.IsRoof).Sum(z => z.Area * z.Nz), 1e-6, "the roof rows cover the plan");
            Assert.IsTrue(e1.Notes.Any(n => n.Contains("C28.3-3")));

            // Load Case 2, wind along the ridge (along the length): D = 60, W = 40. Walls as Case B: hip end 10 ft high,
            // 5E a = 4 wide: (1.04 x 4 + 0.69 x 36) x 10 = 290 q. Roof halves at y = 20 with the Case A values at 30°:
            // windward hip triangle (x 0-20): 2E/3E (x &lt; 2a = 8) 128 sq ft each, 2/3 72 each, rising downwind (+p A tan30);
            // leeward hip triangle (x 40-60): 2 and 3 200 sq ft each, falling (−p A tan30). The main planes slope across (Fx 0).
            // Fx = q tan30 [0.27 x 128 + 0.21 x 72 − 0.53 x 128 − 0.43 x 72 − (0.21 − 0.43) x 200] = −5.12 q tan30 &lt; 0: neglected.
            var e2 = r.EnvelopeAlongLength;
            Assert.AreEqual("B", e2.LoadCase);
            Assert.AreEqual(30.0, e2.Theta, 1e-9, "the roof reads Case A at the roof angle in Load Case 2 too");
            Assert.AreEqual(290 * q / 1000, e2.WallsKips, 1e-6);
            Assert.AreEqual(-5.12 * t * q / 1000, e2.Zones.Where(z => z.IsRoof).Sum(z => z.Fx), 1e-6);
            Assert.IsTrue(e2.RoofNeglected);
            Assert.AreEqual(e2.WallsKips, e2.BasicKips, 1e-9);
            Assert.AreEqual(0.21, e2.Zones.First(z => z.IsRoof && z.Zone == "2").GCpf.Value, 1e-9);
            Assert.AreEqual(-0.53, e2.Zones.First(z => z.IsRoof && z.Zone == "3E").GCpf.Value, 1e-9);
        }

        /// <summary>The minimum is a separate load case (C28.3.6): it does not raise the basic or torsional cases.</summary>
        [TestMethod]
        public void Minimum_IsASeparateCase_DoesNotRaiseTheOthers()
        {
            // 85 mph, Exposure B: the minimum (16 psf x 40 x 15 = 9.6 k) exceeds the calculated wind
            var a = WindAnalysis.Run(ASCE7_Versions.ASCE_VER_7_22, new WindParameters_Base { WindSpeed = 85, ExposureCategory = WindExposureCategories.WIND_EXP_CAT_B }, Bldg.Make(60, 40, 15));
            var r = WindLateralLoads.Compute(a, false, LateralProcedure.Envelope);
            var e = r.EnvelopeAlongLength;
            Assert.IsTrue(e.MinimumKips > e.BasicKips, "the minimum should be larger at 85 mph");
            var basic = r.LoadCases.First(c => c.CaseNumber == 1 && c.Name.Contains("basic A, along length"));
            Assert.AreEqual(e.BasicKips, basic.Vx, 1e-9, "the basic case keeps the calculated force");
            var min = r.LoadCases.Where(c => c.CaseNumber == WindLateralLoads.MinimumCaseNumber && c.Name.Contains("along length")).ToList();
            Assert.AreEqual(2, min.Count);
            Assert.AreEqual(9.6, min[0].Vx, 1e-9);
            Assert.AreEqual(-9.6, min[1].Vx, 1e-9);
            Assert.AreEqual(0, min[0].Mt, 1e-12);
        }
    }
}
