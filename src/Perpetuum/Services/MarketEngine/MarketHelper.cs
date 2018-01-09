using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Perpetuum.Accounting.Characters;
using Perpetuum.Common.Loggers.Transaction;
using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Groups.Corporations;
using Perpetuum.Log;
using Perpetuum.Units.DockingBases;
using Perpetuum.Zones.PBS.DockingBases;

namespace Perpetuum.Services.MarketEngine
{
    public class MarketHelper
    {
        private readonly MarketHandler _marketHandler;
        private readonly DockingBaseHelper _dockingBaseHelper;
        private readonly IMarketOrderRepository _marketOrderRepository;
        private readonly IEntityServices _entityServices;

        public MarketHelper(MarketHandler marketHandler,DockingBaseHelper dockingBaseHelper,IMarketOrderRepository marketOrderRepository,IEntityServices entityServices)
        {
            _marketHandler = marketHandler;
            _dockingBaseHelper = dockingBaseHelper;
            _marketOrderRepository = marketOrderRepository;
            _entityServices = entityServices;
        }

        public void Init()
        {
            GetDefaultMarketsToDictionary = LoadDefaultMarketsToDictionary();
        }

        //alpha and beta zones
        public IDictionary<string, object> GetDefaultMarketsToDictionary = new Dictionary<string, object>();

        private IDictionary<string, object> LoadDefaultMarketsToDictionary()
        {
            return _dockingBaseHelper.GetDefaultDockingBases().Select(b => b.GetMarket()).ToDictionary("m", m => m.ToDictionary());
        }

        public void CashInMarketFee(Character character, bool useCorporationWallet, double pricePerPiece)
		{
			var wallet = character.GetWalletWithAccessCheck(useCorporationWallet, TransactionType.marketFee);
			wallet.Balance -= pricePerPiece;

		    var b = TransactionLogEvent.Builder().SetCharacter(character).SetTransactionType(TransactionType.marketFee).SetCreditBalance(wallet.Balance).SetCreditChange(-pricePerPiece);

            if (wallet is CorporationWallet corpWallet)
            {
                b.SetCorporation(corpWallet.Corporation);
                corpWallet.Corporation.LogTransaction(b);
            }
            else
            {
                character.LogTransaction(b);
            }
        }

		public void CashIn(Character buyer, bool useBuyerCorporationWallet, double price, int definition, int quantity, TransactionType transactionType)
		{
			var wallet = buyer.GetWallet(useBuyerCorporationWallet, transactionType);
			var amount = price*quantity;
			wallet.Balance -= amount;

		    var b = TransactionLogEvent.Builder()
                                       .SetCharacter(buyer)
                                       .SetTransactionType(transactionType)
                                       .SetCreditBalance(wallet.Balance)
                                       .SetCreditChange(-amount)
                                       .SetItem(definition, quantity);

            if (wallet is CorporationWallet corpWallet)
            {
                b.SetCorporation(corpWallet.Corporation);
                corpWallet.Corporation.LogTransaction(b);
            }
            else
            {
                buyer.LogTransaction(b);
            }
        }

        private static readonly int[] _reactorPlasmaDefinitions = {3274, 3273, 3272, 3271};

		public double GetMaxReactorPlasmaPrice(int definition)
		{
			/*
				3274  def_thelodica_reactor_plasma 
				3273  def_nuimqol_reactor_plasma  
				3272  def_pelistal_reactor_plasma  
				3271  def_common_reactor_plasma   
			*/

			var definitionsString = definition.ToString(CultureInfo.InvariantCulture);

			var marketsString = _marketHandler.GetAllDefaultMarketsEids().ArrayToString();

			var query = "SELECT MAX(price) FROM dbo.marketitems WHERE itemdefinition=" + definitionsString + " AND isSell=0 AND isvendoritem=1 AND marketeid IN (" + marketsString + ")";

			var price = Db.Query().CommandText(query).ExecuteScalar<double>();

			Logger.Info("price for " + EntityDefault.Get(definition).Name + " is " + price);

			return price;
		}

		public void InsertGammaPlasmaOrders(Market market)
		{
			foreach (var reactorPlasmaDefinition in _reactorPlasmaDefinitions)
			{
				var price = GetMaxReactorPlasmaPrice(reactorPlasmaDefinition);
				market.InsertVendorBuyOrder(reactorPlasmaDefinition, price);
			}

            market.AddDevItemsToMarket();
			market.AddOtherStuffToGammaMarket();
		}

        public void CreatePlasmaBuyOrdersOnExistingGammaBases()
		{
			const string querySelect = "SELECT eid,(SELECT definition FROM dbo.entities WHERE eid=m.parent) FROM entities m WHERE definition=10";
			const string queryDelete = "delete marketitems where isvendoritem=1 and marketEid=@marketEid and issell=0";

			var markets = Db.Query().CommandText(querySelect).Execute().Select(r => Market.GetOrThrow(r.GetValue<long>(0)));

			foreach (var market in markets)
			{
				var dockingBase = market.GetDockingBase() as PBSDockingBase;
				if (dockingBase == null)
					continue;

				Logger.Info("processing market: " + market.Eid);

				Db.Query().CommandText(queryDelete)
					.SetParameter("@marketEid", market.Eid)
					.ExecuteNonQuery();

				InsertGammaPlasmaOrders(market);
			}
		}

		public void RemoveAll(Character character)
		{
			Db.Query().CommandText("delete marketitems where submittereid=@rootEid")
				.SetParameter("@rootEid", character.Eid)
				.ExecuteNonQuery();
		}

        public void RemoveItemsByCategoryFlags(CategoryFlags categoryFlag, bool withVendor = false)
        {
            var definitions = _entityServices.Defaults.GetAll().GetDefinitionsByCategoryFlag(categoryFlag);

            var defArrayString = definitions.ArrayToString();

            var query = "SELECT marketitemid FROM dbo.marketitems WHERE itemdefinition IN (" + defArrayString + ") and isvendoritem=0";

            var orders = Db.Query().CommandText(query)
                .Execute()
                .Select(r => _marketOrderRepository.Get(r.GetValue<int>("marketitemid")))
                .Where(o => o != null)
                .ToArray();

            var count = 0;
            Logger.Info("cancelling " + orders.Length + " market items for cf:" + categoryFlag);

            foreach (var order in orders)
            {
                Logger.Info("cancelling " + order.EntityDefault.Name + " quantity:" + order.quantity);
                order.Cancel(_marketOrderRepository);
                count++;
            }

            Logger.Info("cancelled " + count + " market items for cf:" + categoryFlag);

            if (!withVendor)
                return;
            Logger.Info("removing vendor market items for cf:" + categoryFlag);

            query = "delete marketitems WHERE itemdefinition IN (" + defArrayString + ") and isvendoritem=1";
            count = Db.Query().CommandText(query).ExecuteNonQuery();

            Logger.Info("removed " + count + " vendor market items for cf:" + categoryFlag);
        }

        public IDictionary<string,object> GetMarketOrdersInfo(Character character)
        {
            //ez van mashol is
            var orders = _marketOrderRepository.GetByCharacter(character).OrderBy(o => o.isSell).ToDictionary("i",o => o.ToDictionary());

            return new Dictionary<string,object>
            {
                {k.items, orders},
                {k.maxBuyOrderCount, Market.GetMaxBuyOrderCount(character)},
                {k.maxSellOrderCount, Market.GetMaxSellOrderCount(character)},
            };
        }

        public bool CheckSellOrderCounts(Character character)
        {
            var submittedItemsCount = GetMarketOrdersCount(character,true);
            var maxSubmittedItems = Market.GetMaxSellOrderCount(character);
            return submittedItemsCount < maxSubmittedItems;
        }

        public bool CheckBuyOrderCounts(Character character)
        {
            var submittedItemsCount = GetMarketOrdersCount(character,false);
            var maxSubmittedItems = Market.GetMaxBuyOrderCount(character);
            return submittedItemsCount < maxSubmittedItems;
        }

        private int GetMarketOrdersCount(Character character,bool isSell)
        {
            return _marketOrderRepository.GetByCharacter(character).Count(o => o.isSell == isSell && o.forMembersOf == null);
        }


    }
}
	

