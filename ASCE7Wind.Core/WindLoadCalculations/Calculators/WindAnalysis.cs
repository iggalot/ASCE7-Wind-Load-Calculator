using System;

namespace ASCE7WindLoadCalculator
{
    /// <summary>The three calculations a building needs: cladding, and whole-building wind in each plan direction.</summary>
    public sealed class WindAnalysisResult
    {
        internal WindAnalysisResult() { }

        /// <summary>
        /// Wraps calculators produced elsewhere (e.g. by the wind input screen). A roof of several sections also needs the winds
        /// from the east and the north on the building seen from there: worked out here from the two given (their edition,
        /// inputs and building), as <see cref="WindAnalysis.Run"/> does, so no caller can leave them out (the 3D view did: its
        /// winds from the east and north were the west's and south's mirrored, their roof zones on the wrong faces).
        /// </summary>
        public WindAnalysisResult(WindLoadCalculator_Base cladding, WindLoadCalculator_Base mwfrsAlongLength, WindLoadCalculator_Base mwfrsAlongWidth)
        {
            Cladding = cladding;
            MwfrsAlongLength = mwfrsAlongLength;
            MwfrsAlongWidth = mwfrsAlongWidth;
            if (mwfrsAlongLength?.buildingData?.HasRoofFaces == true && mwfrsAlongLength.Parameters != null
                && mwfrsAlongWidth?.buildingData != null && mwfrsAlongWidth.Parameters != null)
            {
                var east = mwfrsAlongLength.buildingData.Clone(); east.MirrorX();
                var north = mwfrsAlongWidth.buildingData.Clone(); north.MirrorX();
                MwfrsFromEast = WindAnalysis.Mirrored(mwfrsAlongLength, east);
                MwfrsFromNorth = WindAnalysis.Mirrored(mwfrsAlongWidth, north);
            }
        }

        public WindLoadCalculator_Base Cladding { get; internal set; }

        /// <summary>MWFRS with the wind along the BuildingLength.</summary>
        public WindLoadCalculator_Base MwfrsAlongLength { get; internal set; }

        /// <summary>MWFRS with the wind along the BuildingWidth (analysed on the building turned 90 degrees).</summary>
        public WindLoadCalculator_Base MwfrsAlongWidth { get; internal set; }

        /// <summary>
        /// A roof of several sections only (owner 2026-11-13; null otherwise): the wind from the east (toward −x) and from the
        /// north (toward −y), each on the building seen from that side (<see cref="BuildingData.MirrorX"/>): a wing on one side
        /// meets each wind its own way, so the far side is not the near side mirrored.
        /// </summary>
        public WindLoadCalculator_Base MwfrsFromEast { get; internal set; }
        public WindLoadCalculator_Base MwfrsFromNorth { get; internal set; }
    }

    /// <summary>Single entry point that runs every wind calculation for a building.</summary>
    public static class WindAnalysis
    {
        /// <exception cref="ArgumentException">The building dimensions are not usable (zero or negative).</exception>
        public static WindAnalysisResult Run(ASCE7_Versions version, WindParameters_Base parameters, BuildingData building)
        {
            if (building == null) throw new ArgumentNullException(nameof(building));
            if (!(building.BuildingLength > 0) || !(building.BuildingWidth > 0) || !(building.BuildingHeight > 0))
                throw new ArgumentException("Building length, width and wall height must all be greater than zero.");
            if (building.RoofTypeIsSloped() && !(building.RoofPitch > 0 && building.RoofPitch < 90))
                throw new ArgumentException("Roof pitch must be between 0 and 90 degrees.");

            parameters = parameters ?? new WindParameters_Base();
            var rotated = building.Clone();
            rotated.RotateBuilding();

            var result = new WindAnalysisResult
            {
                Cladding = Calculate(version, WindLoadCalculationTypes.COMPONENT_AND_CLADDING, parameters, building),
                MwfrsAlongLength = Calculate(version, WindLoadCalculationTypes.MWFRS, parameters, building),
                MwfrsAlongWidth = Calculate(version, WindLoadCalculationTypes.MWFRS, parameters, rotated),
            };
            if (building.HasRoofFaces)
            {
                var east = building.Clone(); east.MirrorX();
                var north = rotated.Clone(); north.MirrorX();
                result.MwfrsFromEast = Calculate(version, WindLoadCalculationTypes.MWFRS, parameters, east);
                result.MwfrsFromNorth = Calculate(version, WindLoadCalculationTypes.MWFRS, parameters, north);
            }
            return result;
        }

        /// <summary>The same whole-building calculation (edition, inputs) on another view of its building.</summary>
        internal static WindLoadCalculator_Base Mirrored(WindLoadCalculator_Base calc, BuildingData building) =>
            Calculate(calc.ASCEVersion, WindLoadCalculationTypes.MWFRS, calc.Parameters, building);

        private static WindLoadCalculator_Base Calculate(ASCE7_Versions version, WindLoadCalculationTypes type, WindParameters_Base parameters, BuildingData building)
        {
            var p = parameters.Clone();
            p.AnalysisType = type;
            var calc = WindLoadCalculatorFactory.Create(version, type, p, building);
            calc.Initialize();
            return calc;
        }
    }
}
