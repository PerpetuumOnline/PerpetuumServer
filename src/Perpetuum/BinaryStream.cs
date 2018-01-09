using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;

namespace Perpetuum
{
    public class BinaryStream : MemoryStream
    {
        private readonly BinaryReader _reader;
        private readonly BinaryWriter _writer;

        public BinaryStream()
        {
            _reader = new BinaryReader(this);
            _writer = new BinaryWriter(this);
        }

        public BinaryStream(byte[] bytes) : base(bytes)
        {
            _reader = new BinaryReader(this);
            _writer = new BinaryWriter(this);
        }

        public int ReadInt()
        {
            return _reader.ReadInt32();
        }

        public long ReadLong()
        {
            return _reader.ReadInt64();
        }

        public unsafe Guid ReadGuid()
        {
            var b = _reader.ReadBytes(16);

            fixed (byte* pB = b)
            {
                return *(Guid*)pB;
            }
        }

        public byte[] ReadBytes(int count)
        {
            return _reader.ReadBytes(count);
        }

        public string ReadUtf8String()
        {
            var length = ReadInt();

            if (length == 0)
                return string.Empty;
            
            var data = ReadBytes(length);
            return Encoding.UTF8.GetString(data);
        }

        public string ReadString()
        {
            return _reader.ReadString();
        }

        public ushort ReadUShort()
        {
            return _reader.ReadUInt16();
        }

        public Position ReadPosition()
        {
            var x = ReadInt()/256.0;
            var y = ReadInt() / 256.0;
            var z = ReadInt() / 256.0;
            return new Position(x,y,z);
        }

        public void AppendObject(object o)
        {
            if ( o is byte )
            {
                AppendByte((byte) o);
                return;
            }

            if ( o is int )
            {
                AppendInt((int) o);
                return;
            }

            if ( o is long )
            {
                AppendLong((long) o);
                return;
            }

            if (o is Color)
            {
                var color = (Color)o;
                AppendByte(color.R);
                AppendByte(color.G);
                AppendByte(color.B);
                return;
            }

            if (o is Guid)
            {
                AppendGuid((Guid) o);
                return;
            }

            Debug.Assert(false,"unknown object type = " + o.GetType());
        }

        public void AppendUInt64Array(uint[] values)
        {
            for (var i = 0; i < values.Length; i++)
            {
                _writer.Write(values[i]);
            }
        }

        public void AppendByteArray(byte[] values)
        {
            _writer.Write(values);
        }

        public void AppendByte(byte value)
        {
            _writer.Write(value);
        }

        public void AppendUShort(ushort value)
        {
            _writer.Write(value);
        }

        public void AppendInt(int value)
        {
            _writer.Write(value);
        }

        public void AppendLong(long value)
        {
            _writer.Write(value);
        }

        public void AppendDouble(double value)
        {
            _writer.Write((long) value.ToFixedFloat());
        }

        public void AppendBool(bool value)
        {
            _writer.Write(value);
        }

        public void AppendStream(BinaryStream stream)
        {
            if ( stream == null )
                return;

            AppendByteArray(stream.ToArray());
        }

        public void AppendPoint(Point p)
        {
            AppendInt(p.X);
            AppendInt(p.Y);
        }

        public void AppendPosition(Position p)
        {
            AppendInt((int) (p.X*256.0));
            AppendInt((int) (p.Y*256.0));
            AppendInt((int) (p.Z*256.0));
        }

        public void AppendArea(Area area)
        {
            AppendInt(area.X1);
            AppendInt(area.Y1);
            AppendInt(area.X2);
            AppendInt(area.Y2);
        }

        public unsafe void AppendByteArray(byte* pSrc, int count)
        {
            for (var i = 0; i < count; i++)
            {
                AppendByte(*(pSrc + i));    
            }
        }

        public void AppendUtf8String(string str)
        {
            var data = Encoding.UTF8.GetBytes(str);
            AppendInt(data.Length);
            AppendByteArray(data);
        }

        public void AppendGuid(Guid guid)
        {
            _writer.Write(guid.ToByteArray());
        }

        private T Peek<T>(int offset,Func<T> peeker)
        {
            var origPos = Position;
            Position = offset;
            var result = peeker();
            Position = origPos;
            return result;
        }

        protected byte PeekByte(int offset)
        {
            return Peek(offset, () => (byte)ReadByte());
        }

        public int PeekInt(int offset)
        {
            var origPos = Position;
            Position = offset;
            var result = ReadInt();
            Position = origPos;
            return result;
        }

        public long PeekLong(int offset)
        {
            var origPos = Position;
            Position = offset;
            var result = ReadLong();
            Position = origPos;
            return result;
        }

        public void PutInt(int offset, int value)
        {
            var origPos = Position;
            Position = offset;
            _writer.Write(value);
            Position = origPos;
        }

        public void PutLong(int offset, long value)
        {
            var origPos = Position;
            Position = offset;
            _writer.Write(value);
            Position = origPos;
        }

        public void Skip(int count)
        {
            Position += count;
        }

        public bool AtEnd()
        {
            return Position >= Length;
        }

        public byte[] ReadToEnd()
        {
            Debug.Assert(Position <= Length);
            return ReadBytes((int) (Length - Position));
        }
    }
}
