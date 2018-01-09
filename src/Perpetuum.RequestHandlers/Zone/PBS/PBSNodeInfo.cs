using System.Collections.Generic;
using Perpetuum.Host.Requests;
using Perpetuum.Zones.PBS;

namespace Perpetuum.RequestHandlers.Zone.PBS
{
    public class PBSNodeInfo : IRequestHandler<IZoneRequest>
    {
        public void HandleRequest(IZoneRequest request)
        {
            var eid = request.Data.GetOrDefault<long>(k.eid);
            
            var info = new Dictionary<string, object>();

            var unit = request.Zone.GetUnit(eid);
            if (unit != null)
            {
                var node = unit.ThrowIfNotType<IPBSObject>(ErrorCodes.DefinitionNotSupported);
                var character = request.Session.Character;
                node.CheckAccessAndThrowIfFailed(character);
                info = unit.ToDictionary();
            }

            if (info.Count == 0)
            {
                Message.Builder.FromRequest(request).WithEmpty().Send();
            }
            else
            {
                Message.Builder.FromRequest(request)
                    .WithData(new Dictionary<string, object>
                    {
                        {k.info, info},
                        {k.zoneID, request.Zone.Id},
                    }).Send();
            }
        }
    }
}