using System;
using System.Linq;
using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Log;

namespace Perpetuum.RequestHandlers.Zone
{
    public class ZoneDrawAllDecors : IRequestHandler<IZoneRequest>
    {
        public void HandleRequest(IZoneRequest request)
        {
            const string q = "select id from dbo.decor where zoneid=@zoneId and locked=0";

            var ids =
                Db.Query().CommandText(q)
                    .SetParameter("@zoneId",request.Zone.Id)
                    .Execute()
                    .Select(r => DataRecordExtensions.GetValue<int>(r, 0)).ToArray();


            foreach (var decorId in ids)
            {
                try
                {
                    var ec = request.Zone.DecorHandler.DrawDecorEnvironment(decorId);
                    Logger.Info("decorId:" + decorId + " " + ec);
                }
                catch (Exception ex)
                {
                    Logger.Error(decorId + "was the bad decor.");
                    Logger.Exception(ex);
                }

            }

            Message.Builder.FromRequest(request).WithOk().Send();
        }
    }
}