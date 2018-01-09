using System;
using System.Collections.Generic;
using System.Transactions;
using Perpetuum.Data;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers
{
    public class RequestStarterRobot : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var character = request.Session.Character;

                var nextAvailable = character.NextAvailableRobotRequestTime;
                nextAvailable.ThrowIfGreater(DateTime.Now, ErrorCodes.StarterRobotRequestTimerIsStillRunning,gex => gex.SetData("nextAvailable",nextAvailable));

                var dockingBase = character.GetCurrentDockingBase();
                var robot = dockingBase.CreateStarterRobotForCharacter(character,true).ThrowIfNull(ErrorCodes.StarterRobotFound);

                Transaction.Current.OnCommited(() =>
                {
                    //store request time
                    character.NextAvailableRobotRequestTime = DateTime.Now.AddMinutes(5);

                    // nem volt hiba,de ha talal egy robotot valahol akkor null lesz
                    var robotInfo = robot.BaseInfoToDictionary();
                    Message.Builder.FromRequest(request).WithData(new Dictionary<string, object> { { k.item, robotInfo } }).Send();
                });
                scope.Complete();
            }
        }
    }
}