using System.Linq;
using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Services.MissionEngine.MissionDataCacheObjects;
using Perpetuum.Services.MissionEngine.MissionProcessorObjects;
using Perpetuum.Zones;

namespace Perpetuum.RequestHandlers.Missions
{
    /// <summary>
    /// Resets ALL missions for the server. Also reloads all mission content. Practically enables mission editing without restarting the dev server.
    /// DEV only
    /// </summary>
    public class MissionFlush : IRequestHandler
    {
        private readonly MissionProcessor _missionProcessor;
        private readonly MissionDataCache _missionDataCache;
        private readonly IZoneManager _zoneManager;

        public MissionFlush(MissionProcessor missionProcessor,MissionDataCache missionDataCache,IZoneManager zoneManager)
        {
            _missionProcessor = missionProcessor;
            _missionDataCache = missionDataCache;
            _zoneManager = zoneManager;
        }

        public  void HandleRequest(IRequest request)
        {
            //!!! only in DEBUG !!!
#if !DEBUG
            return;
#endif
            using (var scope = Db.CreateTransaction())
            {
                //clear sql
                Db.Query().CommandText("delete missiontargetsarchive; DELETE  missionlog ").ExecuteNonQuery();

                //reset finished missions
                _missionProcessor.ResetFinishedMissionsOnServer();

                //reset collector
                _missionProcessor.MissionAdministrator.ResetMissionInProgressCollector();

                //reload mission data
                _missionDataCache.CacheMissionData();

                //zone clear %%%
                var players = _zoneManager.Zones.SelectMany(z => z.Players);
                foreach (var player in players)
                {
                    player.MissionHandler.ResetMissionHandler();
                }

                //send data to client
                Message.Builder.FromRequest(request).WithData(_missionDataCache.GetDataForClient()).Send();
                
                scope.Complete();
            }
        }
    }
}