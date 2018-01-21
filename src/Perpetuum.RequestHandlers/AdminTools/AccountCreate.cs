using Perpetuum.Accounting;
using Perpetuum.Data;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.AdminTools
{
    public class AccountCreate : IRequestHandler
    {
        private readonly IAccountRepository _accountRepository;

        public AccountCreate(IAccountRepository accountRepository)
        {
            _accountRepository = accountRepository;
        }
        public void HandleRequest(IRequest request)
        {
            var email = request.Data.GetOrDefault<string>(k.email);
            var password = request.Data.GetOrDefault<string>(k.password);
            var accessLevel = request.Data.GetOrDefault<int>(k.accessLevel);

            var account = new Account
            {
                Email = email,
                Password = password,
                AccessLevel = (AccessLevel) accessLevel,
                CampaignId = "{\"host\":\"tooladmin\"}"
            };

            if (_accountRepository.Get(account.Email,account.Password) != null)
            {
                Message.Builder.FromRequest(request).WithError(ErrorCodes.AccountAlreadyExists).Send();
                return;
            }

            _accountRepository.Insert(account);

            Db.Query().CommandText("extensionPointsInject")
                .SetParameter("@accountID", account.Id)
                .SetParameter("@points", 40000)
                .ExecuteNonQuery();

            Message.Builder.FromRequest(request).SetData(k.account, account.ToDictionary()).Send();
        }
    }
}
