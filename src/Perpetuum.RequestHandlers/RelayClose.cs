using Perpetuum.Host.Requests;
using Perpetuum.Services.Relay;

namespace Perpetuum.RequestHandlers
{
    public class RelayClose : IRequestHandler
    {
        private readonly IRelayStateService _relayStateService;

        public RelayClose(IRelayStateService relayStateService)
        {
            _relayStateService = relayStateService;
        }

        public void HandleRequest(IRequest request)
        {
            _relayStateService.ConfigOnlyAllowAdmins(true);
            _relayStateService.State = RelayState.OpenForAdminsOnly;
            
            Message.Builder.FromRequest(request).WithOk().Send();
        }
    }
}