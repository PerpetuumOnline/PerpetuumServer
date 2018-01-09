using System;
using System.Collections.Generic;
using System.Transactions;
using Perpetuum.Accounting.Characters;
using Perpetuum.Data;
using Perpetuum.Log;
using Perpetuum.Threading.Process;

namespace Perpetuum.Services.MarketEngine
{
    public class MarketCleanUpService : Process
    {
        private readonly IMarketOrderRepository _marketOrderRepository;

        public MarketCleanUpService(IMarketOrderRepository marketOrderRepository)
        {
            _marketOrderRepository = marketOrderRepository;
        }

        public override void Update(TimeSpan time)
        {
            CleanUp(time);
        }

        private void CleanUp(TimeSpan interval)
        {
            foreach (var marketItem in _marketOrderRepository.GetExpiredOrders(interval))
            {
                using (var scope = Db.CreateTransaction())
                {
                    try
                    {
                        var canceledItem = marketItem.Cancel(_marketOrderRepository);

                        var tempDict = new Dictionary<string, object>
                        {
                            {k.marketEID, marketItem.marketEID},
                            {k.marketItemID, marketItem.id}
                        };

                        if (canceledItem != null)
                            tempDict.Add(k.item, canceledItem.BaseInfoToDictionary());

                        var submitter = Character.GetByEid(marketItem.submitterEID);

                        Transaction.Current.OnCommited(() => Message.Builder
                            .SetCommand(Commands.MarketItemExpired)
                            .WithData(tempDict)
                            .ToCharacter(submitter)
                            .Send());

                        scope.Complete();
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("error in " + marketItem);
                        Logger.Exception(ex);
                    }
                }
            }
        }
    }
}