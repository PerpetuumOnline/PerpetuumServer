using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using Perpetuum.Accounting.Characters;
using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Log;
using Perpetuum.Units.DockingBases;

namespace Perpetuum.Services.MarketEngine
{
    /// <summary>
    /// Global market handler.
    /// 
    /// Market average prices is handled here
    /// </summary>
    public class MarketHandler
    {
        private readonly DockingBaseHelper _dockingBaseHelper;
      
        /// <summary>
        /// marketEid -> MarketPriceCollector
        /// </summary>
        private readonly ConcurrentDictionary<long, MarketPriceCollector> _marketPriceCollectors = new ConcurrentDictionary<long, MarketPriceCollector>();
        private readonly ConcurrentDictionary<long,long> _marketEidToDockingBaseEid = new ConcurrentDictionary<long, long>();
        private readonly ObjectCache _visibleMarkets = new MemoryCache("visibleMarkets");

        public MarketHandler(DockingBaseHelper dockingBaseHelper)
        {
            _dockingBaseHelper = dockingBaseHelper;
        }

        public void Init()
        {
            var dockingBases = _dockingBaseHelper.GetDefaultDockingBases();

            foreach (var dockingBase in dockingBases)
            {
                var market = dockingBase.GetMarketOrThrow();
                GetPriceCollectorByMarket(market);//beiniteljuk
            }
        }

        private MarketPriceCollector GetPriceCollectorByMarket(Market market)
        {
            var c = _marketPriceCollectors.GetOrAdd(market.Eid, eid => MarketPriceCollector.CreateCollector(market));
            _marketEidToDockingBaseEid[market.Eid] = c.GetBaseEid();
            return c;
        }

        public bool TryGetDockingBaseEidForMarketEid(long marketEid, out long baseEid)
        {
            if (_marketEidToDockingBaseEid.TryGetValue(marketEid, out baseEid))
            {
                //benne van a kessben 
                return true;
            }

            Market market;
            try
            {
                market = Market.GetOrThrow(marketEid);
            }
            catch (Exception ex)
            {
                Logger.Error("unknown market " + marketEid);
                Logger.Exception(ex);
                return false;
            }
            
            //ok, load price collector for the market
            var pc = GetPriceCollectorByMarket(market);
            baseEid = pc.GetBaseEid();
            return true;
        }

        public IEnumerable<long> GetAllDefaultMarketsEids(bool includeGamma = false, bool includeTraining = false)
        {
            return _marketPriceCollectors.Values
                .Where(pc => includeTraining == pc.IsTrainingMarket)
                .Where(pc => includeGamma == pc.IsGammaMarket)
                .Select(pc => pc.Market.Eid);
        }

        public IEnumerable<long> GetAllVisibleMarketsFor(Character c)
        {
            return _visibleMarkets.Get(c.Eid.ToString(), () =>
            {
                return _marketPriceCollectors.Values
                .Where(pc => !pc.IsTrainingMarket)
                .Where(pc => pc.IsVisible(c))
                .Select(pc => pc.Market.Eid);
            }, TimeSpan.FromMinutes(2));
        }

        private long _trainingMarketEid = -1L;

        public long GetTrainingMarketEid()
        {
            if (_trainingMarketEid < 0)
            {

                var query = @"SELECT TOP 1 eid FROM dbo.entities WHERE parent in (SELECT TOP 1 eid FROM dbo.entities WHERE definition=@tb_def)";

                var tbm_eid =
                Db.Query().CommandText(query)
                       .SetParameter("@tb_def", EntityDefault.GetByName(DefinitionNames.TRAINING_DOCKING_BASE).Definition)
                       .ExecuteScalar<long>();
           
                Logger.Info("training base market eid was found: " + tbm_eid);

                _trainingMarketEid = tbm_eid;
                
            }

            return _trainingMarketEid;
        }

        /// <summary>
        /// This function records a market transaction
        /// </summary>
        public void InsertAveragePrice(Market market, int itemDefinition, double price, int qty)
        {
            HandleInsertAveragePrice(market, itemDefinition, price, qty);
        }

        /// <summary>
        /// Returns the global average price for a definition
        /// </summary>
        public double GetWorldAveragePriceByTrades(EntityDefault entityDefault)
        {
            return HandleGetWorldAveragePriceByTrades(entityDefault);
        }

        public MarketAveragePriceEntry GetAveragePriceByMarket(Market market, int definition)
        {
            return HandleGetAveragePriceByMarket(market, definition);
        }

        private void HandleInsertAveragePrice(Market market, int itemDefinition, double price, int qty)
        {
            GetPriceCollectorByMarket(market).InsertAveragePrice(itemDefinition, price, qty);
        }
        
        private MarketAveragePriceEntry HandleGetAveragePriceByMarket(Market market, int definition)
        {
            var pc = GetPriceCollectorByMarket(market);
            return pc.GetAveragePriceByMarket(definition);
        }

        private double HandleGetWorldAveragePriceByTrades(EntityDefault entityDefault)
        {
            var meaningfulWorldAveragesPerMarket =
               (from priceCollector in _marketPriceCollectors.Values
                where !priceCollector.IsTrainingMarket
                let mape = priceCollector.GetAveragePriceByMarket(entityDefault.Definition)
                where mape != null && mape.AveragePrice > 0
                select mape).ToArray();

            if (meaningfulWorldAveragesPerMarket.Length <= 0)
                return -1;

            var sumQuantity = meaningfulWorldAveragesPerMarket.Sum(mape => mape.SumQuantity);
            var weightedPrice = meaningfulWorldAveragesPerMarket.Sum(mape => mape.SumQuantity * mape.AveragePrice);
            return weightedPrice / sumQuantity;
        }

        public Dictionary<string, object> GetGlobalAverageHistory(int day, int itemDefinition)
        {
            var startDate = DateTime.Today.AddDays(-1*day);

            var marketEidString = GetAllDefaultMarketsEids().ArrayToString(); 

            var count = 0;
            var prices = (from r in
                Db.Query().CommandText(@"select AVG(totalprice / quantity) as price,[date],MAX(dailyhighest),MIN(dailylowest),SUM(quantity) from 
															marketaverageprices where 
															itemdefinition = @itemDefinition and
															marketeid in (" + marketEidString + @") and " +
                                        @"date >= @startDate
															GROUP BY [date]")
                    .SetParameter("@itemDefinition", itemDefinition)
                    .SetParameter("@day", day)
                    .SetParameter("@startDate", startDate)
                    .Execute()
                select (object) new Dictionary<string, object>
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

            };

            return result;
        }
    }
}
