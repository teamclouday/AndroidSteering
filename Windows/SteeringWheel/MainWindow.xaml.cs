using System;
using System.Diagnostics;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Threading;
using System.Threading.Tasks;

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
        private bool notifDisplayedOnce = false;

        private Task connectTask;
        private Task disconnectTask;
        private readonly CancellationTokenSource connectionToken = new CancellationTokenSource();

        [System.Runtime.InteropServices.DllImport("User32.dll")]
        private static extern bool SetForegroundWindow(IntPtr handle);

        public MainWindow()
        {
            // avoid duplicate processes
            Process[] processes = Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName);
            if (processes.Length > 1)
            {
                int prevProcessIdx = 0;
                while (prevProcessIdx < processes.Length &&
                    processes[prevProcessIdx].Id == Process.GetCurrentProcess().Id)
                    prevProcessIdx++;
                if (prevProcessIdx < processes.Length)
                {
                    // bring previous process to foreground
                    SetForegroundWindow(processes[prevProcessIdx].MainWindowHandle);
                }
                // close current process
                Application.Current.Shutdown();
            }
            // initialize components
            InitializeComponent();
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
            // load settings
            if (Properties.Settings.Default.MainWindowIsBluetoothSelected)
            {
                connectionService.Mode = ConnectionMode.Bluetooth;
                RadioButtonBluetooth.IsChecked = true;
            }
            else
            {
                connectionService.Mode = ConnectionMode.Wifi;
                RadioButtonWifi.IsChecked = true;
            }
        }

        /// <summary>
        /// function to setup notify icon
        /// </summary>
        private void SetupNotifyIcon()
        {
            notifyIcon.Icon = Properties.Resources.gamepad;
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
            connectionService.Destroy().Wait();
            if (connectTask?.IsCompleted == false || disconnectTask?.IsCompleted == false)
            {
                connectionToken.Cancel();
                try
                {
                    connectTask?.Wait();
                    disconnectTask?.Wait();
                }
                catch (OperationCanceledException err)
                {
                    Debug.WriteLine("MainWindow_Closing -> " + err.Message);
                }
                finally
                {
                    connectTask?.Dispose();
                    disconnectTask?.Dispose();
                }
            }
            notifyIcon.Dispose();
            Properties.Settings.Default.Save();
        }

        /// <summary>
        /// show message if window minimized to system tray
        /// </summary>
        /// <param name="e"></param>
        protected override void OnStateChanged(EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                if (!notifDisplayedOnce)
                {
                    notifyIcon.ShowBalloonTip(2000, "SteeringWheel", "App minimized to system tray", System.Windows.Forms.ToolTipIcon.Info);
                    notifDisplayedOnce = true;
                }
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
            switch (connectionService.Status)
            {
                case ConnectionStatus.Default:
                    if (connectTask?.IsCompleted != false)
                    {
                        if (!connectionService.CheckWifi()) return;
                        LockRadioButtons();
                        connectTask = Task.Run(async () =>
                        {
                            await connectionService.Connect();
                        }, connectionToken.Token);
                    }
                    else AddLog("Already connecting...");
                    break;
                case ConnectionStatus.Listening:
                case ConnectionStatus.Connected:
                    if (disconnectTask?.IsCompleted != false)
                    {
                        disconnectTask = Task.Run(async () =>
                        {
                            await connectionService.Disconnect();
                        }, connectionToken.Token);
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
            switch (connectionService.Status)
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
        /// Reset controller (used by connection)
        /// </summary>
        public void ResetController()
        {
            controllerService.ResetVJoy();
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
            ControllerWindow controllerWindow = new ControllerWindow(controllerService)
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
            if ((sender as RadioButton) == RadioButtonBluetooth)
            {
                connectionService.Mode = ConnectionMode.Bluetooth;
                Properties.Settings.Default.MainWindowIsBluetoothSelected = true;
            }
            else if ((sender as RadioButton) == RadioButtonWifi)
            {
                connectionService.Mode = ConnectionMode.Wifi;
                Properties.Settings.Default.MainWindowIsBluetoothSelected = false;
            }
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
