using Perpetuum.Common.Loggers.Transaction;
using Perpetuum.Containers;
using Perpetuum.Data;
using Perpetuum.Groups.Corporations;
using Perpetuum.Host.Requests;
using Perpetuum.Services.MarketEngine;

namespace Perpetuum.RequestHandlers.Markets
{
    public class MarketCreateSellOrder : IRequestHandler
    {
        private readonly MarketHandler _marketHandler;
        private readonly MarketHelper _marketHelper;
        private readonly IMarketInfoService _marketInfoService;
        private readonly IMarketOrderRepository _marketOrderRepository;

        public MarketCreateSellOrder(MarketHandler marketHandler,MarketHelper marketHelper,IMarketInfoService marketInfoService,IMarketOrderRepository marketOrderRepository)
        {
            _marketHandler = marketHandler;
            _marketHelper = marketHelper;
            _marketInfoService = marketInfoService;
            _marketOrderRepository = marketOrderRepository;
        }

        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var seller = request.Session.Character;
                var itemEid = request.Data.GetOrDefault<long>(k.itemEID);
                var duration = request.Data.GetOrDefault<int>(k.duration);
                var pricePerPiece = request.Data.GetOrDefault<double>(k.price);
                var quantity = request.Data.GetOrDefault<int>(k.quantity);
                var useSellerCorporationWallet = request.Data.GetOrDefault<int>(k.useCorporationWallet) == 1;
                var containerEid = request.Data.GetOrDefault<long>(k.container);
                var forMyCorporation = request.Data.GetOrDefault<int>(k.forMembersOf) == 1;
                var targetOrderId = request.Data.GetOrDefault<int>(k.targetOrder);

                seller.IsDocked.ThrowIfFalse(ErrorCodes.CharacterHasToBeDocked);
                seller.CheckPrivilegedTransactionsAndThrowIfFailed();

                quantity.ThrowIfLessOrEqual(0, ErrorCodes.WTFErrorMedicalAttentionSuggested);
                pricePerPiece.ThrowIfLessOrEqual(0, ErrorCodes.IllegalMarketPrice);
                duration.ThrowIfLess(1, ErrorCodes.MinimalDurationNotReached);

                var market = seller.GetCurrentDockingBase().GetMarketOrThrow();

                var corporationEid = seller.CorporationEid;

                var publicContainer = seller.GetPublicContainerWithItems();

                var sourceContainer = (Container)publicContainer.GetItemOrThrow(containerEid, true);

                long? forMembersOf = null;
                if (forMyCorporation)
                {
                    if (!DefaultCorporationDataCache.IsCorporationDefault(corporationEid))
                    {
                        forMembersOf = corporationEid;
                    }
                    else
                    {
                        forMyCorporation = false;
                    }
                }

                //unstack a fraction or take the whole item, check for conditions
                var itemToSell = market.PrepareItemForSale(seller, itemEid, quantity, sourceContainer);

                if (_marketInfoService.CheckAveragePrice)
                {
                    var avgPrice = _marketHandler.GetAveragePriceByMarket(market, itemToSell.Definition);

                    if (avgPrice != null && avgPrice.AveragePrice > 0)
                    {
                        (pricePerPiece < avgPrice.AveragePrice * (1 - _marketInfoService.Margin) ||
                         pricePerPiece > avgPrice.AveragePrice * (1 + _marketInfoService.Margin)).ThrowIfTrue(ErrorCodes.PriceOutOfAverageRange);
                    }
                }

                MarketOrder highestBuyOrder;

                if (targetOrderId > 0)
                {
                    //target order was defined by user
                    highestBuyOrder = _marketOrderRepository.Get(targetOrderId).ThrowIfNull(ErrorCodes.ItemNotFound);

                    //for my corp?
                    highestBuyOrder.forMembersOf?.ThrowIfNotEqual(corporationEid, ErrorCodes.AccessDenied);

                    //sell to order
                    market.FulfillSellOrderInstantly(seller, useSellerCorporationWallet, highestBuyOrder, itemToSell, sourceContainer);
                }
                else
                {
                    //try to find a buy order for the currently submitted item, it finds the closest buy order
                    highestBuyOrder = _marketOrderRepository.GetHighestBuyOrder(itemToSell.Definition, pricePerPiece, seller.Eid, market, corporationEid);

                    if (!forMyCorporation && highestBuyOrder != null)
                    {
                        //sell to order
                        market.FulfillSellOrderInstantly(seller, useSellerCorporationWallet, highestBuyOrder, itemToSell, sourceContainer);
                    }
                    else
                    {
                        if (!forMyCorporation)
                        {
                            _marketHelper.CheckSellOrderCounts(seller).ThrowIfFalse(ErrorCodes.MarketItemsExceed);
                        }

                        var realMarketFee = Market.GetRealMarketFee(seller, duration);

                        //cash market fee anyways
                        _marketHelper.CashInMarketFee(seller, useSellerCorporationWallet, realMarketFee);
                        market.GetDockingBase().AddCentralBank(TransactionType.marketFee, realMarketFee);

                        market.CreateSellOrder(seller.Eid, itemToSell, duration, pricePerPiece, quantity, useSellerCorporationWallet, forMembersOf);
                    }
                }

                publicContainer.Save();

                var containerData = publicContainer.ToDictionary();

                Message.Builder.SetCommand(Commands.ListContainer)
                    .WithData(containerData)
                    .ToCharacter(seller)
                    .Send();
                scope.Complete();
            }
        }
    }
}