using System.Collections.Generic;
using Perpetuum.Containers;
using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Items;
using Perpetuum.Robots;

namespace Perpetuum.RequestHandlers
{
    public class RobotEmpty : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var containerEid = request.Data.GetOrDefault<long>(k.container);
                var robotEid = request.Data.GetOrDefault<long>(k.eid);
                var character = request.Session.Character;

                character.IsDocked.ThrowIfFalse(ErrorCodes.CharacterHasToBeDocked);

                var container = Container.GetWithItems(containerEid, character, ContainerAccess.Remove);

                var robot = (Robot)container.GetItemOrThrow(robotEid);
                robot.IsSingleAndUnpacked.ThrowIfFalse(ErrorCodes.RobotMustbeSingleAndNonRepacked);
                robot.EmptyRobot(character, container);
                robot.Initialize(character);

                container.Save();

                var result = new Dictionary<string, object>
                {
                    {k.robot, robot.ToDictionary()},
                    {k.container, container.ToDictionary()}
                };

                Message.Builder.FromRequest(request).WithData(result).WrapToResult().Send();
                
                scope.Complete();
            }
        }
    }
}