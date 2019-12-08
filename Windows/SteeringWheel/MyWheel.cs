using System;
using System.Collections.Generic;
using vJoyInterfaceWrap;

// Idea comes from https://github.com/Bemoliph/Virtual-DualShock/blob/master/Gamepad.cs

namespace SteeringWheel
{
    enum MotionSteering
    {
        LEFT,
        NONE,
        RIGHT
    }

    enum MotionAcceleration
    {
        FORWARD,
        NONE,
        BACKWARD
    }

    /// <summary>
    /// define each move
    /// </summary>
    public class MyMove
    {
        public int motionType;
        public int motionStatus;
        public int data;
        public MyMove(int type, int status, int d)
        {
            motionType = type;
            motionStatus = status;
            data = d;
        }
    }

    /// <summary>
    /// global buffer for transfering data
    /// </summary>
    public class GlobBuffer
    {
        private Queue<MyMove> buffer = new Queue<MyMove>();
        private const int MAX_SIZE = 50;
        public void AddData(int type, int status, int d)
        {
            lock(this)
            {
                if (buffer.Count >= MAX_SIZE) return;
                buffer.Enqueue(new MyMove(type, status, d));
            }
        }
        public MyMove GetData()
        {
            lock(this)
            {
                if (buffer.Count <= 0) return null;
                return buffer.Dequeue();
            }
        }
        public void Reset()
        {
            lock(this)
            {
                buffer.Clear();
            }
        }
    }

    enum ControlButton
    {
        A       = 1,
        B       = 2,
        X       = 3,
        Y       = 4,
        LB      = 5,
        RB      = 6,
        BACK    = 7,
        START   = 8,
        LS      = 9,
        RS      = 10
    }

    //enum ControlAxis
    //{
    //    EMPTY       = 0,
    //    UP          = 1,
    //    DOWN        = 2,
    //    LEFT        = 3,
    //    RIGHT       = 4
    //}

    class MyWheel
    {
        public bool okForRunning = true;
        private vJoy joystick;
        // private vJoy.JoystickState report;
        private uint deviceID;
        private long axisMax = 0;

        // set range of input values
        private readonly int accForwardRange    = 80 - 25;
        private readonly int accBackwardRange   = 40 - 5;
        private readonly int steerLeftRange     = 50 - 10;
        private readonly int steerRightRange    = 50 - 10;

        public MyWheel()
        {
            joystick = new vJoy();
            // report = new vJoy.JoystickState();
            if(!FindTargetDevice())
            {
                Console.WriteLine("Cannot find a free device for use");
                okForRunning = false;
            }
        }

        /// <summary>
        /// start by reseting all values
        /// </summary>
        public void Start()
        {
            joystick.ResetAll();
        }

        /// <summary>
        /// resume the wheel, expected to be called by a different thread
        /// </summary>
        public void Resume()
        {
            lock(this)
            {
                okForRunning = true;
                joystick.ResetAll();
                ProcessData();
            }
        }

        /// <summary>
        /// pause the wheel, expected to be called by a different thread
        /// </summary>
        public void Pause()
        {
            lock(this)
            {
                okForRunning = false;
            }
        }

        /// <summary>
        /// completely stop the wheel
        /// </summary>
        public void Stop()
        {
            joystick.ResetAll();
            joystick.RelinquishVJD(deviceID);
        }

        /// <summary>
        /// loop through possible deivce id and find a free one
        /// </summary>
        /// <returns></returns>
        private bool FindTargetDevice()
        {
            for(uint i = 1; i <= 16; i++)
            {
                VjdStat status = joystick.GetVJDStatus(i);
                if(status == VjdStat.VJD_STAT_FREE && joystick.AcquireVJD(i))
                {
                    if(CheckDeviceSpecs(i))
                    {
                        deviceID = i;
                        joystick.GetVJDAxisMax(deviceID, HID_USAGES.HID_USAGE_X, ref axisMax);
                        Console.WriteLine(String.Format("vJoy initialized\nAxis Max: {0}", axisMax));
                        return true;
                    }
                    else
                    {
                        joystick.RelinquishVJD(i);
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// check necessary components according to xbox controller
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private bool CheckDeviceSpecs(uint id)
        {
            bool buttonOK = joystick.GetVJDButtonNumber(id) >= 10;
            bool contPovOK = joystick.GetVJDContPovNumber(id) >= 1;
            bool axisXOK = joystick.GetVJDAxisExist(id, HID_USAGES.HID_USAGE_X);
            bool axisYOK = joystick.GetVJDAxisExist(id, HID_USAGES.HID_USAGE_Y);
            bool axisZOK = joystick.GetVJDAxisExist(id, HID_USAGES.HID_USAGE_Z);
            bool axisXrotOK = joystick.GetVJDAxisExist(id, HID_USAGES.HID_USAGE_RX);
            bool axisYrotOK = joystick.GetVJDAxisExist(id, HID_USAGES.HID_USAGE_RY);
            bool axisZrotOK = joystick.GetVJDAxisExist(id, HID_USAGES.HID_USAGE_RZ);
            bool result = buttonOK && contPovOK &&
                          axisXOK && axisYOK && axisZOK &&
                          axisXrotOK && axisYrotOK && axisZrotOK;
            return result;
        }

        /// <summary>
        /// read buffer data, and set button calls by configuration
        /// </summary>
        public void ProcessData()
        {
            MyMove nextMove = Program.globBuffer.GetData();
            if (nextMove == null) return;
            int newAxisVal = ConvertAxis(nextMove.data, nextMove.motionType, nextMove.motionStatus);
            // by default, set x axis for steering
            // z axis for LT, z rot for RT
            // may create a configurable interface in the future
            if(nextMove.motionType == 1)
            {
                switch(nextMove.motionStatus)
                {
                    case 0:
                        // forward
                        // Console.WriteLine(String.Format("Forward: {0}", nextMove.data));
                        joystick.SetAxis(newAxisVal, deviceID, HID_USAGES.HID_USAGE_Z);
                        break;
                    case 2:
                        // backward
                        // Console.WriteLine(String.Format("Backward: {0}", nextMove.data));
                        joystick.SetAxis(newAxisVal, deviceID, HID_USAGES.HID_USAGE_RZ);
                        break;
                    default:
                        // reset
                        // Console.WriteLine("ACC NONE");
                        joystick.SetAxis(0, deviceID, HID_USAGES.HID_USAGE_Z);
                        joystick.SetAxis(0, deviceID, HID_USAGES.HID_USAGE_RZ);
                        break;
                }
            }
            else
            {
                switch(nextMove.motionStatus)
                {
                    case 0:
                        // left
                        // Console.WriteLine(String.Format("Left: {0}", nextMove.data));
                        joystick.SetAxis(newAxisVal, deviceID, HID_USAGES.HID_USAGE_X);
                        break;
                    case 2:
                        // right
                        // Console.WriteLine(String.Format("Right: {0}", nextMove.data));
                        joystick.SetAxis(newAxisVal, deviceID, HID_USAGES.HID_USAGE_X);
                        break;
                    default:
                        // Console.WriteLine("STEER NONE");
                        joystick.SetAxis((int)(axisMax / 2), deviceID, HID_USAGES.HID_USAGE_X);
                        break;
                }
            }
        }

        /// <summary>
        /// convert input (expected to be angle values (-180 ~ 180)) to axis values
        /// </summary>
        /// <param name="inputAngle"></param>
        /// <returns></returns>
        private int ConvertAxis(int inputAngle, int type, int status)
        {
            float val = (float)Math.Abs(inputAngle);
            if(type == 1)
            {
                // acceleration
                switch(status)
                {
                    case 0:
                        // forward
                        return (int)((val - 25.0) / accForwardRange * axisMax);
                    case 2:
                        // backward
                        val = Math.Min(val, 40);
                        return (int)((val - 5.0) / accBackwardRange * axisMax);
                    default:
                        // none
                        return 0;
                }
            }
            else
            {
                // steering
                switch(status)
                {
                    case 0:
                        // left
                        val = Math.Min(val, 50);
                        return (int)((1.0 - ((val - 10.0) / steerLeftRange)) * (axisMax / 2));
                    case 2:
                        // right
                        val = Math.Min(val, 50);
                        return (int)((1.0 + ((val - 10.0) / steerRightRange)) * (axisMax / 2));
                    default:
                        // none
                        return 0;
                }
            }
        }
    }
}
