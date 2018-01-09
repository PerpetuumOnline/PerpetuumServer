using Perpetuum.Host;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers
{
    public class ServerShutDownCancel : IRequestHandler
    {
        private readonly HostShutDownManager _shutDownManager;

        public ServerShutDownCancel(HostShutDownManager shutDownManager)
        {
            _shutDownManager = shutDownManager;
        }

        public void HandleRequest(IRequest request)
        {
            _shutDownManager.StopShutDown(request.Command);
        }
    }
}