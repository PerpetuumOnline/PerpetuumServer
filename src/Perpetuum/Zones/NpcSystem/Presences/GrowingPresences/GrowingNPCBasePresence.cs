using Perpetuum.StateMachines;
using Perpetuum.Zones.NpcSystem.Presences.ExpiringStaticPresence;
using System.Linq;

namespace Perpetuum.Zones.NpcSystem.Presences.GrowingPresences
{
    public class GrowingNPCBasePresence : GrowingPresence
    {
        public GrowingNPCBasePresence(IZone zone, IPresenceConfiguration configuration, IEscalatingPresenceFlockSelector selector) : base(zone, configuration, selector) { }
        protected override void InitStateMachine()
        {
            CurrentGrowthLevel = FastRandom.NextInt(9);
            StackFSM = new StackFSM();
            StackFSM.Push(new NPCBaseSpawnState(this));
        }
    }

    public static class NPCBasePresenceUtils
    {
        public static bool WithinRangeOfNPCBase(IZone zone, Position from, double range = DistanceConstants.PBS_DIST_FROM_NPC_BASE)
        {
            return zone.PresenceManager.GetPresences()
                .OfType<GrowingNPCBasePresence>()
                .Where(p => p.Members.Count() > 0)
                .Select(p => p.SpawnOrigin)
                .Any(pos => pos.TotalDistance2D(from) < range);
        }
    }
}
