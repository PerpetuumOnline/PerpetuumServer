using Perpetuum.Data;
using Perpetuum.Zones;
using System.Linq;

namespace Perpetuum.Services.RiftSystem
{
    /// <summary>
    /// DB query for ZoneRiftConfigs
    /// </summary>
    public static class ZoneRiftConfigReader
    {
        /// <summary>
        /// Get the ZoneRiftConfig for a Zone
        /// </summary>
        /// <param name="zone">IZone</param>
        /// <returns>ZoneRiftConfig</returns>
        public static ZoneRiftConfig GetForZone(IZone zone)
        {
            var riftConfigs = Db.Query().CommandText("SELECT TOP 1 id, zoneid, maxrifts, maxlevel FROM zoneriftsconfig WHERE zoneid = @zoneId")
                .SetParameter("@zoneId", zone.Id)
                .Execute()
                .Select((record) =>
                {
                    var maxRifts = record.GetValue<int>("maxrifts");
                    var maxLevel = record.GetValue<int>("maxlevel");
                    return new ZoneRiftConfig(zone, maxRifts, maxLevel);
                });

            return riftConfigs.FirstOrDefault();
        }
    }

    /// <summary>
    /// Zone specs for number of rifts and TAP level
    /// </summary>
    public class ZoneRiftConfig
    {
        public IZone Zone { get; private set; }
        public int MaxRifts { get; private set; }
        public int MaxLevel { get; private set; }
        public ZoneRiftConfig(IZone zone, int maxRifts, int maxLevel)
        {
            Zone = zone;
            MaxRifts = maxRifts;
            MaxLevel = maxLevel;
        }
    }
}

