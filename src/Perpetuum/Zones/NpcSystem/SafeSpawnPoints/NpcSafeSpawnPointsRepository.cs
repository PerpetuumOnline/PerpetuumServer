using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Perpetuum.Data;

namespace Perpetuum.Zones.NpcSystem.SafeSpawnPoints
{
    public class NpcSafeSpawnPointsRepository : ISafeSpawnPointsRepository
    {
        private readonly IZone _zone;

        public NpcSafeSpawnPointsRepository(IZone zone)
        {
            _zone = zone;
        }

        public void Add(SafeSpawnPoint point)
        {
            Db.Query().CommandText("insert npcsafespawnpoints (zoneid,x,y) values (@zoneId,@x,@y)")
                .SetParameter("@zoneId", _zone.Id)
                .SetParameter("@x", point.Location.X)
                .SetParameter("@y", point.Location.Y)
                .ExecuteNonQuery().ThrowIfEqual(0, ErrorCodes.SQLInsertError);
        }

        public void Update(SafeSpawnPoint point)
        {
            Db.Query().CommandText("update npcsafespawnpoints set x=@x,y=@y where id=@id")
                .SetParameter("@id", point.Id)
                .SetParameter("@x", point.Location.X)
                .SetParameter("@y", point.Location.Y)
                .ExecuteNonQuery().ThrowIfEqual(0, ErrorCodes.SQLUpdateError);
        }

        public void Delete(SafeSpawnPoint point)
        {
            Db.Query().CommandText("delete npcsafespawnpoints where id=@id").SetParameter("@id",point.Id).ExecuteNonQuery().ThrowIfEqual(0,ErrorCodes.SQLDeleteError);
        }

        public IEnumerable<SafeSpawnPoint> GetAll()
        {
            return Db.Query().CommandText("select * from npcsafespawnpoints where zoneId = @zoneId").SetParameter("@zoneId",_zone.Id).Execute().Select(r =>
            {
                var point = new SafeSpawnPoint
                {
                    Id = r.GetValue<int>("id"), 
                    ZoneId = r.GetValue<int>("zoneId"), 
                    Location = new Point(r.GetValue<int>("x"), r.GetValue<int>("y"))
                };

                return point;
            });
        }
    }
}