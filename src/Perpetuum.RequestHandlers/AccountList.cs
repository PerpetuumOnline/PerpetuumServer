using Perpetuum.Accounting;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers
{
    public class AccountList : IRequestHandler
    {
        private readonly IAccountRepository _accountRepository;
        

        public AccountList(IAccountRepository accountRepository)
        {
            _accountRepository = accountRepository;
        }

        public void HandleRequest(IRequest request)
        { 
            var data = _accountRepository.GetAll().ToDictionary("a", a => a.ToDictionary());
            Message.Builder.FromRequest(request).WithData(data).Send();
        }
    }
}
