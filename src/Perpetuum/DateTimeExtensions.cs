using System;

namespace Perpetuum
{
    public static class DateTimeExtensions
    {
        public static DateTimeRange ToRange(this DateTime dt, TimeSpan delta)
        {
            return DateTimeRange.FromDelta(dt,delta);
        }
    }
}
