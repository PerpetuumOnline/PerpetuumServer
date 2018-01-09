using System;
using System.Collections.Generic;
using Perpetuum.ExportedTypes;

namespace Perpetuum.EntityFramework
{
    public struct TierInfo : IComparable<TierInfo>, IEquatable<TierInfo>
    {
        public static readonly TierInfo None = new TierInfo(TierType.Undefined,0);

        public readonly TierType type;
        public readonly int level;

        public TierInfo(TierType type,int level)
        {
            this.type = type;
            this.level = level;
        }

        public int CompareTo(TierInfo other)
        {
            if ( type == other.type )
                return level.CompareTo(other.level);

            return type.CompareTo(other.type);
        }

        public static bool operator ==(TierInfo a, TierInfo b)
        {
            return a.CompareTo(b) == 0;
        }

        public static bool operator !=(TierInfo a, TierInfo b)
        {
            return a.CompareTo(b) != 0;
        }

        public static bool operator <(TierInfo a, TierInfo b)
        {
            return a.CompareTo(b) < 0;
        }

        public static bool operator >(TierInfo a, TierInfo b)
        {
            return a.CompareTo(b) < 0;
        }

        public override string ToString()
        {
            return string.Format((string) "Type: {0}, Level: {1}", (object) type, level);
        }

        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
            {
                {k.type,(int)type},
                {k.level,level}
            };
        }

        public bool Equals(TierInfo other)
        {
            return type == other.type && level == other.level;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            return obj is TierInfo && Equals((TierInfo) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int) type*397) ^ level;
            }
        }
    }
}