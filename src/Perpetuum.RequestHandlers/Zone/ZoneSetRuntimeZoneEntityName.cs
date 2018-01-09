using System;
using System.Collections.Generic;
using System.Transactions;
using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Zones;

namespace Perpetuum.RequestHandlers.Zone
{
    public class ZoneSetRuntimeZoneEntityName : IRequestHandler<IZoneRequest>
    {
        public void HandleRequest(IZoneRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var eid = request.Data.GetOrDefault<long>(k.eid);
                var ename = request.Data.GetOrDefault<string>(k.name);

                object finalName = DBNull.Value;

                if (!string.IsNullOrEmpty(ename))
                {
                    finalName = ename;
                }

                Db.Query().CommandText("update zoneentities set ename=@ename where eid=@eid and runtime=1")
                    .SetParameter("@ename", finalName)
                    .SetParameter("@eid", eid)
                    .ExecuteNonQuery().ThrowIfNotEqual(1,ErrorCodes.SQLUpdateError);

                Transaction.Current.OnCommited(() =>
                {
                    var target = request.Zone.GetUnitOrThrow(eid);
                    target.Name = ename;

                    var buildingsDict = new Dictionary<string, object> { {"b", target.ToDictionary()}};

                    var result = new Dictionary<string, object>
                    {
                        {k.zoneID,request.Zone.Id},
                        {k.buildings, buildingsDict}
                    };

                    Message.Builder.SetCommand(Commands.ZoneGetBuildings).WithData(result).ToClient(request.Session).Send();
                });
                
                scope.Complete();
            }
        }
    }



}
