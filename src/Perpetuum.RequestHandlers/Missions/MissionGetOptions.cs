using Perpetuum.Host.Requests;
using Perpetuum.Services.MissionEngine.MissionDataCacheObjects;
using Perpetuum.Services.MissionEngine.MissionProcessorObjects;

namespace Perpetuum.RequestHandlers.Missions
{
    public class MissionGetOptions : IRequestHandler
    {
        private readonly MissionProcessor _missionProcessor;
        private readonly MissionDataCache _missionDataCache;

        public MissionGetOptions(MissionProcessor missionProcessor,MissionDataCache missionDataCache)
        {
            _missionProcessor = missionProcessor;
            _missionDataCache = missionDataCache;
        }

        public void HandleRequest(IRequest request)
        {
            var character = request.Session.Character;
            long locationEid;

            if (character.IsDocked)
            {
                //the base we the player is docked
                locationEid = character.CurrentDockingBaseEid;
            }
            else
            {
                //use the optional parameter
                locationEid = request.Data.GetOrDefault<long>(k.eid);
            }

            var location = _missionDataCache.GetLocationByEid(locationEid).ThrowIfNull(ErrorCodes.ItemNotFound);
            _missionProcessor.GetOptionsByRequest(character, location);
        }
    }
}