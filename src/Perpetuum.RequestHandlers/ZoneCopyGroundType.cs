using Perpetuum.Host.Requests;
using Perpetuum.Zones;
using System.Threading.Tasks;

namespace Perpetuum.RequestHandlers
{
    public class ZoneCopyGroundType : IRequestHandler
    {
        private readonly IZoneManager _zoneManager;

        public ZoneCopyGroundType(IZoneManager zoneManager)
        {
            _zoneManager = zoneManager;
        }

        public void HandleRequest(IRequest request)
        {
            var sourceZoneId = request.Data.GetOrDefault<int>(k.source);
            var targetZoneId = request.Data.GetOrDefault<int>(k.target);
            var targetZone = _zoneManager.GetZone(targetZoneId);
            var sourceZone = _zoneManager.GetZone(sourceZoneId);
            targetZone.IsLayerEditLocked.ThrowIfTrue(ErrorCodes.TileTerraformProtected);
            targetZoneId.ThrowIfEqual(sourceZoneId, ErrorCodes.WTFErrorMedicalAttentionSuggested);
            targetZone.Size.ThrowIfNotEqual(sourceZone.Size, ErrorCodes.WTFErrorMedicalAttentionSuggested);

            var area = Area.FromRectangle(0, 0, sourceZone.Size.Width, sourceZone.Size.Height);
            var altBuffer = new ushort[area.Ground];
            var workAreas = area.Slice(32);
            Parallel.ForEach(workAreas, (workArea) =>
            {
                var target = targetZone.Terrain.Plants.GetArea(workArea);
                var source = sourceZone.Terrain.Plants.GetArea(workArea);
                for (int i=0; i< target.Length; i++)
                {
                    target[i].SetGroundType(source[i].groundType);
                }
                targetZone.Terrain.Plants.SetArea(workArea, target);
            });

            Message.Builder.FromRequest(request).WithOk().Send();
        }
    }
}