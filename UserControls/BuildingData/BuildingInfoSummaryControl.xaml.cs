using System.Windows.Controls;

namespace ASCE7WindLoadCalculator
{
    /// <summary>
    /// Interaction logic for BuildingInfoSummary.xaml
    /// </summary>
    public partial class BuildingInfoSummaryControl : UserControl
    {
        BuildingData buildingData { get; set ; }
        public BuildingInfoSummaryControl()
        {
        }
        public BuildingInfoSummaryControl(BuildingData bldg_data)
        {
            InitializeComponent();

            buildingData = bldg_data;

            this.Loaded += (s, e) =>
            {
                this.DataContext = buildingData;

                tbRoofType.Text = this.buildingData.RoofType.ToString();
                tbBuildingLength.Text = this.buildingData.BuildingLength.ToString("F2");
                tbBuildingWidth.Text = this.buildingData.BuildingWidth.ToString("F2");
                tbBuildingHeight.Text = this.buildingData.BuildingHeight.ToString("F2");
                tbRoofPitch.Text = this.buildingData.RoofPitch.ToString("F2");
                tbMeanRoofHeight.Text = this.buildingData.MeanRoofHeight.ToString("F2");
                tbRidgeHeight.Text = this.buildingData.RidgeHeight.ToString("F2");
                tbh_Over_B.Text = this.buildingData.h_Over_B.ToString("F2");
                tbh_Over_L.Text = this.buildingData.h_Over_L.ToString("F2");
                tbL_Over_B.Text = this.buildingData.L_Over_B.ToString("F2");
            };
        }
    }
}
