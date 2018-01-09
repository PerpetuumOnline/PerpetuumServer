using System;
using System.Collections.Generic;
using Perpetuum.Accounting.Characters;
using Perpetuum.Common;
using Perpetuum.Common.Loggers.Transaction;
using Perpetuum.EntityFramework;

using Perpetuum.Groups.Corporations;
using Perpetuum.Items;
using Perpetuum.Log;
using Perpetuum.Wallets;

namespace Perpetuum.Services.MarketEngine
{
    /// <summary>
    /// Represents a market order
    /// </summary>
    public class MarketOrder
    {
        private readonly MarketHandler _marketHandler;
        private readonly ICentralBank _centralBank;
        public int id;
        public long marketEID; // the market's eid where item can be found
        public long? itemEid; // the item's eid
        public int itemDefinition; // definition of the order
        public long submitterEID; // the owner of the order
        public DateTime submitted; // time of creation
        public int duration; // order's duration in hours
        public double price; 
        public int quantity; 
        public bool isSell; // sell or buy order
        public bool useCorporationWallet; 
        public bool isVendorItem;
        public long? forMembersOf; // visible to corporation only if not null

        public delegate MarketOrder Factory();

        public MarketOrder(MarketHandler marketHandler,ICentralBank centralBank)
        {
            _marketHandler = marketHandler;
            _centralBank = centralBank;
        }

        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
                              {
                                  {k.marketItemID, id},
                                  {k.itemEID,itemEid},
                                  {k.definition, itemDefinition}, 
                                  {k.submitted, submitted}, 
                                  {k.submitterEID, submitterEID}, 
                                  {k.duration, duration}, 
                                  {k.isSell, isSell}, 
                                  {k.price, price},
                                  {k.quantity,quantity},
                                  {k.useCorporationWallet, useCorporationWallet},
                                  {k.isVendorItem, isVendorItem},
                                  {k.autoExpire, (isVendorItem && duration==0) ? DateTime.Now.AddYears(1) : submitted.AddHours(duration)},
                                  {k.marketEID, marketEID},
                                  {k.baseEID,GetBaseEid()},
                                  {k.forMembersOf, forMembersOf},
                              };
        }

        public Market GetMarket()
        {
            return Market.GetOrThrow(marketEID);
        }

        public override string ToString()
        {
            return $"id:{id} marketEID:{marketEID} itemEID:{itemEid} def:{itemDefinition} submitterEID:{submitterEID} submitted:{submitted} duration:{duration} price:{price} quantity:{quantity} isSell:{isSell} corpW:{useCorporationWallet} isVendor:{isVendorItem} formembersof:{((forMembersOf == null) ? "null" : forMembersOf.ToString())}";
        }

        public EntityDefault EntityDefault
        {
            get { return EntityDefault.Get(itemDefinition); }
        }

        public double FullPrice => price * quantity;

        public Dictionary<string, object> GetValidModifyInfo()
        {
            var validModifyTime = submitted.AddMinutes(MarketInfoService.MARKET_CANCEL_TIME);
            var result = new Dictionary<string, object>
                                         {
                                             {k.expire, validModifyTime},
                                             {k.itemEID, itemEid},
                                             {k.marketItemID, id},
                                             {k.marketEID, marketEID},

                                         };
            return result;
        }

        public bool IsAffectsAverage()
        {
            //if null => order is public, not filtered
            return forMembersOf == null;
        }

        public bool IsModifyTimeValid()
        {
            //check minimal duration
            var validModifyTime = submitted.AddMinutes(MarketInfoService.MARKET_CANCEL_TIME);
            return validModifyTime <= DateTime.Now;
        }

        [CanBeNull]
        public Item Cancel(IMarketOrderRepository orderRepository)
        {
            //skip vendor items - this protects the vendor orders in every call
            if (isVendorItem)
                return null;

            Item canceledItem = null;

            if (isSell)
            {
                //return the item to the container
                canceledItem = ReturnMarketItem();
            }
            else
            {
                //pay back the deposit
                PayBackBuyOrder();
            }

            orderRepository.Delete(this);
            return canceledItem;
        }

        private Item ReturnMarketItem()
        {
            if (itemEid == null)
                return null;

            Logger.Info("returning market item " + this);

            var canceledItem = Item.GetOrThrow((long) itemEid);

            //itt kell kiszedni a markethez tartozo bazis public conteneret
            var container = GetMarket().GetDockingBase().GetPublicContainer();
            container.AddItem(canceledItem, true);
            canceledItem.Save();

            return canceledItem;
        }

        private void PayBackBuyOrder()
        {
            var submitter = Character.GetByEid(submitterEID);

            //use corporation wallet based on the order and not on the current corp of the character
            PrivateCorporation corporation = null;
            if (forMembersOf != null)
            {
                corporation = PrivateCorporation.Get((long) forMembersOf);
            }

            IWallet<double> wallet;
            if (corporation != null && useCorporationWallet)
            {
                wallet = new CorporationWallet(corporation);
            }
            else
            {
                wallet = submitter.GetWallet(false, TransactionType.buyOrderPayBack);
            }

            wallet.Balance += FullPrice;

            var b = TransactionLogEvent.Builder()
                .SetTransactionType(TransactionType.buyOrderPayBack)
                .SetCreditBalance(wallet.Balance)
                .SetCreditChange(FullPrice)
                .SetCharacter(submitter)
                .SetItem(itemDefinition, quantity);

           
            var corpWallet = wallet as CorporationWallet;
            if (corpWallet != null)
            {
                b.SetCorporation(corpWallet.Corporation);
                corpWallet.Corporation.LogTransaction(b);
            }
            else
            {
                submitter.LogTransaction(b);
            }

            _centralBank.SubAmount(FullPrice, TransactionType.MarketTax);
        }

        //caches the order todictionary's order=>market=>baseEid lookup
        private long GetBaseEid()
        {
            if (_marketHandler.TryGetDockingBaseEidForMarketEid(marketEID, out long baseEid))
            {
                return baseEid;
            }

            return 0; //fallback for client
        }
    }
}
