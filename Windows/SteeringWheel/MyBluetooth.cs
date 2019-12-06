using System;
using System.IO;
using InTheHand.Net.Bluetooth;
using InTheHand.Net.Sockets;

namespace SteeringWheel
{
    class MyBluetooth
    {
        private readonly Guid myServiceID;
        private BluetoothListener myListener;
        private BluetoothClient sender;
        public bool isConnected;

        public MyBluetooth()
        {
            myServiceID = new Guid("a7bda841-7dbc-4179-9800-1a3eff463f1c");
            isConnected = false;
        }

        public void Start()
        {
            if(myListener != null)
                myListener.Stop();
            myListener = new BluetoothListener(myServiceID);
            myListener.ServiceName = "Android Steering Wheel Host";
            myListener.Start();
        }

        public bool Accept()
        {
            try
            {
                sender = myListener.AcceptBluetoothClient();
            } catch(Exception e)
            {
                Console.WriteLine("Error occured: " + e.Message);
            }
            Console.WriteLine("Device connected");
            Stream inStream = sender.GetStream();

            while(true)
            {
                try
                {
                    byte[] pack = new byte[800];
                    int actualLength = inStream.Read(pack, 0, pack.Length);
                    if (actualLength == 0) return false;
                    for(int i = 0; i < actualLength; i+=4)
                    {
                        byte[] subpack = new byte[4];
                        Array.Copy(pack, i, subpack, 0, 4);
                        int? data = DecodeData(subpack);
                        if (data == null) return false;
                        if (data == 10086)
                            Console.WriteLine();
                        else
                            Console.Write(data + " + ");
                    }
                }
                catch (IOException)
                {
                    Console.WriteLine("Connection is off");
                    sender.Dispose();
                    break;
                }
            }

            return true;
        }

        public void Stop()
        {
            if (myListener == null) return;
            myListener.Server.Dispose();
            myListener.Stop();
            myListener = null;
        }

        private int? DecodeData(byte[] array)
        {
            if (array.Length != 4) return null;
            if (BitConverter.IsLittleEndian)
                Array.Reverse(array);
            return BitConverter.ToInt32(array, 0);
        }
    }
}
