using System;
using System.Collections.Concurrent;
using Perpetuum.EntityFramework;
using Perpetuum.Log;

namespace Perpetuum.Services.MarketEngine
{
    public class MarketPriceCollector
    {
        /// <summary>
        /// definition -> MarketAveragePriceEntry
        /// </summary>
        private readonly ConcurrentDictionary<int, MarketAveragePriceEntry> _averagePrices = new ConcurrentDictionary<int, MarketAveragePriceEntry>();

        public Market Market { get; }

        private MarketPriceCollector(Market market)
        {
            Market = market;
        }

        public long GetBaseEid()
        {
            return Market.GetDockingBase().Eid;
        }

        public bool IsTrainingMarket => Market.IsOnTrainingZone();

        public bool IsGammaMarket => Market.IsOnGammaZone();

        /// <summary>
        /// Filters and inserts an average price entry
        /// </summary>
        public void InsertAveragePrice(int itemDefinition, double price, int qty)
        {
            var today = DateTime.Today;
            var now = DateTime.Now;
            var perPiece = price / qty;

            var hours = (int)(now.Hour / 6.0);

            today = today.AddHours(hours * 6);

            var localAvg = GetAveragePriceByMarket(itemDefinition).AveragePrice;

            if (localAvg > 0)
            {
                // meaningful local avg?

                if (localAvg * 10 < perPiece)
                {
                    // too extreme deal high
                    return;
                }

                if (localAvg / 10 > perPiece)
                {
                    // too extreme deal low
                    return;
                }
            }

            Market.ForceInsertAveragePrice(itemDefinition, price, qty, today);
        }

        /// <summary>
        /// Uses and updates/refreshes the cache of average prices
        /// </summary>
        [CanBeNull]
        public MarketAveragePriceEntry GetAveragePriceByMarket(int definition)
        {
            if (!EntityDefault.Exists(definition))
            {
                Logger.Error("average price reqested for not supported definition: " + definition);
                return null;
            }

            MarketAveragePriceEntry marketAveragePriceEntry;
            if (!_averagePrices.TryGetValue(definition, out marketAveragePriceEntry) || DateTime.Now.Subtract(marketAveragePriceEntry.LastUpdated).TotalMinutes > MarketInfoService.MARKET_AVERAGE_EXPIRY_MINUTES)
            {
                //not in the cache, OR obsolete => load it

                var averagePrice = new MarketAveragePriceEntry();
                averagePrice.LoadAveragePrice(definition,MarketInfoService.MARKET_AVERAGE_DAYSBACK, Market);
                _averagePrices.AddOrUpdate(definition, averagePrice, (k, v) => averagePrice);

                marketAveragePriceEntry = averagePrice;
            }

            return marketAveragePriceEntry;
        }

        public static MarketPriceCollector CreateCollector(Market market)
        {
            var mpc = new MarketPriceCollector(market);
#if DEBUG
            Logger.Info("market price collector was initialized for market:" + market.Eid + " base:" + mpc.GetBaseEid());
#endif
            return mpc;
        }

    }
}
