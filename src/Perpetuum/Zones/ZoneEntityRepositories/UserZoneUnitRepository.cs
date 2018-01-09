using System.Collections.Generic;
using Perpetuum.Data;
using Perpetuum.Units;
using Perpetuum.Units.DockingBases;

namespace Perpetuum.Zones.ZoneEntityRepositories
{
    public class UserZoneUnitRepository : ZoneUnitReader,IZoneUnitRepository
    {
        private readonly IZone _zone;

        public UserZoneUnitRepository(IZone zone,UnitHelper unitHelper) : base(unitHelper)
        {
            _zone = zone;
        }

        public void Insert(Unit unit,Position position,string syncPrefix, bool runtime)
        {
            Db.Query().CommandText("insert zoneuserentities (eid,zoneid,x,y,z,orientation) values (@eid,@zoneID,@x,@y,@z,@orientation)")
                .SetParameter("@eid",unit.Eid)
                .SetParameter("@zoneID", _zone.Id)
                .SetParameter("@x", position.X)
                .SetParameter("@y", position.Y)
                .SetParameter("@z", position.Z)
                .SetParameter("@orientation",(unit.Orientation * 255).Clamp(0,255))
                .ExecuteNonQuery().ThrowIfEqual(0, ErrorCodes.SQLInsertError);
        }

        public void Delete(Unit unit)
        {
            var res =
            Db.Query().CommandText("delete zoneuserentities where eid=@eid")
                   .SetParameter("@eid",unit.Eid)
                   .ExecuteNonQuery();
            
            res.ThrowIfEqual(0,ErrorCodes.SQLDeleteError);
        }

        public void Update(Unit unit)
        {
            //ez meg nem kellett, de lehet
        }

        public override Dictionary<Unit, Position> GetAll()
        {
            var records = Db.Query().CommandText("select * from zoneuserentities where zoneid=@zoneId")
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