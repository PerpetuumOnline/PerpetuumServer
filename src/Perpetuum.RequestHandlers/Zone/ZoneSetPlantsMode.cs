using System;
using Perpetuum.Host.Requests;
using Perpetuum.Zones.Terrains.Materials.Plants;

namespace Perpetuum.RequestHandlers.Zone
{
    public class ZoneSetPlantsMode : IRequestHandler<IZoneRequest>
    {
        public void HandleRequest(IZoneRequest request)
        {
            var scannerMode = request.Data.GetOrDefault<string>(k.mode);

            Enum.TryParse(scannerMode, out PlantScannerMode mode).ThrowIfFalse(ErrorCodes.SyntaxError);

            request.Zone.PlantHandler.ScannerMode = mode;
            Message.Builder.FromRequest(request).WithData(request.Zone.PlantHandler.GetInfoDictionary()).Send();
        }
    }
}