using System.Diagnostics.CodeAnalysis;

namespace Perpetuum.EntityFramework
{
    public interface IEntityVisitor { }

    [SuppressMessage("ReSharper", "TypeParameterCanBeVariant")]
    public interface IEntityVisitor<T> : IEntityVisitor where T : Entity
    {
        void Visit(T entity);
    }
}