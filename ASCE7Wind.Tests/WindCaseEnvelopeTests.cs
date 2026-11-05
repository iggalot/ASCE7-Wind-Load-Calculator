using System;
using System.Linq;
using ASCE7WindLoadCalculator;
using ASCE7WindLoadCalculator.ModelAdapter;
using BuildingModel.Geometry;
using BuildingModel.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ASCE7Wind.Tests
{
    /// <summary>
    /// The envelope of the wind cases on Quimby's Example 7.1 building (see <see cref="WindCaseTests"/>). Max and min are
    /// kept apart, each with the case that gives it; the cells cover every surface once.
    /// </summary>
    [TestClass]
    public class WindCaseEnvelopeTests
    {
        private static WindAnalysisResult Quimby()
        {
            var b = Building.New();
            b.AddDiaphragm(b.AddLevel("2nd floor", 11), new Rect2(0, 0, 90, 50));
            b.AddDiaphragm(b.AddLevel("Roof", 22), new Rect2(0, 0, 90, 50));
            b.Roof = new Roof { Shape = RoofShape.Gable, Pitch = Math.Atan(0.25) * 180 / Math.PI, Ridge = RidgeAxis.WestEast, Overhang = 0, Parapet = 0 };
            var p = new WindParameters_Base { WindSpeed = 120, ExposureCategory = WindExposureCategories.WIND_EXP_CAT_C, Kzt = 1.0 };
            return WindAnalysis.Run(ASCE7_Versions.ASCE_VER_7_16, p, WindAdapter.From(b).Building);
        }

        [TestMethod]
        public void Cells_CoverEachSurfaceOnce_AndEveryCellHasBothCases()
        {
            var a = Quimby();
            var cells = WindCaseEnvelope.Build(WindCases.Build(a));
            var g = new WindGeometry(a.MwfrsAlongLength.buildingData);
            foreach (var s in g.Surfaces)
            {
                double covered = cells.Where(c => c.Face == s.Face).Sum(c => c.Area);
                Assert.AreEqual(s.Area, covered, 1e-6 * Math.Max(1, s.Area), $"{s.Name} covered {covered:0.###} of {s.Area:0.###} ft²");
            }
            foreach (var c in cells)
            {
                Assert.IsNotNull(c.MaxBy, "a case gives the max");
                Assert.IsNotNull(c.MinBy, "a case gives the min");
                Assert.IsTrue(c.MaxNetPsf >= c.MinNetPsf);
            }
        }

        /// <summary>
        /// Near the west eave of the south slope the most negative case is Case 3 from the SW with roof Case A for both winds
        /// and +GCpi: 0.75 × 25.25 × (−0.9 − 0.740) − 5.34 = −36.4 psf. The next worst there, Case 3 from the NW (the north
        /// wind's leeward roof, Cp −0.5), gives about −31.9; Case 1 from the W gives −28.0.
        /// </summary>
        [TestMethod]
        public void Min_NearTheSouthWestRoofCorner_IsTheSouthWestQuarteringCase()
        {
            var all = WindCases.Build(Quimby());
            var cells = WindCaseEnvelope.Build(all);
            var at = new Vec2(3, 20);
            var cell = cells.Single(c => c.Face == BuildingFace.RoofSouth && WindCaseEnvelope.Inside(c.Outline, at)
                                         && !c.Outline.Any(p => Math.Abs(p.X - at.X) < 1e-9 && Math.Abs(p.Y - at.Y) < 1e-9));
            Assert.AreEqual(-36.4, cell.MinNetPsf, 0.2);
            Assert.AreEqual("Case 3, from SW, roof W Case A / S Case A, +GCpi", cell.MinBy.Name);

            // the envelope value is a case's own value, not a sum or swap: the same as that case's zone there
            var z = cell.MinBy.Zones.Single(q => q.Surface == cell.Surface && WindCaseEnvelope.Inside(q.Outline, at));
            Assert.AreEqual(z.NetPsf, cell.MinNetPsf, 1e-12);
        }

        [TestMethod]
        public void Envelope_ScenesShowMaxAndMinSeparately()
        {
            var a = Quimby();
            var all = WindCases.Build(a);
            var cells = WindCaseEnvelope.Build(all);
            var b = a.MwfrsAlongLength.buildingData;
            var max = WindSceneBuilder.FromEnvelope(b, cells, max: true, all.Count);
            var min = WindSceneBuilder.FromEnvelope(b, cells, max: false, all.Count);
            Assert.AreEqual(cells.Count, max.Zones.Count);
            Assert.AreEqual(cells.Max(c => c.MaxNetPsf), max.MaxPsf, 1e-9);
            Assert.AreEqual(cells.Min(c => c.MinNetPsf), min.MinPsf, 1e-9);
            Assert.IsTrue(min.MinPsf < max.MinPsf, "the min view is the suction side");

            var one = WindSceneBuilder.FromCase(b, all[0]);
            Assert.AreEqual(all[0].Zones.Count, one.Zones.Count);
            Assert.AreEqual(all[0].Zones.Sum(q => q.Area), one.Zones.Sum(q => q.SurfaceArea), 1e-6 * all[0].Zones.Sum(q => q.Area), "drawn area = case area");
        }
    }
}
