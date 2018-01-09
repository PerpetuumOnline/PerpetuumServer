using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using Perpetuum.Threading;

namespace Perpetuum.GenXY
{
    public class GenxyReader : Disposable
    {
        private readonly TextReader _reader;

        private static readonly NumberFormatInfo _numberFormatInfo;

        static GenxyReader()
        {
            _numberFormatInfo = (NumberFormatInfo)CultureInfo.InstalledUICulture.NumberFormat.Clone();
            _numberFormatInfo.NumberDecimalSeparator = ".";
        }

        public GenxyReader(TextReader reader)
        {
            _reader = reader;
        }

        private char _currentChar;

        private readonly char[] _charBuffer = new char[1024];
        private int _charReaded;
        private int _charPos;

        private bool Read()
        {
            if (_charPos >= _charReaded)
            {
                _charReaded = _reader.Read(_charBuffer, 0, _charBuffer.Length);
                if (_charReaded == 0)
                {
                    return false;
                }

                _charPos = 0;
            }

            _currentChar = _charBuffer[_charPos];
            return true;
        }

        private string ReadKey()
        {
            var sb = new StringBuilder();

            while (Read())
            {
                if (_currentChar == '#' || _currentChar == '|')
                {
                    _charPos++;
                    continue;
                }

                if (_currentChar == '=')
                    break;

                if (!char.IsWhiteSpace(_currentChar))
                {
                    sb.Append(_currentChar);
                }

                _charPos++;
            }

            return sb.ToString();
        }

        public object ReadValue()
        {
            while (Read())
            {
                if (char.IsWhiteSpace(_currentChar) || _currentChar == '=')
                {
                    _charPos++;
                    continue;
                }

                var token = (GenxyToken)_currentChar;
                _charPos++;

                switch (token)
                {
                    case GenxyToken.String:
                        return ReadEscapedString();
                    case GenxyToken.StringArray:
                        return ReadEscapedStringArray();
                    case GenxyToken.Integer:
                        return ReadInt();
                    case GenxyToken.IntegerArray:
                        return ReadIntArray();
                    case GenxyToken.Long:
                        return ReadLong();
                    case GenxyToken.LongArray:
                        return ReadLongArray();
                    case GenxyToken.Decimal:
                        return ReadDecimal();
                    case GenxyToken.DecimalArray:
                        return ReadDecimalArray();
                    case GenxyToken.ByteArray:
                        return ReadByteArray();
                    case GenxyToken.DecimalLong:
                        return ReadDecimalLong();
                    case GenxyToken.Float:
                        return ReadFloat();
                    case GenxyToken.FloatBytes:
                        return ReadFloatBytes();
                    case GenxyToken.DoubleBytes:
                        return ReadDoubleBytes();
                    case GenxyToken.Date:
                        return ReadDate();
                    case GenxyToken.Color:
                        return ReadColor();
                    case GenxyToken.Area:
                        return ReadArea();
                    case GenxyToken.AreaArray:
                        return ReadAreaArray();
                    case GenxyToken.Point:
                        return ReadPoint();
                    case GenxyToken.Position:
                        return ReadPosition();
                    case GenxyToken.PositionArray:
                        return ReadPositionArray();
                    case GenxyToken.StartProp:
                    {
                        return ReadDictionary();
                    }
                }

                var v = ReadValueAsString();
                return v;
            }

            return null;
        }

        private string ReadEscapedString()
        {
            var v = ReadValueAsString();
            return ParseEscapedString(v);
        }

        private string[] ReadEscapedStringArray()
        {
            return ReadValueAsArray(ParseEscapedString);
        }

        private int ReadInt()
        {
            var valueString = ReadValueAsString();
            return ParseInt(valueString);
        }

        private int[] ReadIntArray()
        {
            return ReadValueAsArray(ParseInt);
        }

        private byte[] ReadByteArray()
        {
            var v = ReadValueAsString();

            var len = v.Length / 2;
            var r = new byte[len];

            for (var i = 0; i < len; i++)
            {
                r[i] = ParseHexNumber(v.Substring(i * 2,2),(s,num) => byte.Parse(num,NumberStyles.HexNumber,null));
            }

            return r;
        }

        private long ReadLong()
        {
            var v = ReadValueAsString();
            return ParseLong(v);
        }

        private long[] ReadLongArray()
        {
            return ReadValueAsArray(ParseLong);
        }

        private int ReadDecimal()
        {
            var v = ReadValueAsString();
            return int.Parse(v);
        }

        private int[] ReadDecimalArray()
        {
            return ReadValueAsArray(int.Parse);
        }

        private long ReadDecimalLong()
        {
            var v = ReadValueAsString();
            return long.Parse(v);
        }

        private double ReadFloat()
        {
            var v = ReadValueAsString();
            return double.Parse(v,_numberFormatInfo);
        }

        private double ReadFloatBytes()
        {
            var v = ReadValueAsString();

            var b = new byte[4];
            for (var i = 0; i < 4; i++)
                b[i] = Convert.ToByte(v.Substring(i * 2, 2), 16);

            return BitConverter.ToSingle(b, 0);
        }

        private double ReadDoubleBytes()
        {
            var v = ReadValueAsString();

            var b = new byte[8];
            for (var i = 0; i < 8; i++)
                b[i] = Convert.ToByte(v.Substring(i * 2, 2), 16);

            return BitConverter.ToDouble(b, 0);
        }

        private DateTime ReadDate()
        {
            var n = ReadValueAsArray(int.Parse, '.');
            return new DateTime(n[0], n[1], n[2], n[3], n[4], n[5]);
        }

        private Color ReadColor()
        {
            var n = ReadValueAsArray(ParseInt, '.');
            return Color.FromArgb(n[3], n[0], n[1], n[2]);
        }

        private Area ReadArea()
        {
            var v = ReadValueAsString();
            return ParseArea(v);
        }

        private Area[] ReadAreaArray()
        {
            return ReadValueAsArray(ParseArea);
        }

        private Point ReadPoint()
        {
            var n = ReadValueAsArray(ParseInt, '.');
            return new Point(n[0], n[1]);
        }

        private Position ReadPosition()
        {
            var v = ReadValueAsString();
            return ParsePosition(v);
        }

        private Position[] ReadPositionArray()
        {
            return ReadValueAsArray(ParsePosition);
        }


        public Dictionary<string, object> ReadDictionary()
        {
            var d = new Dictionary<string, object>();

            while (Read())
            {
                if (char.IsWhiteSpace(_currentChar) || _currentChar == '[')
                {
                    _charPos++;
                    continue;
                }

                if (_currentChar == ']')
                {
                    _charPos++;
                    break;
                }

                var key = ReadKey();
                var value = ReadValue();
                d[key] = value;
            }

            return d;
        }


        private static Position ParsePosition(string positionString)
        {
            var n = ParseValueAsArray(positionString, ParseInt, '.');
            return new Position(n[0], n[1], n[2]);
        }

        private static Area ParseArea(string areaString)
        {
            var n = ParseValueAsArray(areaString, ParseInt, '.');
            return new Area(n[0], n[1], n[2], n[3]);
        }

        private static T ParseHexNumber<T>(string hexNumber, Func<int /* sign */, string, T> parser)
        {
            var sign = 1;
            if (hexNumber[0] == '-')
            {
                hexNumber = hexNumber.Substring(1);
                sign = -1;
            }

            return parser(sign, hexNumber);
        }

        private static int ParseInt(string valueString)
        {
            return ParseHexNumber(valueString, (s, num) => int.Parse(num, NumberStyles.HexNumber, null) * s);
        }

        private static long ParseLong(string longString)
        {
            return ParseHexNumber(longString, (s, num) => long.Parse(num, NumberStyles.HexNumber, null) * s);
        }

        private static string ParseEscapedString(string s)
        {
            var result = new StringBuilder();

            int Decode(char d)
            {
                return (d & 0xf) + ((d & 0x40) >> 3) + ((d & 0x40) >> 6);
            }

            var i = 0;
            while (i < s.Length)
            {
                var c = s[i++];
                if (c == '\\')
                {
                    var hc = Decode(s[i++]);
                    var lc = Decode(s[i++]);
                    c = (char)(hc << 4 | lc);
                }
                result.Append(c);
            }

            return result.ToString();
        }

        private T[] ReadValueAsArray<T>(Func<string, T> valueAction, char separator = ',')
        {
            var v = ReadValueAsString();
            return ParseValueAsArray(v, valueAction, separator);
        }

        private static T[] ParseValueAsArray<T>(string value, Func<string, T> valueAction, char separator)
        {
            if ( string.IsNullOrEmpty(value) )
                return new T[0];

            var stringArray = value.Split(separator);
            var result = new T[stringArray.Length];
            for (var i = 0; i < stringArray.Length; i++)
            {
                result[i] = valueAction(stringArray[i]);
            }
            return result;
        }

        private string ReadValueAsString()
        {
            var sb = new StringBuilder();
            while (Read())
            {
                if (_currentChar == '#' ||
                    _currentChar == '|' ||
                    _currentChar == ']')
                    break;

                sb.Append(_currentChar);
                _charPos++;
            }

            return sb.ToString();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _reader.Dispose();
            }
        }
    }
}