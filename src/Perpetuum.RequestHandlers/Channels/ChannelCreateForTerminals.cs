using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Log;
using Perpetuum.Services.Channels;
using Perpetuum.Units.DockingBases;

namespace Perpetuum.RequestHandlers.Channels
{
    public class ChannelCreateForTerminals : IRequestHandler
    {
        private readonly DockingBaseHelper _dockingBaseHelper;

        public ChannelCreateForTerminals(DockingBaseHelper dockingBaseHelper)
        {
            _dockingBaseHelper = dockingBaseHelper;
        }

        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                foreach (var dockingBase in _dockingBaseHelper.GetDefaultDockingBases())
                {
                    var count = Db.Query().CommandText("select count(*) from channels where name=@terminalChannelName")
                        .SetParameter("@terminalChannelName", dockingBase.ChannelName)
                        .ExecuteScalar<int>();

                    if (count == 1)
                    {
                        Logger.Info($"docking base {dockingBase.Eid} already has terminal channel");
                        continue;
                    }

                    const string deleteSqlCmd = "delete channels where name=@terminalChannelName; insert channels ([name],[type]) values (@terminalChannelName,@type)";

                    Db.Query().CommandText(deleteSqlCmd)
                        .SetParameter("@terminalChannelName", dockingBase.ChannelName)
                        .SetParameter("@type", (int)ChannelType.Station)
                        .ExecuteNonQuery();

                    Logger.Info($"channel created for terminal {dockingBase.Eid} {dockingBase.ChannelName}");
                }

                Message.Builder.FromRequest(request).WithOk().Send();
                
                scope.Complete();
            }
        }
    }
}