using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Perpetuum.GenXY
{
    public class GenxyWriter : IDisposable
    {
        private static readonly string[] _hexTable;

        private readonly TextWriter _writer;

        static GenxyWriter()
        {
            _hexTable = Enumerable.Range(0, 256).Select(i => $"\\{i:X2}").ToArray();
        }

        public GenxyWriter()
        {
            _writer = new StringWriter();   
        }

        public void WriteToken(GenxyToken token)
        {
            _writer.Write((char)token);
        }

        public void WriteChar(char value)
        {
            _writer.Write(value);
        }

        public void WriteString(string value)
        {
            _writer.Write(value);
        }

        public void WriteEscapedString(string value)
        {
            for (var i = 0; i < value.Length; i++)
            {
                var c = value[i];

                if (c < 32 || c == '\\' || c == ':' || c == '#' || c == ',' || 
                    c == (int) GenxyToken.StartProp || c == (int) GenxyToken.EndProp || c == (int) GenxyToken.Prop)
                    _writer.Write(_hexTable[c]);
                else _writer.Write(c);
            }
        }

        public void WriteInteger(int value)
        {
            _writer.Write(value);
        }

        public void WriteHexInteger(int value)
        {
            if ( value < 0 )
                _writer.Write('-');

            _writer.Write("{0:x}",Math.Abs(value));
        }

        public void WriteLong(long value)
        {
            if ( value < 0 )
                _writer.Write('-');

            _writer.Write("{0:x}",Math.Abs(value));
        }

        public void WriteULong(ulong value)
        {
            _writer.Write("{0:x}",value);
        }

        public void WriteDecimal(decimal value)
        {
            _writer.Write(value);
        }

        public void WriteFloatBytes(float value)
        {
            var bytes = BitConverter.GetBytes(value);
            for (var i = 0; i < 4; i++)
            {
                _writer.Write("{0:X2}", bytes[i]);
            }
        }

        public void WritePosition(Position p)
        {
            WriteHexInteger((int)p.X);
            WriteChar('.');
            WriteHexInteger((int)p.Y);
            WriteChar('.');
            WriteHexInteger((int)p.Z);
        }

        public void WriteArray<T>(T[] array, Action<T> writeAction)
        {
            for (var i = 0; i < array.Length; i++)
            {
                writeAction(array[i]);

                if (i < array.Length - 1)
                    _writer.Write(',');
            }
        }

        public void WriteEnumerable<T>(IEnumerable<T> items, Action<T> writeAction)
        {
            WriteArray(items.ToArray(),writeAction);
        }

        public override string ToString()
        {
            return _writer.ToString();
        }

        public void Dispose()
        {
            _writer.Dispose();
        }
    }
}