namespace Perpetuum.Zones.NpcSystem.Presences
{
    public interface IRandomFlockReader
    {
        RandomFlockInfo[] GetByPresence(Presence presence);
    }
}