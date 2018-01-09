using System;
using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Services.MissionEngine;
using Perpetuum.Services.MissionEngine.MissionDataCacheObjects;
using Perpetuum.Services.MissionEngine.MissionProcessorObjects;
using Perpetuum.Services.MissionEngine.MissionStructures;

namespace Perpetuum.RequestHandlers.Missions
{
    public class MissionStart : IRequestHandler
    {
        private readonly MissionProcessor _missionProcessor;
        private readonly MissionDataCache _missionDataCache;

        public MissionStart(MissionProcessor missionProcessor,MissionDataCache missionDataCache)
        {
            _missionProcessor = missionProcessor;
            _missionDataCache = missionDataCache;
        }

        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var category = request.Data.GetOrDefault<int>(k.missionCategory);
                var level = request.Data.GetOrDefault<int>(k.missionLevel);
                level = level.Clamp(-1, 9);

                var character = request.Session.Character;

                var missionCategory = (MissionCategory)category;
                Enum.IsDefined(typeof(MissionCategory), category).ThrowIfFalse(ErrorCodes.MissionCategoryNotDefined);

                MissionLocation location;

                if (character.IsDocked)
                {
                    var dockedLocation = _missionDataCache.GetLocationByEid(character.CurrentDockingBaseEid).ThrowIfNull(ErrorCodes.ItemNotFound);

                    location = dockedLocation;
                }
                else
                {
                    var locationEid = request.Data.GetOrDefault<long>(k.location);

                    var someLocation = _missionDataCache.GetLocationByEid(locationEid).ThrowIfNull(ErrorCodes.ItemNotFound);

                    location = someLocation;
                }

                var result = _missionProcessor.MissionStartForRequest(character, missionCategory, level, location);

                Message.Builder.FromRequest(request).WithData(result).Send();
                
                scope.Complete();
            }
        }
    }
}
