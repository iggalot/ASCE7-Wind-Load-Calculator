using System;
using System.Collections.Generic;
using System.Linq;

namespace ASCE7WindLoadCalculator
{
    /// <summary>The input a message belongs to, so a screen can show it right at that field.</summary>
    public enum WindInputField { WindSpeed, Kzt, Length, Width, WallHeight, RoofPitch, Building }

    public enum InputSeverity
    {
        /// <summary>The value cannot be calculated.</summary>
        Error,
        /// <summary>The value calculates but should be checked.</summary>
        Warning,
    }

    public sealed class InputIssue
    {
        public InputIssue(WindInputField field, InputSeverity severity, string message)
        {
            Field = field;
            Severity = severity;
            Message = message;
        }

        public WindInputField Field { get; }
        public InputSeverity Severity { get; }
        public string Message { get; }

        public override string ToString() => $"{Field} {Severity}: {Message}";
    }

    /// <summary>Plain-language checks on the wind inputs, tied to the field they belong to.</summary>
    public static class WindInputValidator
    {
        /// <summary>Lowest and highest basic wind speeds on the ASCE 7-16/7-22 maps, roughly (mph).</summary>
        public const double MapSpeedLow = 85, MapSpeedHigh = 250;

        /// <summary>Upper limit of the Chapter 30 Part 1 cladding method on mean roof height (ft).</summary>
        public const double CladdingHeightLimit = 60;

        /// <summary>Steepest roof covered by the Chapter 30 cladding figures (degrees).</summary>
        public const double CladdingSlopeLimit = 45;

        public static List<InputIssue> Validate(WindParameters_Base parameters, BuildingData building)
        {
            var issues = new List<InputIssue>();
            void Add(WindInputField f, InputSeverity s, string m) => issues.Add(new InputIssue(f, s, m));

            if (parameters != null)
            {
                double v = parameters.WindSpeed;
                if (!(v > 0))
                    Add(WindInputField.WindSpeed, InputSeverity.Error, "Enter the basic wind speed V from the map.");
                else if (v < MapSpeedLow || v > MapSpeedHigh)
                    Add(WindInputField.WindSpeed, InputSeverity.Warning,
                        $"That is outside the ASCE 7 map range (about {MapSpeedLow:0} to {MapSpeedHigh:0} mph). Check the value.");

                double kzt = parameters.Kzt;
                if (!(kzt > 0))
                    Add(WindInputField.Kzt, InputSeverity.Error, "Kzt must be greater than zero (1.0 on flat ground).");
                else if (kzt < 1.0)
                    Add(WindInputField.Kzt, InputSeverity.Warning, "Kzt is 1.0 or more (Section 26.8). Check the value.");
            }

            if (building != null)
            {
                bool sizeOk = true;
                if (!(building.BuildingLength > 0)) { sizeOk = false; Add(WindInputField.Length, InputSeverity.Error, "Length must be greater than zero."); }
                if (!(building.BuildingWidth > 0)) { sizeOk = false; Add(WindInputField.Width, InputSeverity.Error, "Width must be greater than zero."); }
                if (!(building.BuildingHeight > 0)) { sizeOk = false; Add(WindInputField.WallHeight, InputSeverity.Error, "Wall height must be greater than zero."); }

                bool pitchOk = true;
                if (building.RoofTypeIsSloped())
                {
                    double p = building.RoofPitch;
                    if (!(p > 0 && p < 90))
                    {
                        pitchOk = false;
                        Add(WindInputField.RoofPitch, InputSeverity.Error, p == 0
                            ? "A 0° slope is a flat roof: choose Flat, or enter a slope."
                            : "Enter a pitch between 0 and 90 degrees.");
                    }
                    else if (p > CladdingSlopeLimit)
                        Add(WindInputField.RoofPitch, InputSeverity.Warning,
                            $"Roofs steeper than {CladdingSlopeLimit:0} degrees are outside the cladding figures; cladding results are not available.");
                }

                if (sizeOk && pitchOk && building.MeanRoofHeight > CladdingHeightLimit)
                    Add(WindInputField.Building, InputSeverity.Warning,
                        $"Mean roof height is {building.MeanRoofHeight:0.#} ft. The cladding method here covers {CladdingHeightLimit:0} ft or less, " +
                        "so only the whole-building (shear wall) results are available.");
            }
            return issues;
        }

        public static bool HasErrors(IEnumerable<InputIssue> issues) => issues.Any(i => i.Severity == InputSeverity.Error);

        /// <summary>The roof pitch as rise per 12 in. of run, e.g. "4.8:12".</summary>
        public static string RisePer12(double pitchDegrees) =>
            pitchDegrees > 0 && pitchDegrees < 90 ? (12 * Math.Tan(pitchDegrees * Math.PI / 180)).ToString("0.0") + ":12" : "-";
    }

    /// <summary>One line of the results checklist.</summary>
    public sealed class SummaryCheck
    {
        public SummaryCheck(bool ok, string text) { Ok = ok; Text = text; }
        public bool Ok { get; }
        public string Text { get; }
    }

    /// <summary>The headline numbers a non-engineer needs from a wind calculation.</summary>
    public sealed class WindSummary
    {
        /// <summary>Strongest cladding suction on the roof (psf, negative), or null when cladding is not available.</summary>
        public double? RoofUpliftPsf { get; private set; }
        public string RoofUpliftZone { get; private set; }

        /// <summary>Strongest cladding suction on a wall (psf, negative), or null.</summary>
        public double? WallSuctionPsf { get; private set; }
        public string WallSuctionZone { get; private set; }

        /// <summary>Strongest inward cladding pressure (psf, positive), or null.</summary>
        public double? InwardPsf { get; private set; }

        /// <summary>Whole-building force with the wind along the length (from the west).</summary>
        public DirectionalWindForce FromWest { get; private set; }

        /// <summary>Whole-building force with the wind along the width (from the south).</summary>
        public DirectionalWindForce FromSouth { get; private set; }

        public WindLateralResult Lateral { get; private set; }

        public List<SummaryCheck> Checks { get; } = new List<SummaryCheck>();

        public static WindSummary Compute(WindAnalysisResult analysis, BuildingData building, bool onlyCases1And3)
        {
            if (analysis == null) throw new ArgumentNullException(nameof(analysis));
            if (building == null) throw new ArgumentNullException(nameof(building));
            var s = new WindSummary();

            var messages = new List<string>();
            if (analysis.Cladding != null && analysis.Cladding.IsValid)
            {
                var suction = WindSceneBuilder.Build(building, analysis.Cladding, analysis.MwfrsAlongLength, analysis.MwfrsAlongWidth, WindViewMode.CladdingSuction);
                var roof = Strongest(suction, SurfaceKind.Roof, negative: true);
                var wall = Strongest(suction, SurfaceKind.Wall, negative: true);
                if (roof != null) { s.RoofUpliftPsf = roof.PressurePsf; s.RoofUpliftZone = roof.Description; }
                if (wall != null) { s.WallSuctionPsf = wall.PressurePsf; s.WallSuctionZone = wall.Description; }

                var inward = WindSceneBuilder.Build(building, analysis.Cladding, analysis.MwfrsAlongLength, analysis.MwfrsAlongWidth, WindViewMode.CladdingPressure);
                s.InwardPsf = inward.StrongestPressure?.PressurePsf;
            }
            if (analysis.Cladding != null) messages.AddRange(analysis.Cladding.ValidationMessages);

            if (analysis.MwfrsAlongLength != null && analysis.MwfrsAlongWidth != null)
            {
                s.Lateral = WindLateralLoads.Compute(analysis, onlyCases1And3);
                s.FromWest = s.Lateral.AlongLength;
                s.FromSouth = s.Lateral.AlongWidth;
            }

            foreach (var m in messages.Distinct())
                s.Checks.Add(new SummaryCheck(false, m));

            if (s.RoofUpliftPsf != null)
                s.Checks.Add(new SummaryCheck(true,
                    $"Mean roof height {building.MeanRoofHeight:0.#} ft is within the {WindInputValidator.CladdingHeightLimit:0} ft cladding limit."));

            foreach (var f in new[] { s.FromWest, s.FromSouth }.Where(f => f != null))
            {
                string from = f == s.FromWest ? "west" : "south";
                string sec = s.Lateral?.Procedure == LateralProcedure.Envelope ? analysis?.MwfrsAlongLength?.Edition.EnvelopeMinimumSection ?? "28.3" : "27.1.5";
                // the minimum is a separate load case in addition to the others (C27.1.5, C28); it does not raise them
                s.Checks.Add(f.MinimumGoverns
                    ? new SummaryCheck(false, $"Wind from the {from}: the 16/8 psf minimum case ({sec}) exceeds the calculated wind.")
                    : new SummaryCheck(true, $"Wind from the {from}: calculated wind exceeds the 16/8 psf minimum case ({sec})."));
            }
            return s;
        }

        private static SceneZone Strongest(WindScene scene, SurfaceKind kind, bool negative)
        {
            var zones = scene.Zones.Where(z => z.HasPressure && z.Kind == kind);
            return negative
                ? zones.Where(z => z.PressurePsf < 0).OrderBy(z => z.PressurePsf).FirstOrDefault()
                : zones.Where(z => z.PressurePsf > 0).OrderByDescending(z => z.PressurePsf).FirstOrDefault();
        }
    }
}
