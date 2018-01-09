using System;

namespace Perpetuum.Threading
{
    internal class AnonymousDisposable : Disposable
    {
        private readonly Action _dispose;

        public AnonymousDisposable(Action dispose)
        {
            _dispose = dispose;
        }

        protected override void Dispose(bool disposing)
        {
            _dispose();
        }
    }
}