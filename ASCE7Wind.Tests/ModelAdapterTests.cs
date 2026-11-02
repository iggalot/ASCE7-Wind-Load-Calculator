using System.Linq;
using ASCE7WindLoadCalculator;
using ASCE7WindLoadCalculator.ModelAdapter;
using BuildingModel.Geometry;
using BuildingModel.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ASCE7Wind.Tests
{
    /// <summary>
    /// Building model → wind engine: L and B, the stories and plate heights and the roof come from the model (the one
    /// source); only the enclosure is the wind app's own. The engine's ridge and mean roof height agree with the model's.
    /// </summary>
    [TestClass]
    public class ModelAdapterTests
    {
        private const double Tol = 1e-9;

        private static Building Box()
        {
            var b = Building.New();
            foreach (var (name, z) in new[] { ("Level 2", 12.0), ("Level 3", 22.0), ("Roof", 31.0) })
                b.AddDiaphragm(b.AddLevel(name, z), new Rect2(0, 0, 60, 40));
            return b;
        }

        [TestMethod]
        public void Box_GivesTheWindBuilding()
        {
            var b = Box();
            b.Roof = new Roof { Shape = RoofShape.Gable, Pitch = 20, Overhang = 2 };
            var w = WindAdapter.From(b, new WindOptions { Enclosure = BuildingEnclosures.BLDG_PARTIALLY_ENCLOSED });
            var d = w.Building;
            Assert.AreEqual(60, d.BuildingLength, Tol);
            Assert.AreEqual(40, d.BuildingWidth, Tol);
            Assert.AreEqual(3, d.NumberOfStories);
            CollectionAssert.AreEqual(new[] { 12.0, 10.0, 9.0 }, d.StoryPlateHeights);
            Assert.AreEqual(31, d.BuildingHeight, Tol);
            Assert.AreEqual(RoofTypes.ROOF_TYPE_GABLE, d.RoofType);
            Assert.AreEqual(20, d.RoofPitch, Tol);
            Assert.AreEqual(2, d.OverhangWidth, Tol);
            Assert.AreEqual(BuildingEnclosures.BLDG_PARTIALLY_ENCLOSED, d.EnclosureType);
            Assert.IsFalse(w.Notes.Any(n => n.Contains("not a rectangle")));
        }

        [TestMethod]
        public void FlatRoof_Parapet_AndTheModelsDefaults()
        {
            var b = Box();
            var d = WindAdapter.From(b).Building;
            Assert.AreEqual(RoofTypes.ROOF_TYPE_FLAT, d.RoofType);
            Assert.AreEqual(b.Defaults.Parapet, d.ParapetHeight, Tol, "a new model's parapet: the model's default");
            Assert.AreEqual(b.Defaults.Overhang, d.OverhangWidth, Tol);
            b.Roof.Parapet = 3.5;
            Assert.AreEqual(3.5, WindAdapter.From(b).Building.ParapetHeight, Tol);
        }

        /// <summary>The ridge, the span across it and h: the engine and the model work them out the same way.</summary>
        [TestMethod]
        public void Ridge_AndMeanRoofHeight_AgreeWithTheModel()
        {
            var b = Box();
            foreach (var roof in new[]
            {
                new Roof { Shape = RoofShape.Gable, Pitch = 30 },
                new Roof { Shape = RoofShape.Gable, Pitch = 30, Ridge = RidgeAxis.WestEast },
                new Roof { Shape = RoofShape.Gable, Pitch = 30, Ridge = RidgeAxis.SouthNorth },
                new Roof { Shape = RoofShape.Hip, Pitch = 25 },
                new Roof { Shape = RoofShape.Flat },
            })
            {
                b.Roof = roof;
                var d = WindAdapter.From(b).Building;
                Assert.AreEqual(b.SpanAcrossRidge(), d.SpanAcrossRidge, 1e-6, $"{roof.Shape} {roof.Ridge}");
                Assert.AreEqual(b.MeanRoofHeight(), d.MeanRoofHeight, 1e-6, $"{roof.Shape} {roof.Ridge}");
                Assert.AreEqual(b.RidgeHeight(), d.RidgeHeight, 1e-6);
            }
            b.Roof = new Roof { Shape = RoofShape.Gable, Pitch = 30, Ridge = RidgeAxis.SouthNorth };
            Assert.AreEqual(RidgeDirections.RIDGE_DIR_PERP_TO_BLDGLENGTH, WindAdapter.From(b).Building.RidgeDirection);
        }

        [TestMethod]
        public void LShape_UsesTheEnclosingRectangle_AndSaysSo()
        {
            var b = Building.New();
            var roof = b.AddLevel("Roof", 12);
            b.AddDiaphragm(roof, new Rect2(0, 0, 60, 20), new Rect2(0, 0, 20, 40));
            var w = WindAdapter.From(b);
            Assert.AreEqual(60, w.Building.BuildingLength, Tol);
            Assert.AreEqual(40, w.Building.BuildingWidth, Tol);
            Assert.IsTrue(w.Notes.Any(n => n.Contains("not a rectangle")));
        }

        [TestMethod]
        public void MoreLevelsThanTheEngineSplits_OneStoryWithANote()
        {
            var b = Building.New();
            for (int i = 1; i <= 4; i++) b.AddDiaphragm(b.AddLevel("L" + i, 10 * i), new Rect2(0, 0, 30, 30));
            var w = WindAdapter.From(b);
            Assert.AreEqual(1, w.Building.NumberOfStories);
            Assert.AreEqual(40, w.Building.BuildingHeight, Tol);
            Assert.IsTrue(w.Notes.Any(n => n.Contains("4 diaphragm levels")));
            Assert.AreEqual(5, w.Envelope.Strips.Count(s => s.Face == Face.West), "the strips still cover every level");
        }
    }
}
