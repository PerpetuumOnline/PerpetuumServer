using System;
using System.Text;

namespace Perpetuum.Log.Formatters
{
    public class DefaultLogEventFormatter : ILogEventFormatter<LogEvent,string>
    {
        public string Format(LogEvent logEvent)
        {
            var tag = string.IsNullOrEmpty(logEvent.Tag) ? " " : $" [{logEvent.Tag}] ";

            var logType = "";

            switch (logEvent.LogType)
            {
                case LogType.Info:
                {
                    logType = "INF";
                    break;
                }
                case LogType.Warning:
                {
                    logType = "WRN";
                    break;
                }
                case LogType.Error:
                {
                    logType = "ERR";
                    break;
                }
            }

            var sb = new StringBuilder();

            sb.AppendFormat("[{0}] {1}{2}{3}", logEvent.Timestamp.ToString("HH:mm:ss"), logType, tag, logEvent.Message);

            if (logEvent.ThrownException != null)
            {
                CreateExceptionString(sb,logEvent.ThrownException);
            }

            return sb.ToString();
        }

        private static void CreateExceptionString(StringBuilder sb, Exception e, string indent = null)
        {
            if (indent == null)
            {
                indent = string.Empty;
            }
            else if (indent.Length > 0)
            {
                sb.AppendFormat("{0}", indent);
            }

            sb.AppendLine();
            sb.AppendFormat("Exception Found:\n{0}Type: {1}", indent, e.GetType().FullName);
            sb.AppendFormat("\n{0}Message: {1}", indent, e.Message);
            sb.AppendFormat("\n{0}Source: {1}", indent, e.Source);
            sb.AppendFormat("\n{0}Stacktrace: {1}", indent, e.StackTrace);

            var aex = e as AggregateException;
            aex?.Handle(ie =>
            {
                CreateExceptionString(sb, ie, "Aggregate");
                return true;
            });

            if (e.InnerException == null)
                return;

            CreateExceptionString(sb, e.InnerException, "Inner");
        }
    }
}