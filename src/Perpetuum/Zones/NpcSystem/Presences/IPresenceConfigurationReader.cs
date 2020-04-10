using System.Collections.Generic;

namespace Perpetuum.Zones.NpcSystem.Presences
{
    public interface IPresenceConfigurationReader
    {
        [CanBeNull]
        IPresenceConfiguration Get(int presenceID);
        
        IEnumerable<IPresenceConfiguration> GetAll(int zoneID);
    }
}