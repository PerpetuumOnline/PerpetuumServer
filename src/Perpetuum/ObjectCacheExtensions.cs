using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.Caching;
using Perpetuum.Log;

namespace Perpetuum
{
    public static class ObjectCacheExtensions
    {
        [CanBeNull]
        public static T Get<T>(this ObjectCache cache, string key, Func<T> valueFactory, TimeSpan? expiration = null)
        {
            var result = cache.Get(key);

            if (result == null)
            {
                result = valueFactory();

                if (result == null)
                    return default(T);

                Set(cache,key,result,expiration);
            }

            return (T)result;
        }

        public static void Set(this ObjectCache objectCache, string key, object value, TimeSpan? expiration = null)
        {
            var policy = new CacheItemPolicy
                {
                    RemovedCallback = HandleRemovedCacheItem
                };

            if (expiration == null)
            {
                policy.AbsoluteExpiration = ObjectCache.InfiniteAbsoluteExpiration;
            }
            else
            {
                policy.SlidingExpiration = (TimeSpan) expiration;
            }

            objectCache.Set(key,value,policy);

            Logger.Info($"Cache set. Name = {objectCache.Name} ({key} = {value}) expiration = {(expiration == null ? "never" : DateTime.Now.Add(policy.SlidingExpiration).ToString(CultureInfo.InvariantCulture))}");
        }

        [CanBeNull]
        public static T GetWithAbsoluteExpiration<T>(this ObjectCache cache, string key, Func<T> valueFactory, TimeSpan expiration)
        {
            var result = cache.Get(key);

            if (result == null)
            {
                result = valueFactory();

                if (result == null)
                    return default(T);

                SetWithAbsoluteExpiration(cache, key, result, expiration);
            }

            return (T)result;
        }


        private static void SetWithAbsoluteExpiration(this ObjectCache objectCache, string key, object value, TimeSpan expiration)
        {
            var policy = new CacheItemPolicy
            {
                RemovedCallback = HandleRemovedCacheItem,
                AbsoluteExpiration = new DateTimeOffset(DateTime.Now.Add(expiration))
            };

            objectCache.Set(key, value, policy);

            Logger.Info($"Cache set. Name = {objectCache.Name} ({key} = {value}) expiration = {(expiration == null ? "never" : policy.AbsoluteExpiration.ToString(CultureInfo.InvariantCulture))}");
        }

        private static void HandleRemovedCacheItem(CacheEntryRemovedArguments removedArguments)
        {
            var disposable = removedArguments.CacheItem.Value as IDisposable;
            disposable?.Dispose();

            Logger.Info($"Cache remove. Name = {removedArguments.Source.Name} ({removedArguments.CacheItem.Key} = {removedArguments.CacheItem.Value}) reason = {removedArguments.RemovedReason}");
        }

        public static void Clear(this ObjectCache cache)
        {
            Debug.Assert(cache != null);

            var list = cache.Select(kvp => kvp.Key).ToList();

            foreach (var key in list)
            {
                cache.Remove(key);
            }
        }
    }
}
