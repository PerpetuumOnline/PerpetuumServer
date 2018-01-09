using System.Collections.Generic;
using System.Transactions;
using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Items;
using Perpetuum.Robots;

namespace Perpetuum.RequestHandlers
{
    public class ChangeModule : IRequestHandler
    {
        private readonly RobotHelper _robotHelper;

        public ChangeModule(RobotHelper robotHelper)
        {
            _robotHelper = robotHelper;
        }

        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var character = request.Session.Character;
                character.IsDocked.ThrowIfFalse(ErrorCodes.CharacterHasToBeDocked);

                var robotEid = request.Data.GetOrDefault<long>(k.robotEID);
                var robot = _robotHelper.LoadRobotOrThrow(robotEid);
                robot.IsSingleAndUnpacked.ThrowIfFalse(ErrorCodes.RobotMustbeSingleAndNonRepacked);
                robot.EnlistTransaction();

                var componentType = request.Data.GetOrDefault<string>(k.sourceComponent).ToEnum<RobotComponentType>();
                var component = robot.GetRobotComponentOrThrow(componentType);
                var sourceSlot = request.Data.GetOrDefault<int>(k.source);
                var targetSlot = request.Data.GetOrDefault<int>(k.target);
                component.ChangeModuleOrThrow(sourceSlot, targetSlot);

                robot.Initialize(character);
                robot.Save();

                Transaction.Current.OnCommited(() =>
                {
                    var result = new Dictionary<string, object>
                    {
                        { k.robot, robot.ToDictionary() }
                    };
                    Message.Builder.FromRequest(request).WithData(result).WrapToResult().Send();
                });
                
                scope.Complete();
            }
        }
    }
}