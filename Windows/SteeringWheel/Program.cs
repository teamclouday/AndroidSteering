using System;
using System.Threading;
using System.Windows.Forms;

namespace SteeringWheel
{
    class Program
    {
        public static GlobBuffer globBuffer = new GlobBuffer();
        public static MyBluetooth bltDevice;
        public static MyWheel wheelDevice;
        public static SetupUI setupUI;
        public static Thread setupThread;
        public static MonitorUI monitorUI;
        public static Thread monitorThread;
        public static Thread bltThread;
        public static Thread wheelThread;
        public static Thread trayThread;
        private static bool ProgramRunning = true;

        static void Main(string[] args)
        {
            while (!CheckBluetooth())
            {
                Thread.Sleep(50);
            }

            while(!CheckVJoy())
            {
                Thread.Sleep(50);
            }

            bltDevice = new MyBluetooth();
            wheelDevice = new MyWheel();
            setupUI = new SetupUI();
            monitorUI = new MonitorUI();

            bltThread = new Thread(new ThreadStart(RunBthService));
            wheelThread = new Thread(new ThreadStart(RunWheelService));
            trayThread = new Thread(
            delegate()
            {
                var menuItem1 = new MenuItem();
                menuItem1.Index = 0;
                menuItem1.Text = "Setup Controller";
                menuItem1.Click += new EventHandler(TrayClickEvent1);

                var menuItem2 = new MenuItem();
                menuItem2.Index = 1;
                menuItem2.Text = "Status Monitor";
                menuItem2.Click += new EventHandler(TrayClickEvent2);

                var menuItem3 = new MenuItem();
                menuItem3.Index = 2;
                menuItem3.Text = "Exit";
                menuItem3.Click += new EventHandler(TrayClickEvent3);

                var contextMenu = new ContextMenu();
                contextMenu.MenuItems.AddRange(new MenuItem[] {menuItem1, menuItem2, menuItem3});

                var trayIcon = new NotifyIcon();
                trayIcon.Visible = true;
                trayIcon.Icon = setupUI.GetIcon();
                trayIcon.DoubleClick += new EventHandler(TrayDoubleClickEvent);
                trayIcon.Text = "Steering Wheel Service";
                trayIcon.ContextMenu = contextMenu;

                Application.Run();
            }
            );

            bltThread.Start();
            wheelThread.Start();
            trayThread.Start();

            while (ProgramRunning)
            {
                Thread.Sleep(50);
            }

            bltThread.Join();
            wheelThread.Join();
            trayThread.Join();
        }

        /// <summary>
        /// function that will run the bluetooth service in thread
        /// </summary>
        private static void RunBthService()
        {
            bltDevice.Start();
            bltDevice.TryAccept();
            while(ProgramRunning)
            {
                Thread.Sleep(50);
            }
            bltDevice.Stop();
        }

        /// <summary>
        /// function that will run the wheel service in thread
        /// </summary>
        private static void RunWheelService()
        {
            wheelDevice.Start();
            while(ProgramRunning)
            {
                wheelDevice.ProcessData();
                Thread.Sleep(1);
            }
            wheelDevice.Stop();
        }

        /// <summary>
        /// click event 1: open the controller setup form
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void TrayClickEvent1(object sender, EventArgs e)
        {
            if (setupThread != null && setupThread.IsAlive) return;
            setupThread = new Thread(
            delegate ()
            {
                if(setupUI.IsDisposed)
                    setupUI = new SetupUI();
                Application.EnableVisualStyles();
                Application.Run(setupUI);
            }
            );
            setupThread.Start();
        }

        /// <summary>
        /// click event 2: open the monitor form
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void TrayClickEvent2(object sender, EventArgs e)
        {
            if (monitorThread != null && monitorThread.IsAlive) return;
            monitorThread = new Thread(
            delegate ()
            {
                if (monitorUI.IsDisposed)
                    monitorUI = new MonitorUI();
                Application.EnableVisualStyles();
                Application.Run(monitorUI);
            }
            );
            monitorThread.Start();
        }

        /// <summary>
        /// click event 3: exit program
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void TrayClickEvent3(object sender, EventArgs e)
        {
            ProgramRunning = false;
            bltDevice.Pause();
            wheelDevice.Pause();
            if(monitorThread != null && monitorThread.IsAlive)
            {
                monitorUI.BeginInvoke(new Action(() => monitorUI.Close()));
                monitorThread.Join();
            }
            if(setupThread != null && setupThread.IsAlive)
            {
                setupUI.BeginInvoke(new Action(() => setupUI.Close()));
                setupThread.Join();
            }
            Application.Exit();
        }

        /// <summary>
        /// double click event: open github url of the program in web explorer
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void TrayDoubleClickEvent(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(@"https://github.com/teamclouday/AndroidSteering");
        }

        /// <summary>
        /// check if the bluetooth is turned on. If not, let user turn it on and try again
        /// </summary>
        /// <returns></returns>
        public static bool CheckBluetooth()
        {
            if(!InTheHand.Net.Bluetooth.BluetoothRadio.IsSupported)
            {
                if(MessageBox.Show("Please turn on Bluetooth and try again", "Error", MessageBoxButtons.RetryCancel, MessageBoxIcon.Error) == DialogResult.Cancel)
                {
                    Environment.Exit(1);
                }
                return false;
            }
            return true;
        }

        /// <summary>
        /// check if vjoy driver is installed and enabled on the computer
        /// also make sure that dll version matches driver version
        /// </summary>
        /// <returns></returns>
        public static bool CheckVJoy()
        {
            vJoyInterfaceWrap.vJoy joystick = new vJoyInterfaceWrap.vJoy();
            if(!joystick.vJoyEnabled())
            {
                if (MessageBox.Show("Please enable vJoy driver and try again", "Error", MessageBoxButtons.RetryCancel, MessageBoxIcon.Error) == DialogResult.Cancel)
                {
                    Environment.Exit(1);
                }
                return false;
            }
            uint DllVer = 0, DrvVer = 0;
            if(!joystick.DriverMatch(ref DllVer, ref DrvVer))
            {
                if (MessageBox.Show(string.Format("Please make sure the dll version matches the driver version\nDLL Version: {0:X}\nDriver Version: {1:X}", DllVer, DrvVer), "Error", MessageBoxButtons.RetryCancel, MessageBoxIcon.Error) == DialogResult.Cancel)
                {
                    Environment.Exit(1);
                }
                return false;
            }
            return true;
        }
    }
}
