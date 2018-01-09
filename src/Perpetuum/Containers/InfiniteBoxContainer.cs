using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;

namespace Perpetuum.Containers
{
    /// <summary>
    /// Infinite container to group items
    /// </summary>
    public class InfiniteBoxContainer : Container
    {
        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
                base.AcceptVisitor(visitor);
        }

        public override bool IsTrashable
        {
            get { return true; }
        }

        public static InfiniteBoxContainer Create()
        {
            return (InfiniteBoxContainer)Factory.CreateWithRandomEID(DefinitionNames.INFINITE_CAPACITY_BOX_CONTAINER);
        }
    }
}
