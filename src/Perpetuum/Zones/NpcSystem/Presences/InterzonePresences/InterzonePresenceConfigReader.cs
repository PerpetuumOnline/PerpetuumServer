using System.Collections.Generic;
using System.Data;
using System.Linq;
using Perpetuum.Data;

namespace Perpetuum.Zones.NpcSystem.Presences.InterzonePresences
{
    public class InterzonePresenceConfigReader : IInterzonePresenceConfigurationReader
    {
        public IEnumerable<IInterzoneGroup> GetAll()
        {
            var groups = Db.Query().CommandText("SELECT id, name, respawnTime, respawnNoiseFactor FROM npcinterzonegroup").Execute().Select(CreateIZGroupFromRecord).ToArray();

            var presenceConfigs = Db.Query().CommandText("SELECT p.*, z.id as zoneID FROM npcpresence p INNER JOIN npcinterzonegroup iz ON p.izgroupid=iz.id INNER JOIN zones z ON z.spawnid=p.spawnid WHERE p.enabled=1 AND z.enabled=1")
                .Execute()
                .Select(CreatePresenceConfigurationFromRecord).ToArray();

            foreach (var group in groups)
            {
                var configs = presenceConfigs.Where(p => group.GetId() == p.InterzoneGroupId);
                group.AddConfigs(configs);
            }
            return groups;
        }

        private static IInterzoneGroup CreateIZGroupFromRecord(IDataRecord record)
        {
            var id = record.GetValue<int>("id");
            var name = record.GetValue<string>("name");
            var respawnTimeSeconds = record.GetValue<int>("respawnTime");
            var respawnNoise = record.GetValue<double>("respawnNoiseFactor");
            var g = new InterzoneGroup(id, name, respawnTimeSeconds, respawnNoise);
            return g;
        }

        private static IPresenceConfiguration CreatePresenceConfigurationFromRecord(IDataRecord record)
        {
            var id = record.GetValue<int>("id");
            var presenceType = (PresenceType)record.GetValue<int>("presencetype");
            var topX = record.GetValue<int>("topx");
            var topY = record.GetValue<int>("topy");
            var bottomX = record.GetValue<int>("bottomx");
            var bottomY = record.GetValue<int>("bottomy");

            var p = new PresenceConfiguration(id, presenceType)
            {
                Name = record.GetValue<string>("name"),
                Note = record.GetValue<string>("note"),
                SpawnId = record.GetValue<int?>("spawnid"),
                Roaming = record.GetValue<bool>("roaming"),
                RoamingRespawnSeconds = record.GetValue<int>("roamingRespawnSeconds"),
                MaxRandomFlock = record.GetValue<int?>("maxrandomflock") ?? 0,
                RandomCenterX = record.GetValue<int?>("randomcenterx"),
                RandomCenterY = record.GetValue<int?>("randomcentery"),
                RandomRadius = record.GetValue<int?>("randomradius"),
                DynamicLifeTime = record.GetValue<int?>("dynamiclifetime"),
                IsRespawnAllowed = record.GetValue<bool>("isrespawnallowed"),
                InterzoneGroupId = record.GetValue<int?>("izgroupid"),
                ZoneID = record.GetValue<int>("zoneID"),
                Area = new Area(topX, topY, bottomX, bottomY)
            };
            return p;
        }
    }
}