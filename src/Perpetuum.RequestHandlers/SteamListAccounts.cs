using System.Linq;
using System.Transactions;
using Perpetuum.Accounting;
using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Services.Steam;

namespace Perpetuum.RequestHandlers
{
    public class SteamListAccounts : IRequestHandler
    {
        private readonly ISteamManager _steamManager;
        private readonly IAccountRepository _accountRepository;

        public SteamListAccounts(ISteamManager steamManager,IAccountRepository accountRepository)
        {
            _steamManager = steamManager;
            _accountRepository = accountRepository;
        }

        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var encryptedTicket = request.Data.GetOrDefault<byte[]>("encData");
                var steamId = _steamManager.GetSteamId(encryptedTicket);
                if (string.IsNullOrEmpty(steamId))
                    throw new PerpetuumException(ErrorCodes.SteamDecodingError);

                var accounts = _accountRepository.GetBySteamId(steamId).ToList();
                if (accounts.Count <= 0)
                {
                    var newAccount = CreateNewSteamAccount(steamId);
                    accounts.Add(newAccount);
                }

                Transaction.Current.OnCommited(() => {
                    var accountInfos = accounts.ToDictionary("a", a => a.ToDictionary());
                    Message.Builder.FromRequest(request).WithData(accountInfos).Send();
                });
                scope.Complete();
            }
        }

        private Account CreateNewSteamAccount(string steamId)
        {
            var account = new Account
            {
                SteamId = steamId,
                Email = $"steam:{steamId}",
                EmailConfirmed = true,
                IsActive = true
            };
            _accountRepository.Insert(account);
            return account;
        }
    }
}