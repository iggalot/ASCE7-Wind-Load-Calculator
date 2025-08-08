using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ASCE7WindLoadCalculator
{
    /// <summary>
    /// Interaction logic for WindLoadGraphicCanvas.xaml
    /// </summary>
    public partial class WindLoadGraphicCanvas : UserControl
    {
        private double _margin = 0;  // this blows up the roof loads if non-zero.  Fix this.

        private double SCALE_X = 3.0;
        private double SCALE_Y = 3.0;

        private double cnv_ht_elev, cnv_width_elev, cnv_ht_plan, cnv_width_plan;

        WindParameters_Base _parameters;
        BuildingData _bldg_data;

        public WindParameters_Base Parameters { get => _parameters; set => _parameters = value; }

        public WindLoadGraphicCanvas()
        {
            InitializeComponent();
        }

        public WindLoadGraphicCanvas( WindParameters_Base parameters, BuildingData bldg_data)
        {
            _parameters = parameters;
            _bldg_data = bldg_data;

            InitializeComponent();

            this.Loaded += WindLoadGraphicsCanvas_Loaded;
        }

        private void WindLoadGraphicsCanvas_Loaded(object sender, RoutedEventArgs e)
        {
            double ht = _bldg_data.BuildingHeight;
            double length = _bldg_data.BuildingLength;
            double width = _bldg_data.BuildingWidth;
            double ridge_ht = ht + length * 0.5 * Math.Tan(_bldg_data.RoofPitch * Math.PI / 180.0);


            Canvas cnv_plan = cnvPlanView;
            Canvas cnv_elev = cnvElevationView;

            cnv_ht_elev = cnv_elev.ActualHeight;   // <-- Use ActualHeight
            cnv_width_elev = cnv_elev.ActualWidth; // <-- Use ActualWidth

            cnv_ht_plan = cnv_plan.ActualHeight;   // <-- Use ActualHeight
            cnv_width_plan = cnv_plan.ActualWidth; // <-- Use ActualWidth

            SCALE_X = (cnv_width_elev - 2 * _margin) / length;
            SCALE_Y = Math.Min((cnv_ht_elev - 2 * _margin) / ridge_ht, (cnv_ht_plan - 2 * _margin) / width);

            // set it so there's a uniform scale factor
            SCALE_X = Math.Min(SCALE_X, SCALE_Y);
            SCALE_Y = Math.Min(SCALE_X, SCALE_Y);

            RedrawCanvas();

        }

        private double GetWallPressure(string v, WindLoadCases load_case_num)
        {
        //    foreach (var result in _wall_results)
        //    {
        //        if (result.Surface == v)
        //        {
        //            switch (load_case_num)
        //            {
        //                case WindLoadCases.WLC_BaseA:
        //                    return result.PressBaseA;
        //                case WindLoadCases.WLC_Balloon1:
        //                    return result.Balloon1;
        //                case WindLoadCases.WLC_Suction1:
        //                    return result.Suction1;
        //                default:
        //                    return 0;
        //            }
        //        }
        //    }
            return 0;
        }

        private double GetRoofPressure(string v, WindLoadCases load_case_num)
        {
            //foreach (var result in _roof_results)
            //{
            //    if (result.Surface == v)
            //    {
            //        switch (load_case_num)
            //        {
            //            case WindLoadCases.WLC_BaseA:
            //                return result.PressBaseA;
            //            case WindLoadCases.WLC_BaseB:
            //                return result.PressBaseB;
            //            case WindLoadCases.WLC_Balloon1:
            //                return result.Balloon1;
            //            case WindLoadCases.WLC_Balloon2:
            //                return result.Balloon2;
            //            case WindLoadCases.WLC_Suction1:
            //                return result.Suction1;
            //            case WindLoadCases.WLC_Suction2:
            //                return result.Suction2;
            //            default:
            //                return 0;
            //        }
                        
            //    }
            //}
            return 0;
        }

        private void DrawPlanWindLoads(WindLoadCases load_case_num)
        {
            double offset = _margin;
            // Draw the plan frame
            Point p1 = new Point(offset, cnv_ht_plan - offset);
            Point p2 = new Point(offset + SCALE_X * _bldg_data.BuildingLength, cnv_ht_plan - offset);
            Point p3 = new Point(offset + SCALE_X * _bldg_data.BuildingLength, cnv_ht_plan - offset - SCALE_Y * _bldg_data.BuildingWidth);
            Point p4 = new Point(offset, cnv_ht_plan - offset - SCALE_Y * _bldg_data.BuildingWidth);

            DrawingHelpersLibrary.DrawingHelpers.DrawLine(cnvPlanView, p1.X, p1.Y, p2.X, p2.Y, System.Windows.Media.Brushes.Black, 2.0);
            DrawingHelpersLibrary.DrawingHelpers.DrawLine(cnvPlanView, p2.X, p2.Y, p3.X, p3.Y, System.Windows.Media.Brushes.Black, 2.0);
            DrawingHelpersLibrary.DrawingHelpers.DrawLine(cnvPlanView, p3.X, p3.Y, p4.X, p4.Y, System.Windows.Media.Brushes.Black, 2.0);
            DrawingHelpersLibrary.DrawingHelpers.DrawLine(cnvPlanView, p4.X, p4.Y, p1.X, p1.Y, System.Windows.Media.Brushes.Black, 2.0);

            // Draw the windward wall wind load
            double WW_pressure = GetWallPressure("Windward Wall - z=h", load_case_num);
            Point a = (new Point(offset - WW_pressure, p1.Y));
            Point b = (new Point(offset - WW_pressure, p4.Y));
            DrawingHelpersLibrary.DrawingHelpers.DrawLine(cnvPlanView, a.X, a.Y, b.X, b.Y, System.Windows.Media.Brushes.Blue, 2.0);
            DrawingHelpersLibrary.DrawingHelpers.DrawText(cnvPlanView, a.X, a.Y, 0, WW_pressure.ToString(), System.Windows.Media.Brushes.Blue, 8.0);
            DrawingHelpersLibrary.DrawingHelpers.DrawArrowRight(cnvPlanView, p1.X, p1.Y, System.Windows.Media.Brushes.Blue, System.Windows.Media.Brushes.Blue, 2.0, Math.Abs(WW_pressure), Math.Abs(0.25 * WW_pressure));
            DrawingHelpersLibrary.DrawingHelpers.DrawArrowRight(cnvPlanView, p4.X, p4.Y, System.Windows.Media.Brushes.Blue, System.Windows.Media.Brushes.Blue, 2.0, Math.Abs(WW_pressure), Math.Abs(0.25 * WW_pressure));
            DrawingHelpersLibrary.DrawingHelpers.DrawArrowRight(cnvPlanView, 0.5 * (p4.X + p1.X), p1.Y + 0.25 * (p4.Y - p1.Y), System.Windows.Media.Brushes.Blue, System.Windows.Media.Brushes.Blue, 2.0, Math.Abs(WW_pressure), Math.Abs(0.25 * WW_pressure));
            DrawingHelpersLibrary.DrawingHelpers.DrawArrowRight(cnvPlanView, 0.5 * (p4.X + p1.X), p1.Y + 0.5 * (p4.Y - p1.Y), System.Windows.Media.Brushes.Blue, System.Windows.Media.Brushes.Blue, 2.0, Math.Abs(WW_pressure), Math.Abs(0.25 * WW_pressure));
            DrawingHelpersLibrary.DrawingHelpers.DrawArrowRight(cnvPlanView, 0.5 * (p4.X + p1.X), p1.Y + 0.75 * (p4.Y - p1.Y), System.Windows.Media.Brushes.Blue, System.Windows.Media.Brushes.Blue, 2.0, Math.Abs(WW_pressure), Math.Abs(0.25 * WW_pressure));

            // Draw the leeward wall wind load
            double LW_pressure = GetWallPressure("Leeward Wall", load_case_num);
            Point c = (new Point(p2.X - LW_pressure, p2.Y));
            Point d = (new Point(p3.X - LW_pressure, p3.Y));
            DrawingHelpersLibrary.DrawingHelpers.DrawLine(cnvPlanView, c.X, c.Y, d.X, d.Y, System.Windows.Media.Brushes.Red, 2.0);
            DrawingHelpersLibrary.DrawingHelpers.DrawText(cnvPlanView, c.X, c.Y, 0, LW_pressure.ToString(), System.Windows.Media.Brushes.Red, 8.0);
            DrawingHelpersLibrary.DrawingHelpers.DrawArrowRight(cnvPlanView, c.X, c.Y, System.Windows.Media.Brushes.Blue, System.Windows.Media.Brushes.Red, 2.0, Math.Abs(LW_pressure), 0.25 * Math.Abs(LW_pressure));
            DrawingHelpersLibrary.DrawingHelpers.DrawArrowRight(cnvPlanView, d.X, d.Y, System.Windows.Media.Brushes.Blue, System.Windows.Media.Brushes.Red, 2.0, Math.Abs(LW_pressure), 0.25 * Math.Abs(LW_pressure));
            DrawingHelpersLibrary.DrawingHelpers.DrawArrowRight(cnvPlanView, 0.5 * (c.X + d.X), c.Y + 0.25 * (d.Y - c.Y), System.Windows.Media.Brushes.Blue, System.Windows.Media.Brushes.Red, 2.0, Math.Abs(LW_pressure), 0.25 * Math.Abs(LW_pressure));
            DrawingHelpersLibrary.DrawingHelpers.DrawArrowRight(cnvPlanView, 0.5 * (c.X + d.X), c.Y + 0.5 * (d.Y - c.Y), System.Windows.Media.Brushes.Blue, System.Windows.Media.Brushes.Red, 2.0, Math.Abs(LW_pressure), 0.25 * Math.Abs(LW_pressure));
            DrawingHelpersLibrary.DrawingHelpers.DrawArrowRight(cnvPlanView, 0.5 * (c.X + d.X), c.Y + 0.75 * (d.Y - c.Y), System.Windows.Media.Brushes.Blue, System.Windows.Media.Brushes.Red, 2.0, Math.Abs(LW_pressure), 0.25 * Math.Abs(LW_pressure));

            // Draw the sidewall wall wind load -- Upper plan view wall
            double SW_pressure = GetWallPressure("Sidewall", load_case_num);
            Point e = (new Point(p3.X, p3.Y + SW_pressure));
            Point f = (new Point(p4.X, p4.Y + SW_pressure));
            DrawingHelpersLibrary.DrawingHelpers.DrawLine(cnvPlanView, e.X, e.Y, f.X, f.Y, System.Windows.Media.Brushes.Green, 2.0);
            DrawingHelpersLibrary.DrawingHelpers.DrawText(cnvPlanView, e.X, e.Y, 0, SW_pressure.ToString(), System.Windows.Media.Brushes.Green, 8.0);
            DrawingHelpersLibrary.DrawingHelpers.DrawArrowUp(cnvPlanView, e.X, e.Y, System.Windows.Media.Brushes.Blue, System.Windows.Media.Brushes.Green, 2.0, Math.Abs(SW_pressure), 0.25 * Math.Abs(SW_pressure));
            DrawingHelpersLibrary.DrawingHelpers.DrawArrowUp(cnvPlanView, f.X, f.Y, System.Windows.Media.Brushes.Blue, System.Windows.Media.Brushes.Green, 2.0, Math.Abs(SW_pressure), 0.25 * Math.Abs(SW_pressure));
            DrawingHelpersLibrary.DrawingHelpers.DrawArrowUp(cnvPlanView, 0.25 * (e.X + f.X), 0.5 * (e.Y + f.Y), System.Windows.Media.Brushes.Blue, System.Windows.Media.Brushes.Green, 2.0, Math.Abs(SW_pressure), 0.25 * Math.Abs(SW_pressure));
            DrawingHelpersLibrary.DrawingHelpers.DrawArrowUp(cnvPlanView, 0.5 * (e.X + f.X), 0.5 * (e.Y + f.Y), System.Windows.Media.Brushes.Blue, System.Windows.Media.Brushes.Green, 2.0, Math.Abs(SW_pressure), 0.25 * Math.Abs(SW_pressure));
            DrawingHelpersLibrary.DrawingHelpers.DrawArrowUp(cnvPlanView, 0.75 * (e.X + f.X), 0.5 * (e.Y + f.Y), System.Windows.Media.Brushes.Blue, System.Windows.Media.Brushes.Green, 2.0, Math.Abs(SW_pressure), 0.25 * Math.Abs(SW_pressure));

            Point g = (new Point(p1.X, p1.Y - SW_pressure));
            Point h = (new Point(p2.X, p2.Y - SW_pressure));
            DrawingHelpersLibrary.DrawingHelpers.DrawLine(cnvPlanView, g.X, g.Y, h.X, h.Y, System.Windows.Media.Brushes.Green, 2.0);
            DrawingHelpersLibrary.DrawingHelpers.DrawText(cnvPlanView, g.X, g.Y, 0, SW_pressure.ToString(), System.Windows.Media.Brushes.Green, 8.0);
            DrawingHelpersLibrary.DrawingHelpers.DrawArrowDown(cnvPlanView, g.X, g.Y, System.Windows.Media.Brushes.Blue, System.Windows.Media.Brushes.Green, 2.0, Math.Abs(SW_pressure), 0.25 * Math.Abs(SW_pressure));
            DrawingHelpersLibrary.DrawingHelpers.DrawArrowDown(cnvPlanView, h.X, h.Y, System.Windows.Media.Brushes.Blue, System.Windows.Media.Brushes.Green, 2.0, Math.Abs(SW_pressure), 0.25 * Math.Abs(SW_pressure));
            DrawingHelpersLibrary.DrawingHelpers.DrawArrowDown(cnvPlanView, 0.25 * (g.X + h.X), 0.5 * (g.Y + h.Y), System.Windows.Media.Brushes.Blue, System.Windows.Media.Brushes.Green, 2.0, Math.Abs(SW_pressure), 0.25 * Math.Abs(SW_pressure));
            DrawingHelpersLibrary.DrawingHelpers.DrawArrowDown(cnvPlanView, 0.5 * (g.X + h.X), 0.5 * (g.Y + h.Y), System.Windows.Media.Brushes.Blue, System.Windows.Media.Brushes.Green, 2.0, Math.Abs(SW_pressure), 0.25 * Math.Abs(SW_pressure));
            DrawingHelpersLibrary.DrawingHelpers.DrawArrowDown(cnvPlanView, 0.75 * (g.X + h.X), 0.5 * (g.Y + h.Y), System.Windows.Media.Brushes.Blue, System.Windows.Media.Brushes.Green, 2.0, Math.Abs(SW_pressure), 0.25 * Math.Abs(SW_pressure));
        }

        private void DrawElevationWindLoads(WindLoadCases load_case_num)
        {
            double offset = _margin;

            // Draw the elevation frame
            Point p1 = new Point(offset, cnv_ht_elev - offset);
            Point p2 = new Point(offset, cnv_ht_elev - offset - SCALE_Y * _bldg_data.BuildingHeight);
            Point p3 = new Point(offset + SCALE_X * _bldg_data.BuildingLength * 0.5, cnv_ht_elev - offset - SCALE_Y * (_bldg_data.BuildingHeight + _bldg_data.BuildingLength * 0.5 * Math.Sin(_bldg_data.RoofPitch * 2.0 * Math.PI / 360)));
            Point p4 = new Point(offset + SCALE_X * _bldg_data.BuildingLength, cnv_ht_elev - offset - SCALE_Y * _bldg_data.BuildingHeight);
            Point p5 = new Point(offset + SCALE_X * _bldg_data.BuildingLength, cnv_ht_elev - offset);
            
            // Mark the ridge
            DrawingHelpersLibrary.DrawingHelpers.DrawCircle(cnvElevationView, p3.X, p3.Y, System.Windows.Media.Brushes.Transparent, System.Windows.Media.Brushes.Black, 4.0, 2.0);


            DrawingHelpersLibrary.DrawingHelpers.DrawLine(cnvElevationView, p1.X, p1.Y, p2.X, p2.Y, System.Windows.Media.Brushes.Black, 2.0);
            DrawingHelpersLibrary.DrawingHelpers.DrawLine(cnvElevationView, p2.X, p2.Y, p3.X, p3.Y, System.Windows.Media.Brushes.Black, 2.0);
            DrawingHelpersLibrary.DrawingHelpers.DrawLine(cnvElevationView, p3.X, p3.Y, p4.X, p4.Y, System.Windows.Media.Brushes.Black, 2.0);
            DrawingHelpersLibrary.DrawingHelpers.DrawLine(cnvElevationView, p4.X, p4.Y, p5.X, p5.Y, System.Windows.Media.Brushes.Black, 2.0);

            // Draw the windward wall wind load
            double WW_pressure_z0 = GetWallPressure("Windward Wall - z=0ft", load_case_num);
            double WW_pressure_z15 = WW_pressure_z0;

            if (_bldg_data.BuildingHeight >= 15.0)
            {
                WW_pressure_z15 = GetWallPressure("Windward Wall - z=15ft", load_case_num);
            } 
            double WW_pressure_zh = GetWallPressure("Windward Wall - z=h", load_case_num);

            Point a = (new Point(p1.X - WW_pressure_z0, p1.Y));
            Point b = (new Point(p1.X - WW_pressure_z15, p1.Y - SCALE_Y * 15.0));
            Point c = (new Point(p2.X - WW_pressure_zh, p2.Y));


            DrawingHelpersLibrary.DrawingHelpers.DrawLine(cnvElevationView, a.X, a.Y, b.X, b.Y, System.Windows.Media.Brushes.Blue, 2.0);
            DrawingHelpersLibrary.DrawingHelpers.DrawLine(cnvElevationView, b.X, b.Y, c.X, c.Y, System.Windows.Media.Brushes.Blue, 2.0);

            DrawingHelpersLibrary.DrawingHelpers.DrawText(cnvElevationView, a.X, a.Y, 0, WW_pressure_z0.ToString(), System.Windows.Media.Brushes.Blue, 8.0);
            DrawingHelpersLibrary.DrawingHelpers.DrawText(cnvElevationView, b.X, b.Y, 0, WW_pressure_z15.ToString(), System.Windows.Media.Brushes.Blue, 8.0);
            DrawingHelpersLibrary.DrawingHelpers.DrawText(cnvElevationView, c.X, c.Y, 0, WW_pressure_zh.ToString(), System.Windows.Media.Brushes.Blue, 8.0);

            DrawingHelpersLibrary.DrawingHelpers.DrawArrowRight(cnvElevationView, p1.X, p1.Y, System.Windows.Media.Brushes.Blue, System.Windows.Media.Brushes.Blue, 2.0, Math.Abs(WW_pressure_z0), Math.Abs(0.25 * WW_pressure_z0));
            DrawingHelpersLibrary.DrawingHelpers.DrawArrowRight(cnvElevationView, p1.X, p1.Y - SCALE_Y * 15.0, System.Windows.Media.Brushes.Blue, System.Windows.Media.Brushes.Blue, 2.0, Math.Abs(WW_pressure_z0), Math.Abs(0.25 * WW_pressure_z0));
            DrawingHelpersLibrary.DrawingHelpers.DrawArrowRight(cnvElevationView, p2.X, p2.Y, System.Windows.Media.Brushes.Blue, System.Windows.Media.Brushes.Blue, 2.0, Math.Abs(WW_pressure_zh), Math.Abs(0.25 * WW_pressure_zh));


            // Draw the leeward wall wind load
            double LW_pressure = GetWallPressure("Leeward Wall", load_case_num);

            Point e = (new Point(p4.X - LW_pressure, p4.Y));
            Point d = (new Point(p5.X - LW_pressure, p5.Y));


            DrawingHelpersLibrary.DrawingHelpers.DrawLine(cnvElevationView, d.X, d.Y, e.X, e.Y, System.Windows.Media.Brushes.Red, 2.0);
            DrawingHelpersLibrary.DrawingHelpers.DrawText(cnvElevationView, e.X, e.Y, 0, LW_pressure.ToString(), System.Windows.Media.Brushes.Red, 8.0);

            DrawingHelpersLibrary.DrawingHelpers.DrawArrowRight(cnvElevationView, d.X, d.Y, System.Windows.Media.Brushes.Blue, System.Windows.Media.Brushes.Red, 2.0, Math.Abs(LW_pressure), 0.25 * Math.Abs(LW_pressure));
            DrawingHelpersLibrary.DrawingHelpers.DrawArrowRight(cnvElevationView, e.X, e.Y, System.Windows.Media.Brushes.Blue, System.Windows.Media.Brushes.Red, 2.0, Math.Abs(LW_pressure), 0.25 * Math.Abs(LW_pressure));
            DrawingHelpersLibrary.DrawingHelpers.DrawArrowRight(cnvElevationView, 0.5 * (e.X + d.X), d.Y + 0.25 * (e.Y - d.Y), System.Windows.Media.Brushes.Red, System.Windows.Media.Brushes.Red, 2.0, Math.Abs(LW_pressure), 0.25 * Math.Abs(LW_pressure));
            DrawingHelpersLibrary.DrawingHelpers.DrawArrowRight(cnvElevationView, 0.5 * (e.X + d.X), d.Y + 0.5 * (e.Y - d.Y), System.Windows.Media.Brushes.Red, System.Windows.Media.Brushes.Red, 2.0, Math.Abs(LW_pressure), 0.25 * Math.Abs(LW_pressure));
            DrawingHelpersLibrary.DrawingHelpers.DrawArrowRight(cnvElevationView, 0.5 * (e.X + d.X), d.Y + 0.75 * (e.Y - d.Y), System.Windows.Media.Brushes.Red, System.Windows.Media.Brushes.Red, 2.0, Math.Abs(LW_pressure), 0.25 * Math.Abs(LW_pressure));

            DrawRoofWind(load_case_num, p2, p3, p4);
        }

        private void DrawRoofWind_Flat(WindLoadCases load_case_num, Point p2, Point p3, Point p4)
        {
            ////"Windward Roof 0->h/2"
            ////"Windward Roof h/2->h"
            ////"Windward Roof h->2h" 
            ////"Windward Roof 2h->end"

            //// find how many zones we have on the roof
            //int zone_count = 0;
            //foreach (WindPressureResult_Roof_MWFRS result in _roof_results)
            //{
            //    if (result.Surface == "Windward Roof 0->h/2")
            //    {
            //        zone_count++;
            //    }
            //    else if (result.Surface == "Windward Roof h/2->h")
            //    {
            //        zone_count++;
            //    }
            //    else if (result.Surface == "Windward Roof h->2h")
            //    {
            //        zone_count++;
            //    }
            //    else if (result.Surface == "Windward Roof 2h->end")
            //    {
            //        zone_count++;
            //    }
            //}

            //if (zone_count == 1)
            //{
            //    // ridge point is in zone1
            //    double WW_roof_pressure = GetRoofPressure("Windward Roof 0->h/2", load_case_num);
            //    double LW_roof_pressure = WW_roof_pressure;

            //    double roof_angle_deg_ww = _bldg_data.RoofPitch;
            //    double roof_angle_rad_ww = roof_angle_deg_ww * Math.PI / 180.0;

            //    double roof_angle_deg_lw = 180.0 - _bldg_data.RoofPitch;
            //    double roof_angle_rad_lw = roof_angle_deg_lw * Math.PI / 180.0;

            //    // find point that is perpendicular to line from p2 to p3 at a distance of WW_roof_pressure

            //    var delta_x_ww = -Math.Cos(roof_angle_rad_ww + Math.PI / 2.0) * WW_roof_pressure;
            //    var delta_y_ww = Math.Sin(roof_angle_rad_ww + Math.PI / 2.0) * WW_roof_pressure;

            //    var delta_x_lw = -Math.Cos(roof_angle_rad_lw - Math.PI / 2.0) * LW_roof_pressure;
            //    var delta_y_lw = Math.Sin(roof_angle_rad_lw - Math.PI / 2.0) * LW_roof_pressure;

            //    var a = new Point(p2.X + delta_x_ww, p2.Y + delta_y_ww);
            //    var b1 = new Point(p3.X + delta_x_ww, p3.Y + delta_y_ww);
            //    var b2 = new Point(p3.X + delta_x_lw, p3.Y + delta_y_lw);
            //    var c = new Point(p4.X + delta_x_lw, p4.Y + delta_y_lw);


            //    // Windward
            //    DrawingHelpersLibrary.DrawingHelpers.DrawLine(cnvElevationView, a.X, a.Y, b1.X, b1.Y, System.Windows.Media.Brushes.Purple, 2.0);
            //    DrawingHelpersLibrary.DrawingHelpers.DrawLine(cnvElevationView, a.X, a.Y, p2.X, p2.Y, System.Windows.Media.Brushes.Purple, 2.0);
            //    DrawingHelpersLibrary.DrawingHelpers.DrawLine(cnvElevationView, p3.X, p3.Y, b1.X, b1.Y, System.Windows.Media.Brushes.Purple, 2.0);

            //    DrawingHelpersLibrary.DrawingHelpers.DrawText(cnvElevationView, a.X, a.Y, 0, WW_roof_pressure.ToString(), System.Windows.Media.Brushes.Purple, 8.0);

                
            //    // Leeward
            //    DrawingHelpersLibrary.DrawingHelpers.DrawLine(cnvElevationView, b2.X, b2.Y, c.X, c.Y, System.Windows.Media.Brushes.Purple, 2.0);
            //    DrawingHelpersLibrary.DrawingHelpers.DrawLine(cnvElevationView, b2.X, b2.Y, p3.X, p3.Y, System.Windows.Media.Brushes.Purple, 2.0);
            //    DrawingHelpersLibrary.DrawingHelpers.DrawLine(cnvElevationView, p4.X, p4.Y, c.X, c.Y, System.Windows.Media.Brushes.Purple, 2.0);

            //    DrawingHelpersLibrary.DrawingHelpers.DrawText(cnvElevationView, c.X, c.Y, 0, LW_roof_pressure.ToString(), System.Windows.Media.Brushes.Purple, 8.0);
            //}
            //else if (zone_count == 2)
            //{
            //    // is ridge in zone 1 or zone 2?
            //    double x_ridge, y_ridge;
            //    x_ridge = p3.X;
            //    y_ridge = p3.Y;

            //    WindPressureResult_Roof_MWFRS result1 = _roof_results[0];
            //    WindPressureResult_Roof_MWFRS result2 = _roof_results[1];

            //    double start_x1 = p2.X + SCALE_X * result1.Start;
            //    double end_x1 = p2.X + SCALE_X * result1.End;
            //    double start_x2 = p2.X + SCALE_X * result2.Start;
            //    double end_x2 = p2.X + SCALE_X *result2.End;

            //    double WW_roof_pressure1 = GetRoofPressure("Windward Roof 0->h/2", load_case_num);
            //    double WW_roof_pressure2 = GetRoofPressure("Windward Roof h/2->h", load_case_num);


            //    double roof_angle_deg_ww = _bldg_data.RoofPitch;
            //    double roof_angle_rad_ww = roof_angle_deg_ww * Math.PI / 180.0;

            //    double roof_angle_deg_lw = 180.0 - _bldg_data.RoofPitch;
            //    double roof_angle_rad_lw = roof_angle_deg_lw * Math.PI / 180.0;

            //    // find point that is perpendicular to line from p2 to p3 at a distance of WW_roof_pressure

            //    var delta_x1_ww = -Math.Cos(roof_angle_rad_ww + Math.PI / 2.0) * WW_roof_pressure1;
            //    var delta_y1_ww = Math.Sin(roof_angle_rad_ww + Math.PI / 2.0) * WW_roof_pressure1;

            //    var delta_x1_lw = -Math.Cos(roof_angle_rad_lw - Math.PI / 2.0) * WW_roof_pressure1;
            //    var delta_y1_lw = Math.Sin(roof_angle_rad_lw - Math.PI / 2.0) * WW_roof_pressure1;

            //    var delta_x2_ww = -Math.Cos(roof_angle_rad_ww + Math.PI / 2.0) * WW_roof_pressure2;
            //    var delta_y2_ww = Math.Sin(roof_angle_rad_ww + Math.PI / 2.0) * WW_roof_pressure2;

            //    var delta_x2_lw = -Math.Cos(roof_angle_rad_lw - Math.PI / 2.0) * WW_roof_pressure2;
            //    var delta_y2_lw = Math.Sin(roof_angle_rad_lw - Math.PI / 2.0) * WW_roof_pressure2;

            //    // Ridge is in zone 1
            //    if (x_ridge <= end_x1)
            //    {
            //        var a = new Point(p2.X + start_x1 + delta_x1_ww, p2.Y + delta_y1_ww);
            //        var b_l = new Point(p3.X + delta_x1_ww, p3.Y + delta_y1_ww);
            //        var b_r = new Point(p3.X + delta_x1_lw, p3.Y + delta_y1_lw);

            //        var b1 = new Point(p2.X + end_x1 + delta_x1_lw, p3.Y + delta_y1_lw);
            //        var b2 = new Point(p2.X + end_x1 + delta_x2_lw, p3.Y + delta_y2_lw);
            //        var c = new Point(p4.X + delta_x2_lw, p4.Y + delta_y2_lw);

            //        DrawingHelpersLibrary.DrawingHelpers.DrawLine(cnvElevationView, a.X, a.Y, b_l.X, b_l.Y, System.Windows.Media.Brushes.Purple, 2.0);
            //        DrawingHelpersLibrary.DrawingHelpers.DrawLine(cnvElevationView, b_r.X, b_r.Y, b1.X, b1.Y, System.Windows.Media.Brushes.Purple, 2.0);

            //        DrawingHelpersLibrary.DrawingHelpers.DrawLine(cnvElevationView, b2.X, b2.Y, c.X, c.Y, System.Windows.Media.Brushes.Purple, 2.0);
            //    } else
            //    {
            //        // Ridge is in zone 2
            //        var a = new Point(p2.X + start_x1 + delta_x1_ww, SCALE_Y * p2.Y + delta_y1_ww);
            //        var b1 = new Point(p2.X + end_x1 + delta_x1_ww, SCALE_Y * p3.Y + delta_y1_ww);
            //        var b2 = new Point(p2.X + end_x1 + delta_x2_ww, SCALE_Y * p3.Y + delta_y2_ww);
            //        var b_l = new Point(p3.X  + delta_x2_ww, SCALE_Y * p3.Y + delta_y2_ww);
            //        var b_r = new Point(p3.X + delta_x2_lw, SCALE_Y * p3.Y + delta_y2_lw);
            //        var c = new Point(p4.X + delta_x2_lw, SCALE_Y * p4.Y + delta_y2_lw);

            //        //DrawingHelpersLibrary.DrawingHelpers.DrawCircle(cnvElevationView, a.X, a.Y, System.Windows.Media.Brushes.Transparent, System.Windows.Media.Brushes.Red, 4.0, 2.0);
            //        //DrawingHelpersLibrary.DrawingHelpers.DrawCircle(cnvElevationView, b1.X, b1.Y, System.Windows.Media.Brushes.Transparent, System.Windows.Media.Brushes.Blue, 4.0, 2.0);
            //        //DrawingHelpersLibrary.DrawingHelpers.DrawCircle(cnvElevationView, b2.X, b2.Y, System.Windows.Media.Brushes.Transparent, System.Windows.Media.Brushes.Green, 4.0, 2.0);
            //        //DrawingHelpersLibrary.DrawingHelpers.DrawCircle(cnvElevationView, b.X, b.Y, System.Windows.Media.Brushes.Transparent, System.Windows.Media.Brushes.Cyan, 4.0, 2.0);
            //        //DrawingHelpersLibrary.DrawingHelpers.DrawCircle(cnvElevationView, c.X, c.Y, System.Windows.Media.Brushes.Transparent, System.Windows.Media.Brushes.Purple, 4.0, 2.0);

            //        // The pressure lines
            //        DrawingHelpersLibrary.DrawingHelpers.DrawLine(cnvElevationView, a.X, a.Y, b1.X, b1.Y, System.Windows.Media.Brushes.Purple, 2.0);

            //        DrawingHelpersLibrary.DrawingHelpers.DrawLine(cnvElevationView, b2.X, b2.Y, b_l.X, b_l.Y, System.Windows.Media.Brushes.Purple, 2.0);
            //        DrawingHelpersLibrary.DrawingHelpers.DrawLine(cnvElevationView, b_r.X, b_r.Y, c.X, c.Y, System.Windows.Media.Brushes.Purple, 2.0);
            //    }
                
            //} else if (zone_count == 3)
            //{
            //    MessageBox.Show("3 zones");
            //    // is ridge in zone 1 or zone 2?
            //    double x_ridge, y_ridge;
            //    x_ridge = p3.X;
            //    y_ridge = p3.Y;

            //    WindPressureResult_Roof_MWFRS result1 = _roof_results[0];
            //    WindPressureResult_Roof_MWFRS result2 = _roof_results[1];
            //    WindPressureResult_Roof_MWFRS result3 = _roof_results[2];

            //    double start_x1 = p2.X + SCALE_X * result1.Start;
            //    double end_x1 = p2.X + SCALE_X * result1.End;
            //    double start_x2 = p2.X + SCALE_X * result2.Start;
            //    double end_x2 = p2.X + SCALE_X * result2.End;
            //    double start_x3 = p2.X + SCALE_X * result3.Start;
            //    double end_x3 = p2.X + SCALE_X * result3.End;

            //    double WW_roof_pressure1 = GetRoofPressure("Windward Roof 0->h/2", load_case_num);
            //    double WW_roof_pressure2 = GetRoofPressure("Windward Roof h/2->h", load_case_num);
            //    double WW_roof_pressure3 = GetRoofPressure("Windward Roof h->2h", load_case_num);



            //    double roof_angle_deg_ww = _bldg_data.RoofPitch;
            //    double roof_angle_rad_ww = roof_angle_deg_ww * Math.PI / 180.0;

            //    double roof_angle_deg_lw = 180.0 - _bldg_data.RoofPitch;
            //    double roof_angle_rad_lw = roof_angle_deg_lw * Math.PI / 180.0;

            //    // find point that is perpendicular to line from p2 to p3 at a distance of WW_roof_pressure

            //    var delta_x1_ww = -Math.Cos(roof_angle_rad_ww + Math.PI / 2.0) * WW_roof_pressure1;
            //    var delta_y1_ww = Math.Sin(roof_angle_rad_ww + Math.PI / 2.0) * WW_roof_pressure1;

            //    var delta_x1_lw = -Math.Cos(roof_angle_rad_lw - Math.PI / 2.0) * WW_roof_pressure1;
            //    var delta_y1_lw = Math.Sin(roof_angle_rad_lw - Math.PI / 2.0) * WW_roof_pressure1;

            //    var delta_x2_ww = -Math.Cos(roof_angle_rad_ww + Math.PI / 2.0) * WW_roof_pressure2;
            //    var delta_y2_ww = Math.Sin(roof_angle_rad_ww + Math.PI / 2.0) * WW_roof_pressure2;

            //    var delta_x2_lw = -Math.Cos(roof_angle_rad_lw - Math.PI / 2.0) * WW_roof_pressure2;
            //    var delta_y2_lw = Math.Sin(roof_angle_rad_lw - Math.PI / 2.0) * WW_roof_pressure2;

            //    var delta_x3_ww = -Math.Cos(roof_angle_rad_ww + Math.PI / 2.0) * WW_roof_pressure3;
            //    var delta_y3_ww = Math.Sin(roof_angle_rad_ww + Math.PI / 2.0) * WW_roof_pressure3;

            //    var delta_x3_lw = -Math.Cos(roof_angle_rad_lw - Math.PI / 2.0) * WW_roof_pressure3;
            //    var delta_y3_lw = Math.Sin(roof_angle_rad_lw - Math.PI / 2.0) * WW_roof_pressure3;

            //    // Ridge is in zone 1
            //    if (x_ridge <= end_x1)
            //    {
            //        var a = new Point(p2.X + start_x1 + delta_x1_ww, p2.Y + delta_y1_ww);
            //        var b_l = new Point(p3.X + delta_x1_ww, p3.Y + delta_y1_ww);
            //        var b_r = new Point(p3.X + delta_x1_lw, p3.Y + delta_y1_lw);
            //        var b1 = new Point(p2.X + end_x1 + delta_x1_lw, p3.Y + delta_y1_lw);
            //        var b2 = new Point(p2.X + start_x2 + delta_x2_lw, p3.Y + delta_y2_lw);
            //        var c1 = new Point(p2.X + end_x2 + delta_x2_lw, p4.Y + delta_y2_lw);
            //        var c2 = new Point(p2.X + start_x3 + delta_x3_lw, p4.Y + delta_y3_lw);
            //        var d = new Point(p2.X + end_x3 + delta_x3_lw, p4.Y + delta_y3_lw);

            //        DrawingHelpersLibrary.DrawingHelpers.DrawLine(cnvElevationView, a.X, a.Y, b_l.X, b_l.Y, System.Windows.Media.Brushes.Purple, 2.0);
            //        DrawingHelpersLibrary.DrawingHelpers.DrawLine(cnvElevationView, b_r.X, b_r.Y, b1.X, b1.Y, System.Windows.Media.Brushes.Purple, 2.0);
            //        DrawingHelpersLibrary.DrawingHelpers.DrawLine(cnvElevationView, b2.X, b2.Y, c1.X, c1.Y, System.Windows.Media.Brushes.Purple, 2.0);
            //        DrawingHelpersLibrary.DrawingHelpers.DrawLine(cnvElevationView, c2.X, c2.Y, d.X, d.Y, System.Windows.Media.Brushes.Purple, 2.0);

            //    }
            //    else if (x_ridge <= end_x2)
            //    {
            //        // Ridge is in zone 2
            //        var a = new Point(p2.X + start_x1 + delta_x1_ww, p2.Y + delta_y1_ww);
            //        var b1 = new Point(p2.X + end_x1 + delta_x1_ww, p3.Y + delta_y1_ww);
            //        var b2 = new Point(p2.X + start_x2 + delta_x2_ww, p3.Y + delta_y2_ww);
            //        var b_l = new Point(p3.X + delta_x2_ww, p3.Y + delta_y2_ww);
            //        var b_r = new Point(p3.X + delta_x2_lw, p3.Y + delta_y2_lw);
            //        var c1 = new Point(p2.X + end_x2 + delta_x2_lw, p4.Y + delta_y2_lw);
            //        var c2 = new Point(p2.X + start_x3 + delta_x3_lw, p4.Y + delta_y3_lw);
            //        var d = new Point(p2.X + end_x3, p4.Y + delta_y3_lw);

            //        DrawingHelpersLibrary.DrawingHelpers.DrawCircle(cnvElevationView, a.X, a.Y, System.Windows.Media.Brushes.Transparent, System.Windows.Media.Brushes.Red, 4.0, 2.0);
            //        DrawingHelpersLibrary.DrawingHelpers.DrawCircle(cnvElevationView, b1.X, b1.Y, System.Windows.Media.Brushes.Transparent, System.Windows.Media.Brushes.Blue, 4.0, 2.0);
            //        DrawingHelpersLibrary.DrawingHelpers.DrawCircle(cnvElevationView, b2.X, b2.Y, System.Windows.Media.Brushes.Transparent, System.Windows.Media.Brushes.Green, 4.0, 2.0);
            //        DrawingHelpersLibrary.DrawingHelpers.DrawCircle(cnvElevationView, b_l.X, b_l.Y, System.Windows.Media.Brushes.Transparent, System.Windows.Media.Brushes.Cyan, 4.0, 2.0);
            //        DrawingHelpersLibrary.DrawingHelpers.DrawCircle(cnvElevationView, b_r.X, b_r.Y, System.Windows.Media.Brushes.Transparent, System.Windows.Media.Brushes.Purple, 4.0, 2.0);
            //        DrawingHelpersLibrary.DrawingHelpers.DrawCircle(cnvElevationView, c1.X, c1.Y, System.Windows.Media.Brushes.Transparent, System.Windows.Media.Brushes.Red, 4.0, 2.0);
            //        DrawingHelpersLibrary.DrawingHelpers.DrawCircle(cnvElevationView, c2.X, c2.Y, System.Windows.Media.Brushes.Transparent, System.Windows.Media.Brushes.Blue, 4.0, 2.0);
            //        DrawingHelpersLibrary.DrawingHelpers.DrawCircle(cnvElevationView, d.X, d.Y, System.Windows.Media.Brushes.Transparent, System.Windows.Media.Brushes.Green, 4.0, 2.0);

            //        // The pressure lines
            //        DrawingHelpersLibrary.DrawingHelpers.DrawLine(cnvElevationView, a.X, a.Y, b1.X, b1.Y, System.Windows.Media.Brushes.Purple, 2.0);
            //        DrawingHelpersLibrary.DrawingHelpers.DrawLine(cnvElevationView, b2.X, b2.Y, b_l.X, b_l.Y, System.Windows.Media.Brushes.Purple, 2.0);

            //        DrawingHelpersLibrary.DrawingHelpers.DrawLine(cnvElevationView, b_r.X, b_r.Y, c1.X, c1.Y, System.Windows.Media.Brushes.Purple, 2.0);
            //        DrawingHelpersLibrary.DrawingHelpers.DrawLine(cnvElevationView, c2.X, c2.Y, d.X, d.Y, System.Windows.Media.Brushes.Purple, 2.0);
            //    } else
            //    {
            //        // Ridge is in zone 2
            //        var a = new Point(p2.X + start_x1 + delta_x1_ww, p2.Y + delta_y1_ww);
            //        var b1 = new Point(p2.X + end_x1 + delta_x1_ww, p3.Y + delta_y1_ww);
            //        var b2 = new Point(p2.X + end_x1 + delta_x2_ww, p3.Y + delta_y2_ww);
            //        var c1 = new Point(p2.X + end_x1 + end_x2 + delta_x2_ww, p4.Y + delta_y2_ww);
            //        var c2 = new Point(p2.X + end_x1 + end_x2 + delta_x3_ww, p4.Y + delta_y3_ww);
            //        var b_l = new Point(p3.X + delta_x3_ww, p3.Y + delta_y3_ww);
            //        var b_r = new Point(p3.X + delta_x3_lw, p3.Y + delta_y3_lw);
            //        var d = new Point(p4.X + delta_x3_lw, p4.Y + delta_y3_lw);

            //        // The pressure lines
            //        DrawingHelpersLibrary.DrawingHelpers.DrawLine(cnvElevationView, a.X, a.Y, b1.X, b1.Y, System.Windows.Media.Brushes.Purple, 2.0);
            //        DrawingHelpersLibrary.DrawingHelpers.DrawLine(cnvElevationView, b2.X, b2.Y, c1.X, c1.Y, System.Windows.Media.Brushes.Purple, 2.0);
            //        DrawingHelpersLibrary.DrawingHelpers.DrawLine(cnvElevationView, c2.X, c2.Y, b_l.X, b_l.Y, System.Windows.Media.Brushes.Purple, 2.0);

            //        DrawingHelpersLibrary.DrawingHelpers.DrawLine(cnvElevationView, b_r.X, b_r.Y, d.X, d.Y, System.Windows.Media.Brushes.Purple, 2.0);
            //    }
            //} else if (zone_count == 4)
            //{
            //    MessageBox.Show("4 zones");
            //} else
            //{
            //    throw new InvalidOperationException("There should not be more than four zones on a roof wind load");
            //}

            //// find which zone p3 occurs in -- this is the ridge point

            //// draw p1 zone

            //// draw p2 zone

            //// draw p3 zone

            //// draw p4 zone

        }

        private void DrawRoofWind(WindLoadCases load_case_num, Point p2, Point p3, Point p4)
        {
            if (_bldg_data.RidgeDirection == RidgeDirections.RIDGE_DIR_PARALLEL_TO_BLDGLENGTH && _bldg_data.RoofPitch >= 10.0)
            {
                DrawRoofWind_PerpToRidge(load_case_num, p2, p3, p4);
            }
            else
            {
                DrawRoofWind_Flat(load_case_num, p2, p3, p4);
            }
        }

        private void DrawRoofWind_PerpToRidge(WindLoadCases load_case_num, Point p2, Point p3, Point p4)
        {
            // Draw the windward wall wind load
            double WW_roof_pressure = GetRoofPressure("Windward Roof", load_case_num);
            double LW_roof_pressure = GetRoofPressure("Leeward Roof", load_case_num);

            double roof_angle_deg_ww = _bldg_data.RoofPitch;
            double roof_angle_rad_ww = roof_angle_deg_ww * Math.PI / 180.0;

            double roof_angle_deg_lw = 180.0 - _bldg_data.RoofPitch;
            double roof_angle_rad_lw = roof_angle_deg_lw * Math.PI / 180.0;

            // find point that is perpendicular to line from p2 to p3 at a distance of WW_roof_pressure

            var delta_x_ww = -Math.Cos(roof_angle_rad_ww + Math.PI / 2.0) * WW_roof_pressure;
            var delta_y_ww = Math.Sin(roof_angle_rad_ww + Math.PI / 2.0) * WW_roof_pressure;

            var delta_x_lw = -Math.Cos(roof_angle_rad_lw - Math.PI / 2.0) * LW_roof_pressure;
            var delta_y_lw = Math.Sin(roof_angle_rad_lw - Math.PI / 2.0) * LW_roof_pressure;

            var a = new Point(p2.X + delta_x_ww, p2.Y + delta_y_ww);
            var b1 = new Point(p3.X + delta_x_ww, p3.Y + delta_y_ww);
            var b2 = new Point(p3.X + delta_x_lw, p3.Y + delta_y_lw);
            var c = new Point(p4.X + delta_x_lw, p4.Y + delta_y_lw);

            // Windward roof
            DrawingHelpersLibrary.DrawingHelpers.DrawLine(cnvElevationView, a.X, a.Y, b1.X, b1.Y, System.Windows.Media.Brushes.Purple, 2.0);
            DrawingHelpersLibrary.DrawingHelpers.DrawLine(cnvElevationView, a.X, a.Y, p2.X, p2.Y, System.Windows.Media.Brushes.Purple, 2.0);
            DrawingHelpersLibrary.DrawingHelpers.DrawLine(cnvElevationView, p3.X, p3.Y, b1.X, b1.Y, System.Windows.Media.Brushes.Purple, 2.0);

            DrawingHelpersLibrary.DrawingHelpers.DrawText(cnvElevationView, a.X, a.Y, 0, WW_roof_pressure.ToString(), System.Windows.Media.Brushes.Purple, 8.0);


            // Leeward roof
            DrawingHelpersLibrary.DrawingHelpers.DrawLine(cnvElevationView, b2.X, b2.Y, c.X, c.Y, System.Windows.Media.Brushes.Purple, 2.0);
            DrawingHelpersLibrary.DrawingHelpers.DrawLine(cnvElevationView, b2.X, b2.Y, p3.X, p3.Y, System.Windows.Media.Brushes.Purple, 2.0);
            DrawingHelpersLibrary.DrawingHelpers.DrawLine(cnvElevationView, p4.X, p4.Y, c.X, c.Y, System.Windows.Media.Brushes.Purple, 2.0);

            DrawingHelpersLibrary.DrawingHelpers.DrawText(cnvElevationView, c.X, c.Y, 0, LW_roof_pressure.ToString(), System.Windows.Media.Brushes.Purple, 8.0);
        }

        private void LoadCase_CheckedChanged(object sender, RoutedEventArgs e)
        {
            RedrawCanvas();
        }

        private void RedrawCanvas()
        {
            cnvPlanView.Children.Clear();
            cnvElevationView.Children.Clear();

            if (chkLC1.IsChecked == true)
            {
                DrawPlanWindLoads(WindLoadCases.WLC_BaseA);
                DrawElevationWindLoads(WindLoadCases.WLC_BaseA);
            }
            if (chkLC2.IsChecked == true)
            { 
                DrawPlanWindLoads(WindLoadCases.WLC_BaseB);
                DrawElevationWindLoads(WindLoadCases.WLC_BaseB);
            }
            if (chkLC3.IsChecked == true)
            {
                DrawPlanWindLoads(WindLoadCases.WLC_Balloon1);
                DrawElevationWindLoads(WindLoadCases.WLC_Balloon1);
            }
            if (chkLC4.IsChecked == true)
            {
                DrawPlanWindLoads(WindLoadCases.WLC_Balloon2);
                DrawElevationWindLoads(WindLoadCases.WLC_Balloon2);
            }
            if (chkLC5.IsChecked == true)
            {
                DrawPlanWindLoads(WindLoadCases.WLC_Suction1);
                DrawElevationWindLoads(WindLoadCases.WLC_Suction1);
            }
            if (chkLC6.IsChecked == true)
            {
                DrawPlanWindLoads(WindLoadCases.WLC_Suction2);
                DrawElevationWindLoads(WindLoadCases.WLC_Suction2);
            }
        }

        private Brush GetColorForLoadCase(int loadCaseNumber)
        {
            // Simple color mapping
            switch (loadCaseNumber)
            {
                case 1: return Brushes.Red;
                case 2: return Brushes.Blue;
                case 3: return Brushes.Green;
                case 4: return Brushes.Orange;
                case 5: return Brushes.Purple;
                case 6: return Brushes.Teal;
                default: return Brushes.Black;
            }
        }
    }
}
