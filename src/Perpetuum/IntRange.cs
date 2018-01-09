using System;

namespace Perpetuum
{
    [Serializable]
    public struct IntRange : IEquatable<IntRange>
    {
        private readonly int _min;
        private readonly int _max;

        public int Min
        {
            get { return _min; }
        }

        public int Max
        {
            get { return _max; }
        }

        public IntRange(int min, int max) : this()
        {
            _min = min;
            _max = max;
        }

        public bool Equals(IntRange other)
        {
            return _min == other._min && _max == other._max;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is IntRange && Equals((IntRange) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (_min*397) ^ _max;
            }
        }

        public static bool operator ==(IntRange left, IntRange right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(IntRange left, IntRange right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return $"{Min}, {Max}";
        }
    }
}