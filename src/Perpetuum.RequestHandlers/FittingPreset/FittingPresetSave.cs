using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Robots;

namespace Perpetuum.RequestHandlers.FittingPreset
{
    public class FittingPresetSave : FittingPresetRequestHandler
    {
        private readonly RobotHelper _robotHelper;

        public FittingPresetSave(RobotHelper robotHelper)
        {
            _robotHelper = robotHelper;
        }

        public override void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var robotEid = request.Data.GetOrDefault<long>(k.robotEID);
                var name = request.Data.GetOrDefault<string>(k.name);
                var forCorporation = request.Data.GetOrDefault<int>(k.forCorporation).ToBool();

                var character = request.Session.Character;
                var robot = _robotHelper.LoadRobotForCharacter(robotEid, character);
                var preset = Robots.Fitting.FittingPreset.CreateFrom(robot);
                preset.Name = name;

                var repo = GetFittingPresetRepository(character, forCorporation);
                repo.Insert(preset);

                SendAllPresetsToCharacter(request, repo);
                scope.Complete();
            }
        }
    }
}