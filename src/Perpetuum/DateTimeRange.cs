using System;

namespace Perpetuum
{
    [Serializable]
    public struct DateTimeRange : IEquatable<DateTimeRange>
    {
        public static readonly DateTimeRange None = default(DateTimeRange);

        private readonly DateTime _start;
        private readonly DateTime _end;

        public DateTime Start
        {
            get { return _start; }
        }

        public DateTime End
        {
            get { return _end; }
        }

        public TimeSpan Delta
        {
            get { return _end - _start; }
        }

        public bool IsBetween(DateTime dt)
        {
            return dt >= _start && dt <= _end;
        }

        public DateTimeRange(DateTime start,DateTime end)
        {
            if (start < end)
            {
                _start = start;
                _end = end;
            }
            else
            {
                _start = end;
                _end = start;
            }
        }

        public static DateTimeRange FromDelta(DateTime start, TimeSpan delta)
        {
            return new DateTimeRange(start,start + delta);
        }

        public bool Equals(DateTimeRange other)
        {
            return _start.Equals(other._start) && _end.Equals(other._end);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            return obj is DateTimeRange && Equals((DateTimeRange) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (_start.GetHashCode()*397) ^ _end.GetHashCode();
            }
        }

        public static bool operator ==(DateTimeRange left, DateTimeRange right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(DateTimeRange left, DateTimeRange right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return $"{Start} - {End} Delta: {Delta}";
        }
    }
}