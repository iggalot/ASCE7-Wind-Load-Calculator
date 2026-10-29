using System;
using System.Collections.Generic;
using System.Linq;
using ASCE7WindLoadCalculator;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ASCE7Wind.Tests
{
    /// <summary>
    /// No realistic building may crash the engine: out-of-range inputs produce validation messages, never
    /// exceptions. Sweeps plan sizes, heights, slopes, roof types, ridge directions, editions and analyses,
    /// including the 90-degree-rotated building used for the second wind direction.
    /// </summary>
    [TestClass]
    public class RobustnessTests
    {
        [TestMethod]
        public void NoBuilding_ThrowsDuringAnalysis()
        {
            var failures = new SortedSet<string>();
            var dims = new[] { 6.0, 10.0, 20.0, 30.0, 40.0, 60.0, 100.0, 200.0 };
            var heights = new[] { 4.0, 8.0, 15.0, 30.0, 70.0 };
            var pitches = new[] { 0.0, 3.0, 7.0, 10.0, 20.0, 27.0, 35.0, 45.0, 60.0 };

            foreach (var L in dims)
                foreach (var W in dims)
                    foreach (var H in heights)
                        foreach (var roof in new[] { RoofTypes.ROOF_TYPE_FLAT, RoofTypes.ROOF_TYPE_GABLE, RoofTypes.ROOF_TYPE_HIP })
                            foreach (var pitch in roof == RoofTypes.ROOF_TYPE_FLAT ? new[] { 0.0 } : pitches)
                                foreach (var ridge in roof == RoofTypes.ROOF_TYPE_GABLE
                                    ? new[] { RidgeDirections.RIDGE_DIR_PARALLEL_TO_BLDGLENGTH, RidgeDirections.RIDGE_DIR_PERP_TO_BLDGLENGTH }
                                    : new[] { RidgeDirections.RIDGE_DIR_NONE })
                                {
                                    var b = Bldg.Make(L, W, H, roof, pitch, ridge);
                                    var rotated = b.Clone(); rotated.RotateBuilding();
                                    foreach (var v in new[] { ASCE7_Versions.ASCE_VER_7_16, ASCE7_Versions.ASCE_VER_7_22 })
                                        foreach (var t in new[] { WindLoadCalculationTypes.COMPONENT_AND_CLADDING, WindLoadCalculationTypes.MWFRS })
                                            foreach (var bb in new[] { b, rotated })
                                            {
                                                try { Bldg.Run(v, t, bb); }
                                                catch (Exception ex)
                                                {
                                                    var site = ex.StackTrace?.Split('\n').FirstOrDefault(l => l.Contains("ASCE7WindLoadCalculator"))?.Trim();
                                                    failures.Add($"{ex.GetType().Name} {t} {bb.RoofType} {bb.RidgeDirection} L{bb.BuildingLength} W{bb.BuildingWidth} H{bb.BuildingHeight} p{pitch} | {site}");
                                                }
                                            }
                                }

            if (failures.Count > 0)
                Assert.Fail($"{failures.Count} crashing case(s), e.g.:\n" + string.Join("\n", failures.Take(30)));
        }
    }
}
