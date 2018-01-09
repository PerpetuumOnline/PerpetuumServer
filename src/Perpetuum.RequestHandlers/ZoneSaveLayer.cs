using Perpetuum.Host.Requests;
using Perpetuum.Zones;

namespace Perpetuum.RequestHandlers
{
    public class ZoneSaveLayer : IRequestHandler
    {
        private readonly IZoneManager _zoneManager;
        private readonly LayerFileIO _layerFileIO;

        public ZoneSaveLayer(IZoneManager zoneManager,LayerFileIO layerFileIO)
        {
            _zoneManager = zoneManager;
            _layerFileIO = layerFileIO;
        }

        public void HandleRequest(IRequest request)
        {
            foreach (var zone in _zoneManager.Zones)
            {
                _layerFileIO.SaveLayerToDisk(zone,zone.Terrain.Altitude);
                _layerFileIO.SaveLayerToDisk(zone,zone.Terrain.Blocks);
                _layerFileIO.SaveLayerToDisk(zone,zone.Terrain.Controls);
                _layerFileIO.SaveLayerToDisk(zone,zone.Terrain.Plants);
            }

            Message.Builder.FromRequest(request).WithOk().Send();
        }
    }
}