using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InTheHand.Net.Bluetooth;
using InTheHand.Net.Sockets;

namespace SteeringWheel
{
    class MyBluetooth
    {
        BluetoothDeviceInfo targetDevice;
        BluetoothClient client;
        public bool connected = false;
        public MyBluetooth()
        {
            Scan();
            Display();
            Pair();
            Connect();
        }

        public void Scan()
        {
            targetDevice = null;
            client = null;
            try
            {
                client = new BluetoothClient();
            } catch(Exception)
            {
                Console.WriteLine("Bluetooth is off");
                return;
            }
            BluetoothDeviceInfo[] infos = client.DiscoverDevicesInRange();
            foreach(BluetoothDeviceInfo info in infos)
            {
                if(info.ClassOfDevice.Device == DeviceClass.SmartPhone)
                {
                    targetDevice = info;
                }
            }
        }

        public void Display()
        {
            if(targetDevice == null)
            {
                Console.WriteLine("No Smart Phone Found in Bluetooth Connection");
            }
            else
            {
                Console.WriteLine("Device Name: " + targetDevice.DeviceName);
                Console.WriteLine("Device Mac: " + targetDevice.DeviceAddress.ToString());
                Console.WriteLine(targetDevice.Authenticated);
            }
        }

        public void Pair()
        {
            if (targetDevice == null) return;
            if(!targetDevice.Authenticated)
            {
                bool isPaired = BluetoothSecurity.PairRequest(targetDevice.DeviceAddress, "10086");
                if(isPaired)
                {
                    Console.WriteLine("Deivce Paired");
                }
                else
                {
                    Console.WriteLine("Device Fail to Pair");
                }
            }
        }

        public void Connect()
        {
            if(targetDevice.Authenticated)
            {
                client.Connect(new InTheHand.Net.BluetoothEndPoint(targetDevice.DeviceAddress, new Guid("F6F55BAD-DA40-4C94-A0AB-7B62E6475622")));
            }
        }
    }
}
