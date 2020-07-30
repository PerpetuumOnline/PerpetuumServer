using System.Collections.Generic;
using Perpetuum.EntityFramework;

namespace Perpetuum.Comparers
{
    public class EntityComparer : IEqualityComparer<IEntity>
    {
        public EntityComparer()
        {
        }

        public bool Equals(IEntity left, IEntity right)
        {
            // If they're both null, technically they're the same...
            if (ReferenceEquals(left, right)) return true;

            if (left is null || right is null) return false;

            return left.Eid == right.Eid;
        }

        public int GetHashCode(IEntity obj)
        {
            if (obj is null) return 0;

            return obj.Eid.GetHashCode();
        }
    }
}
