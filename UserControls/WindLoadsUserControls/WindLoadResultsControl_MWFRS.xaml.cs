using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ASCE7WindLoadCalculator
{
    public partial class WindLoadResultsControl_MWFRS : UserControl
    {
        public event EventHandler<OnWindCalculatedEventArgs> WindCalculated;  // the event that signals that the drawing has been updated -- controls will listen for this at the time they are created.

        public class OnWindCalculatedEventArgs : EventArgs
        {
            public WindParameters_Base _parameters { get; }
            public BuildingData _bldg_data { get; }

            public OnWindCalculatedEventArgs(WindParameters_Base parameters, BuildingData bldg_data)
            {
                _parameters = parameters;
                _bldg_data = bldg_data;
            }
        }

        protected virtual void OnWindCalculated(WindParameters_Base parameters, BuildingData bldg_data)
        {
            WindCalculated?.Invoke(this, new OnWindCalculatedEventArgs(parameters, bldg_data));
        }

        WindLoadCalculator_Base windLoadCalculator { get; set; } = null; // the calculator for whic this control is based


        public WindLoadResultsControl_MWFRS()
        {
            
        }

        public WindLoadResultsControl_MWFRS(WindLoadCalculator_Base calculator)
        {
            InitializeComponent();

            windLoadCalculator = calculator;

            this.Loaded += WindLoadResultsControl_MWFRS_Loaded;
        }

        private void WindLoadResultsControl_MWFRS_Loaded(object sender, RoutedEventArgs e)
        {
            if (windLoadCalculator != null)
            {
                tbVersion.Text = windLoadCalculator.ASCEVersion.ToString();

                // populate the bulding data summary
                if (windLoadCalculator.buildingData == null)
                {
                    spBuildingData.Visibility = Visibility.Collapsed;
                }
                else
                {
                    spBuildingData.Visibility = Visibility.Visible;

                    spBuildingData.Children.Clear();
                    BuildingInfoSummaryControl ctrl = new BuildingInfoSummaryControl(windLoadCalculator.buildingData);
                    spBuildingData.Children.Add(ctrl);
                }

                txtTitle_BuildingLengthWalls.Text = "Walls -- " + windLoadCalculator.WallAreaCalculator_BldgLength.Note;

                BuildingDrawer.DrawPlan(cnvMWFRSPlan, windLoadCalculator.buildingData);
                BuildingDrawer.DrawElevation_BuildingLength(cnvMWFRSElevation_BuildingLength, windLoadCalculator.buildingData);
                BuildingDrawer.DrawElevation_BuildingWidth(cnvMWFRSElevation_BuildingWidth, windLoadCalculator.buildingData);

                //EffectiveWindAreaRenderer.Draw(cnvEffectiveRoofAreas, windLoadCalculator.RoofAreaCalculator, windLoadCalculator.buildingData, "Effective Roof Areas");
                //EffectiveWindAreaRenderer.Draw(cnvEffectiveWallAreas_Length, windLoadCalculator.WallAreaCalculator_BldgLength, windLoadCalculator.buildingData, "Effective Wall Areas");
                //EffectiveWindAreaRenderer.Draw(cnvEffectiveWallAreas_Width, windLoadCalculator.WallAreaCalculator_BldgWidth, windLoadCalculator.buildingData, "Effective Wall Areas");

                CreateMWFRS_DataGrid_Walls(MWFRS_WallResultsDataGrid, windLoadCalculator.WallAreaCalculator_BldgLength.effWindAreas, windLoadCalculator);

                CreateMWFRS_DataGrid_Roof(MWFRS_RoofResultsDataGrid, windLoadCalculator.RoofAreaCalculator.effWindAreas, windLoadCalculator);

                DrawEffectiveAreas_OnMWFRSResultCanvas();
            }

        }

        private void CreateMWFRS_DataGrid_Roof(DataGrid data_grid, Dictionary<int, EffectiveWindArea> areas, WindLoadCalculator_Base calc)
        {
            // Get the datagrid from the results control
            data_grid.ItemsSource = null;
            data_grid.Columns.Clear();
            data_grid.AutoGenerateColumns = false;

            // create our data object
            var col_name = new DataGridTextColumn
            {
                Header = "Name",
                Binding = new Binding("Name")
            };
            data_grid.Columns.Add(col_name);

            var col_rectangle = new DataGridTemplateColumn
            {
                Header = ""
            };

            // Define the DataTemplate in code for drawing a colored rectangle
            var factory = new FrameworkElementFactory(typeof(Rectangle));
            factory.SetValue(Rectangle.WidthProperty, 15.0);
            factory.SetValue(Rectangle.HeightProperty, 15.0);
            factory.SetValue(Rectangle.StrokeProperty, Brushes.Black);
            factory.SetBinding(Rectangle.FillProperty, new Binding("RectColor"));

            col_rectangle.CellTemplate = new DataTemplate { VisualTree = factory };
            data_grid.Columns.Add(col_rectangle);

            var col_area = new DataGridTextColumn
            {
                Header = "Area\n(sq ft)",
                Binding = new Binding("Area") { StringFormat = "0" }
            };
            data_grid.Columns.Add(col_area);

            var col_qh = new DataGridTextColumn
            {
                Header = "qh\n(psf)",
                Binding = new Binding("qh") { StringFormat = "0.0" }
            };
            data_grid.Columns.Add(col_qh);

            var col_gcp_pos = new DataGridTextColumn
            {
                Header = "Case A\nGCp",
                Binding = new Binding("GCp_A") { StringFormat = "0.00" }
            };
            data_grid.Columns.Add(col_gcp_pos);

            var col_pos_press = new DataGridTextColumn
            {
                Header = "Press A\n(psf)",
                Binding = new Binding("Press_A") { StringFormat = "0.0" }
            };
            data_grid.Columns.Add(col_pos_press);

            var col_pos_net_press = new DataGridTextColumn
            {
                Header = "Net A\n(psf)",
                Binding = new Binding("NetPress_A") { StringFormat = "0.0" }
            };
            data_grid.Columns.Add(col_pos_net_press);



            var col_gcp_neg = new DataGridTextColumn
            {
                Header = "Case B\nGCp",
                Binding = new Binding("GCp_B") { StringFormat = "0.00" }
            };
            data_grid.Columns.Add(col_gcp_neg);

            var col_neg_press = new DataGridTextColumn
            {
                Header = "Press B\n(psf)",
                Binding = new Binding("Press_B") { StringFormat = "0.0" }
            };
            data_grid.Columns.Add(col_neg_press);

            var col_neg_net_press = new DataGridTextColumn
            {
                Header = "Net B\n(psf)",
                Binding = new Binding("NetPress_B") { StringFormat = "0.0" }
            };
            data_grid.Columns.Add(col_neg_net_press);

            //var col_overhang_press = new DataGridTextColumn
            //{
            //    Header = "Overhang Press\n(psf)",
            //    Binding = new Binding("OverhangPress") { StringFormat = "0.0" }
            //};
            //data_grid.Columns.Add(col_overhang_press);

            var windLoadResults = new List<MWFRS_WindLoadResultsDataGrid_Roof>();
            foreach (KeyValuePair<int, EffectiveWindArea> area in windLoadCalculator.RoofAreaCalculator.effWindAreas)
            {
                MWFRS_WindLoadResultsDataGrid_Roof data = new MWFRS_WindLoadResultsDataGrid_Roof();
                data.Name = area.Value.Label_Short;
                data.Region = area.Value.Label_Short;
                data.Area = area.Value.Area;

                // positive max external pressure
                PressureData ext_pressure;
                if (windLoadCalculator.windPressureRoof_Pos_External.TryGetValue(area.Key, out ext_pressure))
                {
                    data.qh = ext_pressure.qh;
                    data.GCp_A = ext_pressure.GCp;
                    data.Press_A = ext_pressure.ExternalPressure;
                    data.NetPress_A = ext_pressure.NetPressure;

                }

                // negative max GCp value
                if (windLoadCalculator.windPressureRoof_Neg_External.TryGetValue(area.Key, out ext_pressure))
                {
                    data.qh = ext_pressure.qh;
                    data.GCp_B = ext_pressure.GCp;
                    data.Press_B = ext_pressure.ExternalPressure;
                    data.NetPress_B = ext_pressure.NetPressure;
                }

                windLoadResults.Add(data);
            }

            data_grid.ItemsSource = windLoadResults;
        }


        private void CreateMWFRS_DataGrid_Walls(DataGrid data_grid, Dictionary<int, EffectiveWindArea> areas, WindLoadCalculator_Base calc)
        {
            // Get the datagrid from the results control
            data_grid.ItemsSource = null;
            data_grid.Columns.Clear();
            data_grid.AutoGenerateColumns = false;

            // create our data object
            var col_name = new DataGridTextColumn
            {
                Header = "Name",
                Binding = new Binding("Name")
            };
            data_grid.Columns.Add(col_name);

            var col_rectangle = new DataGridTemplateColumn
            {
                Header = ""
            };

            // Define the DataTemplate in code for drawing a colored rectangle
            var factory = new FrameworkElementFactory(typeof(Rectangle));
            factory.SetValue(Rectangle.WidthProperty, 15.0);
            factory.SetValue(Rectangle.HeightProperty, 15.0);
            factory.SetValue(Rectangle.StrokeProperty, Brushes.Black);
            factory.SetBinding(Rectangle.FillProperty, new Binding("RectColor"));

            col_rectangle.CellTemplate = new DataTemplate { VisualTree = factory };
            data_grid.Columns.Add(col_rectangle);

            var col_area = new DataGridTextColumn
            {
                Header = "Area\n(sq ft)",
                Binding = new Binding("Area") { StringFormat = "0" }
            };
            data_grid.Columns.Add(col_area);

            var col_qh = new DataGridTextColumn
            {
                Header = "qh\n(psf)",
                Binding = new Binding("qh") { StringFormat = "0.0" }
            };
            data_grid.Columns.Add(col_qh);

            var col_gcp_pos = new DataGridTextColumn
            {
                Header = "Cp",
                Binding = new Binding("Cp") { StringFormat = "0.00" }
            };
            data_grid.Columns.Add(col_gcp_pos);


            var col_pos_press = new DataGridTextColumn
            {
                Header = "Press\n(psf)",
                Binding = new Binding("Press") { StringFormat = "0.0" }
            };
            data_grid.Columns.Add(col_pos_press);

            var col_pos_net_press = new DataGridTextColumn
            {
                Header = "Net\n(psf)",
                Binding = new Binding("NetPress") { StringFormat = "0.0" }
            };
            data_grid.Columns.Add(col_pos_net_press);

            var windLoadResults = new List<MWFRS_WindLoadResultsDataGrid_Wall>();
            foreach (var area in areas)
            {
                MWFRS_WindLoadResultsDataGrid_Wall data = new MWFRS_WindLoadResultsDataGrid_Wall();
                data.Name = area.Value.Label_Short;
                data.Region = area.Value.Label_Short;
                data.Area = area.Value.Area;

                // positive max external pressure
                PressureData ext_pressure;
                if (calc.windPressureWall_Pos_External_MWFRS.TryGetValue(area.Key, out ext_pressure))
                {
                    data.qh = ext_pressure.qh;
                    data.Cp = ext_pressure.GCp;
                    data.Press = ext_pressure.ExternalPressure;
                    data.NetPress = ext_pressure.NetPressure;
                }

                windLoadResults.Add(data);
            }

            data_grid.ItemsSource = windLoadResults;
        }

        public class MWFRS_WindLoadResultsDataGrid_Wall
        {
            public string Name { get; set; }
            public double Area { get; set; }
            public string Region { get; set; } // used to store the regions name so we can draw the rectangle from it
            public double qh { get; set; }
            public double Cp { get; set; }

            public double Press { get; set; }
            public double NetPress { get; set; }


            public Brush RectColor => EffectiveWindAreaRenderer.GetColorForRegion(Region);
        }

        public class MWFRS_WindLoadResultsDataGrid_Roof
        {
            public string Name { get; set; }
            public double Area { get; set; }
            public string Region { get; set; } // used to store the regions name so we can draw the rectangle from it
            public double qh { get; set; }
            public double GCp_A { get; set; }
            public double GCp_B { get; set; }

            public double Press_A { get; set; }
            public double Press_B { get; set; }
            public double NetPress_A { get; set; }
            public double NetPress_B { get; set; }

            public Brush RectColor => EffectiveWindAreaRenderer.GetColorForRegion(Region);
        }

        private void DrawEffectiveAreas_OnMWFRSResultCanvas()
        {
            cnvEffectiveRoofAreas.Children.Clear();
            cnvEffectiveWallAreas_WW.Children.Clear();
            cnvEffectiveWallAreas_LW.Children.Clear();
            cnvEffectiveWallAreas_SW.Children.Clear();


            // Draw the roof areas
            foreach (var area in windLoadCalculator.RoofAreaCalculator.effWindAreas)
            {
                Rect boundingBox = windLoadCalculator.RoofAreaCalculator.GetBoundingExtents(windLoadCalculator.RoofAreaCalculator.effWindAreas);
                double canvasWidth = cnvEffectiveRoofAreas.ActualWidth;
                double canvasHeight = cnvEffectiveRoofAreas.ActualHeight;

                double offsetX = 0;
                double offsetY = 0;

                Brush color = EffectiveWindAreaRenderer.GetColorForRegion(area.Value.Label_Short);
                EffectiveWindAreaRenderer.DrawEffectiveWindArea(cnvEffectiveRoofAreas,
                    windLoadCalculator.buildingData,
                    area.Value, boundingBox, offsetX, offsetY, color, Brushes.Black, 1);
            }

            // Draw WW wall areas
            foreach (var area in windLoadCalculator.WallAreaCalculator_BldgLength.effWindAreas)
            {
                Canvas cnv = null;

                switch (area.Value.Label_Short)
                {
                    case "WW":
                        cnv = cnvEffectiveWallAreas_WW;
                        break;
                    case "SW":
                        cnv = cnvEffectiveWallAreas_SW;
                        break;
                    case "LW":
                        cnv = cnvEffectiveWallAreas_LW;
                        break;
                }

                if (cnv == null) continue;

                Rect boundingBox = windLoadCalculator.WallAreaCalculator_BldgLength.GetBoundingExtents(windLoadCalculator.WallAreaCalculator_BldgLength.effWindAreas);
                double canvasWidth = cnv.ActualWidth;
                double canvasHeight = cnv.ActualHeight;

                double offsetX = 0;
                double offsetY = 0;

                Brush color = EffectiveWindAreaRenderer.GetColorForRegion(area.Value.Label_Short);
                EffectiveWindAreaRenderer.DrawEffectiveWindArea(cnv,
                    windLoadCalculator.buildingData,
                    area.Value, boundingBox, offsetX, offsetY, color, Brushes.Black, 1);
            }
        }
    }
}
