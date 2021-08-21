using System;
using System.Windows;
using System.Threading;
using System.Diagnostics;
using vJoyInterfaceWrap;

namespace SteeringWheel
{
    /// <summary>
    /// define each motion data
    /// </summary>
    public class MotionData
    {
        public bool IsButton;
        public int Status;
        public float Value;
    }

    /// <summary>
    /// define motion buttons
    /// </summary>
    public enum MotionButton
    {
        X = 0,
        Y = 1,
        A = 2,
        B = 3,
        LB = 4,
        RB = 5,
        UP = 6,
        DOWN = 7,
        RIGHT = 8,
        LEFT = 9,
        BACK = 10,
        START = 11
    }

    /// <summary>
    /// define buttons on controller
    /// </summary>
    public enum ControlButton
    {
        A = 1,
        B = 2,
        X = 3,
        Y = 4,
        LB = 5,
        RB = 6,
        BACK = 7,
        START = 8,
        LS = 9,
        RS = 10,
        HOME = 11
    }

    /// <summary>
    /// define controller axis
    /// </summary>
    public enum ControlAxis
    {
        X,
        Y,
        Z,
        XRot,
        YRot,
        ZRot,
        POVLeft,
        POVRight,
        POVUp,
        POVDown
    }

    /// <summary>
    /// Controller service
    /// </summary>
    class Controller
    {
        private readonly MainWindow mainWindow;
        private readonly SharedBuffer sharedBuffer;
        private Thread processThread;
        private Thread updateThread;
        private readonly int MAX_WAIT_TIME = 1500;
        private bool isProcessAllowed = false;
        private readonly float CAP_Steering = 45.0f; //treat max angle as 60 even though it can reach 90
        private readonly float CAP_AccForward = 80.0f;
        private readonly float CAP_AccBackward = 10.0f;

        // vjoy related
        private readonly vJoy joystick;
        private vJoy.JoystickState joyReport;
        private readonly object joyReportLock = new object();
        private uint joystickID;
        private long axisMax = 0;
        public bool vJoyInitialized { get; private set; }
        private const int triggerInterval = 100;
        private const int updateInterval = 10;

        public Controller(MainWindow window, SharedBuffer buffer)
        {
            mainWindow = window;
            sharedBuffer = buffer;

            vJoyInitialized = false;
            joystick = new vJoy();
            joyReport = new vJoy.JoystickState();
            if (joystick.vJoyEnabled())
            {
                SetupVJoy();
                SetupProcess();
                SetupUpdate();
            }
        }

        /// <summary>
        /// destroy before window closes
        /// </summary>
        public void Destroy()
        {
            isProcessAllowed = false;
            if (processThread != null && processThread.IsAlive)
            {
                if (!processThread.Join(MAX_WAIT_TIME)) processThread.Abort();
            }
            if (updateThread != null && updateThread.IsAlive)
            {
                if (!updateThread.Join(MAX_WAIT_TIME)) updateThread.Abort();
            }
            ResetVJoy();
            joystick.RelinquishVJD(joystickID);
        }

        /// <summary>
        /// setup background thread that write joystate
        /// </summary>
        private void SetupProcess()
        {
            isProcessAllowed = true;
            processThread = new Thread(() =>
            {
                while(isProcessAllowed)
                {
                    var data = sharedBuffer.GetData();
                    if(data == null)
                    {
                        Thread.Sleep(5);
                        continue;
                    }
                    if(data.IsButton)
                    {
                        Debug.WriteLine("[Controller] processThread button pressed (" + data.Status + ")");
                        switch ((MotionButton)data.Status)
                        {
                            case MotionButton.A:
                                TriggerControl(ControlButton.A);
                                break;
                            case MotionButton.B:
                                TriggerControl(ControlButton.B);
                                break;
                            case MotionButton.X:
                                TriggerControl(ControlButton.X);
                                break;
                            case MotionButton.Y:
                                TriggerControl(ControlButton.Y);
                                break;
                            case MotionButton.LB:
                                TriggerControl(ControlButton.LB);
                                break;
                            case MotionButton.RB:
                                TriggerControl(ControlButton.RB);
                                break;
                            case MotionButton.START:
                                TriggerControl(ControlButton.START);
                                break;
                            case MotionButton.BACK:
                                TriggerControl(ControlButton.BACK);
                                break;
                            case MotionButton.UP:
                                TriggerControl(ControlAxis.POVUp);
                                break;
                            case MotionButton.DOWN:
                                TriggerControl(ControlAxis.POVDown);
                                break;
                            case MotionButton.LEFT:
                                TriggerControl(ControlAxis.POVLeft);
                                break;
                            case MotionButton.RIGHT:
                                TriggerControl(ControlAxis.POVRight);
                                break;
                        }
                    }
                    else
                    {
                        switch(data.Status)
                        {
                            case 0:
                                ProcessSteering(data.Value);
                                break;
                            case 1:
                                ProcessAcceleration(data.Value);
                                break;
                        }
                    }
                    Thread.Sleep(1);
                }
            });
            processThread.Priority = ThreadPriority.AboveNormal;
            processThread.Start();
        }

        /// <summary>
        /// set up background thread that updates vJoy state
        /// </summary>
        private void SetupUpdate()
        {
            isProcessAllowed = true;
            updateThread = new Thread(() =>
            {
                while(isProcessAllowed)
                {
                    lock(joyReportLock)
                    {
                        if (!joystick.UpdateVJD(joystickID, ref joyReport))
                        {
                            // AddLog("Failed to update vJoy controller state");
                            Debug.WriteLine("[Controller] updateThread failed to update VJD");
                            joystick.AcquireVJD(joystickID);
                        }
                    }
                    Thread.Sleep(updateInterval);
                }
            });
            updateThread.Priority = ThreadPriority.AboveNormal;
            updateThread.Start();
        }

        /// <summary>
        /// process acceleration based on input value
        /// </summary>
        /// <param name="val"></param>
        private void ProcessAcceleration(float val)
        {
            if(-40.0f <= val && val <= -30.0f)
            {
                lock(joyReportLock)
                {
                    joyReport.AxisZ = 0;
                    joyReport.AxisZRot = 0;
                }
            }
            else if(-90.0f <= val && val < -40.0f)
            {
                // forward
                float step = FilterLinear(-val, 40.0f, CAP_AccForward);
                val = axisMax * step;
                lock(joyReportLock)
                {
                    joyReport.AxisZ = 0;
                    joyReport.AxisZRot = (int)val;
                }
            }
            else if(-30.0f < val && val <= 90.0f)
            {
                // backward
                float step = FilterLinear(val, -30.0f, CAP_AccBackward);
                val = axisMax * step;
                lock (joyReportLock)
                {
                    joyReport.AxisZRot = 0;
                    joyReport.AxisZ = (int)val;
                }
            }
        }

        /// <summary>
        /// process steering based on input value
        /// </summary>
        /// <param name="val"></param>
        private void ProcessSteering(float val)
        {
            if(-2.0f <= val && val <= 2.0f)
            {
                // set to rest mode
                lock (joyReportLock)
                {
                    joyReport.AxisX = (int)(axisMax / 2);
                }
            }
            else if(2.0f < val && val <= 90.0f)
            {
                // turning left
                float step = FilterSmoothStep(val, 2.0f, CAP_Steering);
                float half = axisMax / 2.0f * step;
                lock (joyReportLock)
                {
                    joyReport.AxisX = (int)(axisMax / 2.0f - half);
                }
            }
            else if(-90.0f <= val && val < -2.0f)
            {
                // turning right
                float step = FilterSmoothStep(-val, 2.0f, CAP_Steering);
                float half = axisMax / 2.0f * step;
                lock (joyReportLock)
                {
                    joyReport.AxisX = (int)(axisMax / 2.0f + half);
                }
            }
        }

        /// <summary>
        /// setup vjoy and find target device ID
        /// </summary>
        private void SetupVJoy()
        {
            // get current driver version and dll version
            uint verDriver = 0, verDll = 0;
            joystick.DriverMatch(ref verDll, ref verDriver);
            AddLog(string.Format("(ver {0:X}) vJoy Driver\n(ver {1:X}) vJoy Dll", verDriver, verDll));
            // find target device
            for (uint i = 1; i <= 16; i++)
            {
                VjdStat status = joystick.GetVJDStatus(i);
                if (status == VjdStat.VJD_STAT_FREE && joystick.AcquireVJD(i))
                {
                    if (CheckDeviceSpecs(i))
                    {
                        joystickID = i;
                        joystick.GetVJDAxisMax(joystickID, HID_USAGES.HID_USAGE_X, ref axisMax);
                        joyReport.bDevice = (byte)joystickID;
                        ResetVJoy();
                        AddLog("vJoy valid device found\nID = " + joystickID);
                        Debug.WriteLine("[Controller] SetupVJoy find valid device ID = " + joystickID);
                        vJoyInitialized = true;
                        break;
                    }
                    else
                    {
                        joystick.RelinquishVJD(i);
                    }
                }
            }
        }

        /// <summary>
        /// reset vjoy controller axis
        /// </summary>
        private void ResetVJoy()
        {
            joystick.ResetAll();
            joyReport.bHats = 0xFFFFFFFF;
            joyReport.AxisX = (int)(axisMax / 2.0f);
            joyReport.AxisXRot = (int)(axisMax / 2.0f);
            joyReport.AxisY = (int)(axisMax / 2.0f);
            joyReport.AxisYRot = (int)(axisMax / 2.0f);
            joyReport.AxisZ = 0;
            joyReport.AxisZRot = 0;
            joystick.UpdateVJD(joystickID, ref joyReport);
        }

        /// <summary>
        /// check necessary components according to xbox controller
        /// </summary>
        /// <param name="id">device ID</param>
        /// <returns></returns>
        private bool CheckDeviceSpecs(uint id)
        {
            bool buttonOK = joystick.GetVJDButtonNumber(id) >= 11;
            bool discPovOK = joystick.GetVJDDiscPovNumber(id) >= 1;
            bool axisXOK = joystick.GetVJDAxisExist(id, HID_USAGES.HID_USAGE_X);
            bool axisYOK = joystick.GetVJDAxisExist(id, HID_USAGES.HID_USAGE_Y);
            bool axisZOK = joystick.GetVJDAxisExist(id, HID_USAGES.HID_USAGE_Z);
            bool axisXrotOK = joystick.GetVJDAxisExist(id, HID_USAGES.HID_USAGE_RX);
            bool axisYrotOK = joystick.GetVJDAxisExist(id, HID_USAGES.HID_USAGE_RY);
            bool axisZrotOK = joystick.GetVJDAxisExist(id, HID_USAGES.HID_USAGE_RZ);
            bool result = buttonOK && discPovOK &&
                          axisXOK && axisYOK && axisZOK &&
                          axisXrotOK && axisYrotOK && axisZrotOK;
            return result;
        }

        /// <summary>
        /// emulate pressing buttons on controller
        /// </summary>
        /// <param name="button"></param>
        public void TriggerControl(ControlButton button)
        {
            new Thread(() =>
            {
                lock (joyReportLock)
                {
                    joyReport.Buttons |= (uint)(0x1 << ((int)button - 1));
                }
                Thread.Sleep(triggerInterval);
                lock (joyReportLock)
                {
                    joyReport.Buttons &= ~(uint)(0x1 << ((int)button - 1));
                }
            }).Start();
        }

        /// <summary>
        /// emulate moving sticks on controller
        /// </summary>
        /// <param name="axis"></param>
        public void TriggerControl(ControlAxis axis)
        {
            new Thread(() =>
            {
                lock (joyReportLock)
                {
                    switch (axis)
                    {
                        case ControlAxis.POVUp:
                            joyReport.bHats = GetDiscPov(0);
                            break;
                        case ControlAxis.POVRight:
                            joyReport.bHats = GetDiscPov(1);
                            break;
                        case ControlAxis.POVDown:
                            joyReport.bHats = GetDiscPov(2);
                            break;
                        case ControlAxis.POVLeft:
                            joyReport.bHats = GetDiscPov(3);
                            break;
                        case ControlAxis.X:
                            joyReport.AxisX = 0;
                            break;
                        case ControlAxis.XRot:
                            joyReport.AxisXRot = 0;
                            break;
                        case ControlAxis.Y:
                            joyReport.AxisY = 0;
                            break;
                        case ControlAxis.YRot:
                            joyReport.AxisYRot = 0;
                            break;
                        case ControlAxis.Z:
                            joyReport.AxisZ = (int)(axisMax / 2);
                            break;
                        case ControlAxis.ZRot:
                            joyReport.AxisZRot = (int)(axisMax / 2);
                            break;
                    }
                }
                Thread.Sleep(triggerInterval);
                lock (joyReportLock)
                {
                    switch (axis)
                    {
                        case ControlAxis.POVUp:
                        case ControlAxis.POVRight:
                        case ControlAxis.POVDown:
                        case ControlAxis.POVLeft:
                            joyReport.bHats = 0xFFFFFFFF;
                            break;
                        case ControlAxis.X:
                            joyReport.AxisX = (int)(axisMax / 2);
                            break;
                        case ControlAxis.XRot:
                            joyReport.AxisXRot = (int)(axisMax / 2);
                            break;
                        case ControlAxis.Y:
                            joyReport.AxisY = (int)(axisMax / 2);
                            break;
                        case ControlAxis.YRot:
                            joyReport.AxisYRot = (int)(axisMax / 2);
                            break;
                        case ControlAxis.Z:
                            joyReport.AxisZ = 0;
                            break;
                        case ControlAxis.ZRot:
                            joyReport.AxisZRot = 0;
                            break;
                    }
                }
            }).Start();
        }

        /// <summary>
        /// get discontinuous POV data based on index
        /// </summary>
        /// <param name="idx"></param>
        /// <returns></returns>
        private uint GetDiscPov(int idx)
        {
            byte b1 = (byte)((idx + 0) % 4);
            byte b2 = (byte)((idx + 1) % 4);
            byte b3 = (byte)((idx + 2) % 4);
            byte b4 = (byte)((idx + 3) % 4);
            return (uint)(b4 << 12) | (uint)(b3 << 8) | (uint)(b2 << 4) | (uint)b1;
        }

        /// <summary>
        /// add log message to main window
        /// </summary>
        /// <param name="message"></param>
        private void AddLog(string message)
        {
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                mainWindow.AddLogMessage("[Controller]\n" + message + "\n");
            }));
        }

        // linear filter
        public static float FilterLinear(float val, float edge0, float edge1)
        {
            float x = (val - edge0) / (edge1 - edge0);
            x = x < 0.0f ? 0.0f : x;
            x = x > 1.0f ? 1.0f : x;
            return x;
        }

        /// smoothstep filter
        /// Reference: https://en.wikipedia.org/wiki/Smoothstep
        public static float FilterSmoothStep(float val, float edge0, float edge1)
        {
            float x = (val - edge0) / (edge1 - edge0);
            x = x < 0.0f ? 0.0f : x;
            x = x > 1.0f ? 1.0f : x;
            return x * x * (3.0f - (2.0f * x));
        }
    }
}
