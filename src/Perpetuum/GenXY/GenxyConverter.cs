using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Dynamic;
using System.IO;

namespace Perpetuum.GenXY
{
    public static class GenxyConverter
    {
        private static readonly Dictionary<Type,Converter> _converters = new Dictionary<Type, Converter>();

        private delegate void Converter(GenxyWriter writer, object value);

        static GenxyConverter()
        {
            _converters[typeof(byte)] = ConvertNumber;
            _converters[typeof(short)] = ConvertNumber;
            _converters[typeof(ushort)] = ConvertNumber;
            _converters[typeof(uint)] = ConvertNumber;
            _converters[typeof(bool)] = ConvertNumber;

            RegisterConverter<string>(ConvertString);
            RegisterConverter<string[]>(ConvertStringArray);
            RegisterConverter<int>(ConvertInt);
            RegisterConverter<int[]>(ConvertIntArray);
            RegisterConverter<List<int>>(ConvertIntList);
            RegisterConverter<byte[]>(ConvertByteArray);
            RegisterConverter<long>(ConvertLong);
            RegisterConverter<long[]>(ConvertLongArray);
            RegisterConverter<ulong>(ConvertULong);
            RegisterConverter<decimal>(ConvertDecimal);
            RegisterConverter<decimal[]>(ConvertDecimalArray);
            RegisterConverter<float>(ConvertFloat);
            RegisterConverter<double>(CreateDoubleConverter);
            RegisterConverter<Color>(ConvertColor);
            RegisterConverter<DateTime>(ConvertDateTime);
            RegisterConverter<Point>(ConvertPoint);
            RegisterConverter<Position>(ConvertPosition);
            RegisterConverter<Position[]>(ConvertPositionArray);
            RegisterConverter<Area>(ConvertArea);
            RegisterConverter<Dictionary<string, object>>(ConvertDictionary);
            RegisterConverter<ExpandoObject>(ConvertExpandoObject);
            RegisterConverter<GenxyString>(ConvertGenxyString);
        }

        public static void RegisterConverter<T>(Action<GenxyWriter, T> converter)
        {
            _converters[typeof (T)] = (writer, value) =>
            {
                converter(writer, (T) value);
            };
        }

        private static void ConvertString(GenxyWriter writer,string s)
        {
            writer.WriteToken(GenxyToken.String);
            writer.WriteEscapedString(s);
        }

        private static void ConvertStringArray(GenxyWriter writer,string[] stringArray)
        {
            writer.WriteToken(GenxyToken.StringArray);
            writer.WriteArray(stringArray, writer.WriteEscapedString);
        }

        public static void ConvertInt(GenxyWriter writer,int value)
        {
            writer.WriteToken(GenxyToken.Integer);
            writer.WriteHexInteger(value);
        }

        public static void ConvertLong(GenxyWriter writer,long value)
        {
            writer.WriteToken(GenxyToken.Long);
            writer.WriteLong(value);
        }

        private static void ConvertLongArray(GenxyWriter writer,long[] longArray)
        {
            writer.WriteToken(GenxyToken.LongArray);
            writer.WriteArray(longArray, writer.WriteLong);
        }

        private static void ConvertULong(GenxyWriter writer,ulong value)
        {
            writer.WriteToken(GenxyToken.Long);
            writer.WriteULong(value);
        }

        private static void ConvertIntArray(GenxyWriter writer,int[] value)
        {
            writer.WriteToken(GenxyToken.IntegerArray);
            writer.WriteArray(value, writer.WriteHexInteger);
        }

        private static void ConvertIntList(GenxyWriter writer, List<int> value)
        {
            ConvertIntArray(writer, value.ToArray());
        }

        private static void ConvertNumber(GenxyWriter writer,object value)
        {
            ConvertInt(writer, Convert.ToInt32(value));
        }

        private static void ConvertDecimal(GenxyWriter writer,decimal value)
        {
            writer.WriteToken(GenxyToken.Decimal);
            writer.WriteDecimal(value);
        }

        private static void ConvertDecimalArray(GenxyWriter writer,decimal[] value)
        {
            writer.WriteToken(GenxyToken.DecimalArray);
            writer.WriteArray(value, writer.WriteDecimal);
        }

        private static void ConvertFloat(GenxyWriter writer,float value)
        {
            writer.WriteToken(GenxyToken.FloatBytes);
            writer.WriteFloatBytes(value);
        }

        private static void CreateDoubleConverter(GenxyWriter writer,double value)
        {
            ConvertFloat(writer,(float) value);
        }

        private static void ConvertColor(GenxyWriter writer,Color color)
        {
            writer.WriteToken(GenxyToken.Color);
            writer.WriteHexInteger(color.R);
            writer.WriteChar('.');
            writer.WriteHexInteger(color.G);
            writer.WriteChar('.');
            writer.WriteHexInteger(color.B);
            writer.WriteChar('.');
            writer.WriteHexInteger(color.A);
        }

        private static void ConvertDateTime(GenxyWriter writer,DateTime date)
        {
            writer.WriteToken(GenxyToken.Date);
            writer.WriteInteger(date.Year);
            writer.WriteChar('.');
            writer.WriteInteger(date.Month);
            writer.WriteChar('.');
            writer.WriteInteger(date.Day);
            writer.WriteChar('.');
            writer.WriteInteger(date.Hour);
            writer.WriteChar('.');
            writer.WriteInteger(date.Minute);
            writer.WriteChar('.');
            writer.WriteInteger(date.Second);
        }

        private static void ConvertPoint(GenxyWriter writer,Point point)
        {
            writer.WriteToken(GenxyToken.Point);
            writer.WriteHexInteger(point.X);
            writer.WriteChar('.');
            writer.WriteHexInteger(point.Y);
        }

        private static void ConvertPosition(GenxyWriter writer,Position position)
        {
            writer.WriteToken(GenxyToken.Position);
            writer.WritePosition(position);
        }

        private static void ConvertPositionArray(GenxyWriter writer,Position[] value)
        {
            writer.WriteToken(GenxyToken.PositionArray);
            writer.WriteArray(value, writer.WritePosition);
        }

        private static void ConvertArea(GenxyWriter writer,Area area)
        {
            writer.WriteToken(GenxyToken.Area);
            writer.WriteHexInteger(area.X1);
            writer.WriteChar('.');
            writer.WriteHexInteger(area.Y1);
            writer.WriteChar('.');
            writer.WriteHexInteger(area.X2);
            writer.WriteChar('.');
            writer.WriteHexInteger(area.Y2);
        }

        private static void ConvertByteArray(GenxyWriter writer,byte[] value)
        {
            writer.WriteToken(GenxyToken.ByteArray);
            writer.WriteString(BitConverter.ToString(value).Replace("-", ""));
        }

        private static void ConvertGenxyString(GenxyWriter writer,GenxyString value)
        {
            writer.WriteToken(GenxyToken.StartProp);
            writer.WriteString(value.ToString().Replace('#', '|'));
            writer.WriteToken(GenxyToken.EndProp);
        }

        private static void ConvertDictionary(GenxyWriter writer,Dictionary<string,object> dictionary)
        {
            ConvertEnumerableStringObject(writer,dictionary);
        }

        private static void ConvertExpandoObject(GenxyWriter writer,ExpandoObject value)
        {
            ConvertEnumerableStringObject(writer, value);
        }

        private static void ConvertEnumerableStringObject(GenxyWriter writer,IEnumerable<KeyValuePair<string, object>> d)
        {
            writer.WriteToken(GenxyToken.StartProp);

            foreach (var kvp in d)
            {
                if (!HasValue(kvp.Value))
                    continue;

                writer.WriteToken(GenxyToken.Prop);
                writer.WriteString(kvp.Key);
                writer.WriteChar('=');
                SerializeObject(writer, kvp.Value);
            }

            writer.WriteToken(GenxyToken.EndProp);
        }

        private static bool HasValue(object value)
        {
            if (value == null)
                return false;

            if (value is ICollection collection && collection.Count == 0)
                return false;

            return true;
        }

        public static string SerializeObject(object value)
        {
            using (var w = new GenxyWriter())
            {
                SerializeObject(w, value);
                return w.ToString();
            }
        }

        private static void SerializeObject(GenxyWriter writer, object value)
        {
            if (value == null)
                return;

            switch (value)
            {
                case string s: { ConvertString(writer,s); return; }
                case string[] stringArray: { ConvertStringArray(writer,stringArray); return; }
                case int i: { ConvertInt(writer,i); return; }
                case int[] intArray: { ConvertIntArray(writer,intArray); return; }
            }

            var type = value.GetType();
            if (!_converters.TryGetValue(type, out Converter c))
            {
                if (type.IsEnum)
                {
                    var tc = Convert.GetTypeCode(value);

                    switch (tc)
                    {
                        case TypeCode.Int32:
                            ConvertInt(writer,(int) value);
                            return;
                        case TypeCode.Int64:
                            ConvertLong(writer,(long) value);
                            return;
                    }
                }

                Debug.Assert(false, "unknown type" + type);
            }

            c(writer, value);
        }

        public static string Serialize(IEnumerable<KeyValuePair<string, object>> value)
        {
            if (value == null)
                return string.Empty;

            using (var w = new GenxyWriter())
            {
                foreach (var kvp in value)
                {
                    if (!HasValue(kvp.Value))
                        continue;

                    w.WriteChar('#');
                    w.WriteString(kvp.Key);
                    w.WriteChar('=');
                    SerializeObject(w, kvp.Value);
                }

                return w.ToString();
            }
        }

        public static Dictionary<string, object> Deserialize(string genxyString)
        {
            if ( string.IsNullOrEmpty(genxyString))
                return new Dictionary<string, object>();

            using (var reader = new GenxyReader(new StringReader(genxyString)))
            {
                return reader.ReadDictionary();
            }
        }

        public static object DeserializeObject(string genxyString)
        {
            if (string.IsNullOrEmpty(genxyString))
                return null;

            using (var reader = new GenxyReader(new StringReader(genxyString)))
            {
                return reader.ReadValue();
            }
        }

        public static T DeserializeObject<T>(string genxyString)
        {
            return (T) DeserializeObject(genxyString);
        }
    }
}