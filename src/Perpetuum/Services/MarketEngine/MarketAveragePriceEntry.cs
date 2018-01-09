using System;
using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.Log;

namespace Perpetuum.Services.MarketEngine
{
    /// <summary>
    /// Market average price for a definition
    /// </summary>
    public class MarketAveragePriceEntry
    {
        public double AveragePrice { get; private set; }
        public long SumQuantity { get; private set; }
        public DateTime LastUpdated { get; private set; }

        public MarketAveragePriceEntry()
        {
            SumQuantity = -1;
            AveragePrice = -1;
        }

        /// <summary>
        /// Loads average price data from a given market for a definition
        /// </summary>
        public void LoadAveragePrice(int definition, int daysBack,Market market)
        {
            LastUpdated = DateTime.Now;

            var startDate = DateTime.Today.AddDays(-1 * daysBack);

            //get it from the live data
            var record = Db.Query().CommandText(@"select sum(totalprice) / sum(quantity) as price, sum(quantity) from 
                                                            marketaverageprices where
                                                            marketeid = @marketEID and 
                                                            itemdefinition = @itemDefinition and 
                                                            date >= @startDate")
                        .SetParameter("@itemDefinition", definition)
                        .SetParameter("@day", daysBack)
                        .SetParameter("@marketEID", market.Eid)
                        .SetParameter("@startDate", startDate)
                        .ExecuteSingleRow();

            var entityDefault = EntityDefault.Get(definition);

            if (record != null)
            {
                var price = record.GetValue<double>(0);
                var quantity = record.GetValue<long>(1);

                AveragePrice = price;
                SumQuantity = quantity; //the amount they traded in the current period

                Logger.Info("avg cached on market " + market.Eid + " for definition:" + definition + " " + entityDefault.Name + " price:" + AveragePrice + " sumQuantity:" + quantity);
            }
            else
            {
                Logger.Info("no trade with " + definition + " " + entityDefault.Name + " on market: " + market.Eid);
            }
        }
    }

}
