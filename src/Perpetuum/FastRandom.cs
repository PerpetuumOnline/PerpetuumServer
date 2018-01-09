using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace Perpetuum
{

    /// <summary>
    /// Threadsafe random generator
    /// </summary>
    public static class FastRandom
    {
        private const double REAL_LONG = 1.0 / (long.MaxValue + 1.0);
        private const long LONG_MASK = 0x7fffffffffffffff;
        private const int INT_MASK = 0x7fffffff;
        private static SpinLock _spinLock = new SpinLock(false);
        private static ulong _x, _y, _z, _w;

        static FastRandom()
        {
            using (var md5 = MD5.Create())
            {
                var guid = Guid.NewGuid().ToByteArray();
                unchecked
                {
                    _x = (ulong)DateTime.Now.Ticks;
                    _y = (ulong)BitConverter.ToInt64(md5.ComputeHash(Encoding.UTF8.GetBytes(DateTime.Now.ToString(CultureInfo.InvariantCulture))), 0);
                    _z = (ulong)BitConverter.ToInt64(guid, 0);
                    _w = (ulong)BitConverter.ToInt64(guid, 8);
                }
            }
        }

        public static long NextLong()
        {
            var lockTaken = false;
            try
            {
                _spinLock.Enter(ref lockTaken);
                var t = (_x ^ (_x << 11));
                _x = _y; _y = _z; _z = _w;
                _w = (_w ^ (_w >> 19)) ^ (t ^ (t >> 8));
                return (long)(_w & LONG_MASK);
            }
            finally
            {
                if ( lockTaken )
                    _spinLock.Exit();
            }
        }

        public static long NextLong(long maxValue)
        {
            return (long)((REAL_LONG * NextLong()) * maxValue);
        }

        public static float NextFloat()
        {
            return (float) NextDouble();
        }

        public static float NextFloat(float min,float max)
        {
            return (float) NextDouble(min,max);
        }

        public static double NextDouble()
        {
            return REAL_LONG * NextLong();
        }

        public static double NextDouble(double minValue, double maxValue)
        {
            return minValue + (NextDouble()) * (maxValue - minValue);
        }

        public static int NextInt()
        {
            return (int)(NextLong() & INT_MASK);
        }

        public static int NextInt(IntRange range)
        {
            return NextInt(range.Min, range.Max);
        }

        public static int NextInt(int maxValue)
        {
            return (int)(NextDouble() * (maxValue + 1));
        }

        public static int NextInt(int minValue, int maxValue)
        {
            if (minValue == maxValue)
                return minValue;

            return minValue + (int)(NextDouble() * ((maxValue + 1) - minValue));
        }

        public static byte NextByte()
        {
            return (byte)((NextDouble()) * 256);
        }

        public static byte[] NextBytes(int count)
        {
            var result = new byte[count];

            for (var i = 0; i < count; i++)
            {
                result[i] = NextByte();
            }

            return result;
        }

        public static TimeSpan NextTimeSpan(TimeRange range)
        {
            return NextTimeSpan(range.Min, range.Max);
        }

        public static TimeSpan NextTimeSpan(TimeSpan min,TimeSpan max)
        {
            return TimeSpan.FromTicks(min.Ticks +  NextLong(max.Ticks - min.Ticks));
        }

        public static TimeSpan NextTimeSpan(TimeSpan max)
        {
            return TimeSpan.FromTicks(NextLong(max.Ticks));
        }

        public static string NextString(int length)
        {
            var sb = new StringBuilder(length);

            for (var i = 0; i < length; i++)
            {
                sb.Append(Convert.ToChar(NextInt(0x61, 0x7a))); //a-z
            }

            return sb.ToString();
        }
    }
}
