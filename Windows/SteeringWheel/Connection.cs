using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;
using InTheHand.Net;
using InTheHand.Net.Sockets;
using InTheHand.Net.Bluetooth;
using System.Windows;
using System.Diagnostics;
using System.IO;

namespace SteeringWheel
{
    /// <summary>
    /// global shared buffer
    /// </summary>
    public class SharedBuffer
    {
        private const int MAX_SIZE = 50;
        private readonly Queue<MotionData> buffer = new Queue<MotionData>();
        public void AddData(bool v1, int v2, float v3)
        {
            lock(this)
            {
                buffer.Enqueue(new MotionData()
                {
                    IsButton = v1,
                    Status = v2,
                    Value = v3
                });
                while (buffer.Count > MAX_SIZE) buffer.Dequeue();
            }
        }
        public void AddData(MotionData data)
        {
            lock(this)
            {
                buffer.Enqueue(data);
                while (buffer.Count > MAX_SIZE) buffer.Dequeue();
            }
        }
        public MotionData GetData()
        {
            if (buffer.Count > 0) return buffer.Dequeue();
            else return null;
        }
    }

    /// <summary>
    /// define connection modes
    /// </summary>
    public enum ConnectionMode
    {
        Bluetooth,
        Wifi
    }

    /// <summary>
    /// define connection status
    /// </summary>
    public enum ConnectionStatus
    {
        Default,
        Listening,
        Connected
    }

    /// <summary>
    /// Connection service
    /// </summary>
    class Connection
    {
        private readonly MainWindow mainWindow;
        private readonly SharedBuffer sharedBuffer;
        public ConnectionMode mode { get; set; }
        public ConnectionStatus status { get; private set; }

        private readonly int MAX_WAIT_TIME = 2000;
        private readonly int DATA_SEPARATOR = 10086;
        private readonly int BUFFER_SIZE = 130; // 10 data packs each time
        private readonly int DEVICE_CHECK_EXPECTED = 123456;
        private readonly int DEVICE_CHECK_DATA = 654321;
        private bool isConnectionAllowed = false;

        // bluetooth components
        private readonly Guid bthServerUUID = new Guid("a7bda841-7dbc-4179-9800-1a3eff463f1c");
        private readonly string bthServerName = "SteeringWheel Host";
        private BluetoothListener bthListener;
        private BluetoothClient bthClient;
        private NetworkStream bthClientStream;
        private BluetoothEndPoint bthTargetDeviceID;
        private Thread bthThread;
        // wifi components
        private readonly int wifiPort = 55555;
        private readonly string wifiAddress = "127.0.0.1";
        private Socket wifiServer;
        private Thread wifiThread;

        public Connection(MainWindow window, SharedBuffer buffer)
        {
            mainWindow = window;
            sharedBuffer = buffer;
            mode = ConnectionMode.Bluetooth;
            status = ConnectionStatus.Default;
        }

        /// <summary>
        /// general connect function
        /// </summary>
        public void Connect()
        {
            if (mode == ConnectionMode.Bluetooth) ConnectBluetooth();
            else ConnectWifi();
            if(status == ConnectionStatus.Default)
            {
                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    mainWindow.UnlockRadioButtons();
                }));
            }
        }

        /// <summary>
        /// connect with bluetooth server
        /// </summary>
        private void ConnectBluetooth()
        {
            // first check if bluetooth is enabled
            if(!BluetoothRadio.IsSupported)
            {
                AddLog("(bluetooth) Bluetooth not enabled");
                return;
            }
            // initialize bluetooth server
            if (bthListener != null) Disconnect();
            bthListener = new BluetoothListener(bthServerUUID)
            {
                ServiceName = bthServerName
            };
            bthListener.Start();
            // update status
            SetStatus(ConnectionStatus.Listening);
            AddLog("(bluetooth) Server starts listening...");
            Debug.WriteLine("[Connection] ConnectBluetooth starts listening");
            // begin accepting clients
            isConnectionAllowed = true;
            BluetoothClient tmp = null;
            while (isConnectionAllowed)
            {
                try
                {
                    tmp = bthListener.AcceptBluetoothClient();
                }catch(InvalidOperationException e)
                {
                    Debug.WriteLine("[Connection] ConnectBluetooth -> " + e.Message);
                    break;
                }
                Debug.WriteLine("[Connection] ConnectBluetooth client detected, checking...");
                // check for valid client
                if (TestClient(tmp)) break;
                else
                {
                    tmp.Dispose();
                    tmp.Close();
                }
            }
            if (tmp == null)
            {
                Disconnect();
                return;
            }
            // close temp client
            tmp.Dispose();
            tmp.Close();
            Debug.WriteLine("[Connection] ConnectBluetooth found valid client");
            // prepare thread
            if(bthThread != null && bthThread.IsAlive)
            {
                isConnectionAllowed = false;
                if (!bthThread.Join(MAX_WAIT_TIME)) bthThread.Abort();
                Disconnect();
            }
            // prepare stream
            if (bthClientStream != null)
            {
                bthClientStream.Dispose();
                bthClientStream.Close();
            }
            // try to accept client with same ID
            try
            {
                isConnectionAllowed = true;
                bthClient = bthListener.AcceptBluetoothClient();
                while (!bthClient.RemoteEndPoint.Equals(bthTargetDeviceID) && isConnectionAllowed)
                {
                    bthClient.Dispose();
                    bthClient.Close();
                    bthClient = bthListener.AcceptBluetoothClient();
                }
            }
            catch(InvalidOperationException e)
            {
                Debug.WriteLine("[Connection] ConnectBluetooth -> " + e.Message);
                AddLog("(bluetooth) Server failed to connect valid client");
                Disconnect();
                return;
            }
            // set data stream
            bthClientStream = bthClient.GetStream();
            isConnectionAllowed = true;
            AddLog("(bluetooth) Client connected\nClient Name: " + bthClient.RemoteMachineName + "\nClient Address: " + bthClient.RemoteEndPoint);
            // start thread
            bthThread = new Thread(() =>
            {
                // update status
                SetStatus(ConnectionStatus.Connected);
                // prepare data placeholders
                byte[] placeholder = new byte[4];
                // start processing client
                while(isConnectionAllowed && bthClient != null)
                {
                    try
                    {
                        // read data into a buffer
                        byte[] buffer = new byte[BUFFER_SIZE];
                        int size = bthClientStream.Read(buffer, 0, BUFFER_SIZE);
                        if (size <= 0) break;
                        // process data into data packs
                        int idx = 0;
                        while(idx <= size - 13)
                        {
                            // check for separator
                            Array.Copy(buffer, idx, placeholder, 0, 4);
                            if(DecodeInt(placeholder) != DATA_SEPARATOR)
                            {
                                idx++;
                                continue;
                            }
                            idx += 4;
                            // get following data pack
                            MotionData data = new MotionData();
                            data.IsButton = BitConverter.ToBoolean(buffer, idx);
                            idx++;
                            Array.Copy(buffer, idx, placeholder, 0, 4);
                            data.Status = DecodeInt(placeholder);
                            idx += 4;
                            Array.Copy(buffer, idx, placeholder, 0, 4);
                            data.Value = DecodeFloat(placeholder);
                            idx += 4;
                            // add to shared buffer
                            sharedBuffer.AddData(data);
                        }
                    }catch(IOException e)
                    {
                        Debug.WriteLine("[Connection] ConnectBluetooth thread -> " + e.Message);
                        break;
                    }catch(ObjectDisposedException e)
                    {
                        Debug.WriteLine("[Connection] ConnectBluetooth thread -> " + e.Message);
                        break;
                    }
                    Thread.Sleep(10);
                }
                bthClientStream.Dispose();
                bthClientStream.Close();
                bthClientStream = null;
                Disconnect();
            });
            bthThread.Start();
            Debug.WriteLine("[Connection] ConnectBluetooth thread started");
        }

        /// <summary>
        /// connect with wifi server
        /// </summary>
        private void ConnectWifi()
        {

        }

        /// <summary>
        /// validate client
        /// </summary>
        /// <param name="client">bluetooth client</param>
        /// <returns></returns>
        private bool TestClient(BluetoothClient client)
        {
            if (client == null) return false;
            byte[] receivedPack = new byte[4];
            byte[] sentPack = EncodeInt(DEVICE_CHECK_DATA);
            try
            {
                var stream = client.GetStream();
                if (stream.CanTimeout)
                {
                    stream.ReadTimeout = MAX_WAIT_TIME;
                    stream.WriteTimeout = MAX_WAIT_TIME;
                }
                if (!stream.CanRead || !stream.CanWrite) return false;
                // check received integer
                if (stream.Read(receivedPack, 0, receivedPack.Length) == 0) return false;
                else if (DecodeInt(receivedPack) != DEVICE_CHECK_EXPECTED) return false;
                // send back integer for verification
                stream.Write(sentPack, 0, sentPack.Length);
                // save valid target client ID
                bthTargetDeviceID = client.RemoteEndPoint;
                stream.Flush();
                stream.Dispose();
                stream.Close();
            }
            catch (IOException e)
            {
                Debug.WriteLine("[Connection] TestClient -> " + e.Message);
                return false;
            }
            return true;
        }

        /// <summary>
        /// disconnect server
        /// </summary>
        public void Disconnect()
        {
            if(mode == ConnectionMode.Bluetooth)
            {
                // close client connection
                if(bthClientStream != null)
                {
                    bthClientStream.Dispose();
                    bthClientStream.Close();
                    bthClientStream = null;
                }
                if(bthClient != null)
                {
                    bthClient.Dispose();
                    bthClient.Close();
                    bthClient = null;
                }
                if(bthThread != null && bthThread.IsAlive)
                {
                    if (!bthThread.Join(MAX_WAIT_TIME)) bthThread.Abort();
                }
                if(bthListener != null)
                {
                    bthListener.Server.Dispose();
                    bthListener.Stop();
                    bthListener = null;
                }
                AddLog("(bluetooth) Client disconnected");
                Debug.WriteLine("[Connection] Disconnect client disconnected");
            }
            else
            {

            }
            SetStatus(ConnectionStatus.Default, true);
        }

        /// <summary>
        /// destroy connection service
        /// </summary>
        public void Destroy()
        {
            isConnectionAllowed = false;
            // check bluetooth side
            mode = ConnectionMode.Bluetooth;
            Disconnect();
            // check wifi side
            mode = ConnectionMode.Wifi;
            Disconnect();
        }

        /// <summary>
        /// set new connection status and update UI button
        /// </summary>
        /// <param name="s">new status</param>
        /// <param name="unlock">whether to unlock UI buttons</param>
        private void SetStatus(ConnectionStatus s, bool unlock = false)
        {
            status = s;
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                mainWindow.UpdateConnectButton();
                if(unlock)
                {
                    mainWindow.UnlockRadioButtons();
                }
            }));
        }

        /// <summary>
        /// add log message to main window
        /// </summary>
        /// <param name="message"></param>
        private void AddLog(string message)
        {
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                mainWindow.AddLogMessage("[Connection]\n" + message + "\n");
            }));
        }

        /// <summary>
        /// encode integer to stream byte array
        /// </summary>
        /// <param name="data">integer</param>
        /// <returns></returns>
        public static byte[] EncodeInt(int data)
        {
            byte[] array = BitConverter.GetBytes(data);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(array);
            return array;
        }

        /// <summary>
        /// decode integer from stream byte array
        /// </summary>
        /// <param name="array">byte array</param>
        /// <returns></returns>
        public static int DecodeInt(byte[] array)
        {
            if (BitConverter.IsLittleEndian)
                Array.Reverse(array);
            return BitConverter.ToInt32(array, 0);
        }

        /// <summary>
        /// decode float from stream byte array
        /// </summary>
        /// <param name="array">byte array</param>
        /// <returns></returns>
        public static float DecodeFloat(byte[] array)
        {
            if (BitConverter.IsLittleEndian)
                Array.Reverse(array);
            return BitConverter.ToSingle(array, 0);
        }
    }
}
