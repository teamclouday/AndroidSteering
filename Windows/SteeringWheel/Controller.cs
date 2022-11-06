using System;
using System.Windows;
using System.Threading;
using System.Threading.Tasks;
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
    /// define motion status
    /// </summary>
    public enum MotionStatus
    {
        SetSteerAngle = 0,
        SetAccAngle = 1,
        ResetSteerAngle = 2,
        ResetAccAngle = 3,
        SetAccRatio = 4,
        SetLeftStickX = 5,
        SetLeftStickY = 6,
        SetRightStickX = 7,
        SetRightStickY = 8,
        SetLTValue = 9,
        SetRTValue = 10,
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
        START = 11,
        HOME = 12,
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
    public class Controller
    {
        private readonly MainWindow mainWindow;
        private readonly SharedBuffer sharedBuffer;
        private Task processTask;
        private Task updateTask;
        private readonly CancellationTokenSource cancellationToken = new CancellationTokenSource();
        private bool isProcessAllowed;

        public float CAP_SteeringMin { get; set; } = -60.0f;
        public float CAP_SteeringMax { get; set; } = 60.0f;
        public float CAP_AccMin { get; set; } = -30.0f;
        public float CAP_AccMax { get; set; } = 90.0f;
        public float CAP_AccRestMin { get; set; } = 30.0f;
        public float CAP_AccRestMax { get; set; } = 40.0f;

        // vjoy related
        private readonly vJoy joystick;
        private vJoy.JoystickState joyReport;
        private readonly object joyReportLock = new object();
        private uint joystickID;
        private int axisMax = 0, axisMaxHalf = 0;
        public bool vJoyInitialized { get; private set; }
        private const int triggerInterval = 100;
        private const int updateInterval = 50;

        public Controller(MainWindow window, SharedBuffer buffer)
        {
            mainWindow = window;
            sharedBuffer = buffer;

            isProcessAllowed = true;
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
            if (processTask?.IsCompleted == false || updateTask?.IsCompleted == false)
            {
                cancellationToken.Cancel();
                try
                {
                    processTask?.Wait();
                    updateTask?.Wait();
                }
                catch (OperationCanceledException err)
                {
                    Debug.WriteLine("[Controller] Destroy -> " + err.Message);
                }
                finally
                {
                    processTask?.Dispose();
                    updateTask?.Dispose();
                }
            }
            ResetVJoy();
            joystick.RelinquishVJD(joystickID);
        }

        /// <summary>
        /// setup background task that write joystate
        /// </summary>
        private void SetupProcess()
        {
            isProcessAllowed = true;
            processTask = Task.Factory.StartNew(async () =>
            {
                while (isProcessAllowed)
                {
                    if (cancellationToken.IsCancellationRequested) break;
                    var data = sharedBuffer.GetData();
                    if (data == null)
                    {
                        await Task.Delay(5);
                        continue;
                    }
                    if (data.IsButton)
                    {
                        Debug.WriteLine("[Controller] processTask button pressed (" + data.Status + ")");
                        switch ((MotionButton)data.Status)
                        {
                            case MotionButton.A:
                                if (data.Value > 0.0f) ButtonDown(ControlButton.A);
                                else ButtonUp(ControlButton.A);
                                break;
                            case MotionButton.B:
                                if (data.Value > 0.0f) ButtonDown(ControlButton.B);
                                else ButtonUp(ControlButton.B);
                                break;
                            case MotionButton.X:
                                if (data.Value > 0.0f) ButtonDown(ControlButton.X);
                                else ButtonUp(ControlButton.X);
                                break;
                            case MotionButton.Y:
                                if (data.Value > 0.0f) ButtonDown(ControlButton.Y);
                                else ButtonUp(ControlButton.Y);
                                break;
                            case MotionButton.LB:
                                if (data.Value > 0.0f) ButtonDown(ControlButton.LB);
                                else ButtonUp(ControlButton.LB);
                                break;
                            case MotionButton.RB:
                                if (data.Value > 0.0f) ButtonDown(ControlButton.RB);
                                else ButtonUp(ControlButton.RB);
                                break;
                            case MotionButton.START:
                                if (data.Value > 0.0f) ButtonDown(ControlButton.START);
                                else ButtonUp(ControlButton.START);
                                break;
                            case MotionButton.BACK:
                                if (data.Value > 0.0f) ButtonDown(ControlButton.BACK);
                                else ButtonUp(ControlButton.BACK);
                                break;
                            case MotionButton.HOME:
                                if (data.Value > 0.0f) ButtonDown(ControlButton.HOME);
                                else ButtonUp(ControlButton.HOME);
                                break;
                            case MotionButton.UP:
                                if (data.Value > 0.0f) TriggerControlEnter(ControlAxis.POVUp);
                                else TriggerControlLeave(ControlAxis.POVUp);
                                break;
                            case MotionButton.DOWN:
                                if (data.Value > 0.0f) TriggerControlEnter(ControlAxis.POVDown);
                                else TriggerControlLeave(ControlAxis.POVDown);
                                break;
                            case MotionButton.LEFT:
                                if (data.Value > 0.0f) TriggerControlEnter(ControlAxis.POVLeft);
                                else TriggerControlLeave(ControlAxis.POVLeft);
                                break;
                            case MotionButton.RIGHT:
                                if (data.Value > 0.0f) TriggerControlEnter(ControlAxis.POVRight);
                                else TriggerControlLeave(ControlAxis.POVRight);
                                break;
                        }
                    }
                    else
                    {
                        switch ((MotionStatus)data.Status)
                        {
                            case MotionStatus.SetSteerAngle:
                                ProcessSteering(data.Value);
                                break;
                            case MotionStatus.SetAccAngle:
                                ProcessAcceleration(data.Value);
                                break;
                            case MotionStatus.ResetSteerAngle:
                                ProcessSteering((CAP_SteeringMax + CAP_SteeringMin) * 0.5f);
                                break;
                            case MotionStatus.ResetAccAngle:
                                ProcessAcceleration((CAP_AccRestMax + CAP_AccRestMin) * 0.5f);
                                break;
                            case MotionStatus.SetAccRatio:
                                if (data.Value > 0.0f)
                                {
                                    lock (joyReportLock)
                                    {
                                        joyReport.AxisZ = 0;
                                        joyReport.AxisZRot = (int)(data.Value * axisMax);
                                    }
                                }
                                else
                                {
                                    lock (joyReportLock)
                                    {
                                        joyReport.AxisZ = (int)(-data.Value * axisMax);
                                        joyReport.AxisZRot = 0;
                                    }
                                }
                                break;
                            case MotionStatus.SetLeftStickX:
                                {
                                    float val = axisMaxHalf * data.Value + axisMaxHalf;
                                    lock (joyReportLock)
                                    {
                                        joyReport.AxisX = (int)val;
                                    }
                                    break;
                                }
                            case MotionStatus.SetLeftStickY:
                                {
                                    float val = -axisMaxHalf * data.Value + axisMaxHalf;
                                    lock (joyReportLock)
                                    {
                                        joyReport.AxisY = (int)val;
                                    }
                                    break;
                                }
                            case MotionStatus.SetRightStickX:
                                {
                                    float val = axisMaxHalf * data.Value + axisMaxHalf;
                                    lock (joyReportLock)
                                    {
                                        joyReport.AxisXRot = (int)val;
                                    }
                                    break;
                                }
                            case MotionStatus.SetRightStickY:
                                {
                                    float val = -axisMaxHalf * data.Value + axisMaxHalf;
                                    lock (joyReportLock)
                                    {
                                        joyReport.AxisYRot = (int)val;
                                    }
                                    break;
                                }
                            case MotionStatus.SetLTValue:
                                {
                                    lock (joyReportLock)
                                    {
                                        joyReport.AxisZ = (int)(data.Value * axisMax);
                                    }
                                    break;
                                }
                            case MotionStatus.SetRTValue:
                                {
                                    lock (joyReportLock)
                                    {
                                        joyReport.AxisZRot = (int)(data.Value * axisMax);
                                    }
                                    break;
                                }
                        }
                    }
                }
            }, cancellationToken.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        /// <summary>
        /// set up background task that updates vJoy state
        /// </summary>
        private void SetupUpdate()
        {
            isProcessAllowed = true;
            updateTask = Task.Factory.StartNew(async () =>
            {
                while (isProcessAllowed)
                {
                    lock (joyReportLock)
                    {
                        if (!joystick.UpdateVJD(joystickID, ref joyReport))
                        {
                            // AddLog("Failed to update vJoy controller state");
                            Debug.WriteLine("[Controller] updateTask failed to update VJD");
                            joystick.RelinquishVJD(joystickID);
                            joystick.AcquireVJD(joystickID);
                        }
                    }
                    await Task.Delay(updateInterval);
                }
            }, cancellationToken.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        /// <summary>
        /// process acceleration based on input value
        /// </summary>
        /// <param name="val"></param>
        private void ProcessAcceleration(float val)
        {
            if (val >= CAP_AccRestMin && val <= CAP_AccRestMax)
            {
                lock (joyReportLock)
                {
                    joyReport.AxisZ = 0;
                    joyReport.AxisZRot = 0;
                }
            }
            else if (val > CAP_AccRestMax)
            {
                // forward
                float step = FilterLinear(val, CAP_AccRestMax, CAP_AccMax);
                val = axisMax * step;
                lock (joyReportLock)
                {
                    joyReport.AxisZ = 0;
                    joyReport.AxisZRot = (int)val;
                }
            }
            else // val < CAP_AccRestMin
            {
                // backward
                float step = FilterLinear(-val, CAP_AccMin, CAP_AccRestMin);
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
            float step = FilterLinear(-val, CAP_SteeringMin, CAP_SteeringMax);
            val = axisMax * step;
            lock (joyReportLock)
            {
                joyReport.AxisX = (int)val;
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
                        long val = 0L;
                        joystick.GetVJDAxisMax(joystickID, HID_USAGES.HID_USAGE_X, ref val);
                        axisMax = (int)val;
                        axisMaxHalf = axisMax / 2;
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
        public void ResetVJoy()
        {
            joystick.ResetAll();
            joyReport.bHats = 0xFFFFFFFF;
            joyReport.AxisX = axisMaxHalf;
            joyReport.AxisXRot = axisMaxHalf;
            joyReport.AxisY = axisMaxHalf;
            joyReport.AxisYRot = axisMaxHalf;
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
            Task.Run(async () =>
            {
                ButtonDown(button);
                await Task.Delay(triggerInterval);
                ButtonUp(button);
            });
        }

        public void ButtonDown(ControlButton button)
        {
            lock (joyReportLock)
            {
                joyReport.Buttons |= (uint)(0x1 << ((int)button - 1));
            }
        }

        public void ButtonUp(ControlButton button)
        {
            lock (joyReportLock)
            {
                joyReport.Buttons &= ~(uint)(0x1 << ((int)button - 1));
            }
        }

        /// <summary>
        /// emulate moving sticks on controller
        /// </summary>
        /// <param name="axis"></param>
        public void TriggerControl(ControlAxis axis)
        {
            Task.Run(async () =>
            {
                TriggerControlEnter(axis);
                await Task.Delay(triggerInterval);
                TriggerControlLeave(axis);
            });
        }

        public void TriggerControlEnter(ControlAxis axis)
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
                        joyReport.AxisZ = axisMaxHalf;
                        break;
                    case ControlAxis.ZRot:
                        joyReport.AxisZRot = axisMaxHalf;
                        break;
                }
            }
        }

        public void TriggerControlLeave(ControlAxis axis)
        {
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
                        joyReport.AxisX = axisMaxHalf;
                        break;
                    case ControlAxis.XRot:
                        joyReport.AxisXRot = axisMaxHalf;
                        break;
                    case ControlAxis.Y:
                        joyReport.AxisY = axisMaxHalf;
                        break;
                    case ControlAxis.YRot:
                        joyReport.AxisYRot = axisMaxHalf;
                        break;
                    case ControlAxis.Z:
                        joyReport.AxisZ = 0;
                        break;
                    case ControlAxis.ZRot:
                        joyReport.AxisZRot = 0;
                        break;
                }
            }
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
