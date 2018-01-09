using System;
using Perpetuum.Accounting;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers
{
    public class AccountEpForActivityHistory : IRequestHandler
    {
        private readonly IAccountManager _accountManager;

        public AccountEpForActivityHistory(IAccountManager accountManager)
        {
            _accountManager = accountManager;
        }

        public void HandleRequest(IRequest request)
        {
            var account = _accountManager.Repository.Get(request.Session.AccountId).ThrowIfNull(ErrorCodes.AccountNotFound);

            var fromOffset = request.Data.GetOrDefault<int>(k.from);
            var toOffset = request.Data.GetOrDefault<int>(k.duration);
            var dateFrom = DateTime.Now.AddDays(-fromOffset);
            var dateTo = dateFrom.AddDays(-toOffset);
            var history = _accountManager.GetEpForActivityHistory(account,dateFrom, dateTo);

            var result = history.ToDictionary("a", e => e.ToDictionary());
            Message.Builder.FromRequest(request).WithData(result).Send();
        }
    }
}