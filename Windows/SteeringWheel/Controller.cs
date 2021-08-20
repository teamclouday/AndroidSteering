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
        private readonly int MAX_WAIT_TIME = 1500;
        private bool isProcessAllowed = false;
        private readonly float CAP_Steering = 60.0f; //treat max angle as 60 even though it can reach 90
        private readonly float CAP_AccForward = 80.0f;
        private readonly float CAP_AccBackward = 10.0f;

        // vjoy related
        private readonly vJoy joystick;
        private uint joystickID;
        private long axisMax = 0;
        public bool vJoyInitialized { get; private set; }
        private const int triggerInterval = 50;

        public Controller(MainWindow window, SharedBuffer buffer)
        {
            mainWindow = window;
            sharedBuffer = buffer;

            vJoyInitialized = false;
            joystick = new vJoy();
            if(joystick.vJoyEnabled()) SetupVJoy();

            SetupProcess();
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
            joystick.RelinquishVJD(joystickID);
        }

        /// <summary>
        /// setup background process that keeps running
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
            processThread.Start();
        }

        /// <summary>
        /// process acceleration based on input value
        /// </summary>
        /// <param name="val"></param>
        private void ProcessAcceleration(float val)
        {
            if(-40.0f <= val && val <= -30.0f)
            {
                joystick.SetAxis(0, joystickID, HID_USAGES.HID_USAGE_Z);
                joystick.SetAxis(0, joystickID, HID_USAGES.HID_USAGE_RZ);
            }
            else if(-90.0f <= val && val < -40.0f)
            {
                // forward
                float step = SmoothStep(40.0f, CAP_AccForward, -val);
                val = axisMax * step;
                joystick.SetAxis((int)val, joystickID, HID_USAGES.HID_USAGE_RZ);
            }
            else if(-30.0f < val && val <= 90.0f)
            {
                // backward
                float step = SmoothStep(-30.0f, CAP_AccBackward, val);
                val = axisMax * step;
                joystick.SetAxis((int)val, joystickID, HID_USAGES.HID_USAGE_Z);
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
                joystick.SetAxis((int)(axisMax / 2), joystickID, HID_USAGES.HID_USAGE_X);
            }
            else if(2.0f < val && val <= 90.0f)
            {
                // turning left
                float step = SmoothStep(2.0f, CAP_Steering, val);
                float half = axisMax / 2.0f * step;
                joystick.SetAxis((int)(axisMax / 2.0f - half), joystickID, HID_USAGES.HID_USAGE_X);
            }
            else if(-90.0f <= val && val < -2.0f)
            {
                // turning right
                float step = SmoothStep(2.0f, CAP_Steering, -val);
                float half = axisMax / 2.0f * step;
                joystick.SetAxis((int)(axisMax / 2.0f + half), joystickID, HID_USAGES.HID_USAGE_X);
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
                lock (this)
                {
                    joystick.SetBtn(true, joystickID, (uint)button);
                    Thread.Sleep(triggerInterval);
                    joystick.SetBtn(false, joystickID, (uint)button);
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
                lock (this)
                {
                    switch (axis)
                    {
                        case ControlAxis.POVUp:
                            joystick.SetDiscPov(0, joystickID, 1);
                            Thread.Sleep(triggerInterval);
                            break;
                        case ControlAxis.POVRight:
                            joystick.SetDiscPov(1, joystickID, 1);
                            Thread.Sleep(triggerInterval);
                            break;
                        case ControlAxis.POVDown:
                            joystick.SetDiscPov(2, joystickID, 1);
                            Thread.Sleep(triggerInterval);
                            break;
                        case ControlAxis.POVLeft:
                            joystick.SetDiscPov(3, joystickID, 1);
                            Thread.Sleep(triggerInterval);
                            break;
                        case ControlAxis.X:
                            joystick.SetAxis(0, joystickID, HID_USAGES.HID_USAGE_X);
                            Thread.Sleep(triggerInterval);
                            joystick.SetAxis((int)(axisMax / 2), joystickID, HID_USAGES.HID_USAGE_X);
                            break;
                        case ControlAxis.XRot:
                            joystick.SetAxis(0, joystickID, HID_USAGES.HID_USAGE_RX);
                            Thread.Sleep(triggerInterval);
                            joystick.SetAxis((int)(axisMax / 2), joystickID, HID_USAGES.HID_USAGE_RX);
                            break;
                        case ControlAxis.Y:
                            joystick.SetAxis(0, joystickID, HID_USAGES.HID_USAGE_Y);
                            Thread.Sleep(triggerInterval);
                            joystick.SetAxis((int)(axisMax / 2), joystickID, HID_USAGES.HID_USAGE_Y);
                            break;
                        case ControlAxis.YRot:
                            joystick.SetAxis(0, joystickID, HID_USAGES.HID_USAGE_RY);
                            Thread.Sleep(triggerInterval);
                            joystick.SetAxis((int)(axisMax / 2), joystickID, HID_USAGES.HID_USAGE_RY);
                            break;
                        case ControlAxis.Z:
                            joystick.SetAxis(0, joystickID, HID_USAGES.HID_USAGE_Z);
                            Thread.Sleep(triggerInterval);
                            joystick.SetAxis((int)(axisMax / 2), joystickID, HID_USAGES.HID_USAGE_Z); // Need to be half here
                            Thread.Sleep(triggerInterval);
                            joystick.SetAxis(0, joystickID, HID_USAGES.HID_USAGE_Z);
                            break;
                        case ControlAxis.ZRot:
                            joystick.SetAxis(0, joystickID, HID_USAGES.HID_USAGE_RZ);
                            Thread.Sleep(triggerInterval);
                            joystick.SetAxis((int)(axisMax / 2), joystickID, HID_USAGES.HID_USAGE_RZ); // Need to be half here
                            Thread.Sleep(triggerInterval);
                            joystick.SetAxis(0, joystickID, HID_USAGES.HID_USAGE_RZ);
                            break;
                    }
                    joystick.ResetPovs(joystickID);
                }
            }).Start();
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

        /// <summary>
        /// compute smoothstep
        /// Reference: https://en.wikipedia.org/wiki/Smoothstep
        /// </summary>
        /// <param name="edge0"></param>
        /// <param name="edge1"></param>
        /// <param name="val"></param>
        public static float SmoothStep(float edge0, float edge1, float val)
        {
            float x = (val - edge0) / (edge1 - edge0);
            x = x < 0.0f ? 0.0f : x;
            x = x > 1.0f ? 1.0f : x;
            return x * x * (3.0f - (2.0f * x));
        }
    }
}
