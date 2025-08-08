using System;
using System.Windows;
using System.Windows.Controls;

namespace ASCE7WindLoadCalculator
{
    public partial class WindLoadInputControl : UserControl
    {
        public event EventHandler<OnWindInputCompleteEventArgs> WindInputComplete;  // the event that signals that the drawing has been updated -- controls will listen for this at the time they are created.

        public class OnWindInputCompleteEventArgs : EventArgs
        {
            public WindParameters_Base _parameters { get; }
            public ASCE7_Versions _version { get; }
            public BuildingData _bldg_data { get; }

            public OnWindInputCompleteEventArgs(WindParameters_Base parameters, ASCE7_Versions version)
            {
                _parameters = parameters;
                _version = version;
            }
        }
        public BuildingData buildingData { get; set; } = null;
        public WindParameters_Base Parameters { get; set; } = null;
        public ASCE7_Versions Version { get; set; }


        public WindLoadInputControl()
        {
            InitializeComponent();

            this.Loaded += WindLoadInputControl_Loaded;
        }

        public WindLoadInputControl(BuildingData bldg_data, WindParameters_Base parameters = null)
        {
            InitializeComponent();

            this.Parameters = parameters;
            this.buildingData = bldg_data;

            this.Loaded += WindLoadInputControl_Loaded;
        }

        public WindLoadInputControl(BuildingData bldg_data, ASCE7_Versions version, WindParameters_Base parameters = null)
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

            }
            this.buildingData = bldg_data;
            this.Version = version;
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

        public virtual void OnWindInputComplete(WindParameters_Base parameters, ASCE7_Versions version)
        {
            WindInputComplete?.Invoke(this, new OnWindInputCompleteEventArgs(parameters, version));
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

            OnWindInputComplete(Parameters, version); // raise the event where input has been completed
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
