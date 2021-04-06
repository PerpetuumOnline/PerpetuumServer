using System.Collections.Generic;
using System.Data;
using Perpetuum.Accounting.Characters;
using Perpetuum.Common.Loggers.Transaction;
using Perpetuum.Containers;
using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;

using Perpetuum.Players;
using Perpetuum.Services.Standing;
using Perpetuum.Units;
using Perpetuum.Zones;

namespace Perpetuum.Services.ItemShop
{
    public class ItemShop : Unit
    {
        private readonly IStandingHandler _standingHandler;
        private readonly CharacterWalletFactory _characterWalletFactory;

        public ItemShop(IStandingHandler standingHandler, CharacterWalletFactory characterWalletFactory)
        {
            _standingHandler = standingHandler;
            _characterWalletFactory = characterWalletFactory;
        }

        public ErrorCodes IsInOperationRange(Player player)
        {
            if (!IsInRangeOf3D(player, DistanceConstants.ITEM_SHOP_DISTANCE))
            {
                return ErrorCodes.TargetOutOfRange;
            }

            return ErrorCodes.NoError;
        }

        [CanBeNull]
        private ItemShopEntry GetEntry(int entryID)
        {
            const string qryStr = @"SELECT * FROM dbo.itemshop AS its
                                    JOIN itemshoplocations sl ON its.presetid = sl.presetid
                                    JOIN entitydefaults d on its.targetdefinition = d.definition
                                    WHERE d.enabled=1 AND d.hidden=0 AND
                                    sl.locationeid = @eid AND its.id = @id";

            var record = Db.Query().CommandText(qryStr).SetParameter("@eid", Eid).SetParameter("@id", entryID).ExecuteSingleRow();
            if (record == null)
                return null;

            var entry = CreateItemShopEntryFromRecord(record);
            return entry;
        }

        private List<ItemShopEntry> GetAll()
        {
            const string qryStr = @"SELECT * FROM dbo.itemshop AS its
                                    JOIN itemshoplocations sl ON its.presetid = sl.presetid
                                    JOIN entitydefaults d on its.targetdefinition = d.definition
                                    WHERE d.enabled=1 AND d.hidden=0 AND 
                                    sl.locationeid = @eid";

            var records = Db.Query().CommandText(qryStr).SetParameter("@eid", Eid).Execute();

            var entries = new List<ItemShopEntry>();

            foreach (var record in records)
            {
                var entry = CreateItemShopEntryFromRecord(record);
                entries.Add(entry);
            }

            return entries;
        }

        private static ItemShopEntry CreateItemShopEntryFromRecord(IDataRecord record)
        {
            var id = record.GetValue<int>("id");
            var targetDefinition = record.GetValue<int>("targetdefinition");
            var targetAmount = record.GetValue<int>("targetamount");
            var globalLimit = record.GetValue<int?>("globalLimit");
            var purchaseCount = record.GetValue<int>("purchaseCount");
            var price = record.GetValue<double?>("credit") ?? 0.0;
            var standing = record.GetValue<double?>("standing");

            var tmCoin = record.GetValue<int?>("tmCoin") ?? 0;
            var icsCoin = record.GetValue<int?>("icscoin") ?? 0;
            var asiCoin = record.GetValue<int?>("asicoin") ?? 0;
            var uniCoin = record.GetValue<int?>("unicoin") ?? 0;

            return new ItemShopEntry(id, EntityDefault.Get(targetDefinition), targetAmount, tmCoin, icsCoin, asiCoin, uniCoin, price, globalLimit, purchaseCount, standing);
        }

        public Dictionary<string, object> EntriesToDictionary()
        {
            return GetAll().ToDictionary("c", e => e.ToDictionary());
        }

        private void CheckStanding(Character character, ItemShopEntry shopEntry)
        {
            if (shopEntry.Standing == null)
                return;

            var standing = _standingHandler.GetStanding(Owner, character.Eid);
            if (standing < shopEntry.Standing)
                throw new PerpetuumException(ErrorCodes.StandingTooLow);
        }

        public void Buy(Container container, Character character, int entryID, int quantity = 1)
        {
            if (quantity < 1)
                throw new PerpetuumException(ErrorCodes.WTFErrorMedicalAttentionSuggested);

            var entry = GetEntry(entryID);
            if (entry == null)
                throw new PerpetuumException(ErrorCodes.ItemNotFound);

            entry.CheckGlobalLimit(quantity);
            CheckStanding(character, entry);

            entry.RemoveFromContainer(container, quantity);

            var totalCredit = entry.Credit * quantity;
            if (totalCredit > 0)
            {
                var ownerWallet = _characterWalletFactory(character, TransactionType.ItemShopCreditTake);
                ownerWallet.Balance -= totalCredit;

                character.LogTransaction(TransactionLogEvent.Builder()
                                                            .SetTransactionType(TransactionType.ItemShopCreditTake)
                                                            .SetCreditBalance(ownerWallet.Balance)
                                                            .SetCreditChange(-totalCredit)
                                                            .SetCharacter(character)
                                                            .Build());
            }

            var targetItem = entry.CreateTargetItem(character, quantity);
            container.AddItem(targetItem, true);

            UpdateGlobalPurchaseCount(targetItem.Definition, targetItem.Quantity);

            if (entry.TmCoin > 0)
                character.LogTransaction(TransactionLogEvent.Builder()
                    .SetTransactionType(TransactionType.ItemShopTake)
                    .SetCharacter(character)
                    .SetContainer(container)
                    .SetItem(EntityDefault.GetByName(DefinitionNames.TM_MISSION_COIN).Definition, entry.TmCoin * quantity));

            if (entry.IcsCoin > 0)
                character.LogTransaction(TransactionLogEvent.Builder()
                    .SetTransactionType(TransactionType.ItemShopTake)
                    .SetCharacter(character)
                    .SetContainer(container)
                    .SetItem(EntityDefault.GetByName(DefinitionNames.ICS_MISSION_COIN).Definition, entry.IcsCoin * quantity));

            if (entry.AsiCoin > 0)
                character.LogTransaction(TransactionLogEvent.Builder()
                    .SetTransactionType(TransactionType.ItemShopTake)
                    .SetCharacter(character)
                    .SetContainer(container)
                    .SetItem(EntityDefault.GetByName(DefinitionNames.ASI_MISSION_COIN).Definition, entry.AsiCoin * quantity));

            if (entry.UniCoin > 0)
                character.LogTransaction(TransactionLogEvent.Builder()
                    .SetTransactionType(TransactionType.ItemShopTake)
                    .SetCharacter(character)
                    .SetContainer(container)
                    .SetItem(EntityDefault.GetByName(DefinitionNames.UNIVERSAL_MISSION_COIN).Definition, entry.UniCoin * quantity));


            character.LogTransaction(TransactionLogEvent.Builder()
                .SetTransactionType(TransactionType.ItemShopBuy)
                .SetCharacter(character)
                .SetItem(targetItem));
        }

        private static void UpdateGlobalPurchaseCount(int targetDefinition, int purchaseCount)
        {
            Db.Query().CommandText("update itemshop set purchasecount=@pc where targetdefinition=@definition")
                .SetParameter("@pc", purchaseCount)
                .SetParameter("@definition", targetDefinition)
                .ExecuteNonQuery().ThrowIfEqual(0, ErrorCodes.SQLUpdateError);
        }
    }
}