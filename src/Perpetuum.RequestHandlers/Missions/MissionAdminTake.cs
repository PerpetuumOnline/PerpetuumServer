using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Services.MissionEngine.MissionDataCacheObjects;
using Perpetuum.Services.MissionEngine.MissionProcessorObjects;
using Perpetuum.Services.MissionEngine.MissionStructures;

namespace Perpetuum.RequestHandlers.Missions
{

    public class MissionAdminTake : IRequestHandler
    {
        private readonly MissionProcessor _missionProcessor;
        private readonly MissionDataCache _missionDataCache;

        public MissionAdminTake(MissionProcessor missionProcessor,MissionDataCache missionDataCache)
        {
            _missionProcessor = missionProcessor;
            _missionDataCache = missionDataCache;
        }

        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var missionId = request.Data.GetOrDefault<int>(k.missionID);
                var spreadInGang = request.Data.GetOrDefault(k.spreading, 1) == 1;
                var character = request.Session.Character;
                var level = request.Data.GetOrDefault<int>(k.level);

                level = level.Clamp(0, 9);

                var locationId = request.Data.GetOrDefault(k.location, 1);

                MissionLocation location;
                _missionDataCache.GetLocationById(locationId, out location).ThrowIfFalse(ErrorCodes.InvalidMissionLocation);

                spreadInGang = true; //force gang mode

                var result = _missionProcessor.AdminMissionStartByRequest(character, spreadInGang, missionId, location, level);
            
                Message.Builder.FromRequest(request).WithData(result).Send();
                
                scope.Complete();
            }
        }
    }
}