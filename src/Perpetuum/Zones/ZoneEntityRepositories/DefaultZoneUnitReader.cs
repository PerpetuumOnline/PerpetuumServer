using System.Collections.Generic;
using Perpetuum.Data;
using Perpetuum.Units;
using Perpetuum.Units.DockingBases;

namespace Perpetuum.Zones.ZoneEntityRepositories
{
    public class DefaultZoneUnitReader : ZoneUnitReader
    {
        private readonly IZone _zone;

        public DefaultZoneUnitReader(IZone zone,UnitHelper unitHelper) : base(unitHelper)
        {
            _zone = zone;
        }

        public override Dictionary<Unit, Position> GetAll()
        {
            var records = Db.Query().CommandText("select * from zoneentities where zoneid = @zoneId and enabled=1")
                .SetParameter("@zoneId", _zone.Id)
                .Execute();

            var result = new Dictionary<Unit, Position>();
            foreach (var record in records)
            {
                var unit = CreateUnit(record);
                if ( unit == null )
                    continue;

                var x = record.GetValue<double>("x");
                var y = record.GetValue<double>("y");
                var p = new Position(x, y);

                result.Add(unit,p);
            }

            return result;
        }
    }
}