using System;
using System.Threading.Tasks;
using Perpetuum.Common.Loggers.Transaction;
using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Log;
using Perpetuum.Services.MarketEngine;

namespace Perpetuum.RequestHandlers.Markets
{
    public class MarketModifyOrder : IRequestHandler
    {
        private readonly MarketHelper _marketHelper;
        private readonly IMarketOrderRepository _marketOrderRepository;

        public MarketModifyOrder(MarketHelper marketHelper,IMarketOrderRepository marketOrderRepository)
        {
            _marketHelper = marketHelper;
            _marketOrderRepository = marketOrderRepository;
        }

        public void HandleRequest(IRequest request)
        {
            var character = request.Session.Character;
            var orderId = request.Data.GetOrDefault<int>(k.ID);
            var newPrice = request.Data.GetOrDefault<double>(k.price);
            var testMode = request.Session.AccessLevel.IsAdminOrGm();


            using (var scope = Db.CreateTransaction())
            {
                newPrice.ThrowIfLessOrEqual(0, ErrorCodes.IllegalMarketPrice);

                var order = _marketOrderRepository.Get(orderId).ThrowIfNull(ErrorCodes.ItemNotFound);
                order.submitterEID.ThrowIfNotEqual(character.Eid, ErrorCodes.AccessDenied);

                if (!request.Session.AccessLevel.IsAdminOrGm())
                {
                    if (!order.IsModifyTimeValid())
                    {
                        Message.Builder.FromRequest(request).WithData(order.GetValidModifyInfo()).WrapToResult().Send();
                        return;
                    }
                }


                var newTotalPrice = newPrice * order.quantity;
                var oldPrice = order.FullPrice;

                order.price = newPrice; // set the new price

                if (!order.isSell)
                {
                    //BUY order
                    var priceDifference = newTotalPrice - oldPrice;

                    if (Math.Abs(priceDifference) < double.Epsilon)
                    {
                        //nothing to do
                        return;
                    }

                    // take the deposit from the character or corp
                    _marketHelper.CashIn(character, order.useCorporationWallet, priceDifference, order.itemDefinition, 1, TransactionType.ModifyMarketOrder);

                    //store the deposit in the central bank
                    var dockingBase = order.GetMarket().GetDockingBase();
                    dockingBase.AddCentralBank(TransactionType.ModifyMarketOrder, priceDifference);
                }

                //update the price in sql
                _marketOrderRepository.UpdatePrice(order);

                var result = _marketHelper.GetMarketOrdersInfo(character);
                Message.Builder.FromRequest(request).WithData(result).WrapToResult().Send();
                
                scope.Complete();
            }

            Task.Run(() => { BackgroundProcess(testMode); });
        }

        private void BackgroundProcess(bool testMode)
        {
            const string autoProcessSellordersQuery = @"
SELECT 
sellorders.marketitemid so_id, sellorders.marketeid so_market,sellorders.price so_price,sellorders.quantity so_qty,
buyorders.marketitemid bo_id, buyorders.quantity bo_qty, buyorders.price bo_price, buyorders.submitted bo_submit

FROM marketitems sellorders
JOIN marketitems buyorders ON sellorders.itemdefinition=buyorders.itemdefinition AND sellorders.marketeid=buyorders.marketeid

WHERE
sellorders.isvendoritem=0 AND
sellorders.isSell=1 
AND
buyorders.isvendoritem=0 AND
buyorders.isSell=0
AND
sellorders.submittereid != buyorders.submittereid
AND
buyorders.price >= sellorders.price
AND
COALESCE(sellorders.formembersof,0) = COALESCE(buyorders.formembersof,0)

ORDER BY bo_price DESC, so_price ASC, bo_submit ASC

";

            //this one buys from the same character
            const string autoProcessSellordersQueryTest = @"
SELECT 
sellorders.marketitemid so_id, sellorders.marketeid so_market,sellorders.price so_price,sellorders.quantity so_qty,
buyorders.marketitemid bo_id, buyorders.quantity bo_qty, buyorders.price bo_price, buyorders.submitted bo_submit

FROM marketitems sellorders
JOIN marketitems buyorders ON sellorders.itemdefinition=buyorders.itemdefinition AND sellorders.marketeid=buyorders.marketeid

WHERE
sellorders.isvendoritem=0 AND
sellorders.isSell=1 
AND
buyorders.isvendoritem=0 AND
buyorders.isSell=0
AND
buyorders.price >= sellorders.price
AND
COALESCE(sellorders.formembersof,0) = COALESCE(buyorders.formembersof,0)

ORDER BY bo_price DESC, so_price ASC, bo_submit ASC

";


            try
            {
                var q = testMode ? autoProcessSellordersQueryTest : autoProcessSellordersQuery;

                var records = Db.Query().CommandText(q).Execute();

                Logger.Info("found " + records.Count + " market orders to process for price matching.");

                var count = 0;

                foreach (var record in records)
                {
                    var sellOrderId = record.GetValue<int>("so_id");
                    var buyOrderId = record.GetValue<int>("bo_id");

                    var sellOrder = _marketOrderRepository.Get(sellOrderId);
                    var buyOrder = _marketOrderRepository.Get(buyOrderId);

                    if (sellOrder == null || buyOrder == null)
                    {
                        continue;
                    }

                    using (var scope = Db.CreateTransaction())
                    {
                        try
                        {
                            Market.AutoProcessSellorders(sellOrder, buyOrder);
                            scope.Complete();
                            count++;
                        }
                        catch (Exception ex)
                        {
                            Logger.Error("error in AutoProcessSellorders");
                            Logger.Exception(ex);
                        }
                    }
                }

                Logger.Info(count + " market orders were affected in price matching.");
            }
            catch (Exception ex)
            {
                Logger.Info("error occured in LoadMatchingOrders");
                Logger.Exception(ex);
            }
        }
    }
}