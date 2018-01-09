using Perpetuum.Zones.NpcSystem.Flocks;

namespace Perpetuum.Zones.NpcSystem.Presences
{
    public interface IRandomFlockSelector
    {
        IFlockConfiguration SelectRandomFlockByPresence(RandomPresence presence);
    }
}