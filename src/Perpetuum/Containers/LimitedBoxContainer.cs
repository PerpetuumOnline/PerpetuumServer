using Perpetuum.EntityFramework;

namespace Perpetuum.Containers
{
    /// <summary>
    /// A container with limited capacity
    /// </summary>
    public class LimitedBoxContainer : LimitedCapacityContainer
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
    }
}
