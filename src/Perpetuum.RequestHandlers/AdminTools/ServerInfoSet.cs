using Perpetuum.Host.Requests;
using Perpetuum.Services.Relay;

namespace Perpetuum.RequestHandlers.AdminTools
{
    public class ServerInfoSet : IRequestHandler
    {
        private readonly IServerInfoManager _serverInfoManager;

        public ServerInfoSet(IServerInfoManager serverInfoManager)
        {
            _serverInfoManager = serverInfoManager;
        }

        public void HandleRequest(IRequest request)
        {
            var serverInfo = ServerInfo.Deserialize(request.Data);
            _serverInfoManager.SaveServerInfoToDb(serverInfo);

            serverInfo = _serverInfoManager.GetServerInfo();
            Message.Builder.FromRequest(request).WithData(serverInfo.Serialize()).Send();

            _serverInfoManager.PostCurrentServerInfoToWebService();
        }
    }
}
