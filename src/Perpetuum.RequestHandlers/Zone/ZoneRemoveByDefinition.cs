using System.Linq;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Zone
{
    public class ZoneRemoveByDefinition : IRequestHandler<IZoneRequest>
    {
        public void HandleRequest(IZoneRequest request)
        {
            var definition = request.Data.GetOrDefault<int>(k.definition);

            var units = request.Zone.Units.Where(u => u.Definition == definition).ToArray();

            foreach (var unit in units)
            {
                unit.RemoveFromZone();
            }

            Message.Builder.FromRequest(request).WithOk().Send();
        }
    }
}