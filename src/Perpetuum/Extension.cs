using System;
using System.Collections.Generic;
using System.Data;
using Perpetuum.Data;

namespace Perpetuum
{
    public struct Extension : IEquatable<Extension>
    {
        public readonly int id;
        public readonly int level;

        public Extension(int id,int level)
        {
            this.id = id;
            this.level = level;
        }

        public override string ToString()
        {
            return $"Id: {id}, Level: {level}";
        }

        public Dictionary<string,object> ToDictionary()
        {
            return new Dictionary<string, object>
                {
                    {k.extensionID,id},
                    {k.extensionLevel,level}
                };
        }

        public bool Equals(Extension other)
        {
            return id == other.id && level == other.level;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is Extension && Equals((Extension) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (id*397) ^ level;
            }
        }

        public static bool operator ==(Extension left, Extension right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Extension left, Extension right)
        {
            return !left.Equals(right);
        }

        public static Extension FromDbDataRecord(IDataRecord record)
        {
            var id = record.GetValue<int>("extensionid");
            var level = record.GetValue<int>("extensionlevel");
            return new Extension(id,level);
        }
    }
}