using System.Collections.Generic;
using Perpetuum.Threading.Process;

namespace Perpetuum.Zones.NpcSystem.Presences
{
    public interface IPresenceManager : IProcess
    {
        [CanBeNull]
        Presence CreatePresence(int presenceID);
        void AddPresence(Presence presence);
        IEnumerable<Presence> GetPresences();
    }
}