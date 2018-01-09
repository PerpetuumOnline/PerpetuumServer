using System;
using Perpetuum.Host;
using Perpetuum.Threading.Process;

namespace Perpetuum.Services.Relay
{
    public class ServerInfoService : Process
    {
        private readonly IServerInfoManager _serverInfoManager;

        public ServerInfoService(IHostStateService stateService,IServerInfoManager serverInfoManager)
        {
            _serverInfoManager = serverInfoManager;

            stateService.StateChanged += (service, state) =>
            {
                if (state == HostState.Online)
                {
                    _serverInfoManager.PostCurrentServerInfoToWebService();
                }
            };
        }
        
        public override void Update(TimeSpan time)
        {
            _serverInfoManager.PostCurrentServerInfoToWebService();
        }
    }
}