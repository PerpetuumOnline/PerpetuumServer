using System.Collections.Generic;
using Perpetuum.Accounting;
using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.Host.Requests;
using Perpetuum.Services.MarketEngine;

namespace Perpetuum.RequestHandlers
{
    public class RedeemableItemActivate : IRequestHandler
    {
        private readonly IAccountRepository _accountRepository;

        public RedeemableItemActivate(IAccountRepository accountRepository)
        {
            _accountRepository = accountRepository;
        }

        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var character = request.Session.Character;
                var eid = request.Data.GetOrDefault<long>(k.eid);

                character.IsDocked.ThrowIfFalse(ErrorCodes.CharacterHasToBeDocked);

                var publicContainer = character.GetPublicContainerWithItems();

                var item = (Ice) publicContainer.GetItemOrThrow(eid,true).Unstack(1);
                var account = _accountRepository.Get(request.Session.AccountId);
                item.Activate(account,character);

                _accountRepository.Update(account);
                publicContainer.Save();
                Entity.Repository.Delete(item);

                var result = new Dictionary<string, object>
                {
                    {k.container, publicContainer.ToDictionary()},
                };

                Message.Builder.FromRequest(request).WithData(result).Send();
                
                scope.Complete();
            }
        }
    }
}