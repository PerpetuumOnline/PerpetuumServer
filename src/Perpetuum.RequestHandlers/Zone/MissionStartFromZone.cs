using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Services.MissionEngine;
using Perpetuum.Services.MissionEngine.MissionDataCacheObjects;
using Perpetuum.Services.MissionEngine.MissionProcessorObjects;
using Perpetuum.Units.FieldTerminals;
using Perpetuum.Zones;

namespace Perpetuum.RequestHandlers.Zone
{
    public class MissionStartFromZone : IRequestHandler<IZoneRequest>
    {
        private readonly MissionDataCache _missionDataCache;
        private readonly MissionProcessor _missionProcessor;

        public MissionStartFromZone(MissionDataCache missionDataCache,MissionProcessor missionProcessor)
        {
            _missionDataCache = missionDataCache;
            _missionProcessor = missionProcessor;
        }

        public void HandleRequest(IZoneRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var category = request.Data.GetOrDefault<int>(k.missionCategory);
                var level = request.Data.GetOrDefault<int>(k.missionLevel);
                var eid = request.Data.GetOrDefault<long>(k.eid);
                var spreading = request.Data.GetOrDefault<int>(k.spreading) == 1;

                var character = request.Session.Character;
                var player = request.Zone.GetPlayerOrThrow(character);

                var fieldTerminal = request.Zone.GetUnitOrThrow<FieldTerminal>(eid);
                fieldTerminal.IsInRangeOf3D(player,DistanceConstants.FIELD_TERMINAL_USE).ThrowIfFalse(ErrorCodes.ItemOutOfRange);

                var location = _missionDataCache.GetLocationByEid(eid).ThrowIfNull(ErrorCodes.InvalidMissionLocation);
                _missionProcessor.MissionStartFromFieldTerminal(character,location.id,(MissionCategory)category,level);

                scope.Complete();
            }
        }
    }
}
