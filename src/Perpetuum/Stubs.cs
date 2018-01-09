using System;

namespace Perpetuum
{
    public static class Stubs<T>
    {
        public static readonly Action<T> None = _ => { };
    }

    public static class Stubs
    {
        public static readonly Action None =  () => { };
    }
}
