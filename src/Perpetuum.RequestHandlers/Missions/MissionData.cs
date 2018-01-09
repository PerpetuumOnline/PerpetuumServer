using Perpetuum.Host.Requests;
using Perpetuum.Services.MissionEngine.MissionDataCacheObjects;

namespace Perpetuum.RequestHandlers.Missions
{
    public class MissionData : IRequestHandler
    {
        private readonly MissionDataCache _missionDataCache;

        public MissionData(MissionDataCache missionDataCache)
        {
            _missionDataCache = missionDataCache;
        }

        public void HandleRequest(IRequest request)
        {
            var result = _missionDataCache.GetDataForClient();
            Message.Builder.FromRequest(request).WithData(result).Send();
        }
    }
}