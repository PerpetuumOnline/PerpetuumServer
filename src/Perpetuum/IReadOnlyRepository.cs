using System.Collections.Generic;

namespace Perpetuum
{
    public interface IReadOnlyRepository<out T> : IReadOnlyRepository<int,T>
    {
    }

    public interface IReadOnlyRepository<in TId,out T>
    {
        T Get(TId id);
        IEnumerable<T> GetAll();
    }

    public static class ReadOnlyRepositoryExtensions
    {
        public static bool TryGet<TId, T>(this IReadOnlyRepository<TId,T> repository,TId id,out T item)
        {
            item = repository.Get(id);
            return !Equals(item,default(T));
        }
    }

    public interface IRepository<in TId,T> : IReadOnlyRepository<TId,T>
    {
        void Insert(T item);
        void Update(T item);
        void Delete(T item);
    }
}