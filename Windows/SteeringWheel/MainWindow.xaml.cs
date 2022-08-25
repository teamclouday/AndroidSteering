using System;
using System.Diagnostics;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Threading;

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

        private Thread connectThread;
        private Thread disconnectThread;
        private readonly int MAX_WAIT_TIME = 1500;

        public MainWindow()
        {
            InitializeComponent();
            // initialize components
            sharedBuffer = new SharedBuffer();
            connectionService = new Connection(this, sharedBuffer);
            controllerService = new Controller(this, sharedBuffer);
            if (!controllerService.vJoyInitialized)
            {
                MessageBox.Show("No valid vJoy device found\nPlease check your vJoy device setup", "SteeringWheel", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(-1);
            }
            // setup notify icon
            notifyIcon = new System.Windows.Forms.NotifyIcon();
            SetupNotifyIcon();
            // set debug information
            Debug.AutoFlush = true;
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
                    LaunchConfigureWindow();
                }
            );
            notifyIcon.ContextMenu.MenuItems.Add(
                "Controller",
                delegate (object sender, EventArgs e)
                {
                    Show();
                    WindowState = WindowState.Normal;
                    LaunchControllerWindow();
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
            connectionService.Destroy();
            if (connectThread != null && connectThread.IsAlive)
            {
                if (!connectThread.Join(MAX_WAIT_TIME)) connectThread.Abort();
            }
            if (disconnectThread != null && disconnectThread.IsAlive)
            {
                if (!disconnectThread.Join(MAX_WAIT_TIME)) disconnectThread.Abort();
            }
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
            switch (connectionService.status)
            {
                case ConnectionStatus.Default:
                    if (connectThread == null || !connectThread.IsAlive)
                    {
                        if (!connectionService.CheckWifi()) return;
                        LockRadioButtons();
                        connectThread = new Thread(() =>
                        {
                            connectionService.Connect();
                        });
                        connectThread.Start();
                    }
                    else AddLog("Already connecting...");
                    break;
                case ConnectionStatus.Listening:
                case ConnectionStatus.Connected:
                    if (disconnectThread == null || !disconnectThread.IsAlive)
                    {
                        disconnectThread = new Thread(() =>
                        {
                            connectionService.Disconnect();
                        });
                        disconnectThread.Start();
                    }
                    else AddLog("Already disconnecting...");
                    break;
            }
        }

        /// <summary>
        /// lock radio buttons
        /// </summary>
        public void LockRadioButtons()
        {
            RadioButtonBluetooth.IsEnabled = false;
            RadioButtonWifi.IsEnabled = false;
        }

        /// <summary>
        /// unlock radio buttons
        /// </summary>
        public void UnlockRadioButtons()
        {
            RadioButtonBluetooth.IsEnabled = true;
            RadioButtonWifi.IsEnabled = true;
        }

        /// <summary>
        /// update connect button text based on new connection status
        /// </summary>
        public void UpdateConnectButton()
        {
            switch (connectionService.status)
            {
                case ConnectionStatus.Default:
                    ConnectButton.Content = "Connect";
                    break;
                case ConnectionStatus.Listening:
                    ConnectButton.Content = "Listening";
                    break;
                case ConnectionStatus.Connected:
                    ConnectButton.Content = "Disconnect";
                    break;
            }
        }

        /// <summary>
        /// controller button click callback
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ControllerButton_Click(object sender, RoutedEventArgs e)
        {
            LaunchControllerWindow();
        }

        /// <summary>
        /// launch controller window
        /// </summary>
        private void LaunchControllerWindow()
        {
            ControllerWindow controllerWindow = new ControllerWindow(this)
            {
                ShowInTaskbar = false,
                Owner = this
            };
            controllerWindow.ShowDialog();
        }

        /// <summary>
        /// configure button click callback
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ConfigureButton_Click(object sender, RoutedEventArgs e)
        {
            LaunchConfigureWindow();
        }

        /// <summary>
        /// launch configuration window
        /// </summary>
        private void LaunchConfigureWindow()
        {
            ConfigureWindow configureWindow = new ConfigureWindow(controllerService)
            {
                ShowInTaskbar = false,
                Owner = this
            };
            configureWindow.ShowDialog();
        }

        /// <summary>
        /// radio button click callback
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RadioButton_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as RadioButton) == RadioButtonBluetooth) connectionService.mode = ConnectionMode.Bluetooth;
            else if ((sender as RadioButton) == RadioButtonWifi) connectionService.mode = ConnectionMode.Wifi;
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

        /// <summary>
        /// add log message to main window
        /// </summary>
        /// <param name="message"></param>
        private void AddLog(string message)
        {
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                AddLogMessage("[UI]\n" + message + "\n");
            }));
        }
    }
}
