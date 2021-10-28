using Perpetuum.Data;
using Perpetuum.Zones;
using System.Data;
using System.Linq;

namespace Perpetuum.Services.RiftSystem
{

    public class ZoneRiftConfigRepository
    {
        private ZoneRiftConfigReader _zoneRiftConfigReader;
        private IZone _zone;

        public ZoneRiftConfigRepository(IZone zone)
        {
            _zoneRiftConfigReader = new ZoneRiftConfigReader(zone);
            _zone = zone;
        }

        public ZoneRiftConfig GetZoneConfig()
        {
            return _zoneRiftConfigReader.GetForZone();
        }

    }

    /// <summary>
    /// DB query for ZoneRiftConfigs
    /// </summary>
    public class ZoneRiftConfigReader
    {
        private IZone _zone;

        public ZoneRiftConfigReader(IZone zone) 
        {
            _zone = zone;
        }
        protected ZoneRiftConfig CreateZoneRiftConfigFromRecord(IDataRecord record) 
        {
            var id = record.GetValue<int>("id");
            var zoneid = record.GetValue<int>("zoneid");
            var maxrifts = record.GetValue<int>("maxrifts");
            var maxLevel = record.GetValue<int>("maxlevel");

            var config = new ZoneRiftConfig(zoneid, maxrifts, maxLevel);

            return config;
        }
        /// <summary>
        /// Get the ZoneRiftConfig for a Zone
        /// </summary>
        /// <param name="zone">IZone</param>
        /// <returns>ZoneRiftConfig</returns>
        public ZoneRiftConfig GetForZone()
        {
            var riftConfigs = Db.Query().CommandText("SELECT TOP 1 id, zoneid, maxrifts, maxlevel FROM zoneriftsconfig WHERE zoneid = @zoneId")
                .SetParameter("@zoneId", _zone.Id)
                .Execute()
                .Select(CreateZoneRiftConfigFromRecord);

            return riftConfigs.FirstOrDefault();
        }
    }

    /// <summary>
    /// Zone specs for number of rifts and TAP level
    /// </summary>
    public class ZoneRiftConfig
    {
        private int _zoneid { get; set; }
        private int _maxRifts { get; set; }
        private int _maxLevel { get; set; }
        public ZoneRiftConfig(int zoneid, int maxRifts, int maxLevel)
        {
            _zoneid = zoneid;
            _maxRifts = maxRifts;
            _maxLevel = maxLevel;
        }

        public int GetMaxRifts() {
            return _maxRifts;
        }

        public int GetMaxLevel() {
            return _maxLevel;
        }
    }
}

