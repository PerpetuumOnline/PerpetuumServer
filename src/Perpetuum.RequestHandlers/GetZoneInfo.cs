using System.Collections.Generic;
using Perpetuum.Collections.Spatial;
using Perpetuum.Host.Requests;
using Perpetuum.Zones;

namespace Perpetuum.RequestHandlers
{
    public class GetZoneInfo : IRequestHandler
    {
        private readonly IZoneManager _zoneManager;

        public GetZoneInfo(IZoneManager zoneManager)
        {
            _zoneManager = zoneManager;
        }

        public void HandleRequest(IRequest request)
        {
            var result = GetZoneConfigDictionary();
            Message.Builder.FromRequest(request).WithData(result).Send();
        }

        private IDictionary<string, object> GetZoneConfigDictionary()
        {
            int gridSize = 2048 / Grid.TilesPerGrid;

            var result = new Dictionary<string, object>
            {
                {k.gridSize, gridSize},
                {k.result,_zoneManager.Zones.ToDictionary("z", z => z.Configuration.ToDictionary())}
            };

            return result;
        }
    }
}