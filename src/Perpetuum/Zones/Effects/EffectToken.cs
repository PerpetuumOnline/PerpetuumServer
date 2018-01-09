using System;
using Perpetuum.IDGenerators;

namespace Perpetuum.Zones.Effects
{
    public struct EffectToken : IEquatable<EffectToken>
    {
        private static readonly IIDGenerator<int> _idGenerator = IDGenerator.CreateIntIDGenerator();
        private readonly int _id;

        private EffectToken(int id)
        {
            _id = id;
        }

        public bool Equals(EffectToken other)
        {
            return other._id == _id;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) 
                return false;

            return obj is EffectToken && Equals((EffectToken) obj);
        }

        public override int GetHashCode()
        {
            return _id;
        }

        public static bool operator ==(EffectToken left, EffectToken right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(EffectToken left, EffectToken right)
        {
            return !left.Equals(right);
        }

        public static EffectToken NewToken()
        {
            return new EffectToken(_idGenerator.GetNextID());
        }

        public override string ToString()
        {
            return $"Id: {_id}";
        }
    }
}