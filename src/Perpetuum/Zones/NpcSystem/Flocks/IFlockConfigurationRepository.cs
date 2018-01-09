using System.Collections.Generic;
using System.Linq;
using Perpetuum.Zones.NpcSystem.Presences;

namespace Perpetuum.Zones.NpcSystem.Flocks
{
    public interface IFlockConfigurationRepository : IRepository<int,IFlockConfiguration>
    {
    }

    public static class FlockConfigurationExtensions
    {
        public static IEnumerable<IFlockConfiguration> GetAllByPresence(this IFlockConfigurationRepository repository,Presence presence)
        {
            return repository.GetAll().Where(t => t.PresenceID == presence.Configuration.ID);
        }

        public static IEnumerable<IFlockConfiguration> GetAllByPresence(this IFlockConfigurationRepository repository,int presenceID)
        {
            return repository.GetAll().Where(t => t.PresenceID == presenceID);
        }
    }

}