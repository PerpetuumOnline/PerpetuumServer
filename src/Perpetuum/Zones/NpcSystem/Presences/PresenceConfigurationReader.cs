using System.Collections.Generic;
using System.Data;
using System.Linq;
using Perpetuum.Data;

namespace Perpetuum.Zones.NpcSystem.Presences
{
    public class PresenceConfigurationReader : IPresenceConfigurationReader
    {
        public PresenceConfiguration Get(int presenceID)
        {
            var record = Db.Query().CommandText("select * from npcpresence where id = @presenceID and enabled = 1").SetParameter("@presenceID", presenceID).ExecuteSingleRow();
            if (record == null)
                return null;

            return CreatePresenceConfigurationFromRecord(record);
        }

        public IEnumerable<PresenceConfiguration> GetAll(int zoneID)
        {
            return Db.Query().CommandText("select p.* from npcpresence p inner join zones z on p.spawnid = z.spawnid where z.id = @zoneID and p.enabled = 1")
                .SetParameter("@zoneID", zoneID)
                .Execute()
                .Select(CreatePresenceConfigurationFromRecord).ToArray();
        }

        private static PresenceConfiguration CreatePresenceConfigurationFromRecord(IDataRecord record)
        {
            var id = record.GetValue<int>("id");
            var presenceType = (PresenceType) record.GetValue<int>("presencetype");
            var topX = record.GetValue<int>("topx");
            var topY = record.GetValue<int>("topy");
            var bottomX = record.GetValue<int>("bottomx");
            var bottomY = record.GetValue<int>("bottomy");

            var p = new PresenceConfiguration(id, presenceType)
            {
                name = record.GetValue<string>("name"),
                note = record.GetValue<string>("note"),
                spawnId = record.GetValue<int?>("spawnid"),
                roaming = record.GetValue<bool>("roaming"),
                roamingRespawnSeconds = record.GetValue<int>("roamingRespawnSeconds"),
                maxRandomFlock = record.GetValue<int?>("maxrandomflock") ?? 0,
                randomCenterX = record.GetValue<int?>("randomcenterx"),
                randomCenterY = record.GetValue<int?>("randomcentery"),
                randomRadius = record.GetValue<int?>("randomradius"),
                dynamicLifeTime = record.GetValue<int?>("dynamiclifetime"),
                isRespawnAllowed = record.GetValue<bool>("isrespawnallowed"),
                area = new Area(topX, topY, bottomX, bottomY)
            };
            return p;
        }
    }
}