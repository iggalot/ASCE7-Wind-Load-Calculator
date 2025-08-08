using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ASCE7WindLoadCalculator
{
    /// <summary>
    /// Interaction logic for BuildingDataInputControl.xaml
    /// </summary>
    public partial class BuildingDataInputControl : UserControl
    {
        public event EventHandler<OnBuildingDataInputCompleteEventArgs> BuildingDataInputComplete;  // the event that signals that the drawing has been updated -- controls will listen for this at the time they are created.

        public class OnBuildingDataInputCompleteEventArgs : EventArgs
        {
            public BuildingData _bldg_data { get; }

            public OnBuildingDataInputCompleteEventArgs(BuildingData bldg_data)
            {
                _bldg_data = bldg_data;
            }
        }

        public BuildingData buildingData { get; set; } = null;
        public BuildingData bldgData_temp { get; set; }  // stores the building data while it is being edited
        private bool bFirstLoad = true;
        private bool bIsParsing = false;
        private bool bUpdatingUI = false;  // new flag to prevent infinite UI feedback loop


        public BuildingDataInputControl()
        {
            InitializeComponent();

            this.Loaded += BuildingDataInputControl_Loaded;
        }

        public BuildingDataInputControl(BuildingData bldg_data)
        {
            InitializeComponent();

            buildingData = bldg_data;
            

            
            this.Loaded += BuildingDataInputControl_Loaded;
        }

        private void BuildingDataInputControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (buildingData == null)
            {
                buildingData = new BuildingData();
                buildingData.ValidateRidgeDirection();
            }

            bUpdatingUI = true;
            try
            {

                cmbRoofType.Items.Clear();
                cmbEnclosure.Items.Clear();
                cmbRidgeDirection.Items.Clear();

                // Ridge Direction
                foreach (var value in Enum.GetValues(typeof(RidgeDirections)))
                {
                    cmbRidgeDirection.Items.Add(value);
                }

                switch (buildingData.RidgeDirection)
                {
                    case RidgeDirections.RIDGE_DIR_NONE: cmbRidgeDirection.SelectedIndex = (int)RidgeDirections.RIDGE_DIR_NONE; break;
                    case RidgeDirections.RIDGE_DIR_PARALLEL_TO_BLDGLENGTH: cmbRidgeDirection.SelectedIndex = (int)RidgeDirections.RIDGE_DIR_PARALLEL_TO_BLDGLENGTH; break;
                    case RidgeDirections.RIDGE_DIR_PERP_TO_BLDGLENGTH: cmbRidgeDirection.SelectedIndex = (int)RidgeDirections.RIDGE_DIR_PERP_TO_BLDGLENGTH; break;
                    default: cmbRidgeDirection.SelectedIndex = (int)RidgeDirections.RIDGE_DIR_NONE; break;
                }

                // Building Enclosure type
                foreach (var value in Enum.GetValues(typeof(BuildingEnclosures)))
                {
                    cmbEnclosure.Items.Add(value);
                }

                switch (buildingData.EnclosureType)
                {
                    case BuildingEnclosures.BLDG_ENCLOSED: cmbEnclosure.SelectedIndex = (int)BuildingEnclosures.BLDG_ENCLOSED; break;
                    case BuildingEnclosures.BLDG_PARTIALLY_ENCLOSED: cmbEnclosure.SelectedIndex = (int)BuildingEnclosures.BLDG_PARTIALLY_ENCLOSED; break;
                    case BuildingEnclosures.BLDG_PARTIALLY_OPEN: cmbEnclosure.SelectedIndex = (int)BuildingEnclosures.BLDG_PARTIALLY_OPEN; break;
                    case BuildingEnclosures.BLDG_OPEN: cmbEnclosure.SelectedIndex = (int)BuildingEnclosures.BLDG_OPEN; break;
                    default: cmbEnclosure.SelectedIndex = (int)BuildingEnclosures.BLDG_ENCLOSED; break;
                }


                // Building Enclosure type
                foreach (var value in Enum.GetValues(typeof(RoofTypes)))
                {
                    cmbRoofType.Items.Add(value);
                }

                switch (buildingData.RoofType)
                {
                    case RoofTypes.ROOF_TYPE_FLAT: cmbRoofType.SelectedIndex = (int)RoofTypes.ROOF_TYPE_FLAT; break;
                    case RoofTypes.ROOF_TYPE_GABLE: cmbRoofType.SelectedIndex = (int)RoofTypes.ROOF_TYPE_GABLE; break;
                    case RoofTypes.ROOF_TYPE_HIP: cmbRoofType.SelectedIndex = (int)RoofTypes.ROOF_TYPE_HIP; break;
                    default: cmbRoofType.SelectedIndex = (int)RoofTypes.ROOF_TYPE_FLAT; break;
                }

                BuildingHeightTextBox.Text = buildingData.BuildingHeight.ToString();
                BuildingLengthTextBox.Text = buildingData.BuildingLength.ToString();
                BuildingWidthTextBox.Text = buildingData.BuildingWidth.ToString();

                if (buildingData.RoofType == RoofTypes.ROOF_TYPE_FLAT)
                {
                    buildingData.RoofPitch = 0;
                    spRoofPitch.Visibility = Visibility.Collapsed;
                }
                else
                {
                    spRoofPitch.Visibility = Visibility.Visible;
                }

                tbRoofPitch.Text = buildingData.RoofPitch.ToString();
            } finally
            {
                bUpdatingUI = false;
            }

            bFirstLoad = false;

            UpdateDrawings();
        }

        public void UpdateDrawings()
        {
            // Draw the building
            BuildingDrawer.DrawPlan(cnvBuildingPlanCanvas, buildingData);
            BuildingDrawer.DrawElevation_BuildingLength(cnvBuildingLengthCanvas, buildingData);
            BuildingDrawer.DrawElevation_BuildingWidth(cnvBuildingWidthCanvas, buildingData);
        }

        public virtual void OnBuildingDataInputComplete(BuildingData bldg_data)
        {
            UpdateDrawings();
            BuildingDataInputComplete?.Invoke(this, new OnBuildingDataInputCompleteEventArgs(bldg_data));
        }

        //// Event handler for the Compute Button click
        //private void ComputeButton_Click(object sender, RoutedEventArgs e)
        //{
        //    ParseBuildingData();

        //    OnBuildingDataInputComplete(buildingData); // raise the event where input has been completed
        //}

        public void ParseBuildingData()
        {
            if (bIsParsing || bUpdatingUI)
                return;

            bIsParsing = true;
            try
            {
                // Early out if any field is blank
                if (string.IsNullOrWhiteSpace(BuildingHeightTextBox.Text) ||
                    string.IsNullOrWhiteSpace(BuildingLengthTextBox.Text) ||
                    string.IsNullOrWhiteSpace(BuildingWidthTextBox.Text) ||
                    string.IsNullOrWhiteSpace(tbRoofPitch.Text))
                    return;

                // Temporary parsed values
                bool validHeight = double.TryParse(BuildingHeightTextBox.Text, out double height);
                bool validLength = double.TryParse(BuildingLengthTextBox.Text, out double length);
                bool validWidth = double.TryParse(BuildingWidthTextBox.Text, out double width);
                bool validSlope = double.TryParse(tbRoofPitch.Text, out double pitch);

                if (!validHeight || !validLength || !validWidth || !validSlope)
                    return;

                // Apply parsed values
                buildingData.BuildingHeight = height;
                buildingData.BuildingLength = length;
                buildingData.BuildingWidth = width;
                buildingData.RoofPitch = pitch;

                // Update UI safely (optional formatting)
                bUpdatingUI = true;

                BuildingHeightTextBox.Text = buildingData.BuildingHeight.ToString("0.##");
                BuildingLengthTextBox.Text = buildingData.BuildingLength.ToString("0.##");
                BuildingWidthTextBox.Text = buildingData.BuildingWidth.ToString("0.##");
                tbRoofPitch.Text = buildingData.RoofPitch.ToString("0.#");

                bUpdatingUI = false;


            }
            finally
            {
                bIsParsing = false;
            }

            // **Trigger event here**
            OnBuildingDataInputComplete(buildingData);
        }


        private void cmbRoofType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (bFirstLoad || bIsParsing)
            {
                return;
            }

            ComboBox combo = sender as ComboBox;
            if (combo != null && combo.SelectedItem != null)
            {
                if (combo.SelectedItem is RoofTypes selectedEnum)
                {
                    buildingData.RoofType = selectedEnum;
                    buildingData.ValidateRidgeDirection();

                    if(selectedEnum == RoofTypes.ROOF_TYPE_FLAT)
                    {
                        bUpdatingUI = true;
                        try
                        {
                            spRoofPitch.Visibility = Visibility.Collapsed;
                            buildingData.RoofPitch = 0;
                            tbRoofPitch.Text = buildingData.RoofPitch.ToString();
                            spRoofPitch.Visibility = Visibility.Visible;
                        }
                        finally
                        {
                            bUpdatingUI = false;
                        }

                    } else
                    {
                        bUpdatingUI = true;
                        try
                        {
                            tbRoofPitch.Text = buildingData.RoofPitch.ToString();
                            spRoofPitch.Visibility = Visibility.Visible;
                        }
                        finally
                        {
                            bUpdatingUI = false;
                        }
                    }
                }
            }

            try
            {
                ParseBuildingData();
            }
            finally
            {
            }
        }

        private void cmbRidgeDirection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (bFirstLoad || bIsParsing)
            {
                return;
            }

            ComboBox combo = sender as ComboBox;
            if (combo != null && combo.SelectedItem != null)
            {
                if (combo.SelectedItem is RidgeDirections selectedEnum)
                {
                    buildingData.RidgeDirection = selectedEnum;
                    buildingData.ValidateRidgeDirection();
                }

            }

            try
            {
                ParseBuildingData();
            }
            finally
            {
            }
        }

        private void cmbBuildingEnclosure_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (bFirstLoad || bIsParsing)
            {
                return;
            }

            ComboBox combo = sender as ComboBox;
            if (combo != null && combo.SelectedItem != null)
            {
                if (combo.SelectedItem is BuildingEnclosures selectedEnum)
                {
                    buildingData.EnclosureType = selectedEnum;
                    buildingData.ValidateRidgeDirection();
                }

            }

            try
            {
                ParseBuildingData();

            }
            finally
            {
            }
        }

        private void BuildingData_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                CommitBuildingDataChanges();
            }
        }

        private void BuildingData_LostFocus(object sender, RoutedEventArgs e)
        {
            CommitBuildingDataChanges();
        }

        private void CommitBuildingDataChanges()
        {
            if (bFirstLoad || bIsParsing || bUpdatingUI)
                return;

            try
            {
                ParseBuildingData();
            }
            finally
            {
            }
        }

        private void RootGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && Keyboard.FocusedElement is TextBox)
            {
                // Move focus to the root grid itself
                Keyboard.ClearFocus();
                FocusManager.SetFocusedElement(FocusManager.GetFocusScope(RootGrid), RootGrid);
                e.Handled = true;

                ParseBuildingData();
            }
        }
    }
}
