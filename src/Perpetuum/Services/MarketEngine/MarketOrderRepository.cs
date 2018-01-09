using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Perpetuum.Accounting.Characters;
using Perpetuum.Data;

namespace Perpetuum.Services.MarketEngine
{
    public interface IMarketOrderRepository : IRepository<int,MarketOrder>
    {
        void UpdatePrice(MarketOrder order);
        void UpdateQuantity(MarketOrder order);

        [CanBeNull]
        MarketOrder GetLowestSellOrder(int itemDefinition, double price, long submitterEid, Market market, long forMembersOf);

        [CanBeNull]
        MarketOrder GetHighestBuyOrder(int itemDefinition, double price, long submitterEid, Market market, long forMembersOf);

        IEnumerable<MarketOrder> GetByMarket(Market market);
        IEnumerable<MarketOrder> GetByMarketEids(IEnumerable<long> marketEids, int itemDefinition, long forMembersOf);
        IEnumerable<MarketOrder> GetByCharacter(Character character);
        IEnumerable<MarketOrder> GetByDefinition(int itemDefinition, long forMembersOf, Market market);
        IEnumerable<MarketOrder> GetExpiredOrders(TimeSpan duration);
        IEnumerable<MarketOrder> GetAllByDefinition(int definition);
        IEnumerable<MarketOrder> GetOrdersToCleanup();
        IEnumerable<MarketOrder> GetOrdersOnCorporationLeave(Character character);
    }




    public static class MarketOrderRepositoryExtensions
    {
    }


    public class MarketOrderRepository : IMarketOrderRepository
    {
        private const string ORDER_COLUMNS = "marketitemid, marketeid, itemeid, itemdefinition, submittereid, submitted, duration, isSell, price, quantity, usecorporationwallet, isvendoritem, formembersof FROM marketitems ";
        private const string ORDER_SELECT = "SELECT " + ORDER_COLUMNS;
        private const string TOP1_SELECT = "SELECT TOP (1) " + ORDER_COLUMNS;
        private readonly MarketOrder.Factory _marketOrderFactory;

        public MarketOrderRepository(MarketOrder.Factory marketOrderFactory)
        {
            _marketOrderFactory = marketOrderFactory;
        }

        public void Insert(MarketOrder order)
        {
            const string insertCmd = @"insert into marketitems (marketeid,itemEID,itemdefinition,submittereid,submitted,duration,isSell,price,quantity,usecorporationwallet,formembersof) 
                                       values (@marketeid,@itemEID,@itemdefinition,@submittereid,@submitted,@duration,1,@price,@quantity,@useCorpWallet,@formembersof); 
                                       select cast(scope_identity() as int)";

            order.id = Db.Query().CommandText(insertCmd)
                .SetParameter("@marketeid", order.marketEID)
                .SetParameter("@itemEID", order.itemEid)
                .SetParameter("@itemdefinition", order.itemDefinition)
                .SetParameter("@submittereid", order.submitterEID)
                .SetParameter("@submitted", DateTime.Now)
                .SetParameter("@duration", order.duration)
                .SetParameter("@price", order.price)
                .SetParameter("@quantity", order.quantity)
                .SetParameter("@useCorpWallet", order.useCorporationWallet)
                .SetParameter("@formembersof", order.forMembersOf)
                .ExecuteScalar<int>().ThrowIfEqual(0, ErrorCodes.SQLInsertError);
        }

        public void Delete(MarketOrder order)
        {
            Db.Query().CommandText("delete from marketitems where marketitemid = @marketItemID")
                .SetParameter("@marketItemID", order.id)
                .ExecuteNonQuery().ThrowIfEqual(0, ErrorCodes.SQLDeleteError);
        }

        public void UpdatePrice(MarketOrder order)
        {
            var expireDate = order.submitted.AddHours(order.duration);
            var durationRemains = (int)expireDate.Subtract(DateTime.Now).TotalHours;

            Db.Query().CommandText("update marketitems set price=@price,submitted=@now,duration=@dleft where marketitemid=@ID")
                .SetParameter("@ID", order.id)
                .SetParameter("@price", order.price)
                .SetParameter("@now", DateTime.Now)
                .SetParameter("@dleft", durationRemains)
                .ExecuteNonQuery().ThrowIfEqual(0, ErrorCodes.SQLUpdateError);
        }

        public void UpdateQuantity(MarketOrder order)
        {
            Db.Query().CommandText("update marketitems set quantity = @quantity where marketitemid = @marketitemid")
                .SetParameter("@marketitemid", order.id)
                .SetParameter("@quantity", order.quantity)
                .ExecuteNonQuery().ThrowIfEqual(0, ErrorCodes.SQLUpdateError);
        }

        public MarketOrder Get(int id)
        {
            var record = Db.Query().CommandText(ORDER_SELECT + " (UPDLOCK) where marketitemid = @marketItemID")
                                 .SetParameter("@marketItemID", id)
                                 .ExecuteSingleRow();

            return CreateMarketOrderFromRecord(record);
        }

        public IEnumerable<MarketOrder> GetByMarket(Market market)
        {
            return Db.Query().CommandText("select * from marketitems where marketeid=@marketEID")
                           .SetParameter("@marketEID", market.Eid)
                           .Execute()
                           .Select(CreateMarketOrderFromRecord)
                           .Where(o => o != null)
                           .ToArray();
        }

        public IEnumerable<MarketOrder> GetByMarketEids(IEnumerable<long> marketEids, int itemDefinition, long forMembersOf)
        {
            var marketEidsString = marketEids.ArrayToString();

            var commandText = $"{ORDER_SELECT}where marketeid in ({marketEidsString}) and " + @"itemdefinition = @definition and (( formembersof is not null and @fmo=formembersof) or ( formembersof is null ))";

            return Db.Query().CommandText(commandText)
                           .SetParameter("@definition", itemDefinition)
                           .SetParameter("@fmo", forMembersOf)
                           .Execute()
                           .Select(CreateMarketOrderFromRecord)
                           .Where(o => o != null)
                           .ToArray();
        }

        public IEnumerable<MarketOrder> GetByCharacter(Character character)
        {
            return Db.Query().CommandText(ORDER_SELECT + " where submittereid = @characterEID")
                           .SetParameter("@characterEID", character.Eid)
                           .Execute()
                           .Select(CreateMarketOrderFromRecord);
        }


        public IEnumerable<MarketOrder> GetExpiredOrders(TimeSpan duration)
        {
            var records = Db.Query().CommandText(ORDER_SELECT + "where duration > 0 and getdate() > dateadd(minute,duration * " + duration.TotalMinutes + ",cast(submitted as datetime)) and isvendoritem=0 and formembersof is null").Execute();
            foreach (var record in records)
            {
                var marketItem = CreateMarketOrderFromRecord(record);
                if (marketItem == null)
                    continue;

                marketItem.duration = 0; //ezt itt miert nullazom le? biztos valami kliens info %%%
                yield return marketItem;
            }
        }

        public IEnumerable<MarketOrder> GetAllByDefinition(int definition)
        {
            const string cmd = ORDER_SELECT + @"where itemdefinition = @definition";

            return Db.Query().CommandText(cmd)
                           .SetParameter("@definition", definition)
                           .Execute()
                           .Select(CreateMarketOrderFromRecord);
        }

        /// <summary>
        /// generic cleanup thing. hackable live.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<MarketOrder> GetOrdersToCleanup()
        {

            const string cmd = "marketGetOrdersForCancel";

            return Db.Query().CommandText(cmd)
               .Execute()
               .Select(CreateMarketOrderFromRecord);


        }

        public IEnumerable<MarketOrder> GetOrdersOnCorporationLeave(Character character)
        {
            const string cmd = ORDER_SELECT + @"where submittereid=@characterEID and formembersof is not null";

            return 
            Db.Query().CommandText(cmd)
                .SetParameter("@characterEID", character.Eid)
                .Execute()
                .Select(CreateMarketOrderFromRecord);
            
        }

        public IEnumerable<MarketOrder> GetByDefinition(int itemDefinition, long forMembersOf, Market market)
        {
            const string cmd = ORDER_SELECT + @"where marketeid = @marketeid and 
                                                                 itemdefinition = @definition and 
                                                                 (( formembersof is not null and @fmo=formembersof) or ( formembersof is null ))";

            return Db.Query().CommandText(cmd)
                           .SetParameter("@marketEID", market.Eid)
                           .SetParameter("@definition", itemDefinition)
                           .SetParameter("@fmo", forMembersOf)
                           .Execute()
                           .Select(CreateMarketOrderFromRecord);
        }

        public MarketOrder GetLowestSellOrder(int itemDefinition, double price, long submitterEid, Market market, long forMembersOf)
        {
            var record = Db.Query().CommandText(TOP1_SELECT +
                                                 @"where    marketeid = @marketEID and 
                                                    itemdefinition = @itemdefinition and 
                                                    price <= @price and
                                                    submittereid != @submitterEID and
                                                    isSell = 1 and
                                                    (( formembersof is not null and @fmo=formembersof) or ( formembersof is null ))
                                                    order by price asc")
                .SetParameter("@marketEID", market.Eid)
                .SetParameter("@itemdefinition", itemDefinition)
                .SetParameter("@submitterEID", submitterEid)
                .SetParameter("@price", price)
                .SetParameter("@fmo", forMembersOf)
                .ExecuteSingleRow();

            return CreateMarketOrderFromRecord(record);
        }

        public MarketOrder GetHighestBuyOrder(int itemDefinition, double price, long submitterEid, Market market, long forMembersOf)
        {
            const string queryText = TOP1_SELECT + @"where marketeid = @marketEID and 
                                                              itemdefinition = @itemDefinition and 
                                                              price >= @price and
                                                              submitterEID != @submitterEID and
                                                              isSell = 0 and
                                                              (( formembersof is not null and @fmo=formembersof) or ( formembersof is null ))
                                                              order by price desc";

            var record = Db.Query().CommandText(queryText)
                .SetParameter("@marketEID", market.Eid)
                .SetParameter("@submitterEID", submitterEid)
                .SetParameter("@itemdefinition", itemDefinition)
                .SetParameter("@price", price)
                .SetParameter("@fmo", forMembersOf)
                .ExecuteSingleRow();

            return CreateMarketOrderFromRecord(record);
        }

        [CanBeNull]
        private MarketOrder CreateMarketOrderFromRecord(IDataRecord record)
        {
            if (record == null)
                return null;
            var order = _marketOrderFactory();
            order.id             = record.GetValue<int>("marketitemid");
            order.marketEID      = record.GetValue<long>("marketeid");
            order.itemEid        = record.GetValue<long?>("itemeid");
            order.itemDefinition = record.GetValue<int>("itemdefinition");
            order.submitterEID   = record.GetValue<long>("submittereid");
            order.submitted      = record.GetValue<DateTime>("submitted");
            order.duration       = record.GetValue<int>("duration");
            order.isSell         = record.GetValue<bool>("isSell");
            order.price          = record.GetValue<double>("price");
            order.quantity       = record.GetValue<int>("quantity");
            order.useCorporationWallet = record.GetValue<bool>("usecorporationwallet");
            order.isVendorItem = record.GetValue<bool>("isvendoritem");
            order.forMembersOf = record.GetValue<long?>("formembersof");
            return order;
        }

        public IEnumerable<MarketOrder> GetAll()
        {
            throw new NotImplementedException();
        }

        public void Update(MarketOrder item)
        {
            throw new NotImplementedException();
        }
    }

}
