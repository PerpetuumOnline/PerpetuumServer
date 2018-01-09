using Perpetuum.Accounting;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Extensions
{
    public class ExtensionGetAvailablePoints : IRequestHandler
    {
        private readonly IAccountManager _accountManager;

        public ExtensionGetAvailablePoints(IAccountManager accountManager)
        {
            _accountManager = accountManager;
        }

        public void HandleRequest(IRequest request)
        {
            var account = _accountManager.Repository.Get(request.Session.AccountId).ThrowIfNull(ErrorCodes.AccountNotFound);
            var character = request.Session.Character;

            var result = _accountManager.GetEPData(account,character);
            Message.Builder.FromRequest(request).WithData(result).Send();
        }
    }
}