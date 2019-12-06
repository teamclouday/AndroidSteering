using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using vJoyInterfaceWrap;

namespace SteeringWheel
{
    class Program
    {
        static void Main(string[] args)
        {
            while(!CheckBluetooth())
            {
                System.Threading.Thread.Sleep(50);
            }

            MyBluetooth bltDevice = new MyBluetooth();
            bltDevice.Start();
            while(!bltDevice.Accept())
            {
                System.Threading.Thread.Sleep(100);
            }
            bltDevice.Stop();
        }

        /// <summary>
        /// Check if the bluetooth is turned on. If not, let user turn it on and try again
        /// </summary>
        /// <returns></returns>
        public static bool CheckBluetooth()
        {
            if(!InTheHand.Net.Bluetooth.BluetoothRadio.IsSupported)
            {
                if(MessageBox.Show("Please turn on Bluetooth and try again", "Error", MessageBoxButtons.OKCancel, MessageBoxIcon.Error) == DialogResult.Cancel)
                {
                    Environment.Exit(1);
                }
                return false;
            }
            return true;
        }
    }
}
