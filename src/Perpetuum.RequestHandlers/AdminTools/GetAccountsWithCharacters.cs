using System.Collections.Generic;
using System.Linq;
using Perpetuum.Accounting;
using Perpetuum.Accounting.Characters;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.AdminTools
{
    public class GetAccountsWithCharacters : IRequestHandler
    {
        private readonly IAccountRepository _accountRepository;
        private readonly CharacterProfileRepository _characterProfileRepository;

        public GetAccountsWithCharacters(IAccountRepository accountRepository, CharacterProfileRepository characterProfileRepository)
        {
            _accountRepository = accountRepository;
            _characterProfileRepository = characterProfileRepository;
        }
        public void HandleRequest(IRequest request)
        {
            var profiles = _characterProfileRepository.GetAll().ToLookup(c => c.accountID);
            var accounts = _accountRepository.GetAll();

            var x = accounts.ToDictionary("a",a =>
                {
                    var d = new Dictionary<string, object>
                    {
                        ["account"] = a.ToDictionary(),
                        ["characters"] =  profiles.GetOrEmpty(a.Id).ToDictionary("c", p => p.ToDictionary()),
                    };
                    return d;
                });

            Message.Builder.FromRequest(request).WithData(x).Send();
        }
    }
}
