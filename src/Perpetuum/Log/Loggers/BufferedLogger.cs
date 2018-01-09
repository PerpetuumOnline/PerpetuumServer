using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Perpetuum.Threading;

namespace Perpetuum.Log.Loggers
{
    public abstract class BufferedLogger<T>: Disposable, ILogger<T> where T:ILogEvent
    {
        private readonly ConcurrentQueue<T> _logEvents = new ConcurrentQueue<T>();
        private Timer _autoFlushTimer;

        protected BufferedLogger()
        {
            BufferSize = 10;
            IsAsync = true;
        }

        public bool IsAsync { get; set; }

        public TimeSpan AutoFlushInterval
        {
            set
            {
                _autoFlushTimer = new Timer(x => OnFlushing(), null, value, value);            
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (_autoFlushTimer != null)
            {
                _autoFlushTimer.Dispose();
                _autoFlushTimer = null;
            }

            OnFlushing();
        }

        public int BufferSize { private get; set; }

        public virtual void Log(T logEvent)
        {
            _logEvents.Enqueue(logEvent);

            if (_logEvents.Count >= BufferSize)
            {
                OnFlushing();
            }
        }

        private int _flushing;

        private void OnFlushing()
        {
            if ( Interlocked.CompareExchange(ref _flushing,1,0) == 1)
                return;

            var logEvents = _logEvents.TakeAll().ToArray();

            if (logEvents.Length == 0)
            {
                _flushing = 0;
                return;
            }

            if (IsAsync)
            {
                Task.Run(() => Flush(logEvents)).ContinueWith(t => { _flushing = 0; });
            }
            else
            {
                try
                {
                    Flush(logEvents);
                }
                finally
                {
                    _flushing = 0;
                }
            }
        }

        protected abstract void Flush(T[] logEvents);
    }
}