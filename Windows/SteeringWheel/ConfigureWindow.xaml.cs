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
        private bool initialized = false;

        public ConfigureWindow(Controller controller)
        {
            controllerService = controller;
            InitializeComponent();
            // load settings
            ControlSteeringMinMax.HigherValue = controller.CAP_SteeringMax = Properties.Settings.Default.ConfigureWindowControlSteeringMax;
            ControlSteeringMinMax.LowerValue = controller.CAP_SteeringMin = Properties.Settings.Default.ConfigureWindowControlSteeringMin;

            ControlAccMinMax.HigherValue = controller.CAP_AccMax = Properties.Settings.Default.ConfigureWindowControlAccMax;
            ControlAccMinMax.LowerValue = controller.CAP_AccMin = Properties.Settings.Default.ConfigureWindowControlAccMin;

            ControlAccRestMinMax.HigherValue = controller.CAP_AccRestMax = Properties.Settings.Default.ConfigureWindowControlAccRestMax;
            ControlAccRestMinMax.LowerValue = controller.CAP_AccRestMin = Properties.Settings.Default.ConfigureWindowControlAccRestMin;

            initialized = true;
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
                if (initialized)
                    Properties.Settings.Default.ConfigureWindowControlSteeringMin = controllerService.CAP_SteeringMin;
            }
        }

        private void ControlSteeringMinMax_HigherValueChanged(object sender, RoutedEventArgs e)
        {
            if (e.Source is RangeSlider item && item != null)
            {
                controllerService.CAP_SteeringMax = (float)item.HigherValue;
                if (initialized)
                    Properties.Settings.Default.ConfigureWindowControlSteeringMax = controllerService.CAP_SteeringMax;
            }
        }

        private void ControlAccMinMax_LowerValueChanged(object sender, RoutedEventArgs e)
        {
            if (e.Source is RangeSlider item && item != null)
            {
                controllerService.CAP_AccMin = (float)item.LowerValue;
                if (initialized)
                    Properties.Settings.Default.ConfigureWindowControlAccMin = controllerService.CAP_AccMin;
            }
        }

        private void ControlAccMinMax_HigherValueChanged(object sender, RoutedEventArgs e)
        {
            if (e.Source is RangeSlider item && item != null)
            {
                controllerService.CAP_AccMax = (float)item.HigherValue;
                if (initialized)
                    Properties.Settings.Default.ConfigureWindowControlAccMax = controllerService.CAP_AccMax;
            }
        }

        private void ControlAccRestMinMax_LowerValueChanged(object sender, RoutedEventArgs e)
        {
            if (e.Source is RangeSlider item && item != null)
            {
                controllerService.CAP_AccRestMin = (float)item.LowerValue;
                if (initialized)
                    Properties.Settings.Default.ConfigureWindowControlAccRestMin = controllerService.CAP_AccRestMin;
            }
        }

        private void ControlAccRestMinMax_HigherValueChanged(object sender, RoutedEventArgs e)
        {
            if (e.Source is RangeSlider item && item != null)
            {
                controllerService.CAP_AccRestMax = (float)item.HigherValue;
                if (initialized)
                    Properties.Settings.Default.ConfigureWindowControlAccRestMax = controllerService.CAP_AccRestMax;
            }
        }
    }
}
