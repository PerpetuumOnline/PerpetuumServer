using System.Collections.Generic;
using Perpetuum.Accounting.Characters;
using Perpetuum.Common.Loggers.Transaction;
using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Services.MarketEngine;

namespace Perpetuum.RequestHandlers.Markets
{
    public class MarketBuyItem : IRequestHandler
    {
        private readonly MarketHandler _marketHandler;
        private readonly MarketHelper _marketHelper;
        private readonly IMarketOrderRepository _marketOrderRepository;

        public MarketBuyItem(MarketHandler marketHandler,MarketHelper marketHelper,IMarketOrderRepository marketOrderRepository)
        {
            _marketHandler = marketHandler;
            _marketHelper = marketHelper;
            _marketOrderRepository = marketOrderRepository;
        }

        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var buyer = request.Session.Character;
                var marketItemId = request.Data.GetOrDefault<int>(k.marketItemID);
                var useCorporationWallet = request.Data.GetOrDefault<int>(k.useCorporationWallet) == 1;
                var quantity = request.Data.GetOrDefault<int>(k.quantity);

                quantity.ThrowIfLessOrEqual(0, ErrorCodes.WTFErrorMedicalAttentionSuggested);
                buyer.IsDocked.ThrowIfFalse(ErrorCodes.CharacterHasToBeDocked);
                buyer.CheckPrivilegedTransactionsAndThrowIfFailed();

                var market = buyer.GetCurrentDockingBase().GetMarketOrThrow();

                var sellOrder = _marketOrderRepository.Get(marketItemId).ThrowIfNull(ErrorCodes.ItemNotFound);

                sellOrder.submitterEID.ThrowIfEqual(buyer.Eid, ErrorCodes.CannotBuyFromYourself);

                var corporationEid = buyer.CorporationEid;

                if (sellOrder.forMembersOf != null)
                {
                    corporationEid.ThrowIfNotEqual((long)sellOrder.forMembersOf, ErrorCodes.AccessDenied);
                }

                var publicContainer = buyer.GetPublicContainerWithItems();

                var sellerEid = sellOrder.submitterEID;
                var seller = Character.GetByEid(sellerEid);

                if (!sellOrder.isVendorItem)
                {
                    //nem vendor, hanem player az aki eladja a cuccot

                    var forCorporation = sellOrder.forMembersOf != null;

                    var boughtQuantity = quantity;

                    seller.ThrowIfEqual(null, ErrorCodes.CharacterNotFound);

                    // the item as entity
                    var itemOnMarket = market.GetItemByMarketOrder(sellOrder);

                    var resultItem = itemOnMarket;
                    if (sellOrder.quantity == quantity)
                    {
                        //delete the sell order coz it's done
                        _marketOrderRepository.Delete(sellOrder);
                        sellOrder.quantity = 0;
                    }
                    else if (sellOrder.quantity > quantity)
                    {
                        sellOrder.quantity = sellOrder.quantity - quantity;
                        _marketOrderRepository.UpdateQuantity(sellOrder);

                        resultItem = itemOnMarket.Unstack(quantity);
                        itemOnMarket.Save();
                    }
                    else if (sellOrder.quantity < quantity)
                    {
                        //bought quantity => marketitem.quantity
                        boughtQuantity = sellOrder.quantity;

                        //delete the sell order coz it's done
                        _marketOrderRepository.Delete(sellOrder);
                        sellOrder.quantity = 0;
                    }

                    resultItem.Owner = buyer.Eid;

                    // add to public container
                    publicContainer.AddItem(resultItem, false);
                    publicContainer.Save();

                    //take money
                    _marketHelper.CashIn(buyer, useCorporationWallet, sellOrder.price, sellOrder.itemDefinition, boughtQuantity, TransactionType.marketBuy);
                    //pay out
                    market.PayOutToSeller(seller, sellOrder.useCorporationWallet, resultItem.Definition, sellOrder.price, boughtQuantity, TransactionType.marketSell, sellOrder.IsAffectsAverage(), forCorporation);


                    Market.SendMarketItemBoughtMessage(buyer,resultItem);

                    Message.Builder.SetCommand(Commands.MarketSellOrderUpdate)
                        .WithData(new Dictionary<string, object> { { k.sellOrder, sellOrder.ToDictionary() } })
                        .ToCharacters(seller, buyer)
                        .Send();
                }
                else
                {
                    // the item is a vendor sell order
                    if (sellOrder.quantity < 0)
                    {
                        //infinite quantity case
                        _marketHelper.CashIn(buyer, useCorporationWallet, sellOrder.price, sellOrder.itemDefinition, quantity, TransactionType.marketBuy);

                        var boughtItem = publicContainer.CreateAndAddItem(sellOrder.itemDefinition, false, item =>
                        {
                            item.Owner = buyer.Eid;
                            item.Quantity = quantity;
                        });
                        Market.SendMarketItemBoughtMessage(buyer,boughtItem);

                        //average price
                        _marketHandler.InsertAveragePrice(market, sellOrder.itemDefinition, quantity * sellOrder.price, quantity);
                        market.AddCentralBank(TransactionType.marketBuy, quantity * sellOrder.price); //vendor income
                    }
                    else
                    {
                        // vendor finite 
                        var boughtQuantity = quantity;

                        if (sellOrder.quantity == quantity)
                        {
                            sellOrder.quantity = 0; //signal order delete
                            //all sold
                            _marketOrderRepository.Delete(sellOrder);
                        }
                        else if (sellOrder.quantity < quantity)
                        {
                            boughtQuantity = sellOrder.quantity; //clip the amount
                            sellOrder.quantity = 0; //signal order delete
                            //all sold
                            _marketOrderRepository.Delete(sellOrder);
                        }
                        else if (sellOrder.quantity > quantity)
                        {
                            sellOrder.quantity -= quantity;
                            //update entry
                            _marketOrderRepository.UpdateQuantity(sellOrder);
                        }

                        _marketHelper.CashIn(buyer, useCorporationWallet, sellOrder.price, sellOrder.itemDefinition, boughtQuantity, TransactionType.marketBuy);

                        //average price
                        _marketHandler.InsertAveragePrice(market, sellOrder.itemDefinition, boughtQuantity * sellOrder.price, boughtQuantity);
                        market.AddCentralBank(TransactionType.marketBuy, boughtQuantity * sellOrder.price); //vendor income

                        var boughtItem = publicContainer.CreateAndAddItem(sellOrder.itemDefinition, false, item =>
                        {
                            item.Owner = buyer.Eid;
                            item.Quantity = boughtQuantity;
                        });
                        Market.SendMarketItemBoughtMessage(buyer,boughtItem);

                        Message.Builder.SetCommand(Commands.MarketSellOrderUpdate)
                            .WithData(new Dictionary<string, object> { { k.sellOrder, sellOrder.ToDictionary() } })
                            .ToCharacter(buyer)
                            .Send();
                    }
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