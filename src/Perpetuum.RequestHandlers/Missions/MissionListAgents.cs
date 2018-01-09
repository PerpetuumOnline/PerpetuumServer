using Perpetuum.Host.Requests;
using Perpetuum.Services.MissionEngine.MissionDataCacheObjects;

namespace Perpetuum.RequestHandlers.Missions
{
    public class MissionListAgents : IRequestHandler
    {
        private readonly MissionDataCache _missionDataCache;

        public MissionListAgents(MissionDataCache missionDataCache)
        {
            _missionDataCache = missionDataCache;
        }

        public void HandleRequest(IRequest request)
        {
            var result = _missionDataCache.GetAllAgents.ToDictionary("a", ma => ma.ToDictionary());
            Message.Builder.FromRequest(request).WithData(result).Send();
        }
    }

}
