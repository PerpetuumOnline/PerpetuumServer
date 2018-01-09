using System;
using Perpetuum.Data;
using Perpetuum.Items;

namespace Perpetuum.Containers.SystemContainers
{
    public class Trashcan : SystemContainer
    {
        public void MoveToTrash(Item item,DateTime disconnectTime, bool wasInsured, bool killedByPlayer, TimeSpan inactivePeriod)
        {
            //ide lehet tenni meg mindenfele parametert ami csak kell
            Db.Query().CommandText("insert entitytrash (eid,wasinsured, killedbyplayer, inactiveperiod, dctime,deleted) values (@EID,@wasInsured,@killedByPlayer,@inactivePeriod,@DCTime,@now)")
                .SetParameter("@EID",item.Eid)
                .SetParameter("@wasInsured", wasInsured)
                .SetParameter("@killedByPlayer", killedByPlayer)
                .SetParameter("@inactivePeriod", (int)inactivePeriod.TotalMilliseconds)
                .SetParameter("@DCTime", disconnectTime.Equals(default(DateTime)) ? DBNull.Value : (object)disconnectTime)
                .SetParameter("@now", DateTime.Now)
                .ExecuteNonQuery().ThrowIfEqual(0, ErrorCodes.SQLUpdateError);

            item.Parent = Eid;
        }

        public static bool IsItemTrashed(Item item)
        {
            var res = Db.Query().CommandText("select eid from entitytrash where eid=@EID")
                              .SetParameter("@EID",item.Eid)
                              .ExecuteScalar<long?>();

            return res != null;
        }

        public static Trashcan Get()
        {
            return (Trashcan)GetByName(k.es_admin_trashcan);
        }
    }
}