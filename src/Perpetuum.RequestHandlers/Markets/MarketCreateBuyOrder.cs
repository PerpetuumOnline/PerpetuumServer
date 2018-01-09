using System.Collections.Generic;
using Perpetuum.Common.Loggers.Transaction;
using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.Groups.Corporations;
using Perpetuum.Host.Requests;
using Perpetuum.Services.MarketEngine;

namespace Perpetuum.RequestHandlers.Markets
{
    public class MarketCreateBuyOrder : IRequestHandler
    {
        private readonly MarketHandler _marketHandler;
        private readonly MarketHelper _marketHelper;
        private readonly IMarketInfoService _marketInfoService;
        private readonly IMarketOrderRepository _marketOrderRepository;

        public MarketCreateBuyOrder(MarketHandler marketHandler,MarketHelper marketHelper,IMarketInfoService marketInfoService,IMarketOrderRepository marketOrderRepository)
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
                var buyer = request.Session.Character;
                var itemDefinition = request.Data.GetOrDefault<int>(k.definition);
                var duration = request.Data.GetOrDefault<int>(k.duration);
                var pricePerPiece = request.Data.GetOrDefault<double>(k.price);
                var quantity = request.Data.GetOrDefault(k.quantity, 1);
                var useBuyerCorporationWallet = request.Data.GetOrDefault<int>(k.useCorporationWallet) == 1;
                var forMyCorporation = request.Data.GetOrDefault<int>(k.forMembersOf) == 1;

                quantity.ThrowIfLessOrEqual(0, ErrorCodes.AmountTooLow);

                buyer.IsDocked.ThrowIfFalse(ErrorCodes.CharacterHasToBeDocked);
                buyer.CheckPrivilegedTransactionsAndThrowIfFailed();

                pricePerPiece.ThrowIfLessOrEqual(0, ErrorCodes.IllegalMarketPrice);

                duration.ThrowIfLessOrEqual(1, ErrorCodes.MinimalDurationNotReached);

                var ed = EntityDefault.Get(itemDefinition).ThrowIfEqual(EntityDefault.None, ErrorCodes.DefinitionNotSupported);

                ed.IsSellable.ThrowIfFalse(ErrorCodes.ItemNotSellable);

                var market = buyer.GetCurrentDockingBase().GetMarketOrThrow();

                var realMarketFee = Market.GetRealMarketFee(buyer, duration);

                var corporationEid = buyer.CorporationEid;

                //cash market fee anyways
                _marketHelper.CashInMarketFee(buyer, useBuyerCorporationWallet, realMarketFee);

                buyer.GetCurrentDockingBase().AddCentralBank(TransactionType.marketFee, realMarketFee);

                if (_marketInfoService.CheckAveragePrice)
                {
                    var avgPrice = _marketHandler.GetAveragePriceByMarket(market, itemDefinition);

                    if (avgPrice != null && avgPrice.AveragePrice > 0)
                    {
                        if (pricePerPiece < avgPrice.AveragePrice * (1 - _marketInfoService.Margin) || pricePerPiece > avgPrice.AveragePrice * (1 + _marketInfoService.Margin))
                        {
                            throw new PerpetuumException(ErrorCodes.PriceOutOfAverageRange);
                        }
                    }
                }

                var publicContainer = buyer.GetPublicContainerWithItems();

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

                if (!forMyCorporation)
                {
                    _marketHelper.CheckBuyOrderCounts(buyer).ThrowIfFalse(ErrorCodes.MarketItemsExceed);
                }

                var lowestSellOrder = _marketOrderRepository.GetLowestSellOrder(itemDefinition, pricePerPiece, buyer.Eid, market, corporationEid);

                if (!forMyCorporation && lowestSellOrder != null)
                {
                    // requested item was found on the market, make immediate transaction
                    market.FulfillBuyOrderInstantly(buyer, useBuyerCorporationWallet, lowestSellOrder, pricePerPiece, duration, quantity, publicContainer, forMembersOf);
                }
                else
                {
                    var deposit = pricePerPiece * quantity;

                    // take the deposit from the character
                    _marketHelper.CashIn(buyer, useBuyerCorporationWallet, pricePerPiece, itemDefinition, quantity, TransactionType.buyOrderDeposit);

                    //store the deposit in the central bank
                    market.AddCentralBank(TransactionType.buyOrderDeposit, deposit);

                    // create a new buy order
                    var newBuyOrder = market.CreateBuyOrder(buyer, itemDefinition, duration, pricePerPiece, quantity, useBuyerCorporationWallet, forMembersOf);

                    var data = new Dictionary<string, object>
                    {
                        {k.buyOrder, newBuyOrder.ToDictionary()}
                    };

                    Message.Builder.SetCommand(Commands.MarketBuyOrderCreated)
                        .WithData(data)
                        .ToCharacter(buyer)
                        .Send();
                }

                publicContainer.Save();

                Message.Builder.SetCommand(Commands.ListContainer)
                    .WithData(publicContainer.ToDictionary())
                    .ToCharacter(buyer)
                    .Send();
                
                scope.Complete();
            }
        }
    }
}