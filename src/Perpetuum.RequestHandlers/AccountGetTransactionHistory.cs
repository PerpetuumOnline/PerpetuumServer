using System;
using Perpetuum.Accounting;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers
{
    public class AccountGetTransactionHistory : IRequestHandler
    {
        private readonly IAccountManager _accountManager;

        public AccountGetTransactionHistory(IAccountManager accountManager)
        {
            _accountManager = accountManager;
        }

        public void HandleRequest(IRequest request)
        {
            var account = _accountManager.Repository.Get(request.Session.AccountId).ThrowIfNull(ErrorCodes.AccountNotFound);

            var offsetInDay = request.Data.GetOrDefault<int>(k.offset);
            var history = _accountManager.GetTransactionHistory(account,TimeSpan.FromDays(offsetInDay), TimeSpan.FromDays(2));

            var result = history.ToDictionary("a", e => e.ToDictionary());
            Message.Builder.FromRequest(request).WithData(result).Send();
        }
    }
}