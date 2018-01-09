using System;

namespace Perpetuum
{
    public struct TimeRange : IEquatable<TimeRange>
    {
        private readonly TimeSpan _min;
        private readonly TimeSpan _max;

        public TimeSpan Min
        {
            get { return _min; }
        }

        public TimeSpan Max
        {
            get { return _max; }
        }

        public TimeRange(TimeSpan min,TimeSpan max)
        {
            if (min < max)
            {
                _min = min;
                _max = max;
            }
            else
            {
                _min = max;
                _max = min;
            }
        }

        public static TimeRange FromLength(TimeSpan min, TimeSpan length)
        {
            return new TimeRange(min,min + length);
        }

        public bool Equals(TimeRange other)
        {
            return _min.Equals(other._min) && _max.Equals(other._max);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            return obj is TimeRange && Equals((TimeRange) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (_min.GetHashCode()*397) ^ _max.GetHashCode();
            }
        }

        public static bool operator ==(TimeRange left, TimeRange right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(TimeRange left, TimeRange right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return $"{Min} - {Max}";
        }
    }
}