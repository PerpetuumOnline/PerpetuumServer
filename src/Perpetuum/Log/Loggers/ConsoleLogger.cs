using System;

namespace Perpetuum.Log.Loggers
{
    public class ConsoleLogger<T> : ILogger<T> where T : ILogEvent
    {
        private readonly ILogEventFormatter<T,string> _formatter;

        public ConsoleLogger(ILogEventFormatter<T,string> formatter)
        {
            _formatter = formatter;
        }

        public virtual void Log(T logEvent)
        {
            Console.Out.WriteLine(_formatter.Format(logEvent));
        }
    }
}