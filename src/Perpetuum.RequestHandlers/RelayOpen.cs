using Perpetuum.Host.Requests;
using Perpetuum.Services.Relay;

namespace Perpetuum.RequestHandlers
{
    public class RelayOpen : IRequestHandler
    {
        private readonly IRelayStateService _relayStateService;

        public RelayOpen(IRelayStateService relayStateService)
        {
            _relayStateService = relayStateService;
        }

        public void HandleRequest(IRequest request)
        {
            _relayStateService.ConfigOnlyAllowAdmins(false);
            _relayStateService.State = RelayState.OpenForPublic;

            Message.Builder.FromRequest(request).WithOk().Send();
        }
    }
}