using System.Collections.Generic;

namespace Perpetuum.Zones.NpcSystem.Presences.InterzonePresences
{
    public interface IInterzonePresenceConfigurationReader
    {
        IEnumerable<InterzoneGroup> GetAll();
    }
}
