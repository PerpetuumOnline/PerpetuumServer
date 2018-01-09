using System.Collections.Generic;
using System.Transactions;
using Perpetuum.Accounting.Characters;
using Perpetuum.Common.Loggers.Transaction;
using Perpetuum.Containers;
using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.Host.Requests;
using Perpetuum.Items;

namespace Perpetuum.RequestHandlers
{
    public class UseLotteryItem : IRequestHandler
    {
        private readonly IEntityServices _entityServices;

        public UseLotteryItem(IEntityServices entityServices)
        {
            _entityServices = entityServices;
        }

        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var itemEid = request.Data.GetOrDefault<long>(k.itemEID);
                var containerEid = request.Data.GetOrDefault<long>(k.containerEID);
                var character = request.Session.Character;

                var container = Container.GetWithItems(containerEid, character);
                var lotteryItem = (LotteryItem)container.GetItemOrThrow(itemEid, true).Unstack(1);

                var randomEd = lotteryItem.PickRandomItem();
                var randomItem = (Item)_entityServices.Factory.CreateWithRandomEID(randomEd);
                randomItem.Owner = character.Eid;

                container.AddItem(randomItem,true);
                _entityServices.Repository.Delete(lotteryItem);
                container.Save();

                LogOpen(character, container, lotteryItem);
                LogRandomItemCreated(character, container, randomItem);

                Transaction.Current.OnCommited(() =>
                {
                    var result = new Dictionary<string, object>
                    {
                        {k.container,container.ToDictionary()},
                        {k.item,randomItem.ToDictionary()}
                    };

                    Message.Builder.FromRequest(request).WithData(result).Send();
                });
                
                scope.Complete();
            }
        }

        private static void LogOpen(Character character, Container container, LotteryItem lotteryItem)
        {
            var b = TransactionLogEvent.Builder().SetTransactionType(TransactionType.LotteryOpen)
                                                 .SetCharacter(character)
                                                 .SetContainer(container)
                                                 .SetItem(lotteryItem);
            character.LogTransaction(b);
        }

        private static void LogRandomItemCreated(Character character, Container container, Item randomItem)
        {
            var b = TransactionLogEvent.Builder().SetTransactionType(TransactionType.LotteryRandomItemCreated)
                                                 .SetCharacter(character)
                                                 .SetContainer(container)
                                                 .SetItem(randomItem);
            character.LogTransaction(b);
        }
    }
}