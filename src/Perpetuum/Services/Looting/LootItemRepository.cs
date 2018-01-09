using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Perpetuum.Data;
using Perpetuum.Items;

namespace Perpetuum.Services.Looting
{
    public class LootItemRepository : ILootItemRepository
    {
        public void Add(LootContainer container,LootItem lootItem)
        {
            const string insertCmdText = "insert into lootitems (id,containereid,definition,quantity,health,repackaged) values (@id,@containerEid,@definition,@quantity,@health,@repackaged)";

            Db.Query().CommandText(insertCmdText)
                .SetParameter("@id", lootItem.id)
                .SetParameter("@containerEid", container.Eid)
                .SetParameter("@definition", lootItem.ItemInfo.Definition)
                .SetParameter("@quantity", lootItem.ItemInfo.Quantity)
                .SetParameter("@health", lootItem.ItemInfo.Health)
                .SetParameter("@repackaged", lootItem.ItemInfo.IsRepackaged)
                .ExecuteNonQuery().ThrowIfEqual(0, ErrorCodes.SQLInsertError);
        }

        public void Update(LootContainer container, LootItem lootItem)
        {
            const string updateCmdText = "update lootitems set quantity = @quantity where id = @id and containereid = @containerEid";

            Db.Query().CommandText(updateCmdText)
                .SetParameter("@id",lootItem.id)
                .SetParameter("@containerEid",container.Eid)
                .SetParameter("@quantity",lootItem.ItemInfo.Quantity)
                .ExecuteNonQuery().ThrowIfZero(ErrorCodes.SQLUpdateError);
        }

        public void Delete(LootContainer container, LootItem item)
        {
            const string deleteCmdText = "delete from lootitems where id = @id and containereid = @containerEid";
            Db.Query().CommandText(deleteCmdText)
                .SetParameter("@id",item.id)
                .SetParameter("containerEid",container.Eid)
                .ExecuteNonQuery().ThrowIfZero(ErrorCodes.SQLDeleteError);
        }

        public void DeleteAll(LootContainer container)
        {
            Db.Query().CommandText("delete from lootitems where containereid = @containerEid").SetParameter("@containerEid",container.Eid).ExecuteNonQuery();
        }

        public bool IsEmpty(LootContainer container)
        {
            var count = Db.Query().CommandText("select top 1 count(id) from lootitems where containereid = @containerEid").SetParameter("@containerEid",container.Eid).ExecuteScalar<int>();
            return count == 0;
        }

        public LootItem Get(LootContainer container, Guid id)
        {
            const string selectCmdText = "select id,definition,quantity,health,repackaged from lootitems where id = @id and containereid = @containerEid";
            var record = Db.Query().CommandText(selectCmdText)
                                 .SetParameter("id", id)
                                 .SetParameter("@containerEid", container.Eid)
                                 .ExecuteSingleRow();

            if (record == null)
                return null;

            var item = CreateLootItemFromRecord(record);
            return item;
        }

        public IEnumerable<LootItem> GetAll(LootContainer container)
        {
            const string selectCmdText = "select id,definition,quantity,health,repackaged from lootitems where containereid = @containerEid and quantity > 0";
            var records = Db.Query().CommandText(selectCmdText).SetParameter("@containerEid",container.Eid).Execute();
            return records.Select(CreateLootItemFromRecord).ToList();
        }

        public IEnumerable<LootItem> GetByDefinition(LootContainer container,int definition)
        {
            const string selectCmdText = "select id,definition,quantity,health,repackaged from lootitems where containereid = @containerEid and quantity > 0 and definition = @definition";
            var records = Db.Query().CommandText(selectCmdText).SetParameter("@containerEid",container.Eid).SetParameter("@definition",definition).Execute();
            return records.Select(CreateLootItemFromRecord).ToList();
        }

        private static LootItem CreateLootItemFromRecord(IDataRecord record)
        {
            var id = record.GetValue<Guid>("id");
            var definition = record.GetValue<int>("definition");
            var quantity = record.GetValue<int>("quantity");
            var health = record.GetValue<double>("health");
            var repackaged = record.GetValue<bool>("repackaged");

            var itemInfo = new ItemInfo(definition, quantity) {Health = (float) health, IsRepackaged = repackaged};
            return new LootItem(id, itemInfo);
        }
    }
}