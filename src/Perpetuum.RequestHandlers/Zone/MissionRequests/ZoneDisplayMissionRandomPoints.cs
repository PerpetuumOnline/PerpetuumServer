using System.Linq;
using Perpetuum.ExportedTypes;
using Perpetuum.Host.Requests;
using Perpetuum.Services.MissionEngine;
using Perpetuum.Services.MissionEngine.MissionDataCacheObjects;
using Perpetuum.Zones;

namespace Perpetuum.RequestHandlers.Zone.MissionRequests
{
    public class ZoneDisplayMissionRandomPoints : IRequestHandler<IZoneRequest>
    {
        private readonly MissionDataCache _missionDataCache;

        public ZoneDisplayMissionRandomPoints(MissionDataCache missionDataCache)
        {
            _missionDataCache = missionDataCache;
        }

        public void HandleRequest(IZoneRequest request)
        {
            var rndPoints = _missionDataCache.GetAllMissionTargets.Where(t => t.Type == MissionTargetType.rnd_point && t.ZoneId == request.Zone.Id).ToList();

            foreach (var missionTarget in rndPoints)
            {
                var target = missionTarget;
                request.Zone.CreateBeam(BeamType.artifact_radar, builder => builder.WithPosition(target.targetPosition).WithDuration(100000));
            }

            Message.Builder.FromRequest(request).WithOk().Send();
        }
    }
}