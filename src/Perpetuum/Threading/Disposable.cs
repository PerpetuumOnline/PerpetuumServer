using System;
using System.Threading;

namespace Perpetuum.Threading
{
    /// <summary>
    /// Safe disposable wrapper
    /// </summary>
    public abstract class Disposable : IDisposable
    {
        private int _disposed;

        ~Disposable()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 1)
                return;

            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected abstract void Dispose(bool disposing);

        public static IDisposable Empty
        {
            get { return DefaultDisposable.Instance; }
        }

        public static IDisposable Create(Action dispose)
        {
            return new AnonymousDisposable(dispose);
        }

    }
}