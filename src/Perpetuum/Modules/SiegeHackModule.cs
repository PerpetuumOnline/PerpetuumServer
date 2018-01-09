using Perpetuum.Players;
using Perpetuum.Zones.Intrusion;
using Perpetuum.Zones.Locking.Locks;

namespace Perpetuum.Modules
{
    public class SiegeHackModule : ActiveModule
    {
        public SiegeHackModule() : base(true)
        {
        }

        protected override void OnAction()
        {
            var unitLock = GetLock().ThrowIfNotType<UnitLock>(ErrorCodes.InvalidLockType);

            var activeHackingSAP = unitLock.Target as ActiveHackingSAP;
            activeHackingSAP?.OnModuleUse((Player) ParentRobot);
        }
    }
}