using System.Collections.Generic;
using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Zones;

namespace Perpetuum.RequestHandlers
{
    public class ZoneSectorList : IRequestHandler
    {
        private readonly Dictionary<string, object> _zoneSectorInfos;

        public ZoneSectorList(IZoneManager zoneManager)
        {
            _zoneSectorInfos = LoadAll(zoneManager).ToDictionary("s", ss => ss.ToDictionary());
        }

        private static IEnumerable<ZoneSector> LoadAll(IZoneManager zoneManager)
        {
            var records = Db.Query().CommandText("select id,name,sector,zoneid from zonesectors").Execute();

            foreach (var record in records)
            {
                var zoneID = record.GetValue<int>(3);

                if (!zoneManager.ContainsZone(zoneID))
                    continue;

                var sector = new ZoneSector
                {
                    id = record.GetValue<int>(0),
                    name = record.GetValue<string>(1),
                    sector = record.GetValue<byte[]>(2),
                    zoneID = zoneID
                };

                yield return sector;
            }
        }

        public void HandleRequest(IRequest request)
        {
            Message.Builder.FromRequest(request).WithData(_zoneSectorInfos).WithEmpty().Send();
        }

        private class ZoneSector
        {
            public int id;
            public int zoneID;
            public string name;
            public byte[] sector;

            public Dictionary<string, object> ToDictionary()
            {
                return new Dictionary<string, object>
                {
                    {k.ID, id},
                    {k.zoneID, zoneID},
                    {k.name, name},
                    {k.sector, sector}
                };
            }
        }
    }
}
