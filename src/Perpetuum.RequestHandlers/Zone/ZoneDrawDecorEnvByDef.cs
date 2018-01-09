using System;
using System.Linq;
using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Log;

namespace Perpetuum.RequestHandlers.Zone
{
    public class ZoneDrawDecorEnvByDef : IRequestHandler<IZoneRequest>
    {
        public void HandleRequest(IZoneRequest request)
        {
            var definition = request.Data.GetOrDefault<int>(k.definition);

            const string q = @"SELECT id FROM dbo.decor WHERE zoneid=@zoneId AND definition=@definition";

            var decorIds =
                Db.Query().CommandText(q)
                    .SetParameter("@definition", definition)
                    .SetParameter("@zoneId",request.Zone.Id)
                    .Execute()
                    .Select(r => DataRecordExtensions.GetValue<int>(r, 0)).ToArray();

            foreach (var decorId in decorIds)
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