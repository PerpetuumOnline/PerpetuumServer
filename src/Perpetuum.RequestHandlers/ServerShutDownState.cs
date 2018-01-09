using Perpetuum.Host;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers
{
    public class ServerShutDownState : IRequestHandler
    {
        private readonly HostShutDownManager _shutDownManager;

        public ServerShutDownState(HostShutDownManager shutDownManager)
        {
            _shutDownManager = shutDownManager;
        }

        public void HandleRequest(IRequest request)
        {
            Message.Builder.FromRequest(request).WithData(_shutDownManager.StateToDictionary()).Send();
        }
    }
}