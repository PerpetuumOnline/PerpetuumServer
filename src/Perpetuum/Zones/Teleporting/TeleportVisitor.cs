namespace Perpetuum.Zones.Teleporting
{
    public abstract class TeleportVisitor
    {
        public virtual void VisitTeleport(Teleport teleport) { }
        public virtual void VisitMobileTeleport(MobileTeleport teleport) { }
        public virtual void VisitMobileWorldTeleport(MobileWorldTeleport teleport) { }
    }
}