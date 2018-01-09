using System.Collections.Generic;
using Perpetuum.Data;
using Perpetuum.EntityFramework;

namespace Perpetuum.Containers.SystemContainers
{
    public class SystemContainer : Container
    {
        private static readonly IDictionary<string, long> _entityStorage;

        static SystemContainer()
        {
            _entityStorage = Database.CreateCache<string, long>("entitystorage", "storage_name", "eid");
        }

        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
                base.AcceptVisitor(visitor);
        }

        public static SystemContainer GetByName(string name)
        {
            var eid = _entityStorage[name];
            return (SystemContainer) Repository.LoadOrThrow(eid);
        }
    }
}