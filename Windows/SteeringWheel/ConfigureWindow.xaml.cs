using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SteeringWheel
{
    /// <summary>
    /// Interaction logic for ConfigureWindow.xaml
    /// </summary>
    public partial class ConfigureWindow : Window
    {
        private readonly MainWindow mainWindow;

        public ConfigureWindow(MainWindow window)
        {
            InitializeComponent();
            mainWindow = window;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string key = (e.Source as Button).Content.ToString();
            if (key.Equals("A")) mainWindow.ControllerClick(ControlButton.A);
            else if (key.Equals("B")) mainWindow.ControllerClick(ControlButton.B);
            else if (key.Equals("X")) mainWindow.ControllerClick(ControlButton.X);
            else if (key.Equals("Y")) mainWindow.ControllerClick(ControlButton.Y);
            else if (key.Equals("LB")) mainWindow.ControllerClick(ControlButton.LB);
            else if (key.Equals("RB")) mainWindow.ControllerClick(ControlButton.RB);
            else if (key.Equals("Back")) mainWindow.ControllerClick(ControlButton.BACK);
            else if (key.Equals("Start")) mainWindow.ControllerClick(ControlButton.START);
            else if (key.Equals("Home")) mainWindow.ControllerClick(ControlButton.HOME);
            else if (key.Equals("↑")) mainWindow.ControllerClick(ControlAxis.POVUp);
            else if (key.Equals("↓")) mainWindow.ControllerClick(ControlAxis.POVDown);
            else if (key.Equals("←")) mainWindow.ControllerClick(ControlAxis.POVLeft);
            else if (key.Equals("→")) mainWindow.ControllerClick(ControlAxis.POVRight);
            else if (key.Equals("LT")) mainWindow.ControllerClick(ControlAxis.Z);
            else if (key.Equals("RT")) mainWindow.ControllerClick(ControlAxis.ZRot);
        }

        /// <summary>
        /// left stick button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonLeft_Click(object sender, RoutedEventArgs e)
        {
            string key = (e.Source as Button).Content.ToString();
            if (key.Equals("Horizontal")) mainWindow.ControllerClick(ControlAxis.X);
            else if (key.Equals("Vertical")) mainWindow.ControllerClick(ControlAxis.Y);
            else if (key.Equals("Press")) mainWindow.ControllerClick(ControlButton.LS);
        }

        /// <summary>
        /// right stick buttons
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonRight_Click(object sender, RoutedEventArgs e)
        {
            string key = (e.Source as Button).Content.ToString();
            if (key.Equals("Horizontal")) mainWindow.ControllerClick(ControlAxis.XRot);
            else if (key.Equals("Vertical")) mainWindow.ControllerClick(ControlAxis.YRot);
            else if (key.Equals("Press")) mainWindow.ControllerClick(ControlButton.RS);
        }
    }
}
