using System;

namespace Perpetuum.Builders
{
    public class AnonymousBuilder<T> : IBuilder<T>
    {
        private readonly Func<T> _builder;

        public AnonymousBuilder(Func<T> builder)
        {
            _builder = builder;
        }

        public T Build()
        {
            return _builder();
        }
    }
}