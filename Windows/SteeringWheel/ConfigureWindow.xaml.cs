using System.Windows;
using System.Windows.Input;
using Xceed.Wpf.Toolkit;

namespace SteeringWheel
{
    /// <summary>
    /// Interaction logic for ConfigureWindow.xaml
    /// </summary>
    public partial class ConfigureWindow : Window
    {
        private readonly Controller controllerService;

        public ConfigureWindow(Controller controller)
        {
            controllerService = controller;
            InitializeComponent();
        }

        private void ConfigureWindow_MouseDown(object sender, MouseButtonEventArgs e)
        {
            DefaultGrid.Focus();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.Key == Key.Return)
            {
                DefaultGrid.Focus();
                Keyboard.ClearFocus();
            }
        }

        private void ControlSteeringMinMax_LowerValueChanged(object sender, RoutedEventArgs e)
        {
            if (e.Source is RangeSlider item && item != null)
            {
                controllerService.CAP_SteeringMin = (float)item.LowerValue;
            }
        }

        private void ControlSteeringMinMax_HigherValueChanged(object sender, RoutedEventArgs e)
        {
            if (e.Source is RangeSlider item && item != null)
            {
                controllerService.CAP_SteeringMax = (float)item.HigherValue;
            }
        }

        private void ControlAccMinMax_LowerValueChanged(object sender, RoutedEventArgs e)
        {
            if (e.Source is RangeSlider item && item != null)
            {
                controllerService.CAP_AccMin = (float)item.LowerValue;
            }
        }

        private void ControlAccMinMax_HigherValueChanged(object sender, RoutedEventArgs e)
        {
            if (e.Source is RangeSlider item && item != null)
            {
                controllerService.CAP_AccMax = (float)item.HigherValue;
            }
        }

        private void ControlAccRestMinMax_LowerValueChanged(object sender, RoutedEventArgs e)
        {
            if (e.Source is RangeSlider item && item != null)
            {
                controllerService.CAP_AccRestMin = (float)item.LowerValue;
            }
        }

        private void ControlAccRestMinMax_HigherValueChanged(object sender, RoutedEventArgs e)
        {
            if (e.Source is RangeSlider item && item != null)
            {
                controllerService.CAP_AccRestMax = (float)item.HigherValue;
            }
        }
    }
}
