using System.Collections.Generic;
using System.Drawing;

namespace Perpetuum.Zones.NpcSystem.SafeSpawnPoints
{
    public struct SafeSpawnPoint
    {
        public int Id { get; set; }
        public int ZoneId { private get; set; }
        public Point Location { get; set; }

        public IDictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
            {
                {"ID", Id}, 
                {"zoneID", ZoneId},
                {"x", Location.X}, 
                {"y", Location.Y}
            };
        }
    }
}