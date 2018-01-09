using Perpetuum.Data;
using Perpetuum.Units;
using Perpetuum.Units.DockingBases;

namespace Perpetuum.Zones.ZoneEntityRepositories
{
    public class DefaultZoneUnitRepository : DefaultZoneUnitReader,IZoneUnitRepository
    {
        private readonly IZone _zone;

        public DefaultZoneUnitRepository(IZone zone,UnitHelper unitHelper) : base(zone,unitHelper)
        {
            _zone = zone;
        }

        public void Insert(Unit unit, Position position,string syncPrefix = null,bool runtime=false)
        {
            Db.Query().CommandText(@"insert zoneentities (zoneID,eid, definition, x,y,z, orientation, enabled,runtime, synckey,ename) Values
                                  (@zoneID,@eid,@definition,@x,@y,@z,@orientation,1,@runtime,@synckey,@name)")
                .SetParameter("@zoneID",_zone.Id)
                .SetParameter("@eid", unit.Eid)
                .SetParameter("@definition", runtime ? unit.Definition : (object)null)
                .SetParameter("@x", position.X)
                .SetParameter("@y", position.Y)
                .SetParameter("@z", position.Z)
                .SetParameter("@orientation",unit.Orientation * byte.MaxValue)
                .SetParameter("@synckey", syncPrefix + "_" + FastRandom.NextString(7))
                .SetParameter("@name",unit.Name)
                .SetParameter("@runtime", runtime)
                .ExecuteNonQuery().ThrowIfEqual(0, ErrorCodes.SQLInsertError);
        }

        public void Delete(Unit unit)
        {
            Db.Query().CommandText("delete zoneentities where eid=@EID")
                .SetParameter("@EID", unit.Eid)
                .ExecuteNonQuery().ThrowIfEqual(0, ErrorCodes.SQLDeleteError);
        }

        public void Update(Unit unit)
        {
            var res=
            Db.Query().CommandText("update zoneentities set x=@x,y=@y,orientation=@orientation where eid=@eid")
                .SetParameter("@eid", unit.Eid)
                .SetParameter("@x", unit.CurrentPosition.X)
                .SetParameter("@y", unit.CurrentPosition.Y)
                .SetParameter("@orientation", unit.Orientation * byte.MaxValue)
                .ExecuteNonQuery();

            (res==1).ThrowIfFalse(ErrorCodes.SQLUpdateError);

        }
    }
}