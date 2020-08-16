using Perpetuum.Data;
using System.Collections.Generic;

namespace Perpetuum.Zones.Teleporting
{
    public interface IMobileTeleportToZoneMap
    {
        List<int> GetDestinationZones(int definition);
    }

    public class MobileTeleportZoneMapCache : IMobileTeleportToZoneMap
    {
        private readonly IDictionary<int, List<int>> _cache;
        public MobileTeleportZoneMapCache()
        {
            _cache = MobileTeleportToZoneReader.GetAll();
        }

        public List<int> GetDestinationZones(int definition)
        {
            _cache.TryGetValue(definition, out List<int> zones);
            return zones;
        }
    }

    public static class MobileTeleportToZoneReader
    {
        public static IDictionary<int, List<int>> GetAll()
        {
            var dict = new Dictionary<int, List<int>>();
            var entries = Db.Query().CommandText("SELECT sourcedefinition, zoneid FROM zoneteleportdevicemap")
                .Execute();
            foreach (var entry in entries)
            {
                var def = entry.GetValue<int>("sourcedefinition");
                var zoneid = entry.GetValue<int>("zoneid");
                dict.AddOrUpdate(def, new List<int>() { zoneid }, (listOfZones) => {
                    listOfZones.Add(zoneid);
                    return listOfZones;
                });
            }
            return dict;
        }

    }
}
