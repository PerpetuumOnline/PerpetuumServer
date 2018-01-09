using System.Transactions;
using Perpetuum.Accounting;
using Perpetuum.Accounting.Characters;
using Perpetuum.Common.Loggers.Transaction;
using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Services.Relay;

namespace Perpetuum.RequestHandlers
{
    public class RedeemableItemRedeem : IRequestHandler
    {
        private readonly GoodiePackHandler _goodiePackHandler;
        private readonly IAccountRepository _accountRepository;

        public RedeemableItemRedeem(GoodiePackHandler goodiePackHandler,IAccountRepository accountRepository)
        {
            _goodiePackHandler = goodiePackHandler;
            _accountRepository = accountRepository;
        }

        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var account = _accountRepository.Get(request.Session.AccountId).ThrowIfNull(ErrorCodes.AccountNotFound);

                var id = request.Data.GetOrDefault<int>(k.ID);
                var character = request.Session.Character;

                character.IsDocked.ThrowIfFalse(ErrorCodes.CharacterHasToBeDocked);

                var redeemableItem = RedeemableItemInfo.LoadRedeemableItemById(id, account);
                var container = character.GetPublicContainerWithItems();

                foreach (var item in redeemableItem.CreateItems())
                {
                    item.Owner = character.Eid;

                    try
                    {
                        if (item is RedeemableItem redeemable)
                        {
                            // ha instant akkor itt kell aktivalni
                            if (redeemable.ED.AttributeFlags.InstantActivate)
                            {
                                redeemable.Activate(account, character);
                                continue;
                            }
                        }

                        container.AddItem(item, true);
                    }
                    finally
                    {
                        var b = TransactionLogEvent.Builder()
                            .SetTransactionType(TransactionType.ItemRedeem)
                            .SetCharacter(character)
                            .SetContainer(container)
                            .SetItem(item);

                        character.LogTransaction(b);
                    }
                }

                _accountRepository.Update(account);
                container.Save();

                redeemableItem.SetRedeemed(character);

                Transaction.Current.OnCommited(() =>
                {
                    var result = _goodiePackHandler.GetMyRedeemableItems(account);
                    result.Add(k.container, container.ToDictionary());
                    Message.Builder.FromRequest(request).WithData(result).Send();
                });
                
                scope.Complete();
            }
        }
    }
}