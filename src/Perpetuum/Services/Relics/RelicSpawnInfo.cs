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

    public class RelicSpawnInfoRepository
    {
        private RelicSpawnInfoReader _relicSpawnInfoReader;
        private IZone _zone;

        public RelicSpawnInfoRepository(IZone zone)
        {
            _relicSpawnInfoReader = new RelicSpawnInfoReader(zone);
            _zone = zone;
        }

        public IEnumerable<RelicSpawnInfo> GetAll()
        {
            return _relicSpawnInfoReader.GetAll();
        }

    }


    public class RelicSpawnInfoReader
    {

        private IZone _zone;

        public RelicSpawnInfoReader(IZone zone)
        {
            _zone = zone;
        }

        protected RelicSpawnInfo CreateRelicSpawnInfoFromRecord(IDataRecord record)
        {
            var id = record.GetValue<int>("id");
            var relicinfoid = record.GetValue<int>("relictypeid");
            var zoneid = record.GetValue<int>("zoneid");
            var rate = record.GetValue<int>("rate");
            var x = record.GetValue<int?>("x");
            var y = record.GetValue<int?>("y");

            var info = RelicInfo.GetByIDFromDB(relicinfoid);

            var config = new RelicSpawnInfo(info, _zone, rate, x, y);

            return config;
        }

        public IEnumerable<RelicSpawnInfo> GetAll()
        {
            var relicZoneConfigs = Db.Query().CommandText("SELECT id, relictypeid, zoneid, rate, x, y FROM relicspawninfo WHERE zoneid = @zoneId")
                .SetParameter("@zoneId", _zone.Id)
                .Execute()
                .Select(CreateRelicSpawnInfoFromRecord);

            return relicZoneConfigs.ToList();
        }
    }


    public class RelicSpawnInfo
    {
        private RelicInfo _info;
        private IZone _zone;
        private int _rate;
        private int? _x;
        private int? _y;


        public RelicSpawnInfo(RelicInfo info, IZone zone, int rate, int? x, int? y)
        {
            _info = info;
            _zone = zone;
            _rate = rate;
            _x = x;
            _y = y;
            if (HasPosition())
            {
                this._info.SetPosition(GetPosition());
            }
        }

        public int GetRate()
        {
            return _rate;
        }

        public RelicInfo GetRelicInfo()
        {

            return this._info;
        }

        public bool HasPosition()
        {
            return _x != null && _y != null;
        }

        public Position GetPosition()
        {
            double x = _x ?? 0;
            double y = _y ?? 0;
            return new Position(x, y);
        }

    }
}
