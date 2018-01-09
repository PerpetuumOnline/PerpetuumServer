using System;
using System.Collections.Generic;

namespace Perpetuum.GenXY
{
    public struct GenxyString : IEquatable<GenxyString>
    {
        public static readonly GenxyString Empty = new GenxyString();

        private readonly string _value;

        public GenxyString(string value)
        {
            _value = value;
        }

        public bool IsEmpty()
        {
            return string.IsNullOrEmpty(_value);
        }

        public static implicit operator GenxyString(string value)
        {
            return new GenxyString(value);
        }

        public static GenxyString FromObject(object value)
        {
            return new GenxyString(value as string);
        }

        public static implicit operator string(GenxyString genxyString)
        {
            return genxyString._value;
        }

        public Dictionary<string, object> ToDictionary()
        {
            if ( _value == null )
                return new Dictionary<string, object>();

            return GenxyConverter.Deserialize(_value);
        }

        public override string ToString()
        {
            if (string.IsNullOrEmpty(_value))
                return string.Empty;

            return _value;
        }

        public bool Equals(GenxyString other)
        {
            return Equals(other._value, _value);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;

            return obj is GenxyString && Equals((GenxyString) obj);
        }

        public override int GetHashCode()
        {
            return (_value != null ? _value.GetHashCode() : 0);
        }

        public static bool operator ==(GenxyString left, GenxyString right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(GenxyString left, GenxyString right)
        {
            return !left.Equals(right);
        }

        public static GenxyString FromDictionary(IDictionary<string, object> dictionary)
        {
            if (dictionary.IsNullOrEmpty())
                return Empty;

            var str = GenxyConverter.Serialize(dictionary);
            return new GenxyString(str);
        }
    }
}
