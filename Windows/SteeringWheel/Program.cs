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
        private static bool ProgramRunning = true;

        static void Main(string[] args)
        {
            while(!CheckBluetooth())
            {
                Thread.Sleep(50);
            }

            while(!CheckVJoy())
            {
                Thread.Sleep(50);
            }

            bltDevice = new MyBluetooth();
            wheelDevice = new MyWheel();

            Thread bltThread = new Thread(new ThreadStart(RunBthService));
            Thread wheelThread = new Thread(new ThreadStart(RunWheelService));

            bltThread.Start();
            wheelThread.Start();

            while (ProgramRunning)
            {
                Thread.Sleep(50);
            }

            bltThread.Join();
            wheelThread.Join();
        }

        private static void RunBthService()
        {
            bltDevice.Start();
            bltDevice.TryAccept();
            while(bltDevice.okForConnection)
            {
                Thread.Sleep(50);
            }
            bltDevice.Pause();
            bltDevice.Stop();
        }

        private static void RunWheelService()
        {
            wheelDevice.Start();

            while(wheelDevice.okForRunning)
            {
                wheelDevice.ProcessData();
                Thread.Sleep(1);
            }

            wheelDevice.Stop();
        }

        /// <summary>
        /// Check if the bluetooth is turned on. If not, let user turn it on and try again
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
                if (MessageBox.Show(string.Format("Please make sure the dll version matches the driver version ({0:X} & {1:X})", DllVer, DrvVer), "Error", MessageBoxButtons.RetryCancel, MessageBoxIcon.Error) == DialogResult.Cancel)
                {
                    Environment.Exit(1);
                }
                return false;
            }
            return true;
        }
    }
}
