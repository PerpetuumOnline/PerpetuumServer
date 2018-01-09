using Perpetuum.Host.Requests;
using Perpetuum.Services.MissionEngine.MissionProcessorObjects;

namespace Perpetuum.RequestHandlers.Missions
{
    public class MissionListRunning : IRequestHandler
    {
        private readonly MissionProcessor _missionProcessor;

        public MissionListRunning(MissionProcessor missionProcessor)
        {
            _missionProcessor = missionProcessor;
        }

        public void HandleRequest(IRequest request)
        {
            var character = request.Session.Character;
            _missionProcessor.SendRunningMissionList(character);
        }
    }
}