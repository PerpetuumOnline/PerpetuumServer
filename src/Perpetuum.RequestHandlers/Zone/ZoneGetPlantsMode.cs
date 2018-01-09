using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Zone
{
    public class ZoneGetPlantsMode : IRequestHandler<IZoneRequest>
    {
        public void HandleRequest(IZoneRequest request)
        {
            var info = request.Zone.PlantHandler.GetInfoDictionary();
            Message.Builder.FromRequest(request).WithData(info).Send();
        }
    }
}