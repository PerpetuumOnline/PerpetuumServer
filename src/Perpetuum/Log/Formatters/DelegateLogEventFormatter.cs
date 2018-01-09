using System;

namespace Perpetuum.Log.Formatters
{
    public class DelegateLogEventFormatter<T,TOut> : ILogEventFormatter<T,TOut> where T : ILogEvent
    {
        private readonly Func<T,TOut> _formater;

        public DelegateLogEventFormatter(Func<T,TOut> formater)
        {
            _formater = formater;
        }

        public TOut Format(T logEvent)
        {
            return _formater(logEvent);
        }
    }
}