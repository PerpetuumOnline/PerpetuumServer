using System.Collections.Generic;
using System.Data;
using System.Linq;
using Perpetuum.Data;

namespace Perpetuum.Zones.NpcSystem.Presences
{
    public class PresenceConfigurationReader : IPresenceConfigurationReader
    {
        public IPresenceConfiguration Get(int presenceID)
        {
            var record = Db.Query().CommandText("select * from npcpresence where id = @presenceID and enabled = 1 and izgroupid IS NULL").SetParameter("@presenceID", presenceID).ExecuteSingleRow();
            if (record == null)
                return null;

            return CreatePresenceConfigurationFromRecord(record);
        }

        public IEnumerable<IPresenceConfiguration> GetAll(int zoneID)
        {
            return Db.Query().CommandText("select p.* from npcpresence p inner join zones z on p.spawnid = z.spawnid where z.id = @zoneID and p.enabled = 1 and p.izgroupid IS NULL")
                .SetParameter("@zoneID", zoneID)
                .Execute()
                .Select(CreatePresenceConfigurationFromRecord).ToArray();
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
                Area = new Area(topX, topY, bottomX, bottomY)
            };
            return p;
        }
    }
}