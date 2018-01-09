using System.Collections.Generic;
using Perpetuum.Accounting;
using Perpetuum.Accounting.Characters;
using Perpetuum.Data;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Characters
{
    public class CharacterCorrectNick : IRequestHandler
    {
        private readonly IAccountRepository _accountRepository;

        public CharacterCorrectNick(IAccountRepository accountRepository)
        {
            _accountRepository = accountRepository;
        }

        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var character = Character.Get(request.Data.GetOrDefault<int>(k.characterID));
                var nick = request.Data.GetOrDefault<string>(k.nick);
                var accessLevel = request.Session.AccessLevel;

                var account = _accountRepository.Get(request.Session.AccountId).ThrowIfNull(ErrorCodes.AccountNotFound);

                character.AccountId.ThrowIfNotEqual(account.Id, ErrorCodes.AccessDenied);
                character.IsOffensiveNick.ThrowIfFalse(ErrorCodes.NickNotOffensive);

                Character.CheckNickAndThrowIfFailed(nick, accessLevel, account);

                character.Nick = nick;
                character.IsOffensiveNick = false;

                var result = new Dictionary<string, object>
                {
                    {k.characterID, character.Id},
                    {k.nick, nick},
                };

                Message.Builder.FromRequest(request).WithData(result).Send();
                
                scope.Complete();
            }
        }
    }
}