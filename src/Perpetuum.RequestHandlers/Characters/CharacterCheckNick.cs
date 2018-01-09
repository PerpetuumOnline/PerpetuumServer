using System;
using System.Collections.Generic;
using Perpetuum.Accounting;
using Perpetuum.Accounting.Characters;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Characters
{
    public class CharacterCheckNick : IRequestHandler
    {
        private readonly IAccountRepository _accountRepository;

        public CharacterCheckNick(IAccountRepository accountRepository)
        {
            _accountRepository = accountRepository;
        }

        public void HandleRequest(IRequest request)
        {
            var account = _accountRepository.Get(request.Session.AccountId).ThrowIfNull(ErrorCodes.AccountNotFound); ;

            var nick = request.Data.GetOrDefault<string>(k.nick).Trim();
            var result = 0;
            var comment = string.Empty;
            var eCode = 0;
            try
            {
                Character.CheckNickAndThrowIfFailed(nick, request.Session.AccessLevel, account);
            }
            catch (PerpetuumException gex)
            {
                if (gex.error == ErrorCodes.NickTaken)
                {
                    result = 1;
                    comment = Enum.GetName(typeof(ErrorCodes), gex.error);
                    eCode = (int)gex.error;
                }
                else throw;
            }

            var dictionary = new Dictionary<string, object>
            {
                { k.exists, result }, 
                { k.comment, comment }, 
                { k.code, eCode }
            };

            Message.Builder.FromRequest(request)
                .WithData(dictionary)
                .Send();
        }
    }
}