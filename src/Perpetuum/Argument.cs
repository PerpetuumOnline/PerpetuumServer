using System.Collections.Generic;

namespace Perpetuum
{
    public struct Argument<T> : IArgument
    {
        private readonly string _name;

        public Argument(string name)
        {
            _name = name;
        }

        public void Check(Dictionary<string, object> data)
        {
            if (!data.TryGetValue(_name, out object o))
                throw new PerpetuumException(ErrorCodes.RequiredArgumentIsNotSpecified);

            if (o.GetType() != typeof(T))
                throw new PerpetuumException(ErrorCodes.RequiredArgumentIsNotSpecified);
        }
    }
}