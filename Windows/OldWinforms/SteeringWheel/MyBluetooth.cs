using System;
using System.IO;
using InTheHand.Net.Sockets;

namespace SteeringWheel
{
    enum MyBluetoothStatus
    {
        NONE,
        LISTENING,
        CONNECTED
    }

    class MyBluetooth
    {
        private readonly Guid myServiceID;
        private const int seperator = 0x7FFFFFFF;
        public bool okForConnection = true;
        private BluetoothListener myListener;
        private BluetoothClient sender;
        public MyBluetoothStatus status;

        /// <summary>
        /// Constructor, init the server guid
        /// </summary>
        public MyBluetooth()
        {
            myServiceID = new Guid("a7bda841-7dbc-4179-9800-1a3eff463f1c");
            status = MyBluetoothStatus.NONE;
        }

        /// <summary>
        /// start listening
        /// </summary>
        public void Start()
        {
            if(myListener != null)
                myListener.Stop();
            myListener = new BluetoothListener(myServiceID);
            myListener.ServiceName = "Android Steering Wheel Host";
            myListener.Start();
            status = MyBluetoothStatus.LISTENING;
        }

        /// <summary>
        /// pause the server, expected to be called by a different thread
        /// </summary>
        public void Pause()
        {
            lock(this)
            {
                okForConnection = false;
                status = MyBluetoothStatus.NONE;
                if(sender != null && sender.Connected)
                {
                    sender.Dispose();
                }
                myListener.Stop();
            }
        }

        /// <summary>
        /// resume the server, expected to be called by a different thread
        /// </summary>
        public void Resume()
        {
            lock(this)
            {
                okForConnection = true;
                status = MyBluetoothStatus.LISTENING;
                myListener.Start();
            }
            TryAccept();
        }

        /// <summary>
        /// try to accept incoming connection
        /// </summary>
        public void TryAccept()
        {
            myListener.BeginAcceptBluetoothClient(new AsyncCallback(AcceptCallback), myListener);
        }

        /// <summary>
        /// stop the server completely
        /// </summary>
        public void Stop()
        {
            status = MyBluetoothStatus.NONE;
            if (myListener == null) return;
            myListener.Server.Dispose();
            myListener.Stop();
            myListener = null;
        }

        /// <summary>
        /// callback function used in TryAccept
        /// if the okForConnection is set to false, then immediately end the accepting process
        /// </summary>
        /// <param name="result"></param>
        private void AcceptCallback(IAsyncResult result)
        {
            if(!okForConnection)
            {
                status = MyBluetoothStatus.NONE;
                return;
            }
            if(result.IsCompleted)
            {
                sender = ((BluetoothListener)result.AsyncState).EndAcceptBluetoothClient(result);
                if(!TestSender())
                {
                    if(sender != null)
                    {
                        sender.Dispose();
                        sender = null;
                    }
                    TryAccept();
                }
                else
                {
                    StartConnection();
                }
            }
        }

        /// <summary>
        /// start the data transfer, from the connected device
        /// if the connection is closed, try to accept another connection
        /// </summary>
        private void StartConnection()
        {
            status = MyBluetoothStatus.CONNECTED;
            Stream inStream = sender.GetStream();
            while (okForConnection)
            {
                try
                {
                    byte[] pack = new byte[800];
                    int actualLength = inStream.Read(pack, 0, pack.Length);
                    if (actualLength == 0) break;
                    int[] moveData = new int[2];
                    int pointer = -1;
                    float specialData = 0.0f;
                    int data = 0;
                    for (int i = 0; i < actualLength; i += 4)
                    {
                        byte[] subpack = new byte[4];
                        Array.Copy(pack, i, subpack, 0, 4);
                        if (pointer == 2)
                            specialData = DecodeFloatData(subpack);
                        else
                            data = DecodeData(subpack);
                        if (data == seperator)
                        {
                            goto ENDOFWHILELOOP;
                        }
                        else if(data == 10086)
                        {
                            pointer++;
                        }
                        else
                        {
                            if(pointer == 2)
                            {
                                Program.globBuffer.AddData(moveData[0], moveData[1], specialData);
                                specialData = 0.0f;
                                pointer = -1;
                            }
                            else
                            {
                                if (pointer != 0 && pointer != 1) break;
                                moveData[pointer] = data;
                                pointer++;
                            }
                        }
                    }
                } catch(IOException)
                {
                    break;
                }
                System.Threading.Thread.Sleep(1);
            }
        ENDOFWHILELOOP:

            Program.globBuffer.Reset();
            sender.Dispose();
            sender = null;

            if(okForConnection)
            {
                status = MyBluetoothStatus.LISTENING;
                TryAccept();
            }
        }

        /// <summary>
        /// test the connected device
        /// </summary>
        /// <returns>
        /// true, if the device sent 0x7FFFFFFF at the begining, then start connection
        /// false, if the device sent nothing, or wrong data
        /// </returns>
        private bool TestSender()
        {
            if (sender == null) return false;
            byte[] pack = new byte[4];
            try
            {
                int len = sender.GetStream().Read(pack, 0, pack.Length);
                if (len == 0) return false;
                else
                {
                    if (DecodeData(pack) != seperator) return false;
                }
            }
            catch (IOException)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// decode incoming data, 4 bytes as an int
        /// </summary>
        /// <param name="array"></param>
        /// <returns></returns>
        private int DecodeData(byte[] array)
        {
            if (BitConverter.IsLittleEndian)
                Array.Reverse(array);
            return BitConverter.ToInt32(array, 0);
        }

        /// <summary>
        /// decode 4 bytes as a float
        /// </summary>
        /// <param name="array"></param>
        /// <returns></returns>
        private float DecodeFloatData(byte[] array)
        {
            if (BitConverter.IsLittleEndian)
                Array.Reverse(array);
            return BitConverter.ToSingle(array, 0);
        }

        /// <summary>
        /// get connected device info
        /// </summary>
        /// <returns></returns>
        public string[] GetDeviceInfo()
        {
            lock(this)
            {
                if(sender == null || !sender.Connected)
                {
                    return null;
                }
                else
                {
                    string[] result = new string[5];
                    result[0] = "Device Name: " + sender.RemoteMachineName;
                    result[1] = "Device Address: " + sender.RemoteEndPoint.Address.ToString();
                    result[2] = "Local Address: " + myListener.LocalEndPoint.ToString();
                    result[3] = "Connection Protocol Type: " + sender.Client.ProtocolType.ToString();
                    result[4] = "Connection Socket Type: " + sender.Client.SocketType.ToString();
                    return result;
                }
            }
        }
    }
}
