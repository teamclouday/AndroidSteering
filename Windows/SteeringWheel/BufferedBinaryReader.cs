using System;
using System.IO;

namespace SteeringWheel
{
    // adapted from https://www.jacksondunstan.com/articles/3568
    public class BufferedBinaryReader : IDisposable
    {
        private readonly Stream stream;
        private readonly byte[] buffer;
        private readonly int bufferSize;
        private int bufferOffset;
        private int numBufferedBytes;

        public BufferedBinaryReader(Stream stream)
        {
            this.stream = stream;
            bufferSize = (int)stream.Length;
            buffer = new byte[bufferSize];
            bufferOffset = bufferSize;
            numBufferedBytes = 0;
            FillBuffer();
        }

        public BufferedBinaryReader(Stream stream, int bufferSize)
        {
            this.stream = stream;
            this.bufferSize = bufferSize;
            buffer = new byte[bufferSize];
            bufferOffset = bufferSize;
            numBufferedBytes = 0;
        }

        public int NumBytesAvailable => Math.Max(0, numBufferedBytes - bufferOffset);

        public bool FillBuffer()
        {
            if (stream == null) return false;
            var numBytesUnread = bufferSize - bufferOffset;
            var numBytesToRead = bufferSize - numBytesUnread;
            bufferOffset = 0;
            numBufferedBytes = numBytesUnread;
            if (numBytesUnread > 0)
            {
                Buffer.BlockCopy(buffer, numBytesToRead, buffer, 0, numBytesUnread);
            }
            while (numBytesToRead > 0)
            {
                var numBytesRead = stream.Read(buffer, numBytesUnread, numBytesToRead);
                if (numBytesRead == 0)
                {
                    return false;
                }
                numBufferedBytes += numBytesRead;
                numBytesToRead -= numBytesRead;
                numBytesUnread += numBytesRead;
            }
            return true;
        }

        public void SetReadPosition(int position)
        {
            bufferOffset = position;
        }

        public byte ReadByte()
        {
            return buffer[bufferOffset++];
        }

        public char ReadChar()
        {
            return BitConverter.ToChar(buffer, bufferOffset++);
        }

        public byte[] ReadBytes(int count)
        {
            byte[] val = new byte[count];
            Buffer.BlockCopy(buffer, bufferOffset, val, 0, count);
            bufferOffset += count;
            return val;
        }

        public char[] ReadChars(int count)
        {
            char[] val = new char[count];
            for (var idx = 0; idx < count; ++idx)
            {
                val[idx] = BitConverter.ToChar(buffer, bufferOffset + idx);
            }
            bufferOffset += count;
            return val;
        }

        public string ReadString()
        {
            var val = BitConverter.ToString(buffer, bufferOffset);
            bufferOffset += val.Length;
            return val;
        }

        public bool ReadBoolean()
        {
            return BitConverter.ToBoolean(buffer, bufferOffset++);
        }

        public short ReadInt16()
        {
            CheckLittleEndian(sizeof(short));
            short val = BitConverter.ToInt16(buffer, bufferOffset);
            bufferOffset += sizeof(short);
            return val;
        }

        public ushort ReadUInt16()
        {
            CheckLittleEndian(sizeof(ushort));
            ushort val = BitConverter.ToUInt16(buffer, bufferOffset);
            bufferOffset += sizeof(ushort);
            return val;
        }

        public int ReadInt32()
        {
            CheckLittleEndian(sizeof(int));
            int val = BitConverter.ToInt32(buffer, bufferOffset);
            bufferOffset += sizeof(int);
            return val;
        }

        public uint ReadUInt32()
        {
            CheckLittleEndian(sizeof(uint));
            uint val = BitConverter.ToUInt32(buffer, bufferOffset);
            bufferOffset += sizeof(uint);
            return val;
        }

        public long ReadInt64()
        {
            CheckLittleEndian(sizeof(long));
            long val = BitConverter.ToInt64(buffer, bufferOffset);
            bufferOffset += sizeof(long);
            return val;
        }

        public ulong ReadUInt64()
        {
            CheckLittleEndian(sizeof(ulong));
            ulong val = BitConverter.ToUInt64(buffer, bufferOffset);
            bufferOffset += sizeof(ulong);
            return val;
        }

        public float ReadSingle()
        {
            CheckLittleEndian(sizeof(float));
            float val = BitConverter.ToSingle(buffer, bufferOffset);
            bufferOffset += sizeof(float);
            return val;
        }

        public double ReadDouble()
        {
            CheckLittleEndian(sizeof(double));
            double val = BitConverter.ToDouble(buffer, bufferOffset);
            bufferOffset += sizeof(double);
            return val;
        }

        public void Dispose()
        {
            stream.Close();
        }

        private void CheckLittleEndian(int sizeToRead)
        {
            if (BitConverter.IsLittleEndian)
                Array.Reverse(buffer, bufferOffset, sizeToRead);
        }
    }
}
