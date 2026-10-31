using System.Windows;
using System.Windows.Controls;
using ASCE7WindLoadCalculator;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ASCE7Wind.UI.Tests
{
    /// <summary>
    /// Bad typed input (main 6824457, merged into dev): the control says so on the screen and clears the results; it never
    /// computes from a substituted value. Good input computes again.
    /// </summary>
    [TestClass]
    public class WindLoadInputControlTests
    {
        [STATestMethod]
        public void InvalidWindSpeed_IsReported_AndClearsTheResults()
        {
            var b = new BuildingData { BuildingLength = 60, BuildingWidth = 40, BuildingHeight = 15, RoofType = RoofTypes.ROOF_TYPE_FLAT };
            var p = new WindParameters_Base { WindSpeed = 115, ExposureCategory = WindExposureCategories.WIND_EXP_CAT_C };
            var input = new WindLoadInputControl(b, ASCE7_Versions.ASCE_VER_7_22, p);
            WindLoadCalculator_Base cc = null;
            input.WindInputComplete += (s, e) => { cc = e._windLoadCalculator_CC; };
            var win = new Window { Content = input, Width = 1200, Height = 900, Left = -20000, Top = -20000, ShowInTaskbar = false, ShowActivated = false, WindowStyle = WindowStyle.None };
            win.Show();
            input.UpdateLayout();

            input.Update();
            Assert.IsNotNull(cc);
            Assert.IsNull(input.LastError);

            var speed = (TextBox)input.FindName("WindSpeedTextBox");
            speed.Text = "abc";
            input.Update();
            Assert.IsNull(cc, "no result from a bad wind speed");
            StringAssert.StartsWith(input.LastError, "Invalid wind speed");
            Assert.AreEqual(Visibility.Visible, ((TextBlock)input.FindName("tbCalcError")).Visibility);

            speed.Text = "0";
            input.Update();
            Assert.IsNull(cc, "zero is not a wind speed");

            speed.Text = "130";
            input.Update();
            Assert.IsNotNull(cc);
            Assert.IsNull(input.LastError);
            Assert.AreEqual(130, input.Parameters.WindSpeed, 1e-9);
            win.Close();
        }
    }
}
