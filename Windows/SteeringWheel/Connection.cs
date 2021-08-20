using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    /// Connection service
    /// </summary>
    class Connection
    {
        private readonly MainWindow mainWindow;
        private readonly SharedBuffer sharedBuffer;
        private ConnectionMode mode;

        public Connection(MainWindow window, SharedBuffer buffer)
        {
            mainWindow = window;
            sharedBuffer = buffer;
            mode = ConnectionMode.Bluetooth;
        }
    }
}
