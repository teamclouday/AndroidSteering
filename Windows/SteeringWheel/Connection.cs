using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Windows;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;
using InTheHand.Net;
using InTheHand.Net.Sockets;
using InTheHand.Net.Bluetooth;
using System.Net.NetworkInformation;

namespace SteeringWheel
{
    /// <summary>
    /// global shared buffer
    /// </summary>
    public class SharedBuffer
    {
        private const int MAX_SIZE = 5;
        private readonly List<MotionData> buffer = new List<MotionData>();
        public void AddData(bool v1, int v2, float v3)
        {
            lock (buffer)
            {
                if (v1 || buffer.Count <= MAX_SIZE)
                {
                    buffer.Add(new MotionData()
                    {
                        IsButton = v1,
                        Status = v2,
                        Value = v3
                    });
                }
                else
                {
                    bool updated = false;
                    for (int idx = buffer.Count - 1; idx >= 0; idx--)
                    {
                        if (!buffer[idx].IsButton && buffer[idx].Status == v2)
                        {
                            buffer[idx].Value = buffer[idx].Value * 0.4f + v3 * 0.6f; // take weighted average
                            updated = true;
                            break;
                        }
                    }
                    // if not updated, insert regardless of oversize
                    if (!updated)
                    {
                        buffer.Add(new MotionData()
                        {
                            IsButton = v1,
                            Status = v2,
                            Value = v3
                        });
                    }
                }
            }
        }
        public void AddData(MotionData data)
        {
            lock (buffer)
            {
                if (data.IsButton || buffer.Count <= MAX_SIZE)
                {
                    buffer.Add(data);
                }
                else
                {
                    bool updated = false;
                    for (int idx = buffer.Count - 1; idx >= 0; idx--)
                    {
                        if (!buffer[idx].IsButton && buffer[idx].Status == data.Status)
                        {
                            buffer[idx].Value = buffer[idx].Value * 0.4f + data.Value * 0.6f; // take weighted average
                            updated = true;
                            break;
                        }
                    }
                    // if not updated, insert regardless of oversize
                    if (!updated)
                    {
                        buffer.Add(data);
                    }
                }
            }
        }
        public MotionData GetData()
        {
            lock (buffer)
            {
                if (buffer.Count > 0)
                {
                    MotionData data = buffer[0];
                    buffer.RemoveAt(0);
                    return data;
                }
                else return null;
            }
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
    public class Connection
    {
        private readonly MainWindow mainWindow;
        private readonly SharedBuffer sharedBuffer;
        public ConnectionMode mode { get; set; }
        public ConnectionStatus status { get; private set; }

        private readonly int MAX_WAIT_TIME = 1500;
        private readonly int DATA_SEPARATOR = 10086;
        private readonly int PACK_SIZE = 13;
        private readonly int NUM_PACKS = 4; // 4 packs a time
        private readonly int DEVICE_CHECK_EXPECTED = 123456;
        private readonly int DEVICE_CHECK_DATA = 654321;
        private bool isConnectionAllowed = false;
        private byte[] lastPack = new byte[13];
        private int lastPackLength = 0;

        // bluetooth components
        private readonly Guid bthServerUUID = new Guid("a7bda841-7dbc-4179-9800-1a3eff463f1c");
        private readonly string bthServerName = "SteeringWheel Host";
        private BluetoothListener bthListener = null;
        private BluetoothClient bthClient = null;
        private BluetoothEndPoint bthTargetDeviceID = null;
        private Thread bthThread = null;
        // wifi components
        private readonly int wifiPort = 55555;
        private string wifiAddress;
        private string wifiAdapterName;
        private IPEndPoint wifiTargetDeviceID = null;
        private Socket wifiServer = null;
        private Socket wifiClient = null;
        private Thread wifiThread = null;

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
            if (status == ConnectionStatus.Default)
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
            if (!BluetoothRadio.IsSupported)
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
            bthTargetDeviceID = null;
            while (isConnectionAllowed && bthListener != null)
            {
                try
                {
                    tmp = bthListener.AcceptBluetoothClient();
                }
                catch (InvalidOperationException e)
                {
                    Debug.WriteLine("[Connection] ConnectBluetooth -> " + e.Message);
                    break;
                }
                catch (SocketException e)
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
            if (tmp != null)
            {
                tmp.Dispose();
                tmp.Close();
            }
            if (bthTargetDeviceID == null)
            {
                Disconnect();
                return;
            }
            Debug.WriteLine("[Connection] ConnectBluetooth found valid client");
            // prepare thread
            if (bthThread != null && bthThread.IsAlive)
            {
                isConnectionAllowed = false;
                if (!bthThread.Join(MAX_WAIT_TIME)) bthThread.Abort();
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
            catch (InvalidOperationException e)
            {
                Debug.WriteLine("[Connection] ConnectBluetooth -> " + e.Message);
                AddLog("(bluetooth) Server failed to connect valid client");
                Disconnect();
                return;
            }
            catch (SocketException e)
            {
                Debug.WriteLine("[Connection] ConnectBluetooth -> " + e.Message);
                AddLog("(bluetooth) Server failed to connect valid client");
                Disconnect();
                return;
            }
            // update status
            SetStatus(ConnectionStatus.Connected);
            AddLog("(bluetooth) Client connected\nClient Name: " + bthClient.RemoteMachineName + "\nClient Address: " + bthClient.RemoteEndPoint);
            // start thread
            isConnectionAllowed = true;
            bthThread = new Thread(() =>
            {
                // prepare data placeholders
                byte[] placeholder = new byte[4];
                // start processing client
                try
                {
                    using (var bthStream = bthClient.GetStream())
                    {
                        while (isConnectionAllowed && bthClient != null)
                        {
                            // read data into a buffer
                            byte[] buffer = new byte[PACK_SIZE * (NUM_PACKS + 1)];
                            Array.Copy(lastPack, 0, buffer, 0, lastPackLength); // add last pack
                            int size = bthStream.Read(buffer, lastPackLength, PACK_SIZE * NUM_PACKS);
                            if (size <= 0) break;
                            int totalSize = size + lastPackLength;
                            // process data into data packs
                            int idx = 0;
                            while (idx <= totalSize - PACK_SIZE)
                            {
                                // check for separator
                                Array.Copy(buffer, idx, placeholder, 0, 4);
                                if (DecodeInt(placeholder) != DATA_SEPARATOR)
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
                            // check for remaining pack, and store for next loop
                            Array.Copy(buffer, idx, lastPack, 0, totalSize - idx);
                            lastPackLength = totalSize - idx;
                            Thread.Sleep(1);
                        }
                    }
                }
                catch (SocketException e)
                {
                    Debug.WriteLine("[Connection] ConnectBluetooth thread -> " + e.Message);
                }
                catch (IOException e)
                {
                    Debug.WriteLine("[Connection] ConnectBluetooth thread -> " + e.Message);
                }
                catch (ObjectDisposedException e)
                {
                    Debug.WriteLine("[Connection] ConnectBluetooth thread -> " + e.Message);
                }

                Disconnect();
            });
            bthThread.Priority = ThreadPriority.AboveNormal;
            bthThread.Start();
            Debug.WriteLine("[Connection] ConnectBluetooth thread started");
        }

        /// <summary>
        /// check if wifi / LAN is properly set
        /// </summary>
        /// <returns></returns>
        public bool CheckWifi()
        {
            if (mode != ConnectionMode.Wifi)
                return true;
            // get wifi IP address
            // reference: https://stackoverflow.com/questions/9855230/how-do-i-get-the-network-interface-and-its-right-ipv4-address
            wifiAddress = "";
            wifiAdapterName = "";
            List<Tuple<string, string>> validAddresses = new List<Tuple<string, string>>();
            // get adapters
            NetworkInterface[] nis = NetworkInterface.GetAllNetworkInterfaces();
            foreach (var ni in nis)
            {
                // skip unenabled adapters
                if (ni.OperationalStatus != OperationalStatus.Up) continue;
                // skip adapters that are not ethernet or wifi
                if (ni.NetworkInterfaceType != NetworkInterfaceType.Ethernet &&
                    ni.NetworkInterfaceType != NetworkInterfaceType.Wireless80211) continue;
                // get IP properties
                var props = ni.GetIPProperties();
                // analyze addresses
                foreach (var addr in props.UnicastAddresses)
                {
                    // select IPv4 except loopback
                    if (addr.Address.AddressFamily == AddressFamily.InterNetwork &&
                        !IPAddress.IsLoopback(addr.Address) && addr.IsDnsEligible)
                    {
                        wifiAdapterName = ni.Name;
                        wifiAddress = addr.Address.ToString();
                        validAddresses.Add(new Tuple<string, string>(wifiAdapterName, wifiAddress));
                        Debug.WriteLine("[Connection] ConnectWifi -> new valid address " + wifiAddress);
                    }
                }
            }
            // check if at least 1 address is found
            if (validAddresses.Count == 0)
            {
                AddLog("(wifi) No valid IPv4 network (Wifi/Ethernet) found");
                return false;
            }
            // user selection
            SelectNetworkWindow dialog = new SelectNetworkWindow(validAddresses);
            if (dialog.ShowDialog() == true)
            {
                var pair = validAddresses[dialog.selectedIdx];
                wifiAdapterName = pair.Item1;
                wifiAddress = pair.Item2;
                Debug.WriteLine("[Connection] ConnectWifi -> selected address " + wifiAddress);
            }
            return true;
        }

        /// <summary>
        /// connect with wifi server
        /// </summary>
        private void ConnectWifi()
        {
            AddLog("(wifi) Server network adapter = " + wifiAdapterName);
            AddLog("(wifi) Server IP Address = " + wifiAddress.ToString());
            if (wifiServer != null) Disconnect();
            // create server and start listening
            try
            {
                wifiServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                wifiServer.Bind(new IPEndPoint(IPAddress.Parse(wifiAddress), wifiPort));
                wifiServer.Listen(1); // 1 request at a time
            }
            catch (SocketException e)
            {
                Debug.WriteLine("[Connection] ConnectWifi -> " + e.Message);
                AddLog("(wifi) Server failed to start");
                Disconnect();
                return;
            }
            SetStatus(ConnectionStatus.Listening);
            AddLog("(wifi) Server starts listening...");
            Socket tmp = null;
            wifiTargetDeviceID = null;
            isConnectionAllowed = true;
            // find target device
            try
            {
                while (isConnectionAllowed && wifiServer != null)
                {
                    tmp = wifiServer.Accept();
                    Debug.WriteLine("[Connection] ConnectWifi client detected, checking...");
                    if (TestClient(tmp)) break;
                    else
                    {
                        tmp.Close();
                        tmp.Dispose();
                    }
                }
            }
            catch (SocketException e)
            {
                Debug.WriteLine("[Connection] ConnectWifi -> " + e.Message);
                Disconnect();
                return;
            }
            catch (ObjectDisposedException e)
            {
                Debug.WriteLine("[Connection] ConnectWifi -> " + e.Message);
                Disconnect();
                return;
            }
            // check if target is found
            if (wifiTargetDeviceID == null)
            {
                Disconnect();
                return;
            }
            // prepare thread
            if (wifiThread != null && wifiThread.IsAlive)
            {
                isConnectionAllowed = false;
                if (!wifiThread.Join(MAX_WAIT_TIME)) wifiThread.Abort();
            }
            // try to accept client with same ID
            try
            {
                isConnectionAllowed = true;
                wifiClient = wifiServer.Accept();
                while (!(wifiClient.RemoteEndPoint as IPEndPoint).Address.Equals(wifiTargetDeviceID.Address) && isConnectionAllowed)
                {
                    wifiClient.Dispose();
                    wifiClient.Close();
                    wifiClient = wifiServer.Accept();
                }
            }
            catch (SocketException e)
            {
                Debug.WriteLine("[Connection] ConnectWifi -> " + e.Message);
                AddLog("(wifi) Server failed to connect valid client");
                Disconnect();
                return;
            }
            catch (ObjectDisposedException e)
            {
                Debug.WriteLine("[Connection] ConnectWifi -> " + e.Message);
                AddLog("(wifi) Server failed to connect valid client");
                Disconnect();
                return;
            }
            // update status
            SetStatus(ConnectionStatus.Connected);
            AddLog("(wifi) Client connected\nClient Address: " + wifiClient.RemoteEndPoint);
            isConnectionAllowed = true;
            // start thread
            wifiThread = new Thread(() =>
            {
                // prepare data placeholders
                byte[] placeholder = new byte[4];
                // start processing client
                try
                {
                    using (var wifiStream = new NetworkStream(wifiClient))
                    {
                        while (isConnectionAllowed && wifiClient != null)
                        {
                            // read data into a buffer
                            byte[] buffer = new byte[PACK_SIZE * (NUM_PACKS + 1)];
                            Array.Copy(lastPack, 0, buffer, 0, lastPackLength); // add last pack
                            int size = wifiStream.Read(buffer, lastPackLength, PACK_SIZE * NUM_PACKS);
                            if (size <= 0) break;
                            int totalSize = size + lastPackLength;
                            // process data into data packs
                            int idx = 0;
                            while (idx <= totalSize - PACK_SIZE)
                            {
                                // check for separator
                                Array.Copy(buffer, idx, placeholder, 0, 4);
                                if (DecodeInt(placeholder) != DATA_SEPARATOR)
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
                            // check for remaining pack, and store for next loop
                            Array.Copy(buffer, idx, lastPack, 0, totalSize - idx);
                            lastPackLength = totalSize - idx;
                            Thread.Sleep(1);
                        }
                    }
                }
                catch (SocketException e)
                {
                    Debug.WriteLine("[Connection] ConnectWifi thread -> " + e.Message);
                }
                catch (IOException e)
                {
                    Debug.WriteLine("[Connection] ConnectWifi thread -> " + e.Message);
                }
                catch (ObjectDisposedException e)
                {
                    Debug.WriteLine("[Connection] ConnectWifi thread -> " + e.Message);
                }
                Disconnect();
            });
            wifiThread.Priority = ThreadPriority.AboveNormal;
            wifiThread.Start();
            Debug.WriteLine("[Connection] ConnectWifi thread started");
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
                int offset = 0;
                do
                {
                    int size = stream.Read(receivedPack, offset, receivedPack.Length - offset);
                    if (size <= 0)
                    {
                        Debug.WriteLine("[Connection] TestClient invalid received size (" + size + ")");
                        return false;
                    }
                    offset += size;
                } while (offset < 4);
                int decoded = DecodeInt(receivedPack);
                if (decoded != DEVICE_CHECK_EXPECTED)
                {
                    Debug.WriteLine("[Connection] TestClient decoded number not expected (" + decoded + ")");
                    return false;
                }
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
        /// validate client
        /// </summary>
        /// <param name="client">wifi client</param>
        /// <returns></returns>
        private bool TestClient(Socket client)
        {
            if (client == null) return false;
            byte[] receivedPack = new byte[4];
            byte[] sentPack = EncodeInt(DEVICE_CHECK_DATA);
            try
            {
                // check received integer
                int offset = 0;
                do
                {
                    int size = client.Receive(receivedPack, offset, receivedPack.Length - offset, SocketFlags.None);
                    if (size <= 0)
                    {
                        Debug.WriteLine("[Connection] TestClient invalid received size (" + size + ")");
                        return false;
                    }
                    offset += size;
                } while (offset < 4);
                int decoded = DecodeInt(receivedPack);
                if (decoded != DEVICE_CHECK_EXPECTED)
                {
                    Debug.WriteLine("[Connection] TestClient decoded number not expected (" + decoded + ")");
                    return false;
                }
                // send back integer for verification
                client.Send(sentPack, sentPack.Length, SocketFlags.None);
                wifiTargetDeviceID = client.RemoteEndPoint as IPEndPoint;
            }
            catch (IOException e)
            {
                Debug.WriteLine("[Connection] TestClient -> " + e.Message);
                return false;
            }
            Debug.WriteLine("[Connection] ConnectWifi valid client found");
            client.Dispose();
            client.Close();
            return true;
        }

        /// <summary>
        /// disconnect server
        /// </summary>
        public void Disconnect()
        {
            if (mode == ConnectionMode.Bluetooth)
            {
                if (bthClient != null)
                {
                    bthClient.Dispose();
                    bthClient.Close();
                    bthClient = null;
                }
                // shut down thread
                if (bthThread != null && bthThread.IsAlive)
                {
                    if (!bthThread.Join(MAX_WAIT_TIME)) bthThread.Abort();
                }
                // stop server
                if (bthListener != null)
                {
                    try
                    {
                        if (bthListener.Server != null) bthListener.Server.Dispose();
                        bthListener.Stop();
                        bthListener = null;
                    }
                    catch (SocketException e)
                    {
                        Debug.WriteLine("[Connection] Disconnect -> " + e.Message);
                    }
                }
                if (SetStatus(ConnectionStatus.Default, true))
                    AddLog("(bluetooth) Client disconnected");
                Debug.WriteLine("[Connection] Disconnect client disconnected");
            }
            else
            {
                // close client connection
                if (wifiClient != null)
                {
                    wifiClient.Dispose();
                    wifiClient.Close();
                    wifiClient = null;
                }
                // shut down thread
                if (wifiThread != null && wifiThread.IsAlive)
                {
                    if (!wifiThread.Join(MAX_WAIT_TIME)) wifiThread.Abort();
                }
                // stop server
                if (wifiServer != null)
                {
                    try
                    {
                        wifiServer.Dispose();
                        wifiServer.Close();
                        wifiServer = null;
                    }
                    catch (SocketException e)
                    {
                        Debug.WriteLine("[Connection] Disconnect -> " + e.Message);
                    }
                }
                if (SetStatus(ConnectionStatus.Default, true))
                    AddLog("(wifi) Client disconnected");
                Debug.WriteLine("[Connection] Disconnect client disconnected");
            }
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
        /// <returns>whether status is updated</returns>
        private bool SetStatus(ConnectionStatus s, bool unlock = false)
        {
            bool ret = s != status;
            status = s;
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                mainWindow.UpdateConnectButton();
                if (unlock)
                {
                    mainWindow.UnlockRadioButtons();
                }
            }));
            return ret;
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
