using System;
using Perpetuum.IDGenerators;

namespace Perpetuum.Services.Sessions
{
    [Serializable]
    public struct SessionID : IEquatable<SessionID>
    {
        public static readonly SessionID None = new SessionID(0);
        private static readonly IIDGenerator<int> _idGenerator = IDGenerator.CreateIntIDGenerator();
        
        private readonly int _id;

        private SessionID(int id)
        {
            _id = id;
        }

        public bool Equals(SessionID other)
        {
            return _id == other._id;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) 
                return false;

            return obj is SessionID && Equals((SessionID) obj);
        }

        public override int GetHashCode()
        {
            return _id;
        }

        public static bool operator ==(SessionID left, SessionID right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SessionID left, SessionID right)
        {
            return !left.Equals(right);
        }

        public static explicit operator int(SessionID sessionId)
        {
            return sessionId._id;
        }

        public override string ToString()
        {
            return $"{_id}";
        }

        public static SessionID New()
        {
            return new SessionID(_idGenerator.GetNextID());
        }
    }
}