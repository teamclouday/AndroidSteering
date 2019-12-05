using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using vJoyInterfaceWrap;

namespace SteeringWheel
{
    class Program
    {
        static void Main(string[] args)
        {
            MyBluetooth bltDevice = new MyBluetooth();

            while(!bltDevice.connected)
            {
                System.Threading.Thread.Sleep(50);
            }
        }
    }
}
