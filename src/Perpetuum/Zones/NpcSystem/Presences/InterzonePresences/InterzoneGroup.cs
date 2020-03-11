using System.Collections.Generic;

namespace Perpetuum.Zones.NpcSystem.Presences.InterzonePresences
{
    public class InterzoneGroup
    {
        public  int id;
        public string name;
        private List<IPresenceConfiguration> presences = new List<IPresenceConfiguration>();

        public void AddConfigs(IEnumerable<IPresenceConfiguration> presences)
        {
            this.presences.AddMany(presences);
        }

        public List<IPresenceConfiguration> GetAll()
        {
            return this.presences;
        }

        public IPresenceConfiguration GetRandom()
        {
            return presences.RandomElement();
        }
    }
}
