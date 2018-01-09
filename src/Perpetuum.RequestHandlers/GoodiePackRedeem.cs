using Perpetuum.Accounting;
using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Services.Relay;

namespace Perpetuum.RequestHandlers
{
    public class GoodiePackRedeem : IRequestHandler
    {
        private readonly GoodiePackHandler _goodiePackHandler;
        private readonly IAccountRepository _accountRepository;

        public GoodiePackRedeem(GoodiePackHandler goodiePackHandler,IAccountRepository accountRepository)
        {
            _goodiePackHandler = goodiePackHandler;
            _accountRepository = accountRepository;
        }

        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var isPackIndy = request.Data.GetOrDefault<int>("indy") == 1;
                var campaignId = request.Data.GetOrDefault<int>("campaignID");

                var account = _accountRepository.Get(request.Session.AccountId);
                var character = request.Session.Character;
                var result = _goodiePackHandler.RedeemPackBySelection(campaignId, account, character, isPackIndy);
                Message.Builder.FromRequest(request).WithData(result).WithEmpty().Send();
                
                scope.Complete();
            }
        }
    }
}