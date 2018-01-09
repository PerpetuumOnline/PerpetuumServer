using System;

namespace Perpetuum.Log.Loggers
{
    public class DelegateLogger<T> : ILogger<T> where T : ILogEvent
    {
        private readonly Action<T> _logger;

        public DelegateLogger(Action<T> logger)
        {
            _logger = logger;
        }

        public void Log(T logEvent)
        {
            _logger(logEvent);
        }
    }
}