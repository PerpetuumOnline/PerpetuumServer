using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Transactions;
using Perpetuum.Accounting.Characters;
using Perpetuum.Common;
using Perpetuum.Common.Loggers.Transaction;
using Perpetuum.Containers;
using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;

using Perpetuum.Groups.Corporations;
using Perpetuum.Items;
using Perpetuum.Log;
using Perpetuum.Robots;
using Perpetuum.Units.DockingBases;
using Perpetuum.Zones;
using Perpetuum.Zones.PBS.DockingBases;

namespace Perpetuum.Services.MarketEngine
{
    /// <summary>
    /// Physical market entity
    /// </summary>
    public class Market : Entity
    {
        private readonly MarketHelper _marketHelper;
        private readonly IMarketOrderRepository _orderRepository;
        private readonly MarketHandler _marketHandler;
        private readonly MarketOrder.Factory _marketOrderFactory;
        private readonly IEntityServices _entityServices;
        private readonly ICentralBank _centralBank;
        private readonly DockingBaseHelper _dockingBaseHelper;

        public Market(MarketHelper marketHelper,IMarketOrderRepository orderRepository,MarketHandler marketHandler,MarketOrder.Factory marketOrderFactory,IEntityServices entityServices,ICentralBank centralBank,DockingBaseHelper dockingBaseHelper)
        {
            _marketHelper = marketHelper;
            _orderRepository = orderRepository;
            _marketHandler = marketHandler;
            _marketOrderFactory = marketOrderFactory;
            _entityServices = entityServices;
            _centralBank = centralBank;
            _dockingBaseHelper = dockingBaseHelper;
        }

        [NotNull]
        public Item GetItemByMarketOrder(MarketOrder marketOrder)
        {
            if ( marketOrder.itemEid == null)
                throw new PerpetuumException(ErrorCodes.ServerError);

            // load market entity
            var itemOnMarket = Item.GetOrThrow((long) marketOrder.itemEid);
            itemOnMarket.Parent.ThrowIfNotEqual(Eid,ErrorCodes.AccessDenied);
            return itemOnMarket;
        }

        [CanBeNull]
        public DockingBase GetDockingBase()
        {
            return _dockingBaseHelper.GetDockingBase(Parent);
        }

        [NotNull]
        public static Market GetOrThrow(long marketEid)
        {
            return (Market) Repository.Load(marketEid).ThrowIfNull(ErrorCodes.MarketNotFound);
        }

        public int GetItemsCount()
        {
            return Repository.GetChildrenCount(Eid);
        }

        public double VendorSellProfit
        {
            get
            {
                return Db.Query().CommandText("select vendorsellprofit from vendors where marketeid=@marketEID")
                               .SetParameter("@marketEID", Eid)
                               .ExecuteScalar<double>();
            }
        }

        public static Market CreateWithRandomEID()
        {
            return (Market)Factory.CreateWithRandomEID(DefinitionNames.PUBLIC_MARKET);
        }

        public bool IsOnTrainingZone()
        {
            var zone = GetDockingBase()?.Zone;
            return zone is TrainingZone;
        }

        public bool IsOnGammaZone()
        {
            return GetDockingBase().IsOnGammaZone();
        }

        public bool IsPlayerControlledMarketTax()
        {
            var helper = new MarketTaxHelper();
            GetDockingBase()?.AcceptVisitor(helper);
            return helper.PlayerControlled;
        }

        private class MarketTaxHelper : IEntityVisitor<PBSDockingBase>
        {
            public bool PlayerControlled { get; private set; }

            public MarketTaxHelper()
            {
                PlayerControlled = false;
            }

            public void Visit(PBSDockingBase entity)
            {
                PlayerControlled = true;
            }
        }

        public MarketOrder CreateSellOrder(long sellerEid, Item item, int duration, double price, int qty, bool useCorporationWallet, long? forMembersOf)
        {
            var newId = Db.Query().CommandText("insert into marketitems (marketeid,itemEID,itemdefinition,submittereid,submitted,duration,isSell,price,quantity,usecorporationwallet,formembersof) values (@marketeid,@itemEID,@itemdefinition,@submittereid,@submitted,@duration,1,@price,@quantity,@useCorpWallet,@formembersof); select cast(scope_identity() as int)")
                .SetParameter("@marketeid", Eid).SetParameter("@itemEID", item.Eid)
                .SetParameter("@itemdefinition", item.Definition)
                .SetParameter("@submittereid", sellerEid)
                .SetParameter("@submitted", DateTime.Now)
                .SetParameter("@duration", duration)
                .SetParameter("@price", price)
                .SetParameter("@quantity", qty)
                .SetParameter("@useCorpWallet", useCorporationWallet)
                .SetParameter("@formembersof", forMembersOf)
                .ExecuteScalar<int>().ThrowIfEqual(0, ErrorCodes.SQLInsertError);

            var marketSellOrder = _marketOrderFactory();
            marketSellOrder.id = newId;
            marketSellOrder.duration = duration;
            marketSellOrder.isSell = true;
            marketSellOrder.itemDefinition = item.Definition;
            marketSellOrder.itemEid = item.Eid;
            marketSellOrder.marketEID = Eid;
            marketSellOrder.price = price;
            marketSellOrder.quantity = qty;
            marketSellOrder.submitted = DateTime.Now;
            marketSellOrder.submitterEID = sellerEid;
            marketSellOrder.useCorporationWallet = useCorporationWallet;
            marketSellOrder.forMembersOf = forMembersOf;
            //save the item with the parent set to the market
            item.Parent = Eid;
            item.Save();

            Transaction.Current.OnCommited(() =>
            {
                //a new sell order is created and the item is taken from the container
                var data = new Dictionary<string, object> { { k.sellOrder, marketSellOrder.ToDictionary() } };
                Message.Builder.SetCommand(Commands.MarketSellOrderCreated)
                    .WithData(data)
                    .ToCharacter(Character.GetByEid(sellerEid))
                    .Send();
            });

            return marketSellOrder;
        }

        public MarketOrder CreateBuyOrder(Character buyer, int itemDefinition, int duration, double price, int qty, bool useCorporationWallet, long? forMembersOf)
        {
            var newId = Db.Query().CommandText("insert into marketitems (marketeid,itemdefinition,submittereid,submitted,duration,isSell,price,quantity,usecorporationwallet,formembersof) values (@marketeid,@itemdefinition,@submittereid,@submitted,@duration,0,@price,@quantity,@useCorpWallet,@forMembersOf); select cast(scope_identity() as int)")
                .SetParameter("@marketeid", Eid)
                .SetParameter("@itemdefinition", itemDefinition)
                .SetParameter("@submittereid", buyer.Eid)
                .SetParameter("@submitted", DateTime.Now)
                .SetParameter("@duration", duration)
                .SetParameter("@price", price)
                .SetParameter("@quantity", qty)
                .SetParameter("@useCorpWallet", useCorporationWallet)
                .SetParameter("@forMembersOf", forMembersOf)
                .ExecuteScalar<int>().ThrowIfEqual(0, ErrorCodes.SQLInsertError);

            var order = _marketOrderFactory();
            order.id = newId;
            order.duration = duration;
            order.isSell = false;
            order.itemDefinition = itemDefinition;
            order.itemEid = null;
            order.marketEID = Eid;
            order.price = price;
            order.quantity = qty;
            order.submitted = DateTime.Now;
            order.submitterEID = buyer.Eid;
            order.useCorporationWallet = useCorporationWallet;
            order.forMembersOf = forMembersOf;
            return order;
        }


        private const double DEFAULT_MARKET_TAX = 0.12;

        private double Tax
        {
            get { return GetTax();  }
            set { StoreTax(value);  }
        }

        private double GetTax()
        {
            if (!IsPlayerControlledMarketTax()) return DEFAULT_MARKET_TAX;

            var tmp = DynamicProperties.GetOrDefault<double>(k.tax);
            if (tmp >= 0)
            {
                //tovabbi szures itt, pl nem lehet kisebb, mint default

                return tmp;
            }

            return DEFAULT_MARKET_TAX;
        }

        protected virtual void StoreTax(double newTax)
        {
            DynamicProperties.Set(k.tax, newTax.Clamp());
            this.Save();
            Logger.Info("new market tax: " + newTax + " " + this);
        }

        public double TaxMultiplier
        {
            get { return 1.0 - Tax; }
        }

        public void SetTax(Character character, double newTax)
        {
            IsPlayerControlledMarketTax().ThrowIfFalse(ErrorCodes.AccessDenied);

            var oldTax = Tax;
                 
            newTax = newTax.Clamp();
            
            var coporationEid = character.CorporationEid;

            ProfitingOwnerSelector.GetProfitingOwner(GetDockingBase()).ThrowIfNull(ErrorCodes.AccessDenied);

            var corporation = PrivateCorporation.GetOrThrow(coporationEid);
            var role = corporation.GetMemberRole(character);

            role.IsAnyRole(CorporationRole.CEO, CorporationRole.DeputyCEO, CorporationRole.Accountant ).ThrowIfFalse(ErrorCodes.InsufficientPrivileges);
            
            //write log
            var e = new MarketTaxChangeLogEvent
            {
                BaseEid = GetDockingBase().Eid,
                ChangeFrom = oldTax,
                ChangeTo = newTax,
                CharacterId = character.Id,
                Owner = coporationEid,
            };

            GetTaxChangeLogger().Log(e);
            
            //set value
            Tax = newTax;
        }

        public MarketTaxChangeLogger GetTaxChangeLogger()
        {
            return new MarketTaxChangeLogger(this);
        }

        public override Dictionary<string, object> ToDictionary()
        {
            var info = base.ToDictionary();
            info.Add(k.tax, Tax);
            info.Add("taxControl", IsPlayerControlledMarketTax());
            return info;
        }

        public void AddCentralBank(TransactionType transactionType, double amount)
        {
            if (IsOnTrainingZone())
                return;

            _centralBank.AddAmount(amount,transactionType);
        }

        public void ForceInsertAveragePrice(int itemDefinition, double price, int quantity, DateTime dateTime)
        {
            Db.Query().CommandText("insertAveragePrice")
                .SetParameter("@marketEID", Eid)
                .SetParameter("@itemDefinition", itemDefinition)
                .SetParameter("@price", price)
                .SetParameter("@quantity", (long)quantity)
                .SetParameter("@date", dateTime)
                .ExecuteNonQuery().ThrowIfEqual(0, ErrorCodes.SQLInsertError);
        }

        public void InsertStatsForPeriod(int days, double price, int amount, int definition)
        {
            var startDate = DateTime.Today.AddHours(12);
            var totalPrice = price * amount;

            for (var i = 0; i < days; i++)
            {
                var currentDate = startDate.AddDays(-1 * i);
                ForceInsertAveragePrice(definition, totalPrice, amount, currentDate);
            }
        }

        public Dictionary<string, object> GetAverageHistory(int day, int itemDefinition)
        {
            var startDate = DateTime.Today.AddDays(-1*day);

            var count = 0;
            var prices = Db.Query().CommandText(@"select totalprice / quantity as price,date,dailyhighest,dailylowest,quantity from 
															marketaverageprices where 
															marketeid = @marketEID and 
															itemdefinition = @itemDefinition and 
															date >= @startDate")
                .SetParameter("@marketEID", Eid)
                .SetParameter("@itemDefinition", itemDefinition)
                .SetParameter("@day", day)
                .SetParameter("@startDate", startDate)
                .Execute().Select(r => (object) new Dictionary<string, object>
                {
                    {k.price, r.GetValue<double>(0)},
                    {k.date, r.GetValue<DateTime>(1)},
                    {k.high, r.GetValue<double>(2)},
                    {k.low, r.GetValue<double>(3)},
                    {k.quantity, r.GetValue<long>(4)},
                }).ToDictionary(i => "i" + count++);

            var result = new Dictionary<string, object>
            {
                {k.data, prices},
                {k.definition, itemDefinition},
                {k.marketEID, Eid}
            };

            return result;
        }

        [NotNull]
        public Item PrepareItemForSale(Character character, long itemEid, int quantity, Container container)
        {
            container.ThrowIfType<VolumeWrapperContainer>(ErrorCodes.AccessDenied);

            var itemToSell = container.GetItemOrThrow(itemEid);
            itemToSell.CheckOwnerOnlyCharacterAndThrowIfFailed(character);
            itemToSell.ED.IsSellable.ThrowIfFalse(ErrorCodes.ItemNotSellable);
            itemToSell.IsDamaged.ThrowIfTrue(ErrorCodes.ItemHealthMismatch);
            itemToSell.Quantity.ThrowIfLess(quantity, ErrorCodes.InvalidQuantity);
            itemToSell.Parent.ThrowIfEqual(Eid, ErrorCodes.ItemAlreadyExists);

            if (itemToSell is Robot robot)
            {
                robot.IsSelected.ThrowIfTrue(ErrorCodes.RobotMustBeDeselected);
            }

            //is it repacked?
            if (itemToSell.ED.AttributeFlags.Repackable)
            {
                itemToSell.IsRepackaged.ThrowIfFalse(ErrorCodes.ItemNotPacked);
            }

            if (itemToSell.Quantity > quantity)
            {
                //work with the fraction item from now on 
                itemToSell = itemToSell.Unstack(quantity);
            }

            return itemToSell;
        }

        // A sell order was found so the deal is done instantly
        public void FulfillBuyOrderInstantly(Character buyer, bool useBuyerCorporationWallet, MarketOrder marketSellOrder, double pricePerPiece, int duration, int quantity, PublicContainer publicContainer, long? forMembersOf)
        {
            var forCorporation = forMembersOf != null;
            var seller = Character.GetByEid(marketSellOrder.submitterEID);

            Item itemOnMarket;

            if (!marketSellOrder.isVendorItem)
            {
                //the seller is NOT a vendor
                itemOnMarket = GetItemByMarketOrder(marketSellOrder);

                seller.ThrowIfEqual(null, ErrorCodes.CharacterNotFound);

                if (itemOnMarket.Quantity > quantity)
                {
                    // unstack a fraction
                    var fractionItem = itemOnMarket.Unstack(quantity);
                    fractionItem.Owner = buyer.Eid;

                    // save the remaining item 
                    itemOnMarket.Save();

                    // add to public container and change owner
                    publicContainer.AddItem(fractionItem, true);

                    //the remaining amount
                    marketSellOrder.quantity = itemOnMarket.Quantity;

                    // update remaining amount
                    _orderRepository.UpdateQuantity(marketSellOrder);

                    //cash in
                    _marketHelper.CashIn(buyer, useBuyerCorporationWallet, marketSellOrder.price, marketSellOrder.itemDefinition, quantity, TransactionType.marketBuy);

                    //pay out
                    this.PayOutToSeller(seller,marketSellOrder.useCorporationWallet,itemOnMarket.Definition,marketSellOrder.price,quantity,TransactionType.marketSell,marketSellOrder.IsAffectsAverage(),forCorporation);
                }
                else if (itemOnMarket.Quantity == quantity)
                {

                    itemOnMarket.Owner = buyer.Eid;

                    // add to public container and change owner
                    publicContainer.AddItem(itemOnMarket, true);

                    //delete the sell order coz it's done
                    _orderRepository.Delete(marketSellOrder);

                    //cash in
                    _marketHelper.CashIn(buyer, useBuyerCorporationWallet, marketSellOrder.price, marketSellOrder.itemDefinition, quantity, TransactionType.marketBuy);

                    //pay out
                    this.PayOutToSeller(seller,marketSellOrder.useCorporationWallet,itemOnMarket.Definition,marketSellOrder.price,quantity,TransactionType.marketSell,marketSellOrder.IsAffectsAverage(),forCorporation);

                    marketSellOrder.quantity = 0; //signal the sell order delete to the client
                }
                else if (itemOnMarket.Quantity < quantity)
                {
                    //a part of the buy order is fulfilled immediately
                    itemOnMarket.Owner = buyer.Eid;

                    // add to public container and change owner
                    publicContainer.AddItem(itemOnMarket, true);

                    _orderRepository.Delete(marketSellOrder);

                    // create a buy order for the rest of the quantity
                    var newBuyOrder = CreateBuyOrder(buyer, marketSellOrder.itemDefinition, duration, pricePerPiece, quantity - itemOnMarket.Quantity, marketSellOrder.useCorporationWallet, forMembersOf);

                    // cash in for the actual transaction
                    _marketHelper.CashIn(buyer, useBuyerCorporationWallet, marketSellOrder.price, marketSellOrder.itemDefinition, itemOnMarket.Quantity, TransactionType.marketBuy);

                    // cash in for the deposit - for the rest of the quantity
                    _marketHelper.CashIn(buyer, useBuyerCorporationWallet, pricePerPiece, marketSellOrder.itemDefinition, quantity - itemOnMarket.Quantity, TransactionType.buyOrderDeposit);

                    AddCentralBank(TransactionType.buyOrderDeposit, pricePerPiece * (quantity - itemOnMarket.Quantity));

                    //pay out for the current market item
                    this.PayOutToSeller(seller,marketSellOrder.useCorporationWallet,itemOnMarket.Definition,marketSellOrder.price,itemOnMarket.Quantity,TransactionType.marketSell,marketSellOrder.IsAffectsAverage(),forCorporation);

                    marketSellOrder.quantity = 0; //signal to the client

                    //the item he just bought, the sell order update

                    //the new buy order
                    Message.Builder.SetCommand(Commands.MarketBuyOrderCreated)
                        .WithData(new Dictionary<string, object> { { k.buyOrder, newBuyOrder.ToDictionary() } })
                        .ToCharacter(buyer)
                        .Send();
                }

                Market.SendMarketItemBoughtMessage(buyer,itemOnMarket);

                Message.Builder.SetCommand(Commands.MarketSellOrderUpdate)
                    .WithData(new Dictionary<string, object> { { k.sellOrder, marketSellOrder.ToDictionary() } })
                    .ToCharacters(seller, buyer)
                    .Send();

                return;
            }

            //check VENDOR sell order's quantity
            if (marketSellOrder.quantity > 0)
            {
                //finite order cases

                var boughtQuantity = quantity;

                if (marketSellOrder.quantity == quantity)
                {
                    _orderRepository.Delete(marketSellOrder);
                    marketSellOrder.quantity = 0; //signal client
                }
                else if (marketSellOrder.quantity > quantity)
                {
                    marketSellOrder.quantity -= quantity; //signal client
                    _orderRepository.UpdateQuantity(marketSellOrder);
                }
                else if (marketSellOrder.quantity < quantity)
                {
                    _orderRepository.Delete(marketSellOrder);
                    boughtQuantity = marketSellOrder.quantity;

                    //create buyorder for the rest of the quantity
                    var buyOrder = CreateBuyOrder(buyer, marketSellOrder.itemDefinition, duration, pricePerPiece, quantity - marketSellOrder.quantity, marketSellOrder.useCorporationWallet, forMembersOf);

                    Message.Builder.SetCommand(Commands.MarketBuyOrderCreated)
                        .WithData(new Dictionary<string, object> { { k.item, buyOrder.ToDictionary() } })
                        .ToCharacter(buyer)
                        .Send();

                    marketSellOrder.quantity = 0; //signal client

                    //cash in deposit
                    _marketHelper.CashIn(buyer, useBuyerCorporationWallet, pricePerPiece, marketSellOrder.itemDefinition, quantity - boughtQuantity, TransactionType.buyOrderDeposit);

                    AddCentralBank(TransactionType.buyOrderDeposit, pricePerPiece * (quantity - boughtQuantity));
                }

                //take the money for the quantity bought
                _marketHelper.CashIn(buyer, useBuyerCorporationWallet, marketSellOrder.price, marketSellOrder.itemDefinition, boughtQuantity, TransactionType.marketBuy);

                Message.Builder.SetCommand(Commands.MarketSellOrderUpdate)
                    .WithData(new Dictionary<string, object> { { k.sellOrder, marketSellOrder.ToDictionary() } })
                    .ToCharacter(buyer)
                    .Send();

                // vendor stuff
                itemOnMarket = publicContainer.CreateAndAddItem(marketSellOrder.itemDefinition, false, item =>
                {
                    item.Owner = buyer.Eid;
                    item.Quantity = boughtQuantity;
                });

                Market.SendMarketItemBoughtMessage(buyer,itemOnMarket);

                //average price
                _marketHandler.InsertAveragePrice(this, marketSellOrder.itemDefinition, boughtQuantity * marketSellOrder.price, boughtQuantity);

                AddCentralBank(TransactionType.marketBuy, boughtQuantity * pricePerPiece); //vendor income
                return;
            }

            //infinite quantity case
            _marketHelper.CashIn(buyer, useBuyerCorporationWallet, marketSellOrder.price, marketSellOrder.itemDefinition, quantity, TransactionType.marketBuy);

            itemOnMarket = publicContainer.CreateAndAddItem(marketSellOrder.itemDefinition, false, item =>
            {
                item.Owner = buyer.Eid;
                item.Quantity = quantity;
            });

            Market.SendMarketItemBoughtMessage(buyer,itemOnMarket);

            //average price
            _marketHandler.InsertAveragePrice(this, marketSellOrder.itemDefinition, quantity * marketSellOrder.price, quantity);

            AddCentralBank(TransactionType.marketBuy, quantity * pricePerPiece); //vendor income
        }

        public void PayOutToSeller(Character seller, bool useSellerCorporationWallet, int definition, double price, int quantity, TransactionType transactionType, bool affectAverage, bool forCorporation)
        {
            var taxRate = forCorporation ? 1.0 : GetMarketTaxRate(seller);

            var resultPrice = price * quantity * taxRate;

            var sellerWallet = seller.GetWallet(useSellerCorporationWallet, transactionType);
            sellerWallet.Balance += resultPrice;

            var b = TransactionLogEvent.Builder()
                                       .SetCharacter(seller)
                                       .SetTransactionType(transactionType)
                                       .SetCreditBalance(sellerWallet.Balance)
                                       .SetCreditChange(resultPrice)
                                       .SetItem(definition, quantity);

            var corpWallet = sellerWallet as CorporationWallet;
            if (corpWallet != null)
            {
                b.SetCorporation(corpWallet.Corporation);
                corpWallet.Corporation.LogTransaction(b);
            }
            else
            {
                seller.LogTransaction(b);
            }

            if (affectAverage)
            {
                //itt playernek fizetunk ki
                _marketHandler.InsertAveragePrice(this, definition, price * quantity, quantity);
            }

            if (forCorporation) return;
            var tax = price * quantity * (1 - taxRate);

            var dockingBase = GetDockingBase();
            dockingBase.AddCentralBank(TransactionType.MarketTax, tax);
        }

        public void BuyOrderFulfilledToCharacter(Character seller, bool useSellersCorporationWallet, MarketOrder buyOrder, int boughtQuantity, Container container, Item boughtItem, Character buyer)
        {
            var forCorporation = buyOrder.forMembersOf != null;

            buyOrder.quantity = buyOrder.quantity - boughtQuantity;

            //the container is null -> automatic sell order process
            //the container is NOT null -> a player started a request to sell an item from a container
            //dont remove fresh fraction items, they are NOT in the container
            if (boughtItem.Parent != 0)
            {
                container?.RemoveItemOrThrow(boughtItem);
            }

            var publicContainer = GetDockingBase().GetPublicContainer();

            //put the new item to the local public container, set the owner the buyer
            boughtItem.Parent = publicContainer.Eid;
            boughtItem.Owner = buyer.Eid;

            //ez azert kell, mert a megvett item nem abban a kontenerben van mar, tehat a save nem fogja elmenteni
            //a masik pedig ha az item egy fraction -unstack csinalta- akkor el kell menteni mindenkeppen
            boughtItem.Save();

            if (buyOrder.quantity == 0)
            {
                //buy completely order fulfilled, delete it
                _orderRepository.Delete(buyOrder);
            }
            else
            {
                //update the buy order's quantity, and leave the rest on the market
                _orderRepository.UpdateQuantity(buyOrder);
            }

            //pay out the fulfilled amount immediately using the price of the found buyorder to the seller
            PayOutToSeller(seller,useSellersCorporationWallet,boughtItem.Definition,buyOrder.price,boughtItem.Quantity,TransactionType.marketSell,buyOrder.IsAffectsAverage(),forCorporation);

            _centralBank.SubAmount(buyOrder.price*boughtItem.Quantity,TransactionType.marketSell);

            Market.SendMarketItemBoughtMessage(buyer,boughtItem);

            Message.Builder.SetCommand(Commands.MarketBuyOrderUpdate)
                .WithData(new Dictionary<string, object> {{k.buyOrder, buyOrder.ToDictionary()}})
                .ToCharacters(seller, buyer)
                .Send();
        }

        public void FiniteVendorBuyOrderTakesTheItem(bool useSellerCorporationWallet, MarketOrder vendorBuyOrder, Item boughtItem, Character seller)
        {
            vendorBuyOrder.quantity = vendorBuyOrder.quantity - boughtItem.Quantity;

            Repository.Delete(boughtItem);

            if (vendorBuyOrder.quantity == 0)
            {
                _orderRepository.Delete(vendorBuyOrder);
            }
            else
            {
                _orderRepository.UpdateQuantity(vendorBuyOrder);
            }

            //do payout
            PayOutToSeller(seller,useSellerCorporationWallet,boughtItem.Definition,vendorBuyOrder.price,boughtItem.Quantity,TransactionType.marketSell,true,false);

            _centralBank.SubAmount(vendorBuyOrder.price*boughtItem.Quantity,TransactionType.marketSell);

            Message.Builder.SetCommand(Commands.MarketBuyOrderUpdate)
                .WithData(new Dictionary<string, object> {{k.buyOrder, vendorBuyOrder.ToDictionary()}})
                .ToCharacter(seller)
                .Send();
        }

        public void FulfillSellOrderInstantly(Character seller, bool useSellerCorporationWallet, MarketOrder buyOrder, Item itemToSell, Container container)
        {
			
            if (!buyOrder.isVendorItem)
            {
                var buyer = Character.GetByEid(buyOrder.submitterEID);

                //nem vendor akar venni ilyet akkor---
                if (itemToSell.Quantity > buyOrder.quantity)
                {
                    //talalt vasarlot, de a item amit el akarok adni az tobb darabbol all, ezert kistackolunk belole annyit amennyit meg akarnak venni
                    var boughtItem = itemToSell.Unstack(buyOrder.quantity);
                    BuyOrderFulfilledToCharacter(seller, useSellerCorporationWallet, buyOrder, buyOrder.quantity, container, boughtItem, buyer);
                    container.AddItem(itemToSell, false);
                }
                else if (itemToSell.Quantity <= buyOrder.quantity)
                {
                    //item sold completely
                    //OR
                    //a part of the buy order is fulfilled
                    BuyOrderFulfilledToCharacter(seller, useSellerCorporationWallet, buyOrder, itemToSell.Quantity, container, itemToSell, buyer);

                    Message.Builder.SetCommand(Commands.MarketItemSold)
                        .WithData(new Dictionary<string, object> {{k.item, itemToSell.BaseInfoToDictionary()}})
                        .ToCharacter(seller)
                        .Send();
                }

                return;
            }

            // a vendor wants to buy this item
            if (buyOrder.quantity > 0)
            {
                //finite vendor buy order
                if (buyOrder.quantity >= itemToSell.Quantity)
                {
                    //item completely sold to vendor
                    FiniteVendorBuyOrderTakesTheItem(useSellerCorporationWallet, buyOrder, itemToSell, seller);

                    Message.Builder.SetCommand(Commands.MarketItemSold)
                        .WithData(new Dictionary<string, object> {{k.item, itemToSell.BaseInfoToDictionary()}})
                        .ToCharacter(seller)
                        .Send();
                }
                else if (buyOrder.quantity < itemToSell.Quantity)
                {
                    _orderRepository.Delete(buyOrder);
                    itemToSell.Quantity = itemToSell.Quantity - buyOrder.quantity;

                    //do payout
                    PayOutToSeller(seller,useSellerCorporationWallet,itemToSell.Definition,buyOrder.price,buyOrder.quantity,TransactionType.marketSell,true,false);

                    //average price
                    _marketHandler.InsertAveragePrice(this, itemToSell.Definition, buyOrder.quantity*buyOrder.price, buyOrder.quantity);

                    _centralBank.SubAmount(buyOrder.quantity*buyOrder.price,TransactionType.marketSell);

                    buyOrder.quantity = 0; //signal to client

                    Message.Builder.SetCommand(Commands.MarketBuyOrderUpdate)
                        .WithData(new Dictionary<string, object> {{k.buyOrder, buyOrder.ToDictionary()}})
                        .ToCharacter(seller)
                        .Send();
                }

                return;
            }

            //infinite vendor buy order
            Repository.Delete(itemToSell);

            //do payout
            PayOutToSeller(seller,useSellerCorporationWallet,itemToSell.Definition,buyOrder.price,itemToSell.Quantity,TransactionType.marketSell,true,false);

            _centralBank.SubAmount(buyOrder.price*itemToSell.Quantity,TransactionType.marketSell);

            Message.Builder.SetCommand(Commands.MarketItemSold)
                .WithData(new Dictionary<string, object> {{k.item, itemToSell.BaseInfoToDictionary()}})
                .ToCharacter(seller)
                .Send();
        }

        /// <summary>
        /// add purchasable stuff to market
        /// </summary>
        [Conditional("DEBUG")]
        public void AddDevItemsToMarket()
        {
            Db.Query().CommandText("marketAddAllItemsDev").SetParameter("@marketEID", Eid).ExecuteNonQuery();
        }

        public void InsertVendorBuyOrder(int definition, double price, long vendorEid = 0)
        {
            const string query = @"INSERT dbo.marketitems (
	marketeid,
	itemdefinition,
	submittereid,
	duration,
	isSell,
	price,
	quantity,
	isvendoritem
) VALUES ( 
	@marketEid,
	@definition,
	@vendorEid,
	0,
	0,
	@price,
	-1,
	1
) ";
            Db.Query().CommandText(query)
                .SetParameter("@marketEid", Eid)
                .SetParameter("@price", price)
                .SetParameter("@definition", definition)
                .SetParameter("@vendorEid", vendorEid)
                .ExecuteNonQuery().ThrowIfEqual(0, ErrorCodes.SQLInsertError);
        }

        public void AddOtherStuffToGammaMarket()
        {
            //5373 5375
            //def_ammo_mining_probe_energymineral_direction
            //def_ammo_mining_probe_energymineral_tile
            InsertVendorBuyOrder(5373,45);
            InsertVendorBuyOrder(5375,45);

            //5376	def_gate_capsule
            InsertVendorBuyOrder(5376, 250000);
        }

        public long GetVendorEid()
        {
            return Db.Query().CommandText("select vendoreid from vendors where marketeid=@marketeid")
                .SetParameter("@marketeid", Eid)
                .ExecuteScalar<long>();
        }

        public void AddCategoryToMarket(long vendorEID, string categoryFlag, int duration, long price, bool isSell, int quantity, bool addNamed, string nameFilter)
        {
            CategoryFlags cf;
            Enum.TryParse(categoryFlag, true, out cf).ThrowIfFalse(ErrorCodes.CategoryflagNotFound);

            //get the definitions for this category flag

            Db.Query().CommandText("select count(*) from vendors where marketeid=@marketEID and vendoreid=@vendorEID")
                .SetParameter("@marketEID", Eid)
                .SetParameter("@vendorEID", vendorEID)
                .ExecuteScalar<int>().ThrowIfEqual(0, ErrorCodes.ItemNotFound);

            foreach (var ed in _entityServices.Defaults.GetAll().GetByCategoryFlags(cf))
            {
                if (!ed.IsSellable) continue;
                
                if (!addNamed && ed.Name.Contains("named"))
                    continue;

                if (!nameFilter.IsNullOrEmpty())
                {
                    if (!ed.Name.Contains(nameFilter)) continue;
                }
                
                const string insertCmdText = @"insert marketitems (marketeid, itemdefinition, submittereid, duration, isSell, price, quantity, isvendoritem) values
                                                          (@marketeid, @itemdefinition, @submittereid, @duration, @isSell, @price, @quantity, @isvendoritem)";
                Db.Query().CommandText(insertCmdText)
                    .SetParameter("@marketeid", Eid)
                    .SetParameter("@itemdefinition",ed.Definition)
                    .SetParameter("@submittereid", vendorEID)
                    .SetParameter("@duration", duration)
                    .SetParameter("@isSell", isSell)
                    .SetParameter("@price", price)
                    .SetParameter("@quantity", quantity)
                    .SetParameter("@isvendoritem", true)
                    .ExecuteNonQuery();
            }

            Logger.Info("category flag:" + categoryFlag + " added to market:" + Eid + " vendor:" + vendorEID + " isSell:" + isSell + " quantity:" + quantity + " duration:" + duration);
        }

        public static void ClearVendorItems(long vendorEID, bool isSell)
        {
            var res = Db.Query().CommandText("delete marketitems where submittereid=@vendorEID and isSell=@isSell")
                .SetParameter("@vendorEID", vendorEID)
                .SetParameter("@isSell", isSell)
                .ExecuteNonQuery();

            Logger.Info(res + " item(s) deleted from the market for vendor:" + vendorEID + " isSell:" + isSell);
        }

        public static void AutoProcessSellorders(MarketOrder sellOrder, MarketOrder buyOrder)
        {
            var seller = Character.GetByEid(sellOrder.submitterEID);
            var market = sellOrder.GetMarket();
            var useSellerCorporationWallet = sellOrder.useCorporationWallet;

            if (sellOrder.itemEid == null)
                return; //wtf

            var itemToSell = Item.GetOrThrow((long) sellOrder.itemEid);
            var buyer = Character.GetByEid(buyOrder.submitterEID);
			
            if (itemToSell.Quantity > buyOrder.quantity)
            {
                //az item tobb darabbol all, kistackolunk belole
                var boughtItem = itemToSell.Unstack(buyOrder.quantity);
                market.BuyOrderFulfilledToCharacter(seller, useSellerCorporationWallet, buyOrder, buyOrder.quantity, null, boughtItem, buyer);
				
                //elmentjuk, o marad fent a marketen
                itemToSell.Save();

                //ennyit adtunk el az orderbol
                sellOrder.quantity = sellOrder.quantity - boughtItem.Quantity;
                market._orderRepository.UpdateQuantity(sellOrder);
            }
            else if (itemToSell.Quantity <= buyOrder.quantity)
            {
                //item sold completely
                //OR
                //a part of the buy order is fulfilled
                market.BuyOrderFulfilledToCharacter(seller, useSellerCorporationWallet, buyOrder, itemToSell.Quantity, null, itemToSell, buyer);

                //a sellordert le lehet torolni, sikerult mindet eladni
                //az item at lett parentelve/owner a vevohoz
                market._orderRepository.Delete(sellOrder);
            }
        }

        public static void SendMarketItemBoughtMessage(Character character,Item item)
        {
            Message.Builder.SetCommand(Commands.MarketItemBought)
                .WithData(new Dictionary<string,object> { { k.item,item.BaseInfoToDictionary() } })
                .ToCharacter(character)
                .Send();
        }


        public const int MARKET_FEE = 10;

        public double GetMarketTaxRate(Character character)
        {
            // 0.12 => 12% tax
            // 0.88 on default markets
            return (TaxMultiplier + character.GetExtensionsBonusSummary(ExtensionNames.MARKET_TRANSACTION_TAX)).Clamp();
        }

        public static double GetRealMarketFee(Character character,int duration)
        {
            var realMarketFee = MARKET_FEE * GetMarketFeeRate(character) * duration;
            return realMarketFee;
        }

        public static double GetMarketFeeRate(Character character)
        {
            var extensionValue = character.GetExtensionsBonusSummary(ExtensionNames.MARKET_TRANSACTION_FEE);
            return 1.0 - extensionValue;
        }

        public static int GetMaxSellOrderCount(Character character)
        {
            return (int)(character.GetExtensionBonusWithPrerequiredExtensions(ExtensionNames.TRADING_MARKET_SELLORDERCOUNT_EXPERT) + 1);
        }

        public static int GetMaxBuyOrderCount(Character character)
        {
            return (int)(character.GetExtensionBonusWithPrerequiredExtensions(ExtensionNames.TRADING_MARKET_BUYORDERCOUNT_EXPERT) + 1);
        }
    }


}