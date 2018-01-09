using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Zone
{
    public class ZoneSwitchDegrade : IRequestHandler<IZoneRequest>
    {
        public void HandleRequest(IZoneRequest request)
        {
            var state = request.Data.GetOrDefault<int>(k.state)  == 1;
            request.Zone.TerraformHandler.Degrade = state;
        }
    }
}