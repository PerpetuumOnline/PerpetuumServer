using System;
using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Host.Requests;
using Perpetuum.Items;
using Perpetuum.Log;
using Perpetuum.Services.MarketEngine;

namespace Perpetuum.RequestHandlers.Markets
{
    public class MarketInsertAverageForCF : IRequestHandler
    {
        private readonly IEntityServices _entityServices;
        private readonly ItemPriceHelper _itemPriceHelper;
        private readonly MarketHandler _marketHandler;

        public MarketInsertAverageForCF(IEntityServices entityServices,ItemPriceHelper itemPriceHelper,MarketHandler marketHandler)
        {
            _entityServices = entityServices;
            _itemPriceHelper = itemPriceHelper;
            _marketHandler = marketHandler;
        }

        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var category = request.Data.GetOrDefault<string>(k.category);
                var days = request.Data.GetOrDefault<int>(k.day);
                var amount = request.Data.GetOrDefault<int>(k.amount);

                CategoryFlags cf;
                Enum.TryParse(category, true, out cf).ThrowIfFalse(ErrorCodes.CategoryflagNotFound);

                foreach (var ed in _entityServices.Defaults.GetAll().GetByCategoryFlags(cf))
                {
                    Logger.Info("-------------------------");
                    Logger.Info("processing definition: " + ed.Definition);

                    if (ed == EntityDefault.None)
                    {
                        Logger.Error("definition was not found");
                        continue;
                    }

                    Logger.Info("processing " + ed.Name);

                    var price = _itemPriceHelper.GetDefaultPrice(ed.Definition);

                    if (Math.Abs(price) < double.Epsilon)
                    {
                        Logger.Error("item price was not found. ");
                        continue;
                    }

                    Logger.Info("price for " + ed.Definition + "      is: " + price);

                    var marketEids = _marketHandler.GetAllDefaultMarketsEids(false, false);

                    //insert the prices to every market
                    foreach (var marketEid in marketEids)
                    {
                        var market = Market.GetOrThrow(marketEid);

                        Logger.Info("inserting to market: " + marketEid);

                        var profit = market.VendorSellProfit;

                        if (Math.Abs(profit) < double.Epsilon)
                        {
                            //default vendor profit
                            profit = 3.0;
                        }

                        var totalPrice = price * profit;

                        Logger.Info("resulting price:" + totalPrice + " with profit:" + profit);

                        market.InsertStatsForPeriod(days, totalPrice, amount, ed.Definition);
                    }
                }

                Message.Builder.FromRequest(request).WithOk().Send();
                
                scope.Complete();
            }
        }
    }
}