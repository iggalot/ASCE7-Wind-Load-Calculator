using System;
using System.Collections.Generic;
using System.Linq;
using ASCE7WindLoadCalculator;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ASCE7Wind.Tests
{
    internal static class Bldg
    {
        public static BuildingData Make(
            double L, double W, double H,
            RoofTypes roof = RoofTypes.ROOF_TYPE_FLAT,
            double pitch = 0,
            RidgeDirections ridge = RidgeDirections.RIDGE_DIR_NONE,
            BuildingEnclosures enclosure = BuildingEnclosures.BLDG_ENCLOSED)
        {
            return new BuildingData
            {
                BuildingLength = L,
                BuildingWidth = W,
                BuildingHeight = H,
                RoofType = roof,
                RoofPitch = pitch,
                RidgeDirection = ridge,
                EnclosureType = enclosure,
            };
        }

        public static WindParameters_Base Params(
            WindLoadCalculationTypes type,
            WindExposureCategories exposure = WindExposureCategories.WIND_EXP_CAT_C,
            double V = 115, double kzt = 1.0)
        {
            return new WindParameters_Base { WindSpeed = V, ExposureCategory = exposure, Kzt = kzt, AnalysisType = type };
        }

        public static WindLoadCalculator_Base Run(ASCE7_Versions ver, WindLoadCalculationTypes type, BuildingData b,
            WindExposureCategories exposure = WindExposureCategories.WIND_EXP_CAT_C, double V = 115)
        {
            var calc = WindLoadCalculatorFactory.Create(ver, type, Params(type, exposure, V), b);
            calc.Initialize();
            return calc;
        }

        public static double SumArea(AreaCalculator_Base zones) => zones.effWindAreas.Values.Sum(z => z.Area);

        public static IEnumerable<EffectiveWindArea> Zones(AreaCalculator_Base zones, string label) =>
            zones.effWindAreas.Values.Where(z => z.Label_Full == label);
    }

    internal static class Tol
    {
        /// <summary>Tolerance for values read from ASCE 7 tables printed to two decimals.</summary>
        public const double Table = 0.01;
        public const double Tight = 1e-9;
    }
}
