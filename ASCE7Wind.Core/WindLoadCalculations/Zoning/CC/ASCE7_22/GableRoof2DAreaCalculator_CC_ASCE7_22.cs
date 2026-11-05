using System.Collections.Generic;

namespace ASCE7WindLoadCalculator
{
    /// <summary>
    /// ASCE 7-22 Fig. 30.3-2D zones, gable roofs 27° &lt; θ ≤ 45° (p. 322): Zone 3 is an a × a square at each of the four
    /// eave corners, Zone 2 an a-wide strip along each rake (gable end) between those corners, and Zone 1 the rest of the roof.
    /// There is no ridge strip. Every piece is split at the ridge so it lies on one roof plane.
    /// </summary>
    public class GableRoof2DAreaCalculator_CC_ASCE7_22 : AreaCalculator_CC_Base
    {
        public GableRoof2DAreaCalculator_CC_ASCE7_22(BuildingData bldg_data)
        {
            buildingData = bldg_data;
        }

        public override void ComputeEffectiveWindAreas()
        {
            double L = buildingData.BuildingLength, B = buildingData.BuildingWidth, a = CritDim_a;
            int key = 1;
            void Add(string zone, double x0, double y0, double x1, double y1)
            {
                if (x1 - x0 <= 1e-9 || y1 - y0 <= 1e-9) return;
                effWindAreas.Add(key++, new EffectiveWindArea(zone, new List<Vec2>
                    { new Vec2(x0, y0), new Vec2(x1, y0), new Vec2(x1, y1), new Vec2(x0, y1) }, null));
            }

            if (buildingData.RidgeDirection == RidgeDirections.RIDGE_DIR_PARALLEL_TO_BLDGLENGTH)
            {
                // ridge along x at y = B/2; the eaves are y = 0 and y = B, the rakes x = 0 and x = L
                foreach (var (y0, y1) in new[] { (0.0, 0.5 * B), (0.5 * B, B) })
                {
                    double ya = y0 == 0 ? a : B - a;   // the corner square's inner edge on this slope
                    double c0 = y0 == 0 ? 0 : ya, c1 = y0 == 0 ? ya : B;   // the corner band (at the eave)
                    double r0 = y0 == 0 ? ya : y0, r1 = y0 == 0 ? y1 : ya;  // the rest of the slope up to the ridge
                    Add("Zone3", 0, c0, a, c1);
                    Add("Zone3", L - a, c0, L, c1);
                    Add("Zone2", 0, r0, a, r1);
                    Add("Zone2", L - a, r0, L, r1);
                    Add("Zone1", a, y0, L - a, y1);
                }
            }
            else
            {
                // ridge along y at x = L/2 (also when the sides are equal, as the other gable layouts do); the eaves are
                // x = 0 and x = L, the rakes y = 0 and y = B
                foreach (var (x0, x1) in new[] { (0.0, 0.5 * L), (0.5 * L, L) })
                {
                    double xa = x0 == 0 ? a : L - a;
                    double c0 = x0 == 0 ? 0 : xa, c1 = x0 == 0 ? xa : L;
                    double r0 = x0 == 0 ? xa : x0, r1 = x0 == 0 ? x1 : xa;
                    Add("Zone3", c0, 0, c1, a);
                    Add("Zone3", c0, B - a, c1, B);
                    Add("Zone2", r0, 0, r1, a);
                    Add("Zone2", r0, B - a, r1, B);
                    Add("Zone1", x0, a, x1, B - a);
                }
            }
        }
    }
}
