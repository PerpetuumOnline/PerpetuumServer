using System.Collections.Generic;
using Perpetuum.EntityFramework;
using Perpetuum.Units;

namespace Perpetuum.Zones.Teleporting
{
    public abstract class Teleport : Unit
    {
        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
                base.AcceptVisitor(visitor);
        }

        public virtual void AcceptVisitor(TeleportVisitor visitor)
        {
            visitor.VisitTeleport(this);
        }

        public override bool IsLockable
        {
            get { return false; }
        }

        public virtual bool IsEnabled
        {
            get { return true; }
            set {  }
        }

        public static int TeleportRange
        {
            get { return (int) DistanceConstants.MOBILE_TELEPORT_USE_RANGE; }
        }

        public abstract IEnumerable<TeleportDescription> GetTeleportDescriptions();

        public override Dictionary<string,object> ToDictionary()
        {
            var result = base.ToDictionary();
            result.Add(k.enabled,IsEnabled);
            return result;
        }
    }
}