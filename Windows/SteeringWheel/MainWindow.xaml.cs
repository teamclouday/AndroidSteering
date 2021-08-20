using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;

namespace SteeringWheel
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly SharedBuffer sharedBuffer;
        private readonly Connection connectionService;
        private readonly Controller controllerService;

        private readonly System.Windows.Forms.NotifyIcon notifyIcon;

        public MainWindow()
        {
            InitializeComponent();
            // initialize components
            sharedBuffer = new SharedBuffer();
            connectionService = new Connection(this, sharedBuffer);
            controllerService = new Controller(this, sharedBuffer);
            if(!controllerService.vJoyInitialized)
            {
                MessageBox.Show("vJoy component not initialized\nPlease check your diver", "SteeringWheel", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(-1);
            }
            // setup notify icon
            notifyIcon = new System.Windows.Forms.NotifyIcon();
            SetupNotifyIcon();
            // raise process priority to keep connection stable
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.RealTime;
        }

        /// <summary>
        /// function to setup notify icon
        /// </summary>
        private void SetupNotifyIcon()
        {
            using (System.IO.Stream iconStream = Application.GetResourceStream(new Uri("pack://application:,,,/SteeringWheel;component/gamepad.ico", UriKind.Absolute)).Stream)
            {
                notifyIcon.Icon = new System.Drawing.Icon(iconStream);
            }
            notifyIcon.Visible = true;
            notifyIcon.DoubleClick +=
                delegate (object sender, EventArgs e)
                {
                    Show();
                    WindowState = WindowState.Normal;
                };
            notifyIcon.ContextMenu = new System.Windows.Forms.ContextMenu();
            notifyIcon.ContextMenu.MenuItems.Add(
                "Configure",
                delegate (object sender, EventArgs e)
                {
                    Show();
                    WindowState = WindowState.Normal;
                    ConfigureWindow configureWindow = new ConfigureWindow(this);
                    configureWindow.ShowDialog();
                }
            );
            notifyIcon.ContextMenu.MenuItems.Add(
                "Quit",
                delegate (object sender, EventArgs e)
                {
                    Close();
                }
            );
        }

        /// <summary>
        /// callback when window is closing
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            controllerService.Destroy();
            notifyIcon.Dispose();
        }

        /// <summary>
        /// show message if window minimized to system tray
        /// </summary>
        /// <param name="e"></param>
        protected override void OnStateChanged(EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                notifyIcon.ShowBalloonTip(2000, "SteeringWheel", "App minimized to system tray", System.Windows.Forms.ToolTipIcon.Info);
                Hide();
            }
            base.OnStateChanged(e);
        }

        /// <summary>
        /// mouse down and check double click for log message block
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LogBlock_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                LogBlock.Inlines.Clear(); // clear message if double clicked
            }
        }

        /// <summary>
        /// log message auto scroll to bottom
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LogBlockScroll_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.ExtentHeightChange != 0)
            {
                LogBlockScroll.ScrollToVerticalOffset(LogBlockScroll.ExtentHeight);
            }
        }

        /// <summary>
        /// connect button click callback
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {

        }

        /// <summary>
        /// configure button click callback
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ConfigureButton_Click(object sender, RoutedEventArgs e)
        {
            ConfigureWindow configureWindow = new ConfigureWindow(this);
            configureWindow.ShowDialog();
        }

        /// <summary>
        /// add log message
        /// </summary>
        /// <param name="message">string of message to display on window</param>
        public void AddLogMessage(string message)
        {
            string[] messages = message.Split('\n');
            foreach (string m in messages)
            {
                LogBlock.Inlines.Add(m);
                LogBlock.Inlines.Add(new LineBreak());
            }
        }

        /// <summary>
        /// trigger button on controller
        /// </summary>
        /// <param name="button"></param>
        public void ControllerClick(ControlButton button)
        {
            controllerService.TriggerControl(button);
        }

        /// <summary>
        /// trigger stick axis movement on controller
        /// </summary>
        /// <param name="axis"></param>
        public void ControllerClick(ControlAxis axis)
        {
            controllerService.TriggerControl(axis);
        }
    }
}
