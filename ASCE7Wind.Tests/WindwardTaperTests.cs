using System;
using System.Linq;
using ASCE7WindLoadCalculator;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ASCE7Wind.Tests
{
    /// <summary>ASCE 7 27.3: windward wall uses qz at each height; leeward, side walls and roof use qh.</summary>
    [TestClass]
    public class WindwardTaperTests
    {
        private static WindLoadCalculator_MWFRS Mwfrs(BuildingData b, ASCE7_Versions v = ASCE7_Versions.ASCE_VER_7_22) =>
            (WindLoadCalculator_MWFRS)Bldg.Run(v, WindLoadCalculationTypes.MWFRS, b);

        [TestMethod]
        public void WindwardPressure_ConstantBelow15ft_ThenGrows_AndEqualsTableValueAtH()
        {
            var c = Mwfrs(Bldg.Make(60, 40, 40));
            Assert.AreEqual(c.WindwardWallNetPressureAt(0), c.WindwardWallNetPressureAt(15), 1e-12, "Kz is constant below 15 ft");
            Assert.IsTrue(c.WindwardWallNetPressureAt(30) > c.WindwardWallNetPressureAt(15));
            Assert.IsTrue(c.WindwardWallNetPressureAt(40) > c.WindwardWallNetPressureAt(30));

            var ww = c.WallAreaCalculator_BldgLength.effWindAreas.Single(z => z.Value.Label_Full == "ZoneWW");
            Assert.AreEqual(c.windPressureWall_Pos_External_MWFRS[ww.Key].NetPressure, c.WindwardWallNetPressureAt(c.buildingData.MeanRoofHeight), 1e-9,
                "at z = h the taper equals the tabulated windward value");
        }

        [TestMethod]
        public void Scene_WindwardBands_FollowTheTaper()
        {
            var b = Bldg.Make(60, 40, 40);
            var c = Mwfrs(b);
            var rotated = b.Clone(); rotated.RotateBuilding();
            var s = WindSceneBuilder.Build(b, null, c, Mwfrs(rotated), WindViewMode.MwfrsAlongLength);
            var bands = s.Zones.Where(z => z.Face == BuildingFace.WestWall).OrderBy(z => z.Triangles.Min(p => p.Z)).ToList();
            Assert.IsTrue(bands.Count >= 6, "0-15 ft plus 5 ft bands to 40 ft");
            for (int i = 1; i < bands.Count; i++)
                Assert.IsTrue(bands[i].PressurePsf > bands[i - 1].PressurePsf, "higher bands carry more pressure");
            Assert.AreEqual(40 * 40, bands.Sum(z => z.SurfaceArea), 1e-6, "bands cover the whole windward wall");
            var top = new Vec3(0, 20, 40);
            Assert.AreEqual(c.WindwardWallNetPressureAt(40), bands.Last().PressureAtPoint(top), 1e-12);
        }

        /// <summary>Base shear integrates qz up the windward wall: check against an independent integration.</summary>
        [TestMethod]
        public void BaseShear_IntegratesWindwardTaper()
        {
            double L = 60, B = 40, H = 40;
            var c = Mwfrs(Bldg.Make(L, B, H));
            double kd = 0.85, G = 0.85, V = 115;
            // 7-22: p = 0.00256 Kz V^2 Kd G Cp, Kz floored at 15 ft.
            double ww = 0;
            int n = 4000;
            for (int i = 0; i < n; i++)
            {
                double z = (i + 0.5) * H / n;
                ww += 0.00256 * c.GetKz(Math.Max(15, z), WindExposureCategories.WIND_EXP_CAT_C) * V * V * kd * G * 0.8 * B * (H / n);
            }
            double qh = c.CalculateDynamicWindPressure(H);
            double lw = qh * kd * G * Math.Abs(c.Cp_LW) * B * H;
            Assert.AreEqual((ww + lw) / 1000, MwfrsLateralLoad.WallBaseShearKips(c), 0.002 * (ww + lw) / 1000);

            double constant = (qh * kd * G * 0.8 * B * H + lw) / 1000;
            Assert.IsTrue(MwfrsLateralLoad.WallBaseShearKips(c) < constant, "taper gives less than qh over the full height");
        }
    }
}
