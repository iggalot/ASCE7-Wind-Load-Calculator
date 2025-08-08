using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ASCE7WindLoadCalculator
{
    public partial class WindLoadResultsControl_CC : UserControl
    {
        public event EventHandler<OnWindCalculatedEventArgs> WindCalculated;  // the event that signals that the drawing has been updated -- controls will listen for this at the time they are created.

        public class OnWindCalculatedEventArgs : EventArgs
        {
            public WindParameters_Base _parameters { get; }

            public OnWindCalculatedEventArgs(WindParameters_Base parameters)
            {
                _parameters = parameters;
            }
        }

        protected virtual void OnWindCalculated(WindParameters_Base parameters)
        {
            WindCalculated?.Invoke(this, new OnWindCalculatedEventArgs(parameters));
        }

        WindLoadCalculator_Base windLoadCalculator { get; set; } = null; // the calculator for whic this control is based

        public WindLoadResultsControl_CC()
        {
            
        }

        public WindLoadResultsControl_CC(WindLoadCalculator_Base calculator)
        {
            InitializeComponent();

            windLoadCalculator = calculator;

            this.Loaded += WindLoadResultsControl_CC_Loaded;
        }


        private void WindLoadResultsControl_CC_Loaded(object sender, RoutedEventArgs e)
        {
            if (windLoadCalculator != null)
            {
                tbVersion.Text = windLoadCalculator.ASCEVersion.ToString();

                if (windLoadCalculator.RoofAreaCalculator.HasCritDim)
                {
                    sp_a.Visibility = Visibility.Visible;
                    tbl_a.Text = windLoadCalculator.RoofAreaCalculator.CritDim_a.ToString("F2");
                } else {
                    sp_a.Visibility = Visibility.Collapsed;
                }

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

                txtTitle_BuildingLengthWalls.Text = "BuildingLength Wall -- " + windLoadCalculator.WallAreaCalculator_BldgLength.Note;
                txtTitle_BuildingWidthWalls.Text = "BuildingWidth Walls -- " + windLoadCalculator.WallAreaCalculator_BldgWidth.Note;
                SetupComponentAndCladdingFigures();

                PopulateComponentAndCladdingDataGrids();

                DrawEffectiveAreas_OnCCResultCanvas();
            }
        }

        private void PopulateComponentAndCladdingDataGrids()
        {
            Chapter27and30_GCpCurveBase figureCC_Roof = windLoadCalculator.extGCpCurve_Roof;
            Chapter27and30_GCpCurveBase figureCC_Wall = windLoadCalculator.extGCpCurve_Wall;
            if (figureCC_Roof == null || figureCC_Wall == null) return;

            CreateCC_DataGrid_Roof(figureCC_Roof, RoofResultsDataGrid);

            // For the BuildingLength wall
            CreateCC_DataGrid_Walls(figureCC_Wall, WallsResultsDataGrid_SideWall,
                windLoadCalculator.WallAreaCalculator_BldgLength.effWindAreas, "building_length");

            // For the BuildingWidth wall
            CreateCC_DataGrid_Walls(figureCC_Wall, WallsResultsDataGrid_EndWall,
                windLoadCalculator.WallAreaCalculator_BldgWidth.effWindAreas, "building_width");
        }

        private void CreateCC_DataGrid_Roof(Chapter27and30_GCpCurveBase figureCC, DataGrid data_grid)
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
                Header = "GCp+",
                Binding = new Binding("GCp_pos") { StringFormat = "0.00" }
            };
            data_grid.Columns.Add(col_gcp_pos);

            var col_gcp_neg = new DataGridTextColumn
            {
                Header = "GCp-",
                Binding = new Binding("GCp_neg") { StringFormat = "0.00" }
            };
            data_grid.Columns.Add(col_gcp_neg);

            var col_pos_press = new DataGridTextColumn
            {
                Header = "+ Press\n(psf)",
                Binding = new Binding("PosPress") { StringFormat = "0.0" }
            };
            data_grid.Columns.Add(col_pos_press);

            var col_neg_press = new DataGridTextColumn
            {
                Header = "- Press\n(psf)",
                Binding = new Binding("NegPress") { StringFormat = "0.0" }
            };
            data_grid.Columns.Add(col_neg_press);

            var col_pos_net_press = new DataGridTextColumn
            {
                Header = "+ Net\n(psf)",
                Binding = new Binding("NetPosPress") { StringFormat = "0.0" }
            };
            data_grid.Columns.Add(col_pos_net_press);

            var col_neg_net_press = new DataGridTextColumn
            {
                Header = "- Net\n(psf)",
                Binding = new Binding("NetNegPress") { StringFormat = "0.0" }
            };
            data_grid.Columns.Add(col_neg_net_press);

            //var col_overhang_press = new DataGridTextColumn
            //{
            //    Header = "Overhang Press\n(psf)",
            //    Binding = new Binding("OverhangPress") { StringFormat = "0.0" }
            //};
            //data_grid.Columns.Add(col_overhang_press);

            var windLoadResults = new List<CC_WindLoadResultsDataGrid>();
            foreach (KeyValuePair<int, EffectiveWindArea> area in windLoadCalculator.RoofAreaCalculator.effWindAreas)
            {
                CC_WindLoadResultsDataGrid data = new CC_WindLoadResultsDataGrid();
                data.Name = area.Value.Label_Short;
                data.Region = area.Value.Label_Short;
                data.Area = area.Value.Area;

                // positive max external pressure
                PressureData ext_pressure;
                if (windLoadCalculator.windPressureRoof_Pos_External.TryGetValue(area.Key, out ext_pressure))
                {
                    data.qh = ext_pressure.qh;
                    data.GCp_pos = figureCC.RoofCurves_Pos[area.Value.Label_Full].Evaluate(area.Value.Area);
                    data.PosPress = ext_pressure.ExternalPressure;
                    data.NetPosPress = ext_pressure.NetPressure;

                }

                // negative max GCp value
                if (windLoadCalculator.windPressureRoof_Neg_External.TryGetValue(area.Key, out ext_pressure))
                {
                    data.GCp_neg = figureCC.RoofCurves_Neg[area.Value.Label_Full].Evaluate(area.Value.Area);
                    data.NegPress = ext_pressure.ExternalPressure;
                    data.NetNegPress = ext_pressure.NetPressure;

                }

                windLoadResults.Add(data);
            }

            data_grid.ItemsSource = windLoadResults;
        }
        private void CreateCC_DataGrid_Walls(Chapter27and30_GCpCurveBase figureCC, DataGrid data_grid, Dictionary<int, EffectiveWindArea> areas, string wall_type)
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
                Header = "GCp+",
                Binding = new Binding("GCp_pos") { StringFormat = "0.00" }
            };
            data_grid.Columns.Add(col_gcp_pos);

            var col_gcp_neg = new DataGridTextColumn
            {
                Header = "GCp-",
                Binding = new Binding("GCp_neg") { StringFormat = "0.00" }
            };
            data_grid.Columns.Add(col_gcp_neg);

            var col_pos_press = new DataGridTextColumn
            {
                Header = "+ Press\n(psf)",
                Binding = new Binding("PosPress") { StringFormat = "0.0" }
            };
            data_grid.Columns.Add(col_pos_press);

            var col_neg_press = new DataGridTextColumn
            {
                Header = "- Press\n(psf)",
                Binding = new Binding("NegPress") { StringFormat = "0.0" }
            };
            data_grid.Columns.Add(col_neg_press);

            var col_pos_net_press = new DataGridTextColumn
            {
                Header = "+ Net\n(psf)",
                Binding = new Binding("NetPosPress") { StringFormat = "0.0" }
            };
            data_grid.Columns.Add(col_pos_net_press);

            var col_neg_net_press = new DataGridTextColumn
            {
                Header = "- Net\n(psf)",
                Binding = new Binding("NetNegPress") { StringFormat = "0.0" }
            };
            data_grid.Columns.Add(col_neg_net_press);

            var windLoadResults = new List<CC_WindLoadResultsDataGrid>();
            foreach (var area in areas)
            {
                CC_WindLoadResultsDataGrid data = new CC_WindLoadResultsDataGrid();
                data.Name = area.Value.Label_Short;
                data.Region = area.Value.Label_Short;
                data.Area = area.Value.Area;

                if (wall_type == "building_length")
                {
                    // positive max external pressure
                    PressureData ext_pressure;
                    if (windLoadCalculator.windPressureBuildingLengthWall_Pos_External.TryGetValue(area.Key, out ext_pressure))
                    {
                        data.qh = ext_pressure.qh;
                        data.GCp_pos = figureCC.WallCurves_Pos[area.Value.Label_Full].Evaluate(area.Value.Area);
                        data.PosPress = ext_pressure.ExternalPressure;
                        data.NetPosPress = ext_pressure.NetPressure;
                    }

                    // negative max GCp value
                    if (windLoadCalculator.windPressureBuildingLengthWall_Neg_External.TryGetValue(area.Key, out ext_pressure))
                    {
                        data.GCp_neg = figureCC.WallCurves_Neg[area.Value.Label_Full].Evaluate(area.Value.Area);
                        data.NegPress = ext_pressure.ExternalPressure;
                        data.NetNegPress = ext_pressure.NetPressure;
                    }
                }
                else if (wall_type == "building_width")
                {
                    // positive max external pressure
                    PressureData ext_pressure;
                    if (windLoadCalculator.windPressureBuildingWidthWall_Pos_External.TryGetValue(area.Key, out ext_pressure))
                    {
                        data.qh = ext_pressure.qh;
                        data.GCp_pos = figureCC.WallCurves_Pos[area.Value.Label_Full].Evaluate(area.Value.Area);
                        data.PosPress = ext_pressure.ExternalPressure;
                        data.NetPosPress = ext_pressure.NetPressure;
                    }

                    // negative max GCp value
                    if (windLoadCalculator.windPressureBuildingWidthWall_Neg_External.TryGetValue(area.Key, out ext_pressure))
                    {
                        data.GCp_neg = figureCC.WallCurves_Neg[area.Value.Label_Full].Evaluate(area.Value.Area);
                        data.NegPress = ext_pressure.ExternalPressure;
                        data.NetNegPress = ext_pressure.NetPressure;
                    }
                }
                else
                {
                    throw new Exception("ERROR:  In CreateCC_DataGrid_Walls(): Unknown wall type " + wall_type);
                }

                windLoadResults.Add(data);
            }

            data_grid.ItemsSource = windLoadResults;
        }

        private void SetupComponentAndCladdingFigures()
        {
            //TabControlManager.RemoveTab(MainTabControl, tabWindResultsTabItem_MWFRS_BldgLength);
            //TabControlManager.RemoveTab(MainTabControl, tabWindResultsTabItem_MWFRS_BldgWidth);

            var roofCanvas = cnvFigure30_3;
            var roofTitle = txtFigureTitle_Roof;
            var roofCriteria = txtFigureCriteria_Roof;

            var wallCanvas = cnvFigure30_1;
            var wallTitle = txtFigureTitle_Walls;
            var wallCriteria = txtFigureCriteria_Walls;


            Chapter27and30_GCpCurveBase figureCC_Roof = windLoadCalculator.extGCpCurve_Roof;
            Chapter27and30_GCpCurveBase figureCC_Wall = windLoadCalculator.extGCpCurve_Wall;

            roofTitle.Text = figureCC_Roof?.ChartTitle;
            roofCriteria.Text = figureCC_Roof?.ChartCriteria;
            wallTitle.Text = figureCC_Wall?.ChartTitle;
            wallCriteria.Text = figureCC_Wall?.ChartCriteria;

            FigureDrawer.DrawCurvesOnCanvas(roofCanvas, figureCC_Roof);
            FigureDrawer.DrawCurvesOnCanvas(wallCanvas, figureCC_Wall);
        }

        private void DrawEffectiveAreas_OnCCResultCanvas()
        {
            Canvas resultCanvasCC = cnvWindLoadResultCanvasCC;

            if (resultCanvasCC == null) return;

            resultCanvasCC.Children.Clear();
            double scale = Math.Min(resultCanvasCC.Width / windLoadCalculator.buildingData.BuildingWidth, resultCanvasCC.Height / windLoadCalculator.buildingData.BuildingLength);

            foreach (var area in windLoadCalculator.RoofAreaCalculator.effWindAreas)
            {
                Rect boundingBox = windLoadCalculator.RoofAreaCalculator.GetBoundingExtents(windLoadCalculator.RoofAreaCalculator.effWindAreas);
                double canvasWidth = cnvWindLoadResultCanvasCC.ActualWidth;
                double canvasHeight = cnvWindLoadResultCanvasCC.ActualHeight;

                double offsetX = 0;
                double offsetY = 0;

                Brush color = EffectiveWindAreaRenderer.GetColorForRegion(area.Value.Label_Short);
                EffectiveWindAreaRenderer.DrawEffectiveWindArea(cnvWindLoadResultCanvasCC,
                    windLoadCalculator.buildingData,
                    area.Value, boundingBox, offsetX, offsetY, color, Brushes.Black, 1);
            }
        }

        public class CC_WindLoadResultsDataGrid
        {
            public string Name { get; set; }
            public double Area { get; set; }
            public string Region { get; set; } // used to store the regions name so we can draw the rectangle from it
            public double qh { get; set; }
            public double GCp_pos { get; set; }
            public double GCp_neg { get; set; }

            public double PosPress { get; set; }
            public double NegPress { get; set; }
            public double NetPosPress { get; set; }
            public double NetNegPress { get; set; }

            public Brush RectColor => EffectiveWindAreaRenderer.GetColorForRegion(Region);
        }

    }
}
