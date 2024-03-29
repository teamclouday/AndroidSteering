﻿using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Windows;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections.Generic;
using InTheHand.Net;
using InTheHand.Net.Sockets;
using InTheHand.Net.Bluetooth;

namespace SteeringWheel
{
    /// <summary>
    /// global shared buffer
    /// </summary>
    public class SharedBuffer
    {
        private const int MAX_SIZE = 200;
        private readonly Queue<MotionData> buffer = new Queue<MotionData>();
        public void AddData(bool v1, int v2, float v3)
        {
            lock (buffer)
            {
                buffer.Enqueue(new MotionData()
                {
                    IsButton = v1,
                    Status = v2,
                    Value = v3
                });
                while (buffer.Count > MAX_SIZE)
                {
                    buffer.Dequeue();
                }
            }
        }
        public void AddData(MotionData data)
        {
            lock (buffer)
            {
                buffer.Enqueue(data);
                while (buffer.Count > MAX_SIZE)
                {
                    buffer.Dequeue();
                }
            }
        }
        public MotionData GetData()
        {
            lock (buffer)
            {
                if (buffer.Count > 0)
                {
                    return buffer.Dequeue();
                }
                else return null;
            }
        }

        public void Clear()
        {
            lock (buffer)
            {
                buffer.Clear();
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
        public ConnectionMode Mode { get; set; }
        public ConnectionStatus Status { get; private set; }

        private readonly int MAX_WAIT_TIME = 1500;
        private readonly int DATA_SEPARATOR = 10086;
        private readonly int PACK_SIZE = 13; // 13 bytes per pack
        private readonly int NUM_PACKS = 100; // 100 packs a time
        private readonly int DEVICE_CHECK_EXPECTED = 123456;
        private readonly int DEVICE_CHECK_DATA = 654321;
        private readonly byte[] streamBuffer;
        private int streamBufferOffset = 0;
        private bool isConnectionAllowed = false;

        // bluetooth components
        private readonly Guid bthServerUUID = new Guid("a7bda841-7dbc-4179-9800-1a3eff463f1c");
        private readonly string bthServerName = "SteeringWheel Host";
        private BluetoothListener bthListener = null;
        private BluetoothClient bthClient = null;
        private BluetoothEndPoint bthTargetDeviceID = null;
        private Task bthTask = null;
        private CancellationTokenSource bthTaskToken = new CancellationTokenSource();
        // wifi components
        private readonly int wifiPort = 55555;
        private string wifiAddress;
        private string wifiAdapterName;
        private IPEndPoint wifiTargetDeviceID = null;
        private Socket wifiServer = null;
        private Socket wifiClient = null;
        private Task wifiTask = null;
        private CancellationTokenSource wifiTaskToken = new CancellationTokenSource();

        public Connection(MainWindow window, SharedBuffer buffer)
        {
            mainWindow = window;
            sharedBuffer = buffer;
            Mode = ConnectionMode.Bluetooth;
            Status = ConnectionStatus.Default;
            streamBuffer = new byte[PACK_SIZE * NUM_PACKS];
        }

        /// <summary>
        /// general connect function
        /// </summary>
        public async Task Connect()
        {
            sharedBuffer.Clear();
            mainWindow.ResetController();
            if (Mode == ConnectionMode.Bluetooth) await ConnectBluetooth();
            else await ConnectWifi();
            if (Status == ConnectionStatus.Default)
            {
                Application.Current?.Dispatcher.Invoke(new Action(() =>
                {
                    mainWindow?.UnlockRadioButtons();
                }));
            }
        }

        /// <summary>
        /// connect with bluetooth server
        /// </summary>
        private async Task ConnectBluetooth()
        {
            // first check if bluetooth is enabled
            if (!BluetoothRadio.IsSupported)
            {
                AddLog("(bluetooth) Bluetooth not enabled");
                return;
            }
            // initialize bluetooth server
            if (bthListener != null) await Disconnect();
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
            try
            {
                while (isConnectionAllowed && bthListener != null)
                {
                    tmp = bthListener.AcceptBluetoothClient();

                    Debug.WriteLine("[Connection] ConnectBluetooth client detected, checking...");
                    if (TestClient(tmp)) break;
                    else
                    {
                        tmp.Dispose();
                        tmp.Close();
                    }
                }
            }
            catch (InvalidOperationException e)
            {
                Debug.WriteLine("[Connection] ConnectBluetooth -> " + e.Message);
                return;
            }
            catch (SocketException e)
            {
                Debug.WriteLine("[Connection] ConnectBluetooth -> " + e.Message);
                return;
            }
            // check for valid client
            if (tmp != null)
            {
                tmp.Dispose();
                tmp.Close();
            }
            if (bthTargetDeviceID == null)
            {
                await Disconnect();
                return;
            }
            Debug.WriteLine("[Connection] ConnectBluetooth found valid client");
            // prepare task
            if (bthTask?.IsCompleted == false)
            {
                isConnectionAllowed = false;
                bthTaskToken.Cancel();
                try
                {
                    await bthTask;
                }
                catch (OperationCanceledException e)
                {
                    Debug.WriteLine("[Connection] ConnectBluetooth -> " + e.Message);
                }
                finally
                {
                    bthTask.Dispose();
                }
                bthTaskToken = new CancellationTokenSource();
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
                await Disconnect();
                return;
            }
            catch (SocketException e)
            {
                Debug.WriteLine("[Connection] ConnectBluetooth -> " + e.Message);
                AddLog("(bluetooth) Server failed to connect valid client");
                await Disconnect();
                return;
            }
            // update status
            SetStatus(ConnectionStatus.Connected);
            AddLog("(bluetooth) Client connected\nClient Name: " + bthClient.RemoteMachineName + "\nClient Address: " + bthClient.RemoteEndPoint);
            // start task
            isConnectionAllowed = true;
            bthTask = Task.Factory.StartNew(async () =>
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
                            if (bthTaskToken.IsCancellationRequested) break;
                            await ReadStream(bthStream, bthTaskToken.Token);
                        }
                    }
                }
                catch (SocketException e)
                {
                    Debug.WriteLine("[Connection] ConnectBluetooth task -> " + e.Message);
                }
                catch (IOException e)
                {
                    Debug.WriteLine("[Connection] ConnectBluetooth task -> " + e.Message);
                }
                catch (ObjectDisposedException e)
                {
                    Debug.WriteLine("[Connection] ConnectBluetooth task -> " + e.Message);
                }
                await Disconnect();
            }, bthTaskToken.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            Debug.WriteLine("[Connection] ConnectBluetooth task started");
        }

        /// <summary>
        /// check if wifi / LAN is properly set
        /// </summary>
        /// <returns></returns>
        public bool CheckWifi()
        {
            if (Mode != ConnectionMode.Wifi)
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
            SelectNetworkWindow dialog = new SelectNetworkWindow(validAddresses)
            {
                ShowInTaskbar = false
            };
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
        private async Task ConnectWifi()
        {
            AddLog("(wifi) Server network adapter = " + wifiAdapterName);
            AddLog("(wifi) Server IP Address = " + wifiAddress.ToString());
            if (wifiServer != null) await Disconnect();
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
                await Disconnect();
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
                await Disconnect();
                return;
            }
            catch (ObjectDisposedException e)
            {
                Debug.WriteLine("[Connection] ConnectWifi -> " + e.Message);
                await Disconnect();
                return;
            }
            // check if target is found
            if (wifiTargetDeviceID == null)
            {
                await Disconnect();
                return;
            }
            // prepare task
            if (wifiTask?.IsCompleted == false)
            {
                isConnectionAllowed = false;
                wifiTaskToken.Cancel();
                try
                {
                    await wifiTask;
                }
                catch (OperationCanceledException e)
                {
                    Debug.WriteLine("[Connection] ConnectWifi -> " + e.Message);
                }
                finally
                {
                    wifiTask.Dispose();
                }
                wifiTaskToken = new CancellationTokenSource();
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
                await Disconnect();
                return;
            }
            catch (ObjectDisposedException e)
            {
                Debug.WriteLine("[Connection] ConnectWifi -> " + e.Message);
                AddLog("(wifi) Server failed to connect valid client");
                await Disconnect();
                return;
            }
            // update status
            SetStatus(ConnectionStatus.Connected);
            AddLog("(wifi) Client connected\nClient Address: " + wifiClient.RemoteEndPoint);
            isConnectionAllowed = true;
            // start task
            wifiTask = Task.Factory.StartNew(async () =>
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
                            if (wifiTaskToken.IsCancellationRequested) break;
                            await ReadStream(wifiStream, wifiTaskToken.Token);
                        }
                    }
                }
                catch (SocketException e)
                {
                    Debug.WriteLine("[Connection] ConnectWifi task -> " + e.Message);
                }
                catch (IOException e)
                {
                    Debug.WriteLine("[Connection] ConnectWifi task -> " + e.Message);
                }
                catch (ObjectDisposedException e)
                {
                    Debug.WriteLine("[Connection] ConnectWifi task -> " + e.Message);
                }
                await Disconnect();
            }, wifiTaskToken.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            Debug.WriteLine("[Connection] ConnectWifi task started");
        }

        private async Task ReadStream(NetworkStream stream, CancellationToken token)
        {
            // read data into a buffer
            int readSize = await stream.ReadAsync(streamBuffer, streamBufferOffset, streamBuffer.Length - streamBufferOffset, token);
            using (var memory = new MemoryStream(streamBuffer, 0, streamBufferOffset + readSize, false))
            {
                var reader = new BufferedBinaryReader(memory);
                try
                {
                    int readIdx = 0;
                    int readTotal = streamBufferOffset + readSize;
                    while (readIdx < readTotal)
                    {
                        // find data separator
                        while (readIdx < readTotal - PACK_SIZE && !reader.ReadInt32().Equals(DATA_SEPARATOR))
                        {
                            reader.SetReadPosition(++readIdx);
                        }

                        // read next packet
                        if (readIdx < readTotal - PACK_SIZE)
                        {
                            MotionData data = new MotionData()
                            {
                                IsButton = reader.ReadBoolean(),
                                Status = reader.ReadInt32(),
                                Value = reader.ReadSingle()
                            };
                            sharedBuffer.AddData(data);
                            readIdx += PACK_SIZE;
                        }
                        else
                        {
                            streamBufferOffset = readTotal - readIdx;
                            Buffer.BlockCopy(streamBuffer, readIdx, streamBuffer, 0, streamBufferOffset);
                            break;
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine($"[Connection] ReadStream -> {e}");
                }
            }
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
        public async Task Disconnect()
        {
            isConnectionAllowed = false;
            if (Mode == ConnectionMode.Bluetooth)
            {
                if (bthClient != null)
                {
                    bthClient.Dispose();
                    bthClient.Close();
                    bthClient = null;
                }
                // shut down task
                if (bthTask?.IsCompleted == false)
                {
                    bthTaskToken.Cancel();
                    try
                    {
                        await bthTask;
                    }
                    catch (OperationCanceledException e)
                    {
                        Debug.WriteLine("[Connection] Disconnect -> " + e.Message);
                    }
                    finally
                    {
                        bthTask.Dispose();
                        bthTask = null;
                    }
                    bthTaskToken = new CancellationTokenSource();
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
                // shut down task
                if (wifiTask?.IsCompleted == false)
                {
                    wifiTaskToken.Cancel();
                    try
                    {
                        await wifiTask;
                    }
                    catch (OperationCanceledException e)
                    {
                        Debug.WriteLine("[Connection] Disconnect -> " + e.Message);
                    }
                    finally
                    {
                        wifiTask.Dispose();
                        wifiTask = null;
                    }
                    wifiTaskToken = new CancellationTokenSource();
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
            mainWindow.ResetController();
        }

        /// <summary>
        /// destroy connection service
        /// </summary>
        public async Task Destroy()
        {
            isConnectionAllowed = false;
            // check bluetooth side
            Mode = ConnectionMode.Bluetooth;
            await Disconnect();
            // check wifi side
            Mode = ConnectionMode.Wifi;
            await Disconnect();
        }

        /// <summary>
        /// set new connection status and update UI button
        /// </summary>
        /// <param name="s">new status</param>
        /// <param name="unlock">whether to unlock UI buttons</param>
        /// <returns>whether status is updated</returns>
        private bool SetStatus(ConnectionStatus s, bool unlock = false)
        {
            bool ret = s != Status;
            Status = s;
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
    }
}
