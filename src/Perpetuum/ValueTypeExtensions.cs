using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Perpetuum
{
    public static class ValueTypeExtensions
    {
        [Pure]
        public static bool ToBool(this int value)
        {
            return value > 0;
        }

        [Pure]
        public static int Clamp(this int value, int lowerBound, int upperBound)
        {
            return Math.Min(upperBound, Math.Max(lowerBound, value));
        }

        [Pure]
        public static ushort Clamp(this ushort value, ushort lowerBound, ushort upperBound)
        {
            return Math.Min(upperBound, Math.Max(lowerBound, value));
        }

        public static int Min(this int value, int min)
        {
            return Math.Min(value, min);
        }

        public static int Max(this int value, int max)
        {
            return Math.Max(value, max);
        }

        [Pure]
        public static bool IsValueInEnum<T>(this int value)
        {
            Debug.Assert(typeof (T).IsEnum, "invalid enum type (" + typeof (T) + ")");
            Debug.Assert(Enum.GetUnderlyingType(typeof (T)) == typeof (int), $"Invalid underlying type ({Enum.GetUnderlyingType(typeof (T)).Name} != int)");
            return Enum.GetValues(typeof (T)).Cast<int>().Any(enumValue => enumValue == value);
        }

        [UsedImplicitly, Pure]
        public static int ToInt(this bool value)
        {
            return value ? 1 : 0;
        }

        public static byte ToByte(this bool value)
        {
            return (byte) (value ? 1 : 0);
        }


        public static double Ratio(this double current, double max)
        {
            if (max.IsZero() || double.IsNaN(max))
                return 0.0;

            return (current/max).Clamp();
        }


        public static long ToFixedFloat(this double value)
        {
            return unchecked((long) (value*0x100000000));
        }

        [Pure, UsedImplicitly]
        public static bool IsInRange(this double num, double minRange, double maxRange)
        {
            return num >= minRange && num <= maxRange;
        }

        public static bool IsZero(this double num)
        {
            return Math.Abs(num) < double.Epsilon;
        }

        public static bool IsZero(this float num)
        {
            return Math.Abs(num) < float.Epsilon;
        }

        public static double Clamp(this double value, double lowerBound = 0.0, double upperBound = 1.0)
        {
            return Math.Min(upperBound, Math.Max(lowerBound, value));
        }

        public static float Clamp(this float value, float lowerBound = 0.0f, float upperBound = 1.0f)
        {
            return Math.Min(upperBound, Math.Max(lowerBound, value));
        }

        public static int Mix(this int source, int target, double mix, bool safe = true)
        {
            return (int) ((double) source).Mix(target, mix, safe);
        }

        /// <summary>
        /// Blends two ushort values
        /// </summary>
        public static ushort Mix(this ushort source, ushort target, double mix, bool safe = true)
        {
            return (ushort) ((double) source).Mix(target, mix, safe);
        }

        /// <summary>
        /// Blends two double values
        /// </summary>
        public static double Mix(this double source, double target, double mix, bool safe = true)
        {
            if (safe)
                mix = mix.Clamp();

            var diff = (target - source)*mix;
            return source + diff;
        }

        /// <summary>
        /// Ease in out falloff
        /// </summary>
        public static double LimitWithFalloff(this double value, double thresholdLimit, double thresholdLenght)
        {
            if (value <= thresholdLimit)
                return 1.0;

            if (value >= thresholdLimit + thresholdLenght)
                return 0.0;

            var remaining = (value - thresholdLimit)/thresholdLenght;

            return (Math.Sin((Math.PI/2) + (remaining*Math.PI)) + 1)/2;
        }

        public static IEnumerable<decimal> SplitIntoChunks(this decimal num, int chunkSize)
        {
            decimal offset = 0;
            while (offset < num)
            {
                var size = Math.Min(chunkSize, num - offset);
                yield return (int) size;
                offset += size;
            }
        }

        public static double CosineInterpolate(this double from, double to, double m)
        {
            var m2 = (1.0 - Math.Cos(m*Math.PI))/2;
            return from.LinearInterpolate(to, m2);
        }

        public static double LinearInterpolate(this double from, double to, double m)
        {
            return from*(1.0 - m) + to*m;
        }

        public static double Normalize(this double value, double min, double max)
        {
            Debug.Assert(value >= 0.0 && value <= 1.0);
            return value*(max - min) + min;
        }

        public static bool TryGetValue<T>(this T? source, out T value) where T : struct
        {
            if (source == null)
            {
                value = default(T);
                return false;
            }

            value = (T) source;
            return true;
        }


        public static int RotateLeft(this int value, int count)
        {
            uint val = (uint) value;
            return (int) ((val << count) | (val >> (32 - count)));
        }

        public static int RotateRight(this int value, int count)
        {
            uint val = (uint) value;
            return (int)((val >> count) | (val << (32 - count)));
        }

    }
}
