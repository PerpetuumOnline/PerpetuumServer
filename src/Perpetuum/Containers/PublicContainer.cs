using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;

namespace Perpetuum.Containers
{
    /// <summary>
    /// Infinite container on docking bases
    /// </summary>
    public class PublicContainer : Container
    {
        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
                base.AcceptVisitor(visitor);
        }

        public new static PublicContainer GetOrThrow(long eid)
        {
            return (PublicContainer) Container.GetOrThrow(eid);
        }

        public static PublicContainer CreateWithRandomEID()
        {
            return (PublicContainer)Factory.CreateWithRandomEID(DefinitionNames.PUBLIC_CONTAINER);
        }
    }
}
