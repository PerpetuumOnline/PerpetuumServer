using System.Collections.Generic;
using Perpetuum.Host.Requests;
using Perpetuum.Log;
using Perpetuum.Services.MissionEngine.MissionDataCacheObjects;

namespace Perpetuum.RequestHandlers.Missions
{
    /// <summary>
    /// Admin command
    /// </summary>
    public class MissionAdminListAll : IRequestHandler
    {
        private readonly MissionDataCache _missionDataCache;

        public MissionAdminListAll(MissionDataCache missionDataCache)
        {
            _missionDataCache = missionDataCache;
        }

        public void HandleRequest(IRequest request)
        {
            var reply = new Dictionary<string, object>();
            var counter = 0;

            foreach (var mission in _missionDataCache.GetAllMissions)
            {
                var oneEntry = mission.ToDictionary();

                reply.Add("m"+counter++, oneEntry);

            }

            Logger.Info(reply.Count +  " missions listed");

            Message.Builder.FromRequest(request).WithData(reply).Send();
        }
    }
}