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
        public abstract AreaCalculator_Base CreateCCWallZones(BuildingData building, bool isGableEndWall);

        public override string ToString() => Name;
    }

    internal sealed class Asce7Edition16 : Asce7Edition
    {
        public override ASCE7_Versions Version => ASCE7_Versions.ASCE_VER_7_16;
        public override string Name => "ASCE 7-16";
        public override bool KdInVelocityPressure => true;
        public override double KzCoefficient => 2.01;

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
                case RoofTypes.ROOF_TYPE_GABLE: return b.RoofPitch < 7 ? (AreaCalculator_Base)new FlatRoofAreaCalculator_CC(b) : new GableRoofAreaCalculator_CC_ASCE7_16(b);
                case RoofTypes.ROOF_TYPE_HIP: return b.RoofPitch < 7 ? (AreaCalculator_Base)new FlatRoofAreaCalculator_CC(b) : new HipRoofAreaCalculator_CC_ASCE7_16(b);
                default: throw new NotSupportedException("Unsupported roof type: " + b.RoofType);
            }
        }

        // ASCE 7-16 Fig. 30.3-1 zoning does not distinguish gable-end walls.
        public override AreaCalculator_Base CreateCCWallZones(BuildingData b, bool isGableEndWall) =>
            new WallAreaCalculator_NonGableEnd_CC(b);
    }

    internal sealed class Asce7Edition22 : Asce7Edition
    {
        public override ASCE7_Versions Version => ASCE7_Versions.ASCE_VER_7_22;
        public override string Name => "ASCE 7-22";
        public override bool KdInVelocityPressure => false;
        public override double KzCoefficient => 2.01;

        public override (double alpha, double zg) TerrainConstants(WindExposureCategories exposure) =>
            ASCE7_16.TerrainConstants(exposure);

        public override Chapter27and30_GCpCurveBase CreateCCRoofFigure(BuildingData building) =>
            Chapter30RoofFigureFactory_ASCE7_22.CreateRoofFigure_ASCE7_22(building);

        public override Chapter27and30_GCpCurveBase CreateCCWallFigure() => new Figure30_3_1_ASCE7_22();

        public override AreaCalculator_Base CreateCCRoofZones(BuildingData b)
        {
            switch (b.RoofType)
            {
                case RoofTypes.ROOF_TYPE_FLAT: return new FlatRoofAreaCalculator_CC(b);
                case RoofTypes.ROOF_TYPE_GABLE: return b.RoofPitch < 7 ? (AreaCalculator_Base)new FlatRoofAreaCalculator_CC(b) : new GableRoofAreaCalculator_CC_ASCE7_22(b);
                case RoofTypes.ROOF_TYPE_HIP: return b.RoofPitch < 7 ? (AreaCalculator_Base)new FlatRoofAreaCalculator_CC(b) : new HipRoofAreaCalculator_CC_ASCE7_22(b);
                default: throw new NotSupportedException("Unsupported roof type: " + b.RoofType);
            }
        }

        public override AreaCalculator_Base CreateCCWallZones(BuildingData b, bool isGableEndWall) =>
            b.RoofType == RoofTypes.ROOF_TYPE_GABLE && isGableEndWall
                ? (AreaCalculator_Base)new WallAreaCalculator_GableEnd_CC(b)
                : new WallAreaCalculator_NonGableEnd_CC(b);
    }
}
