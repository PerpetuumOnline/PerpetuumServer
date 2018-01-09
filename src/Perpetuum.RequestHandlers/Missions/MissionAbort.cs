using System;
using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Services.MissionEngine.MissionProcessorObjects;

namespace Perpetuum.RequestHandlers.Missions
{
    public class MissionAbort : IRequestHandler
    {
        private readonly MissionProcessor _missionProcessor;

        public MissionAbort(MissionProcessor missionProcessor)
        {
            _missionProcessor = missionProcessor;
        }

        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var character = request.Session.Character;
                var guidString = request.Data.GetOrDefault<string>(k.guid);
                Guid.TryParse(guidString, out Guid missionGuid).ThrowIfFalse(ErrorCodes.SyntaxError);

                _missionProcessor.AbortMissionByRequest(character, missionGuid, ErrorCodes.MissionAbortedByOwner);
                
                scope.Complete();
            }
        }
    }
}