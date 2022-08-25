using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace SteeringWheel
{
    /// <summary>
    /// Interaction logic for ControllerWindow.xaml
    /// </summary>
    public partial class ControllerWindow : Window
    {
        private readonly Controller controller;

        private readonly Dictionary<string, ControlButton> controlButtonNameMap = new Dictionary<string, ControlButton>()
        {
            {"A", ControlButton.A },
            {"B", ControlButton.B },
            {"X", ControlButton.X },
            {"Y", ControlButton.Y },
            {"LB", ControlButton.LB },
            {"RB", ControlButton.RB },
            {"Back", ControlButton.BACK },
            {"Start", ControlButton.START },
            {"Home", ControlButton.HOME },
        };

        private readonly Dictionary<string, ControlAxis> controlAxisNameMap = new Dictionary<string, ControlAxis>()
        {
            {"↑", ControlAxis.POVUp },
            {"↓", ControlAxis.POVDown },
            {"←", ControlAxis.POVLeft },
            {"→", ControlAxis.POVRight },
            {"LT", ControlAxis.Z },
            {"RT", ControlAxis.ZRot },
        };

        public ControllerWindow(Controller controller)
        {
            InitializeComponent();
            this.controller = controller;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string key = (e.Source as Button).Content.ToString();
            if (controlButtonNameMap.ContainsKey(key))
                controller.TriggerControl(controlButtonNameMap[key]);
            else if (controlAxisNameMap.ContainsKey(key))
                controller.TriggerControl(controlAxisNameMap[key]);
        }

        /// <summary>
        /// left stick button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonLeft_Click(object sender, RoutedEventArgs e)
        {
            string key = (e.Source as Button).Content.ToString();
            if (key.Equals("Horizontal")) controller.TriggerControl(ControlAxis.X);
            else if (key.Equals("Vertical")) controller.TriggerControl(ControlAxis.Y);
            else if (key.Equals("Press")) controller.TriggerControl(ControlButton.LS);
        }

        /// <summary>
        /// right stick buttons
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonRight_Click(object sender, RoutedEventArgs e)
        {
            string key = (e.Source as Button).Content.ToString();
            if (key.Equals("Horizontal")) controller.TriggerControl(ControlAxis.XRot);
            else if (key.Equals("Vertical")) controller.TriggerControl(ControlAxis.YRot);
            else if (key.Equals("Press")) controller.TriggerControl(ControlButton.RS);
        }
    }
}
