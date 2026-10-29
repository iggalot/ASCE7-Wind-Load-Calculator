namespace ASCE7WindLoadCalculator
{
    public class Figure30_3_2H_2I_ASCE7_16 : Chapter27and30_GCpCurveBase
    {
        public override string ChartTitle { get; set; } = "ASCE 7-16 Figure 30-3-2H and 2I - Hip Roofs";
        public override string ChartCriteria { get; set; } = "h <= 60ft, 27deg < slope <= 45deg";

        // TODO:  These figures have limits on Amax and Amin when retrieving values
        public Figure30_3_2H_2I_ASCE7_16(double slope)
        {
            // Calculate parameters
            double a = -0.6175 - 0.02 * slope;    // Zone1_neg_Roof upper Y1
            double b1 = -0.0950 - 0.0135 * slope; // Zone1_neg_Roof lower Y2
            double c = 0.2 - 0.067 * slope;       // Zone2e_neg_Roof Y1
            double d = 1.0 - 0.082 * slope;       // Zone2r_neg_Roof Y1
            double e = 1.25 - 0.108 * slope;      // Zone3_neg_Roof Y1

            double amin = 9 - 0.135 * slope;
            double amax = 280 - 5.0 * slope;

            // Roof Negative Curves
            RoofCurves_Neg["Zone1"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, a),
                (10, a),
                (20, a-(a-b1)/4),
                (50, a-(a-b1)/2),
                (100, a-(a-b1)*3/4),
                (200, b1),
                (500, b1),
                (1000, b1)
            });
            RoofCurves_Neg["Zone2e"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, c),
                (3, c),
                (5, c-(c+0.8)/4),
                (10, c-(c+0.8)/2),
                (20, c-(c+0.8)*3/4),
                (50, -0.8),
                (500, -0.8),
                (1000, -0.8)
            });
            RoofCurves_Neg["Zone2r"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, d),
                (7, d),
                (10, d-(d+1.0)/4),
                (20, d-(d+1.0)/2),
                (50, d-(d+1.0)*3/4),
                (100, -1.0),
                (500, -1.0),
                (1000, -1.0)
            });
            RoofCurves_Neg["Zone3"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, e),
                (5, e),
                (8, e-(e+1.0)/4),
                (10, e-(e+1.0)/2),
                (20, e-(e+1.0)*3/4),
                (50, -1.0),
                (200, -1.0),
                (1000, -1.0)
            });

            // Roof Positive Curves (fixed values)
            RoofCurves_Pos["Zone3"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, 0.9),
                (3, 0.9),
                (10, 0.65),
                (20, 0.55),
                (50, 0.4),
                (100, 0.3),
                (200, 0.3),
                (1000, 0.3)
            });
            RoofCurves_Pos["Zone2r"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, 0.9),
                (3, 0.9),
                (10, 0.65),
                (20, 0.55),
                (50, 0.4),
                (100, 0.3),
                (200, 0.3),
                (1000, 0.3)
            });
            RoofCurves_Pos["Zone2e"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, 0.9),
                (3, 0.9),
                (10, 0.65),
                (20, 0.55),
                (50, 0.4),
                (100, 0.3),
                (200, 0.3),
                (1000, 0.3)
            });
            RoofCurves_Pos["Zone1"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, 0.9),
                (3, 0.9),
                (10, 0.65),
                (20, 0.55),
                (50, 0.4),
                (100, 0.3),
                (200, 0.3),
                (1000, 0.3)
            });

            // Overhang negative parameters
            double f = -1.4175 - 0.02 * slope;
            double g = -0.895 - 0.0135 * slope;
            double i = -0.6 - 0.067 * slope;
            double j = 0.2 - 0.082 * slope;
            double k = 0.45 - 0.108 * slope;

            // Overhang Curves
            OverhangCurves["Zone3"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, k),
                (5, k),
                (7, k-(k+1.8)/4),
                (10, k-(k+1.8)/2),
                (20, k-(k+1.8)*3/4),
                (50, -1.8),
                (200, -1.8),
                (1000, -1.8)
            });
            OverhangCurves["Zone2r"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, j),
                (3, j),
                (7, j-(j+1.8)/4),
                (10, j-(j+1.8)/2),
                (20, j-(j+1.8)*3/4),
                (50, -1.8),
                (200, -1.8),
                (1000, -1.8)
            });
            OverhangCurves["Zone2e"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, i),
                (3, i),
                (7, i-(i+1.6)/4),
                (10, i-(i+1.6)/2),
                (20, i-(i+1.6)*3/4),
                (50, -1.6),
                (200, -1.6),
                (1000, -1.6)
            });
            OverhangCurves["Zone1"] = new ExternalGCpCurve(new (double X, double Y)[]
            {
                (1.0, f),
                (10, f),
                (20, f-(f+1.5)/4),
                (50, f-(f+1.5)/2),
                (100, f-(f+1.5)*3/4),
                (200, -1.5),
                (500, -1.5),
                (1000, -1.5)
            });
        }
    }
}
