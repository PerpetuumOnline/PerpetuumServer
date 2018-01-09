using System;

namespace Perpetuum.Log
{
    public class LogEventBase : ILogEvent
    {
        public DateTime Timestamp { get; private set; }

        protected LogEventBase()
        {
            Timestamp = DateTime.Now;
        }
    }
    
    public class LogEvent : LogEventBase
    {
        public LogType LogType { get; set; }
        public string Tag { get; set; }
        public string Message { get; set; }
        public Exception ThrownException { get; set; }

        public LogEvent()
        {
            LogType = LogType.None;
        }
    }
}