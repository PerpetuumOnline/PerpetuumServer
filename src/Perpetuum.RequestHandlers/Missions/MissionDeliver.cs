using System;
using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Services.MissionEngine.MissionDataCacheObjects;
using Perpetuum.Services.MissionEngine.MissionProcessorObjects;

namespace Perpetuum.RequestHandlers.Missions
{
    public class MissionDeliver : IRequestHandler
    {
        private readonly MissionProcessor _missionProcessor;
        private readonly MissionDataCache _missionDataCache;

        public MissionDeliver(MissionProcessor missionProcessor,MissionDataCache missionDataCache)
        {
            _missionProcessor = missionProcessor;
            _missionDataCache = missionDataCache;
        }

        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var character = request.Session.Character;
                var locationId = 0;

                var guidString = request.Data.GetOrDefault<string>(k.guid);
                Guid missionGuid;
                Guid.TryParse(guidString, out missionGuid).ThrowIfFalse(ErrorCodes.SyntaxError);

                if (!character.IsDocked)
                {
                    var eid = request.Data.GetOrDefault<long>(k.eid);
                    var location = _missionDataCache.GetLocationByEid(eid);

                    location.ThrowIfNull(ErrorCodes.InvalidMissionLocation);

                    locationId = location.id;
                }

                _missionProcessor.DeliverSingleMission(character, missionGuid, locationId);
                
                scope.Complete();
            }
        }
    }
}