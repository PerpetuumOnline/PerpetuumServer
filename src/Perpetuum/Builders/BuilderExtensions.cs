namespace Perpetuum.Builders
{
    public static class BuilderExtensions
    {
        private class ProxyBuilder<T> : IBuilder<T> where T:class
        {
            private readonly IBuilder<T> _builder;
            private T _object;

            public ProxyBuilder(IBuilder<T> builder)
            {
                _builder = builder;
            }

            public T Build()
            {
                return _object ?? (_object = _builder.Build());
            }
        }

        public static IBuilder<T> ToProxy<T>(this IBuilder<T> builder) where T : class
        {
            return new ProxyBuilder<T>(builder);
        }
    }

}