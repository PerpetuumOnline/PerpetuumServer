using System;
using System.Collections.Generic;
using System.Linq;

namespace Perpetuum
{
    public static class TimeSpanExtensions
    {
        public static TimeSpan Average(this IEnumerable<TimeSpan> spans)
        {
            return TimeSpan.FromTicks((long) spans.Average(s => s.Ticks));
        }

        public static TimeSpan Min(this TimeSpan span, TimeSpan min)
        {
            return TimeSpan.FromTicks(Math.Min(span.Ticks, min.Ticks));
        }

        public static TimeSpan Max(this TimeSpan span, TimeSpan max)
        {
            return TimeSpan.FromTicks(Math.Max(span.Ticks, max.Ticks));
        }

        public static TimeSpan Multiply(this TimeSpan span, int multiplier)
        {
            return TimeSpan.FromTicks(span.Ticks * multiplier);
        }

        public static TimeSpan Divide(this TimeSpan left, TimeSpan right)
        {
            return TimeSpan.FromTicks(left.Ticks/right.Ticks);
        }

        public static TimeSpan Divide(this TimeSpan left, int divider)
        {
            return TimeSpan.FromTicks(left.Ticks / divider);
        }

        public static TimeSpan Multiply(this TimeSpan span, double multiplier)
        {
            return TimeSpan.FromTicks((long)(span.Ticks * multiplier));
        }

        public static string ToHumanTimeString(this TimeSpan span)
        {
            var format = "g3";
            if (span.TotalMilliseconds < 1000)
            {
                return $"{span.TotalMilliseconds.ToString(format)} milliseconds";
            }
            else if (span.TotalSeconds < 60)
            {
                return $"{span.TotalSeconds.ToString(format)} seconds";
            }
            else if (span.TotalMinutes < 60)
            {
                return $"{span.TotalMinutes.ToString(format)} minutes";
            }
            else if (span.TotalHours < 24)
            {
                return $"{span.TotalHours.ToString(format)} hours";
            }
            return $"{span.TotalDays.ToString(format)} days";
        }
    }
}
