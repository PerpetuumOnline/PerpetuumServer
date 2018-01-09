namespace Perpetuum.EntityFramework
{
    public static class EntityRepositoryExtensions
    {
        [NotNull]
        public static Entity LoadOrThrow(this IEntityRepository repository,long eid)
        {
            return repository.Load(eid).ThrowIfNull(ErrorCodes.EntityNotFound);
        }

        public static void DeleteTree(this IEntityRepository repository,long eid)
        {
            var entity = repository.LoadTree(eid, null).ThrowIfNull(ErrorCodes.EntityNotFound);
            repository.Delete(entity);
        }

        public static void ForceUpdate(this IEntityRepository repository, Entity entity)
        {
            foreach (var child in entity.Children)
            {
                repository.ForceUpdate(child);
            }

            repository.Update(entity);
        }
    }
}