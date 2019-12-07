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
    }

    enum ControlButton
    {
        X       = 1,
        Y       = 2,
        A       = 3,
        B       = 4,
        Select  = 5,
        Start   = 6,
        L1      = 7,
        R1      = 8,
        L2      = 9,
        R2      = 10,
        L3      = 11,
        R3      = 12
    }

    enum ControlAxis
    {
        EMPTY       = 0,
        UP          = 1,
        DOWN        = 2,
        LEFT        = 3,
        RIGHT       = 4
    }

    class MyWheel
    {
        public bool okForRunning = true;
        private vJoy joystick;
        private vJoy.JoystickState report;
        private uint deviceID;
        private int axisMax = 0;
        private int numBtns = 0;
        private int numContPov = 0;
        private int numDiscPov = 0;

        public MyWheel()
        {
            joystick = new vJoy();
            report = new vJoy.JoystickState();
            if(!FindTargetDevice())
            {
                Console.WriteLine("Cannot find a free device for use");
                okForRunning = false;
            }
            LoadDeviceSpecs();
        }

        public void Resume()
        {
            lock(this)
            {
                okForRunning = true;
            }
        }

        public void Pause()
        {
            lock(this)
            {
                okForRunning = false;
            }
        }

        public void Stop()
        {
            joystick.RelinquishVJD(deviceID);
        }

        private bool FindTargetDevice()
        {
            for(uint i = 1; i <= 16; i++)
            {
                VjdStat status = joystick.GetVJDStatus(i);
                if(status == VjdStat.VJD_STAT_FREE && joystick.AcquireVJD(i))
                {
                    deviceID = i;
                    return true;
                }
            }
            return false;
        }

        private void LoadDeviceSpecs()
        {
            if (!okForRunning) return;
            long max = 0;
            joystick.GetVJDAxisMax(deviceID, HID_USAGES.HID_USAGE_X, ref max);
            axisMax = (int)(max / 2) - 1;
            numBtns = joystick.GetVJDButtonNumber(deviceID);
            numContPov = joystick.GetVJDContPovNumber(deviceID);
            numDiscPov = joystick.GetVJDDiscPovNumber(deviceID);
        }

        public void ReadData()
        {

        }
    }
}
