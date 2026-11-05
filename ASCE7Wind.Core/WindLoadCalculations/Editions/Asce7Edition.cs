using System;

namespace ASCE7WindLoadCalculator
{
    /// <summary>
    /// Everything that differs between ASCE 7 editions, expressed as data plus a few factory hooks.
    /// Calculators are edition-agnostic and ask the edition for constants, coefficient figures and
    /// zone layouts. Add a new edition by deriving from this class; no calculator changes needed.
    /// </summary>
    public abstract class Asce7Edition
    {
        public static Asce7Edition ASCE7_16 { get; } = new Asce7Edition16();
        public static Asce7Edition ASCE7_22 { get; } = new Asce7Edition22();

        public static Asce7Edition For(ASCE7_Versions version)
        {
            switch (version)
            {
                case ASCE7_Versions.ASCE_VER_7_16: return ASCE7_16;
                case ASCE7_Versions.ASCE_VER_7_22: return ASCE7_22;
                default: throw new ArgumentOutOfRangeException(nameof(version), version, "Unsupported ASCE 7 edition.");
            }
        }

        public abstract ASCE7_Versions Version { get; }

        /// <summary>Display name, e.g. "ASCE 7-16".</summary>
        public abstract string Name { get; }

        /// <summary>
        /// True when the directionality factor Kd is part of the velocity pressure q (ASCE 7-16 Eq. 26.10-1).
        /// False when Kd is applied in the pressure equations instead (ASCE 7-22 Eq. 26.10-1, 27.3-1, 30.3-1).
        /// Final design pressures are identical either way; only the reported q differs.
        /// </summary>
        public abstract bool KdInVelocityPressure { get; }

        /// <summary>
        /// Fig. 27.3-8 note 2 (new in ASCE 7-22, C27.3.5 and Fig. C27.3-4): in Cases 3 and 4 each roof area takes 100% of the larger
        /// of the two principal-axis roof pressures of Case 1 (Case 3) or Case 2 (Case 4). False for 7-16, where Case 3 is "Case 1
        /// acting simultaneously at 75%" on every surface, roof included.
        /// </summary>
        public abstract bool QuarteringRoofTakesLarger { get; }

        /// <summary>
        /// Chapter 28 torsional load cases: true when the T zones take the printed Fig. 28.3-2 coefficients (ASCE 7-22), false when
        /// they are 25% of the full design pressures (ASCE 7-16 Fig. 28.3-1 note 5).
        /// </summary>
        public abstract bool PrintedTorsionalTable { get; }

        /// <summary>Section of the Chapter 28 minimum design wind loads: 28.3.4 (7-16), 28.3.6 (7-22).</summary>
        public abstract string EnvelopeMinimumSection { get; }

        /// <summary>Section of the Chapter 28 parapet loads: 28.3.2 (7-16), 28.3.4 (7-22).</summary>
        public abstract string EnvelopeParapetSection { get; }

        /// <summary>Commentary figure for hip roofs under Chapter 28: Fig. C28.3-2 (7-16), Fig. C28.3-3 (7-22).</summary>
        public abstract string EnvelopeHipFigure { get; }

        /// <summary>Where Chapter 28 puts each corner in turn as the windward corner: Fig. 28.3-1 note 3 (7-16), 28.3.2.1 (7-22).</summary>
        public abstract string EnvelopeCornerRule { get; }

        /// <summary>Where Chapter 28 lets the torsional load cases be skipped: Fig. 28.3-1 note 5 exception (7-16), 28.3.2 exception (7-22).</summary>
        public abstract string EnvelopeTorsionExemption { get; }

        /// <summary>Where the negative zone 2/2E coefficient is limited to min(0.5 L, 2.5 he): Fig. 28.3-1 note 8 (7-16), 28.3.2.1 (7-22).</summary>
        public abstract string EnvelopeZone2Reach { get; }

        /// <summary>Leading coefficient of the Kz power law, Kz = c (z/zg)^(2/alpha).</summary>
        public abstract double KzCoefficient { get; }

        /// <summary>Terrain exposure constants alpha and zg (ft).</summary>
        public abstract (double alpha, double zg) TerrainConstants(WindExposureCategories exposure);

        /// <summary>Chapter 30 roof GCp figure for the building.</summary>
        public abstract Chapter27and30_GCpCurveBase CreateCCRoofFigure(BuildingData building);

        /// <summary>Chapter 30 wall GCp figure (Fig. 30.3-1).</summary>
        public abstract Chapter27and30_GCpCurveBase CreateCCWallFigure();

        /// <summary>Chapter 30 roof zone layout for the building.</summary>
        public abstract AreaCalculator_Base CreateCCRoofZones(BuildingData building);

        /// <summary>Chapter 30 wall zone layout.</summary>
        public abstract AreaCalculator_Base CreateCCWallZones(BuildingData building, bool isGableEndWall, double wallLength);

        public override string ToString() => Name;
    }

    internal sealed class Asce7Edition16 : Asce7Edition
    {
        public override ASCE7_Versions Version => ASCE7_Versions.ASCE_VER_7_16;
        public override string Name => "ASCE 7-16";
        public override bool KdInVelocityPressure => true;
        public override double KzCoefficient => 2.01;
        public override bool QuarteringRoofTakesLarger => false;
        public override bool PrintedTorsionalTable => false;
        public override string EnvelopeMinimumSection => "28.3.4";
        public override string EnvelopeParapetSection => "28.3.2";
        public override string EnvelopeHipFigure => "Fig. C28.3-2";
        public override string EnvelopeCornerRule => "Fig. 28.3-1 note 3";
        public override string EnvelopeTorsionExemption => "Fig. 28.3-1 note 5 exception";
        public override string EnvelopeZone2Reach => "Fig. 28.3-1 note 8";

        // ASCE 7-16 Table 26.11-1
        public override (double alpha, double zg) TerrainConstants(WindExposureCategories exposure)
        {
            switch (exposure)
            {
                case WindExposureCategories.WIND_EXP_CAT_B: return (7.0, 1200.0);
                case WindExposureCategories.WIND_EXP_CAT_D: return (11.5, 700.0);
                default: return (9.5, 900.0);
            }
        }

        public override Chapter27and30_GCpCurveBase CreateCCRoofFigure(BuildingData building) =>
            Chapter30RoofFigureFactory_ASCE7_16.CreateRoofFigure_ASCE7_16(building);

        public override Chapter27and30_GCpCurveBase CreateCCWallFigure() => new Figure30_3_1_ASCE7_16();

        public override AreaCalculator_Base CreateCCRoofZones(BuildingData b)
        {
            switch (b.RoofType)
            {
                case RoofTypes.ROOF_TYPE_FLAT: return new FlatRoofAreaCalculator_CC(b);
                case RoofTypes.ROOF_TYPE_GABLE: return b.RoofPitch <= 7 ? (AreaCalculator_Base)new FlatRoofAreaCalculator_CC(b) : new GableRoofAreaCalculator_CC_ASCE7_16(b);
                case RoofTypes.ROOF_TYPE_HIP: return b.RoofPitch <= 7 ? (AreaCalculator_Base)new FlatRoofAreaCalculator_CC(b) : new HipRoofAreaCalculator_CC_ASCE7_16(b);
                default: throw new NotSupportedException("Unsupported roof type: " + b.RoofType);
            }
        }

        // Fig. 30.3-1 zones cover the whole wall surface, including the gable triangle on gable ends.
        public override AreaCalculator_Base CreateCCWallZones(BuildingData b, bool isGableEndWall, double wallLength) =>
            b.RoofType == RoofTypes.ROOF_TYPE_GABLE && isGableEndWall
                ? (AreaCalculator_Base)new WallAreaCalculator_GableEnd_CC(b, wallLength)
                : new WallAreaCalculator_NonGableEnd_CC(b, wallLength);
    }

    internal sealed class Asce7Edition22 : Asce7Edition
    {
        public override ASCE7_Versions Version => ASCE7_Versions.ASCE_VER_7_22;
        public override string Name => "ASCE 7-22";
        public override bool KdInVelocityPressure => false;
        public override double KzCoefficient => 2.41;
        public override bool QuarteringRoofTakesLarger => true;
        public override bool PrintedTorsionalTable => true;
        public override string EnvelopeMinimumSection => "28.3.6";
        public override string EnvelopeParapetSection => "28.3.4";
        public override string EnvelopeHipFigure => "Fig. C28.3-3";
        public override string EnvelopeCornerRule => "28.3.2.1";
        public override string EnvelopeTorsionExemption => "28.3.2 exception";
        public override string EnvelopeZone2Reach => "28.3.2.1";

        // ASCE 7-22 Table 26.11-1 (revised from 7-16: gradient heights and alpha re-derived; with the
        // 2.41 coefficient this reproduces the tabulated Kz at 15 ft: 0.57 / 0.85 / 1.03).
        public override (double alpha, double zg) TerrainConstants(WindExposureCategories exposure)
        {
            switch (exposure)
            {
                case WindExposureCategories.WIND_EXP_CAT_B: return (7.5, 3280.0);
                case WindExposureCategories.WIND_EXP_CAT_D: return (11.5, 1935.0);
                default: return (9.8, 2460.0);
            }
        }

        public override Chapter27and30_GCpCurveBase CreateCCRoofFigure(BuildingData building) =>
            Chapter30RoofFigureFactory_ASCE7_22.CreateRoofFigure_ASCE7_22(building);

        public override Chapter27and30_GCpCurveBase CreateCCWallFigure() => new Figure30_3_1_ASCE7_22();

        public override AreaCalculator_Base CreateCCRoofZones(BuildingData b)
        {
            switch (b.RoofType)
            {
                case RoofTypes.ROOF_TYPE_FLAT: return new FlatRoofAreaCalculator_CC(b);
                case RoofTypes.ROOF_TYPE_GABLE: return b.RoofPitch <= 7 ? (AreaCalculator_Base)new FlatRoofAreaCalculator_CC(b)
                    : b.RoofPitch > 27 ? new GableRoof2DAreaCalculator_CC_ASCE7_22(b)   // Fig. 30.3-2D layout (27° to 45°)
                    : new GableRoofAreaCalculator_CC_ASCE7_22(b);
                case RoofTypes.ROOF_TYPE_HIP: return b.RoofPitch <= 7 ? (AreaCalculator_Base)new FlatRoofAreaCalculator_CC(b) : new HipRoofAreaCalculator_CC_ASCE7_22(b);
                default: throw new NotSupportedException("Unsupported roof type: " + b.RoofType);
            }
        }

        public override AreaCalculator_Base CreateCCWallZones(BuildingData b, bool isGableEndWall, double wallLength) =>
            b.RoofType == RoofTypes.ROOF_TYPE_GABLE && isGableEndWall
                ? (AreaCalculator_Base)new WallAreaCalculator_GableEnd_CC(b, wallLength)
                : new WallAreaCalculator_NonGableEnd_CC(b, wallLength);
    }
}
