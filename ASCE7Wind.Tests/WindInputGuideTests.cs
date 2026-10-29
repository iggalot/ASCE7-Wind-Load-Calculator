using System;
using System.Linq;
using ASCE7WindLoadCalculator;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ASCE7Wind.Tests
{
    /// <summary>
    /// M5 guided input: field-level checks (with the code limits they come from) and the results summary.
    /// </summary>
    [TestClass]
    public class WindInputGuideTests
    {
        private static WindParameters_Base Site(double v = 115, double kzt = 1.0) =>
            new WindParameters_Base { WindSpeed = v, Kzt = kzt, ExposureCategory = WindExposureCategories.WIND_EXP_CAT_C, RiskCategory = "II" };

        private static InputIssue Only(WindParameters_Base p, BuildingData b)
        {
            var issues = WindInputValidator.Validate(p, b);
            Assert.AreEqual(1, issues.Count, string.Join("; ", issues));
            return issues[0];
        }

        [TestMethod]
        public void TypicalInputs_HaveNoIssues()
        {
            Assert.AreEqual(0, WindInputValidator.Validate(Site(), Bldg.Make(60, 40, 15)).Count);
            Assert.AreEqual(0, WindInputValidator.Validate(Site(), Bldg.Make(60, 40, 15, RoofTypes.ROOF_TYPE_GABLE, 22)).Count);
        }

        [DataTestMethod]
        [DataRow(0.0, InputSeverity.Error)]
        [DataRow(double.NaN, InputSeverity.Error)]
        [DataRow(-5.0, InputSeverity.Error)]
        [DataRow(70.0, InputSeverity.Warning)]
        [DataRow(260.0, InputSeverity.Warning)]
        public void WindSpeed_OutOfRange_IsFlaggedAtTheField(double v, InputSeverity severity)
        {
            var i = Only(Site(v), Bldg.Make(60, 40, 15));
            Assert.AreEqual(WindInputField.WindSpeed, i.Field);
            Assert.AreEqual(severity, i.Severity);
        }

        [DataTestMethod]
        [DataRow(85.0)]
        [DataRow(250.0)]
        public void WindSpeed_MapRangeEnds_AreAccepted(double v) =>
            Assert.AreEqual(0, WindInputValidator.Validate(Site(v), Bldg.Make(60, 40, 15)).Count);

        /// <summary>26.8: Kzt = (1 + K1 K2 K3)^2 is never below 1.</summary>
        [TestMethod]
        public void Kzt_BelowOne_IsAWarning_ZeroIsAnError()
        {
            var w = Only(Site(kzt: 0.9), Bldg.Make(60, 40, 15));
            Assert.AreEqual(WindInputField.Kzt, w.Field);
            Assert.AreEqual(InputSeverity.Warning, w.Severity);
            Assert.AreEqual(InputSeverity.Error, Only(Site(kzt: 0), Bldg.Make(60, 40, 15)).Severity);
        }

        [TestMethod]
        public void Dimensions_MustBePositive_EachAtItsOwnField()
        {
            Assert.AreEqual(WindInputField.Length, Only(Site(), Bldg.Make(0, 40, 15)).Field);
            Assert.AreEqual(WindInputField.Width, Only(Site(), Bldg.Make(60, double.NaN, 15)).Field);
            Assert.AreEqual(WindInputField.WallHeight, Only(Site(), Bldg.Make(60, 40, -1)).Field);
            Assert.IsTrue(WindInputValidator.HasErrors(WindInputValidator.Validate(Site(), Bldg.Make(0, 0, 0))));
        }

        [TestMethod]
        public void Pitch_IsCheckedOnlyForSlopedRoofs()
        {
            Assert.AreEqual(0, WindInputValidator.Validate(Site(), Bldg.Make(60, 40, 15, RoofTypes.ROOF_TYPE_FLAT, 0)).Count);

            var zero = Only(Site(), Bldg.Make(60, 40, 15, RoofTypes.ROOF_TYPE_GABLE, 0));
            Assert.AreEqual(WindInputField.RoofPitch, zero.Field);
            Assert.AreEqual(InputSeverity.Error, zero.Severity);

            // Chapter 30 Part 1 figures stop at 45 degrees.
            var steep = Only(Site(), Bldg.Make(60, 40, 15, RoofTypes.ROOF_TYPE_HIP, 50));
            Assert.AreEqual(InputSeverity.Warning, steep.Severity);
            Assert.AreEqual(0, WindInputValidator.Validate(Site(), Bldg.Make(60, 40, 15, RoofTypes.ROOF_TYPE_HIP, 45)).Count);
        }

        /// <summary>Chapter 30 Part 1: h &lt;= 60 ft. Above that the screen warns but still calculates MWFRS.</summary>
        [TestMethod]
        public void MeanRoofHeightOver60_IsAWarningNotAnError()
        {
            var i = Only(Site(), Bldg.Make(60, 40, 61));
            Assert.AreEqual(WindInputField.Building, i.Field);
            Assert.AreEqual(InputSeverity.Warning, i.Severity);
            Assert.AreEqual(0, WindInputValidator.Validate(Site(), Bldg.Make(60, 40, 60)).Count);
        }

        [DataTestMethod]
        [DataRow(45.0, "12.0:12")]
        [DataRow(22.619864948, "5.0:12")]
        [DataRow(0.0, "-")]
        [DataRow(90.0, "-")]
        public void RisePer12_MatchesTanTheta(double deg, string expected) =>
            Assert.AreEqual(expected, WindInputValidator.RisePer12(deg));

        // ------------------------------------------------------------------ summary

        private static (WindAnalysisResult a, BuildingData b) Run(BuildingData b) =>
            (WindAnalysis.Run(ASCE7_Versions.ASCE_VER_7_22, Site(), b), b);

        [TestMethod]
        public void Summary_UpliftAndSuction_AreTheStrongestZonesInTheScene()
        {
            var (a, b) = Run(Bldg.Make(60, 40, 15, RoofTypes.ROOF_TYPE_GABLE, 22, RidgeDirections.RIDGE_DIR_PARALLEL_TO_BLDGLENGTH));
            var s = WindSummary.Compute(a, b, onlyCases1And3: false);
            var scene = WindSceneBuilder.Build(b, a.Cladding, a.MwfrsAlongLength, a.MwfrsAlongWidth, WindViewMode.CladdingSuction);

            double roofMin = scene.Zones.Where(z => z.HasPressure && z.Kind == SurfaceKind.Roof).Min(z => z.PressurePsf);
            double wallMin = scene.Zones.Where(z => z.HasPressure && z.Kind == SurfaceKind.Wall).Min(z => z.PressurePsf);
            Assert.AreEqual(roofMin, s.RoofUpliftPsf.Value, 1e-9);
            Assert.AreEqual(wallMin, s.WallSuctionPsf.Value, 1e-9);
            Assert.IsTrue(s.RoofUpliftPsf < 0 && s.WallSuctionPsf < 0);
            Assert.IsTrue(s.InwardPsf > 0);
            Assert.IsFalse(string.IsNullOrEmpty(s.RoofUpliftZone));
        }

        /// <summary>Fig. 30.3-2A: on a flat roof the corner (zone 3) has the highest uplift; on walls zone 5.</summary>
        [TestMethod]
        public void Summary_FlatRoof_UpliftIsAtTheCorner_WallSuctionAtTheCornerStrip()
        {
            var (a, b) = Run(Bldg.Make(60, 40, 15));
            var s = WindSummary.Compute(a, b, false);
            StringAssert.Contains(s.RoofUpliftZone, "corner");
            StringAssert.Contains(s.WallSuctionZone, "corner");
        }

        [TestMethod]
        public void Summary_Forces_MatchTheLateralLoads_AndCaseCountFollowsTheException()
        {
            var (a, b) = Run(Bldg.Make(60, 40, 15, RoofTypes.ROOF_TYPE_HIP, 25));
            var full = WindSummary.Compute(a, b, false);
            var reduced = WindSummary.Compute(a, b, true);
            var lateral = WindLateralLoads.Compute(a, false);

            Assert.AreEqual(lateral.AlongLength.DesignKips, full.FromWest.DesignKips, 1e-12);
            Assert.AreEqual(lateral.AlongWidth.DesignKips, full.FromSouth.DesignKips, 1e-12);
            Assert.AreEqual(16, full.Lateral.LoadCases.Count);
            Assert.AreEqual(4, reduced.Lateral.LoadCases.Count);
        }

        [TestMethod]
        public void Summary_Over60ft_HasNoCladding_ButStillHasShearWallForces()
        {
            var (a, b) = Run(Bldg.Make(80, 60, 70));
            var s = WindSummary.Compute(a, b, false);
            Assert.IsNull(s.RoofUpliftPsf);
            Assert.IsNull(s.WallSuctionPsf);
            Assert.IsNotNull(s.FromWest);
            Assert.IsTrue(s.FromWest.DesignKips > 0);
            Assert.IsTrue(s.Checks.Any(c => !c.Ok), "the cladding limit must appear as a check");
        }

        [TestMethod]
        public void Summary_ReportsWhenTheMinimumGoverns()
        {
            // Low wind speed on a small building: 27.1.5 minimum (16 psf walls) governs.
            var b = Bldg.Make(20, 20, 10);
            var a = WindAnalysis.Run(ASCE7_Versions.ASCE_VER_7_22, Site(v: 90), b);
            var s = WindSummary.Compute(a, b, false);
            Assert.AreEqual(s.FromWest.MinimumGoverns, s.Checks.Any(c => !c.Ok && c.Text.Contains("west") && c.Text.Contains("minimum")));
        }
    }
}
