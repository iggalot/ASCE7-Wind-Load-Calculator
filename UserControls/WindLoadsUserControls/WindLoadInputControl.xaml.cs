using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ASCE7WindLoadCalculator
{
    public partial class WindLoadInputControl : UserControl
    {
        public event EventHandler<OnWindInputCompleteEventArgs> WindInputComplete;  // the event that signals that the drawing has been updated -- controls will listen for this at the time they are created.

        public class OnWindInputCompleteEventArgs : EventArgs
        {
            public WindLoadCalculator_Base _windLoadCalculator_CC { get; set; }
            public WindLoadCalculator_Base _windLoadCalculator_MWFRS_Length { get; set; }
            public WindLoadCalculator_Base _windLoadCalculator_MWFRS_Width { get; set; }


            public OnWindInputCompleteEventArgs(WindLoadCalculator_Base calc_cc, WindLoadCalculator_Base calc_mwfrs_length, WindLoadCalculator_Base calc_mwfrs_width)
            {
                _windLoadCalculator_CC = calc_cc;
                _windLoadCalculator_MWFRS_Length = calc_mwfrs_length;
                _windLoadCalculator_MWFRS_Width = calc_mwfrs_width;
            }
        }
        public WindLoadCalculator_Base windLoadCalculator { get; set; }

        public WindLoadCalculator_Base windLoadCalculator_MWFRS_Length { get; set; }
        public WindLoadCalculator_Base windLoadCalculator_MWFRS_Width { get; set; }
        public WindLoadCalculator_Base windLoadCalculator_CC { get; set; }

        public BuildingData buildingData { get; set; } = null;
        public WindParameters_Base Parameters { get; set; } = null;
        public ASCE7_Versions Version { get; set; }


        public WindLoadInputControl()
        {
            InitializeComponent();

            this.Loaded += WindLoadInputControl_Loaded;
        }

        public WindLoadInputControl(BuildingData bldg_data, ASCE7_Versions? version = null, WindParameters_Base parameters = null)
        {
            InitializeComponent();

            // in case we don't have a building defined (usually first run)
            if (bldg_data == null)
            {
                bldg_data = new BuildingData();
            }

            if (parameters == null)
            {
                if(bldg_data.RoofType == RoofTypes.ROOF_TYPE_FLAT)  parameters = new FlatRoofWindLoadParameters();
                else if (bldg_data.RoofType == RoofTypes.ROOF_TYPE_GABLE) parameters = new GableRoofWindLoadParameters();
                else if (bldg_data.RoofType == RoofTypes.ROOF_TYPE_HIP) parameters = new HipRoofWindLoadParameters();
                else throw new Exception("ERROR: In WindLoadInputControl: Unrecognized roof type." + bldg_data.RoofType.ToString());

                version = ASCE7_Versions.ASCE_VER_7_16;
            }

            if (version == null)
            {
                version = ASCE7_Versions.ASCE_VER_7_16;
            }

            this.buildingData = bldg_data;
            this.Version = (ASCE7_Versions)version;
            this.Parameters = parameters;

            this.Loaded += WindLoadInputControl_Loaded;
        }

        private void WindLoadInputControl_Loaded(object sender, RoutedEventArgs e)
        {
            // populate the bulding data summary
            if(this.buildingData == null)
            {
                spBuildingData.Visibility = Visibility.Collapsed;
            } else
            {
                spBuildingData.Visibility = Visibility.Visible;
            
                spBuildingData.Children.Clear();
                BuildingInfoSummaryControl ctrl = new BuildingInfoSummaryControl(buildingData);
                spBuildingData.Children.Add(ctrl);
            }

            // populate the combo boxes.
            cmbASCEVersion.Items.Clear();

            foreach (var value in Enum.GetValues(typeof(ASCE7_Versions)))
            {
                cmbASCEVersion.Items.Add(value);
            }

            // populate existing parameters if any
            if (this.Parameters != null)
            {
                WindSpeedTextBox.Text = Parameters.WindSpeed.ToString();
                KztTextBox.Text = Parameters.Kzt.ToString();
                ImportanceFactorTextBox.Text = Parameters.ImportanceFactor.ToString();

                bool found_version = false;
                foreach (ASCE7_Versions item in Enum.GetValues(typeof(ASCE7_Versions)))
                {
                    if (item == Version)
                    {
                        cmbASCEVersion.SelectedIndex = (int)item;
                        found_version = true;
                        break;
                    }
                }
                if (found_version == false)
                {
                    throw new Exception("ERROR:  In WindLoadInputControl_Loaded() -- Version " + Version.ToString() + " not found.");
                }

            } else
            {
                cmbASCEVersion.SelectedIndex = (int)ASCE7_Versions.ASCE_VER_7_16;
            }
        }

        public void UpdateDrawings()
        {
            if (windLoadCalculator_CC != null)
            {

                foreach (var area in windLoadCalculator_CC.RoofAreaCalculator.effWindAreas)
                {
                    Rect boundingBox = windLoadCalculator_CC.RoofAreaCalculator.GetBoundingExtents(windLoadCalculator_CC.RoofAreaCalculator.effWindAreas);
                    double canvasWidth = cnvEffectiveRoofAreas_CC.ActualWidth;
                    double canvasHeight = cnvEffectiveRoofAreas_CC.ActualHeight;

                    double offsetX = 0;
                    double offsetY = 0;

                    Brush color = EffectiveWindAreaRenderer.GetColorForRegion(area.Value.Label_Short);
                    EffectiveWindAreaRenderer.DrawEffectiveWindArea(cnvEffectiveRoofAreas_CC,
                        windLoadCalculator_CC.buildingData,
                        area.Value, boundingBox, offsetX, offsetY, color, Brushes.Black, 1);
                }
                BuildingDrawer.DrawPlan(cnvBuildingPlan_CC, windLoadCalculator_CC.buildingData);

            }

            if (windLoadCalculator_MWFRS_Length != null)
            {
                foreach (var area in windLoadCalculator_MWFRS_Length.RoofAreaCalculator.effWindAreas)
                {
                    Rect boundingBox = windLoadCalculator_CC.RoofAreaCalculator.GetBoundingExtents(windLoadCalculator_MWFRS_Length.RoofAreaCalculator.effWindAreas);
                    double canvasWidth = cnvEffectiveRoofAreas_MWFRS_Length.ActualWidth;
                    double canvasHeight = cnvEffectiveRoofAreas_MWFRS_Length.ActualHeight;

                    double offsetX = 0;
                    double offsetY = 0;

                    Brush color = EffectiveWindAreaRenderer.GetColorForRegion(area.Value.Label_Short);
                    EffectiveWindAreaRenderer.DrawEffectiveWindArea(cnvEffectiveRoofAreas_MWFRS_Length,
                        windLoadCalculator_MWFRS_Length.buildingData,
                        area.Value, boundingBox, offsetX, offsetY, color, Brushes.Black, 1);
                }
                BuildingDrawer.DrawPlan(cnvBuildingPlan_MWFRS_Length, windLoadCalculator_MWFRS_Length.buildingData);

            }

            if (windLoadCalculator_MWFRS_Width != null)
            {
                foreach (var area in windLoadCalculator_MWFRS_Width.RoofAreaCalculator.effWindAreas)
                {
                    Rect boundingBox = windLoadCalculator_MWFRS_Width.RoofAreaCalculator.GetBoundingExtents(windLoadCalculator_MWFRS_Length.RoofAreaCalculator.effWindAreas);
                    double canvasWidth = cnvEffectiveRoofAreas_MWFRS_Width.ActualWidth;
                    double canvasHeight = cnvEffectiveRoofAreas_MWFRS_Width.ActualHeight;

                    double offsetX = 0;
                    double offsetY = 0;

                    Brush color = EffectiveWindAreaRenderer.GetColorForRegion(area.Value.Label_Short);
                    EffectiveWindAreaRenderer.DrawEffectiveWindArea(cnvEffectiveRoofAreas_MWFRS_Width,
                        windLoadCalculator_MWFRS_Width.buildingData,
                        area.Value, boundingBox, offsetX, offsetY, color, Brushes.Black, 1);
                }
                BuildingDrawer.DrawPlan(cnvBuildingplan_MWFRS_Width, windLoadCalculator_MWFRS_Width.buildingData);

            }

        }

        public virtual void OnWindInputComplete(WindLoadCalculator_Base calc_cc, WindLoadCalculator_Base calc_mwfrs_length, WindLoadCalculator_Base calc_mwfrs_width)
        {
            UpdateDrawings();
            WindInputComplete?.Invoke(this, new OnWindInputCompleteEventArgs(calc_cc, calc_mwfrs_length, calc_mwfrs_width));
        }

        // Event handler for the Compute Button click
        private void ComputeButton_Click(object sender, RoutedEventArgs e)
        {
            if(buildingData == null)
            {
                MessageBox.Show("Please input building data first");
                return;
            }

            var version_index = cmbASCEVersion.SelectedIndex;
            ASCE7_Versions version;
            switch (version_index)
            {
                case (int)ASCE7_Versions.ASCE_VER_7_16:
                    version = ASCE7_Versions.ASCE_VER_7_16;
                    break;
                case (int)ASCE7_Versions.ASCE_VER_7_22:
                    version = ASCE7_Versions.ASCE_VER_7_22;
                    break;
                default:
                    throw new Exception("ERROR:  In WindLoadInputControl_Loaded() -- Version " + version_index.ToString() + " not found.");

            }
            Parameters = GetWindLoadParameters(buildingData.RoofType, version);

            MakeCalculators();

            OnWindInputComplete(windLoadCalculator_CC, windLoadCalculator_MWFRS_Length, windLoadCalculator_MWFRS_Width); // raise the event where input has been completed
        }

        /// <summary>
        /// Creates the two wind load calculators...one for where the wind is acting on the BuildingWidth wall 
        /// and the other for where the wind is acting on the BuildingLength wall
        /// </summary>
        private void MakeCalculators()
        {
            // Setup our buildings -- building 1 is the original building
            // and building 2 is the rotated (flipped) building
            var bldg_data1 = buildingData;
            var bldg_data2 = bldg_data1.Clone();
            bldg_data2.RotateBuilding();

            // Create MWFRS calculators
            var mwfrs_calc_building_length = CreateAndComputeCalculator(bldg_data1, WindLoadCalculationTypes.MWFRS);
            windLoadCalculator_MWFRS_Length = mwfrs_calc_building_length;

            var mwfrs_calc_building_width = CreateAndComputeCalculator(bldg_data2, WindLoadCalculationTypes.MWFRS);
            windLoadCalculator_MWFRS_Width = mwfrs_calc_building_width;

            // Create CC calculator using building data #1
            var cc_calc_building_length = CreateAndComputeCalculator(bldg_data1, WindLoadCalculationTypes.COMPONENT_AND_CLADDING);
            windLoadCalculator_CC = cc_calc_building_length;
        }

        private WindLoadCalculator_Base CreateAndComputeCalculator(BuildingData building, WindLoadCalculationTypes type)
        {
            var parameters = Parameters.Clone();
            parameters.AnalysisType = type;

            var calculator = WindLoadCalculatorFactory.Create(Version, type, parameters, building);
            calculator.Initialize();
            return calculator;
        }

        // Method to retrieve parameters from the input fields
        private WindParameters_Base GetWindLoadParameters(RoofTypes roof_type, ASCE7_Versions version)
        {
            Version = version;

            double windSpeed = double.Parse(WindSpeedTextBox.Text);
            double kzt = double.Parse(KztTextBox.Text);
            double importance = double.Parse(ImportanceFactorTextBox.Text);
            string risk = ((ComboBoxItem)RiskCategoryComboBox.SelectedItem).Content.ToString();
            //WindLoadCalculationTypes analysis_type = (WindLoadCalculationTypes)cmbWindAnalysisType.SelectedIndex;
            string exposure_string = ((ComboBoxItem)ExposureCategoryComboBox.SelectedItem).Content.ToString();
            WindExposureCategories exposure;

            switch (exposure_string)
            {
                case "B":
                    exposure = WindExposureCategories.WIND_EXP_CAT_B;
                    break;
                case "C":
                    exposure = WindExposureCategories.WIND_EXP_CAT_C;
                    break;
                case "D":
                    exposure = WindExposureCategories.WIND_EXP_CAT_D;
                    break;
                default:
                    exposure = WindExposureCategories.WIND_EXP_CAT_C;
                    break;
            }

            var windParams = WindLoadParametersFactory.Create(
                roof_type, 
                risk, 
                windSpeed, 
                exposure, 
                kzt, 
                importance
                
                //, 
                //analysis_type
                );

            return windParams;
        }
    }
}
