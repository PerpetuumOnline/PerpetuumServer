using System;

namespace Perpetuum.Threading
{
    internal sealed class DefaultDisposable : IDisposable
    {
        public static readonly DefaultDisposable Instance = new DefaultDisposable();

        public void Dispose()
        {
            
        }
    }
}