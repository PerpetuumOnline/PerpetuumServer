using System;
using System.Collections.Generic;
using System.Runtime.Caching;

namespace Perpetuum
{
    public class CachedReadOnlyRepository<TId,T> : IReadOnlyRepository<TId,T>
    {
        private readonly ObjectCache _cache;
        private readonly IReadOnlyRepository<TId, T> _repository;

        public TimeSpan Expiration { get; set; }

        public CachedReadOnlyRepository(ObjectCache cache,IReadOnlyRepository<TId,T> repository)
        {
            _cache = cache;
            _repository = repository;
            Expiration = TimeSpan.FromHours(1);
        }

        public T Get(TId id)
        {
            return _cache.Get(id.ToString(), () => _repository.Get(id),Expiration);
        }

        public IEnumerable<T> GetAll()
        {
            throw new NotImplementedException();
        }

        public void Remove(TId id)
        {
            _cache.Remove(id.ToString());
        }
    }
}