using System;
using System.Diagnostics;
using Perpetuum.Log.Formatters;
using Perpetuum.Log.Loggers;

namespace Perpetuum.Log
{
    public static class Logger
    {
        public static ILogger<LogEvent> Current { private get; set; }

        static Logger()
        {
            Current = new ColoredConsoleLogger(new DefaultLogEventFormatter());
        }

        [Conditional("DEBUG")]
        public static void DebugInfo(string message)
        {
            Info(message);
        }

        public static void Info(string message)
        {
            var e = new LogEvent
            {
                LogType = LogType.Info,
                Message = message
            };

            Log(e);
        }

        [Conditional("DEBUG")]
        public static void DebugWarning(string message)
        {
            Warning(message);
        }

        public static void Warning(string message)
        {
            var e = new LogEvent
            {
                LogType = LogType.Warning,
                Message = message
            };

            Log(e);
        }

        public static void Error(string message)
        {
            var logEvent = new LogEvent
            {
                LogType = LogType.Error,
                Message = message
            };

            Log(logEvent);
        }

        public static void Exception(Exception ex)
        {
            var logEvent = new LogEvent
            {
                LogType = LogType.Error,
                ThrownException = ex
            };

            Current.Log(logEvent);
        }

        public static void Log(LogEvent logEvent)
        {
            Current.Log(logEvent);
        }
    }
}