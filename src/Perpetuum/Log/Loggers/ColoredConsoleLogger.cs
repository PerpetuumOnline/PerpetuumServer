using System;

namespace Perpetuum.Log.Loggers
{
    public class ColoredConsoleLogger : ConsoleLogger<LogEvent>
    {
        private readonly ConsoleColor _defaultColor = Console.ForegroundColor;

        public ColoredConsoleLogger(ILogEventFormatter<LogEvent,string> formatter) : base(formatter)
        {
        }

        public override void Log(LogEvent logEvent)
        {
            try
            {
                switch (logEvent.LogType)
                {
                    case LogType.Warning:
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        break;
                    }
                    case LogType.Error:
                    {
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.Beep(4000, 30);
                            break;
                        }
                    }
                }

                base.Log(logEvent);
            }
            finally
            {
                Console.ForegroundColor = _defaultColor;
            }
        }
    }
}