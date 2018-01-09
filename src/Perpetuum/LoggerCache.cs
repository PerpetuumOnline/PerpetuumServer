using System;
using System.Runtime.Caching;
using Perpetuum.Log;

namespace Perpetuum
{
    public interface ILoggerCache
    {
        T GetOrAddLogger<T>(string loggerName, Func<T> loggerFactory) where T : ILogger;
    }

    public class LoggerCache : ILoggerCache
    {
        private readonly ObjectCache _cache;

        public LoggerCache(ObjectCache cache)
        {
            _cache = cache;
        }

        public TimeSpan Expiration { private get; set; }

        T ILoggerCache.GetOrAddLogger<T>(string loggerName, Func<T> loggerFactory)
        {
            return _cache.Get(loggerName,loggerFactory,Expiration);
        }
    }
}