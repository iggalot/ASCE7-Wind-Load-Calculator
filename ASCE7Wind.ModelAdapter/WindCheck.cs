using System;
using System.Collections.Generic;
using System.Linq;
using BuildingModel.Model;

namespace ASCE7WindLoadCalculator.ModelAdapter
{
    /// <summary>The wind calculated for a project as it is now (<see cref="WindCheck.Run"/>).</summary>
    public sealed class WindCheckResult
    {
        /// <summary>Why there is no wind to check (no Wind section yet), or null.</summary>
        public string NotChecked { get; set; }
        public WindSection Section { get; set; }
        /// <summary>The engine's building from the model (null when the model gives none).</summary>
        public WindModelInput Input { get; set; }
        /// <summary>The wind calculator's own checks of its inputs against this building.</summary>
        public IReadOnlyList<InputIssue> Issues { get; set; } = new List<InputIssue>();
        /// <summary>The analysis (null when it cannot be run).</summary>
        public WindAnalysisResult Analysis { get; set; }
        /// <summary>The wind cases, each once, as the wind calculator makes them.</summary>
        public List<WindCase> Cases { get; set; } = new List<WindCase>();
        /// <summary>What stops the wind being calculated for this building, in a sentence each (empty: it is).</summary>
        public List<string> Problems { get; } = new List<string>();

        public bool Calculated => NotChecked == null && Problems.Count == 0 && Analysis != null;

        /// <summary>The largest base shear of any case along X and Y (kips).</summary>
        public double MaxVx => Cases.Count == 0 ? 0 : Cases.Max(c => Math.Abs(c.Vx));
        public double MaxVy => Cases.Count == 0 ? 0 : Cases.Max(c => Math.Abs(c.Vy));
    }

    /// <summary>
    /// The wind calculator's work with no window: its inputs (the project's Wind section) on the building as the model has it
    /// now, checked as its own screen checks them (<see cref="WindInputValidator"/>) and run (<see cref="WindAnalysis.Run"/>).
    /// Every app that needs the wind (the shear walls, the loads, the hub) takes it from here, so the wind is found one way.
    /// Only reads.
    /// </summary>
    public static class WindCheck
    {
        public static WindCheckResult Run(Building b)
        {
            var r = new WindCheckResult();
            r.Section = b == null ? null : WindSection.Read(b);
            if (r.Section == null) { r.NotChecked = "no wind set up"; return r; }
            try
            {
                r.Input = r.Section.BuildingFor(b);
            }
            catch (InvalidOperationException ex)
            {
                r.Problems.Add("The building gives no wind geometry: " + ex.Message);
                return r;
            }
            var p = r.Section.ToParameters();
            r.Issues = WindInputValidator.Validate(p, r.Input.Building);
            r.Problems.AddRange(r.Issues.Where(i => i.Severity == InputSeverity.Error).Select(i => i.Message));
            if (r.Problems.Count > 0) return r;
            try
            {
                r.Analysis = WindAnalysis.Run(r.Section.Edition, p, r.Input.Building);
                r.Cases = WindCases.For(r.Analysis, r.Section.OnlyCases1And3);
            }
            catch (Exception ex) when (ex is InvalidOperationException || ex is ArgumentException || ex is NotSupportedException ||
                                       ex is NotImplementedException || ex is NullReferenceException || ex is IndexOutOfRangeException)
            {
                r.Analysis = null;
                r.Problems.Add("The wind could not be calculated for this building: " + ex.Message);
            }
            return r;
        }
    }
}
