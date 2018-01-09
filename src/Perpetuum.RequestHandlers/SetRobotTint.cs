using System.Collections.Generic;
using System.Drawing;
using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Robots;

namespace Perpetuum.RequestHandlers
{
    public class SetRobotTint : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var robotEid = request.Data.GetOrDefault<long>(k.robotEID);
                var tint = request.Data.GetOrDefault<Color>(k.tint);

                var robot = Robot.GetOrThrow(robotEid);
                robot.Tint = tint;
                robot.Save();

                var result = new Dictionary<string, object> { { k.robot, robot.ToDictionary() } };
                Message.Builder.FromRequest(request).WithData(result).WrapToResult().Send();
                
                scope.Complete();
            }
        }
    }
}