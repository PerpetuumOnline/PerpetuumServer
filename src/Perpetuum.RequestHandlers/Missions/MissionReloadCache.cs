using Perpetuum.Host.Requests;
using Perpetuum.Services.MissionEngine.MissionDataCacheObjects;

namespace Perpetuum.RequestHandlers.Missions
{
    /// <summary>
    /// Reloads mission data without reseting any mission.
    /// </summary>
    public class MissionReloadCache : IRequestHandler
    {
        private readonly MissionDataCache _missionDataCache;

        public MissionReloadCache(MissionDataCache missionDataCache)
        {
            _missionDataCache = missionDataCache;
        }

        public void HandleRequest(IRequest request)
        {
            _missionDataCache.CacheMissionData();
            Message.Builder.FromRequest(request).WithData(_missionDataCache.GetDataForClient()).Send();
        }
    }
}