using System;
using System.Globalization;
using System.Linq;
using Perpetuum.Accounting.Characters;
using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Items;
using Perpetuum.Log;
using Perpetuum.Robots;
using Perpetuum.Units.DockingBases;

namespace Perpetuum.RequestHandlers
{
    public class DockAll : IRequestHandler
    {
        private readonly DockingBaseHelper _dockingBaseHelper;

        public DockAll(DockingBaseHelper dockingBaseHelper)
        {
            _dockingBaseHelper = dockingBaseHelper;
        }

        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                const string sqlCmd = "SELECT characterID,activeChassis,baseEID FROM dbo.characters WHERE docked = 0";

                var records = Db.Query().CommandText(sqlCmd).Execute();

                foreach (var group in records.GroupBy(r => r.GetValue<long>(2)))
                {
                    try
                    {
                        var publicContainer = _dockingBaseHelper.GetDockingBase(group.Key).GetPublicContainer();

                        foreach (var record in group)
                        {
                            var character = Character.Get(record.GetValue<int>(0));
                            var robotEid = record.GetValue<long>(1);

                            var robot = Robot.GetOrThrow(robotEid);

                            robot.Initialize(character);
                            robot.Parent = publicContainer.Eid;

                            robot.Save();
                            Logger.Info("dockAll: robot (" + robotEid + ") => container (" + publicContainer.Eid + ") ok.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Exception(ex);
                    }
                }

                var id = records.Select(r => r.GetValue<int>(0).ToString(CultureInfo.InvariantCulture)).Aggregate((a, b) => a + "," + b);

                var updateCmd = "update characters set docked = 1,zoneid = null,instanceid = null,positionX = null,positionY = null,positionZ = null where characterid in (" + id + ")";
                Db.Query().CommandText(updateCmd).ExecuteNonQuery();

                Message.Builder.FromRequest(request).WithOk().Send();
                
                scope.Complete();
            }
        }
    }
}