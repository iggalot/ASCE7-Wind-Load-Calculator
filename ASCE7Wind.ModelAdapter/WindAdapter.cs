using System;
using System.Collections.Generic;
using System.Linq;
using BuildingModel.Model;

namespace ASCE7WindLoadCalculator.ModelAdapter
{
    /// <summary>
    /// The wind inputs the building model does not hold (they are about the wind, not the building): the enclosure of the
    /// building and of the parapet. Everything about the building, the roof included, comes from the model.
    /// </summary>
    public sealed class WindOptions
    {
        public BuildingEnclosures Enclosure { get; set; } = BuildingEnclosures.BLDG_ENCLOSED;
        public BuildingEnclosures ParapetEnclosure { get; set; } = BuildingEnclosures.BLDG_ENCLOSED;
    }

    /// <summary>The model as the wind engine sees it: the engine's building, the envelope it came from, and what was assumed.</summary>
    public sealed class WindModelInput
    {
        /// <summary>For the wind engine: L, B, stories and plate heights, the roof.</summary>
        public BuildingData Building { get; set; }

        /// <summary>The model's envelope: L × B, the levels and each face's share per level.</summary>
        public BuildingEnvelope Envelope { get; set; }

        /// <summary>What the conversion assumed or could not carry over (the envelope's notes and the wind's own).</summary>
        public List<string> Notes { get; set; } = new List<string>();
    }

    /// <summary>
    /// Building model → wind engine. L and B are the model's enclosing rectangle (L W-E, B S-N), the stories and plate
    /// heights are its levels, and the roof (flat / gable / hip, slope, ridge, parapet, eaves) is the model's roof. Only
    /// the enclosure is the wind app's own input.
    /// </summary>
    public static class WindAdapter
    {
        public static WindModelInput From(Building b, WindOptions options = null)
        {
            if (b == null) throw new ArgumentNullException(nameof(b));
            options = options ?? new WindOptions();
            var env = b.Envelope();
            var input = new WindModelInput { Envelope = env };
            input.Notes.AddRange(env.Notes);

            var roof = b.Roof ?? b.Defaults.NewRoof();
            var data = new BuildingData
            {
                BuildingLength = env.Length,
                BuildingWidth = env.Width,
                RoofType = RoofType(roof.Shape),
                RoofPitch = roof.Pitch,
                OverhangWidth = roof.Overhang,
                ParapetHeight = roof.Parapet,
                ParapetEnclosure = options.ParapetEnclosure,
                EnclosureType = options.Enclosure,
            };
            data.RidgeDirection = Ridge(roof);

            var stories = env.StoryHeights();
            if (stories.Count <= BuildingData.MaxStories)
            {
                data.NumberOfStories = stories.Count;
                data.PlateHeights = stories;
            }
            else
            {
                data.BuildingHeight = env.EaveHeight;
                input.Notes.Add($"{stories.Count} diaphragm levels: the wind engine splits up to {BuildingData.MaxStories}; use the face strips for the levels.");
            }
            input.Building = data;
            return input;
        }

        public static RoofTypes RoofType(RoofShape s) =>
            s == RoofShape.Gable ? RoofTypes.ROOF_TYPE_GABLE : s == RoofShape.Hip ? RoofTypes.ROOF_TYPE_HIP : RoofTypes.ROOF_TYPE_FLAT;

        /// <summary>
        /// The ridge relative to L (the W-E side): a gable whose ridge was chosen W-E runs along L, S-N across it. Left on
        /// Auto, and for a hip, the engine takes the longer side, as the model does.
        /// </summary>
        public static RidgeDirections Ridge(Roof r)
        {
            if (r.Shape != RoofShape.Gable || r.Ridge == RidgeAxis.Auto) return RidgeDirections.RIDGE_DIR_NONE;
            return r.Ridge == RidgeAxis.WestEast ? RidgeDirections.RIDGE_DIR_PARALLEL_TO_BLDGLENGTH : RidgeDirections.RIDGE_DIR_PERP_TO_BLDGLENGTH;
        }
    }
}
