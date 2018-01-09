using System.Collections.Generic;

namespace Perpetuum.Zones.NpcSystem.Presences
{
    public interface IPresenceConfigurationReader
    {
        [CanBeNull]
        PresenceConfiguration Get(int presenceID);
        
        IEnumerable<PresenceConfiguration> GetAll(int zoneID);
    }
}