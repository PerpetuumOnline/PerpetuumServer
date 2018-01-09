using System;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Zone
{
    public class ZoneSetPlantSpeed : IRequestHandler<IZoneRequest>
    {
        public void HandleRequest(IZoneRequest request)
        {
            var speed = request.Data.GetOrDefault<int>(k.speed);
            request.Zone.PlantHandler.RenewSpeed = TimeSpan.FromMilliseconds(speed);
            Message.Builder.FromRequest(request).WithOk().Send();
        }
    }
}