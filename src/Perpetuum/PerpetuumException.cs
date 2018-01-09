using System;
using System.Collections.Generic;

namespace Perpetuum
{
    public class PerpetuumException : Exception
    {
        public readonly ErrorCodes error;

        public PerpetuumException(ErrorCodes error)
        {
            this.error = error;
        }

        public PerpetuumException SetData<T>(string key, T value)
        {
            Data[key] = value;
            return this;
        }

        public PerpetuumException SetData(IDictionary<string,object> dictionary)
        {
            if (dictionary == null)
                return this;

            foreach (var kvp in dictionary)
            {
                SetData(kvp.Key, kvp.Value);
            }

            return this;
        }

        public static PerpetuumException Create(ErrorCodes error)
        {
            return new PerpetuumException(error);
        }

        public override string ToString()
        {
            return Data.Count == 0 ? error.ToString() : $"{error} Data = {Data.ToDictionary().ToDebugString()}";
        }
    }
}