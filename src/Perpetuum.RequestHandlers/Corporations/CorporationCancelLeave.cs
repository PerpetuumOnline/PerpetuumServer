using Perpetuum.Groups.Corporations;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Corporations
{
    public class CorporationCancelLeave : IRequestHandler
    {
        private readonly ICorporationManager _corporationManager;

        public CorporationCancelLeave(ICorporationManager corporationManager)
        {
            _corporationManager = corporationManager;
        }

        public void HandleRequest(IRequest request)
        {
            var character = request.Session.Character;

            // is he in leave state?
            _corporationManager.IsInLeavePeriod(character).ThrowIfFalse(ErrorCodes.AccessDenied);

            // yes, clean up leave
            _corporationManager.CleanUpCharacterLeave(character);
            Message.Builder.FromRequest(request).WithOk().Send();
        }
    }
}