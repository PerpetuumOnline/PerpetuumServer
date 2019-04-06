using Perpetuum.Data;
using Perpetuum.Zones;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Perpetuum.Services.Relics
{

    public class RelicZoneConfigRepository
    {
        private RelicZoneConfigReader _relicZoneConfigReader;
        private IZone _zone;

        public RelicZoneConfigRepository(IZone zone)
        {
            _relicZoneConfigReader = new RelicZoneConfigReader(zone);
            _zone = zone;
        }

        public RelicZoneConfig GetZoneConfig()
        {
            return _relicZoneConfigReader.GetZoneRelicConfiguration();
        }

    }


    public class RelicZoneConfigReader
    {

        private IZone _zone;

        public RelicZoneConfigReader(IZone zone)
        {
            _zone = zone;
        }

        protected RelicZoneConfig CreateRelicZoneConfigFromRecord(IDataRecord record)
        {
            var id = record.GetValue<int>("id");
            var zoneid = record.GetValue<int>("zoneid");
            var maxspawn = record.GetValue<int>("maxspawn");
            var respawnrate = record.GetValue<int>("respawnrate");

            var config = new RelicZoneConfig(zoneid, maxspawn, TimeSpan.FromSeconds(respawnrate));

            return config;
        }

        public RelicZoneConfig GetZoneRelicConfiguration()
        {
            var relicZoneConfigs = Db.Query().CommandText("SELECT TOP 1 id, zoneid, maxspawn, respawnrate FROM reliczoneconfig WHERE zoneid = @zoneId")
                .SetParameter("@zoneId", _zone.Id)
                .Execute()
                .Select(CreateRelicZoneConfigFromRecord);

            return relicZoneConfigs.SingleOrDefault();
        }
    }

    public class RelicZoneConfig
    {
        private int _zoneid;
        private int _max;
        private TimeSpan _refreshRate;

        public RelicZoneConfig(int zoneid, int max, TimeSpan refreshRate)
        {
            _zoneid = zoneid;
            _max = max;
            _refreshRate = refreshRate;
        }

        public TimeSpan GetTimeSpan()
        {
            return _refreshRate;
        }

        public int GetMax()
        {
            return _max;
        }
    }
}
