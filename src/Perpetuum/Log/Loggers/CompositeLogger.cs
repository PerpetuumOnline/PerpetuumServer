namespace Perpetuum.Log.Loggers
{
    public class CompositeLogger<T> : ILogger<T> where T : ILogEvent
    {
        private readonly ILogger<T>[] _loggers;

        public CompositeLogger(params ILogger<T>[] loggers)
        {
            _loggers = loggers;
        }

        public void Log(T logEvent)
        {
            foreach (var logger in _loggers)
            {
                logger.Log(logEvent);
            }
        }
    }
}