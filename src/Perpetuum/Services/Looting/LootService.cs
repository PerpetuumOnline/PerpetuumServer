using System.Collections.Generic;
using System.Data;
using System.Linq;
using Perpetuum.Data;
using Perpetuum.Items;
using Perpetuum.Zones.Intrusion;

namespace Perpetuum.Services.Looting
{
    public class LootService : ILootService
    {
        private ILookup<int, LootGeneratorItemInfo> _npcLootInfos;
        private ILookup<int, LootGeneratorItemInfo> _flockLootInfos;
        private IntrusionLootInfo[] _intrusionLootInfos;

        public void Init()
        {
            _npcLootInfos = LoadNpcLootInfosFromDb();
            _flockLootInfos = LoadFlockLootInfosFromDb();
            _intrusionLootInfos = LoadIntrusionLootInfos();
        }

        public IEnumerable<LootGeneratorItemInfo> GetNpcLootInfos(int definition)
        {
            return _npcLootInfos.GetOrEmpty(definition);
        }

        public IEnumerable<LootGeneratorItemInfo> GetFlockLootInfos(int flockID)
        {
            return _flockLootInfos.GetOrEmpty(flockID);
        }

        public IEnumerable<LootGeneratorItemInfo> GetIntrusionLootInfos(Outpost outpost,SAP sap)
        {
            var stability = outpost.GetIntrusionSiteInfo().Stability;
            var loots = _intrusionLootInfos.Where(i => i.siteDefinition == outpost.Definition && 
                                                       i.sapDefinition == sap.Definition && 
                                                       i.stabilityThreshold.Min <= stability && i.stabilityThreshold.Max >= stability);

            foreach (var loot in loots)
            {
                var item = new ItemInfo(loot.itemDefinition,FastRandom.NextInt(loot.quantity));
                yield return new LootGeneratorItemInfo(item,false,loot.probability);
            }
        }

        private static ILookup<int, LootGeneratorItemInfo> LoadNpcLootInfosFromDb()
        {
            return Db.Query().CommandText("select * from npcloot").Execute().Select(r =>
            {
                return new
                {
                    definition = r.GetValue<int>("definition"),
                    info = CreateNpcLootInfoFromRecord(r)
                };
            }).ToLookup(i => i.definition, i => i.info);
        }

        private static ILookup<int, LootGeneratorItemInfo> LoadFlockLootInfosFromDb()
        {
            return Db.Query().CommandText("select * from npcflockloot").Execute().Select(r =>
            {
                return new
                {
                    flockId = r.GetValue<int>("flockid"),
                    info = CreateNpcLootInfoFromRecord(r)
                };
            }).ToLookup(i => i.flockId, i => i.info);
        }

        private static LootGeneratorItemInfo CreateNpcLootInfoFromRecord(IDataRecord record)
        {
            var definition = record.GetValue<int>(k.lootDefinition.ToLower());
            var minq = record.GetValue<int>("minquantity");
            var maxq = record.GetValue<int>(k.quantity);
            var item = new ItemInfo(definition, FastRandom.NextInt(minq, maxq))
            {
                IsRepackaged = record.GetValue<bool>(k.repackaged)
            };

            var damageit = !record.GetValue<bool>(k.dontdamage);
            var damaged = false;

            if (!item.EntityDefault.AttributeFlags.Repackable)
            {
                //force false
                item.IsRepackaged = false;
                damageit = false;
            }

            //is it forced to be repacked from config AND damageable? 
            if (!item.IsRepackaged && damageit)
            {
                //no, so damage it!
                damaged = true;
            }

            var probability = record.GetValue<double>(k.probability);
            return new LootGeneratorItemInfo(item, damaged, probability);
        }

        private class IntrusionLootInfo
        {
            public readonly int siteDefinition;
            public readonly int sapDefinition;
            public readonly int itemDefinition;
            public readonly IntRange quantity;
            public readonly IntRange stabilityThreshold;
            public readonly double probability;

            public IntrusionLootInfo(int siteDefinition, int sapDefinition, int itemDefinition, IntRange quantity, IntRange stabilityThreshold, double probability)
            {
                this.siteDefinition = siteDefinition;
                this.sapDefinition = sapDefinition;
                this.itemDefinition = itemDefinition;
                this.quantity = quantity;
                this.stabilityThreshold = stabilityThreshold;
                this.probability = probability;
            }
        }

        private IntrusionLootInfo[] LoadIntrusionLootInfos()
        {
            var x = Db.Query().CommandText("select * from intrusionloot").Execute().Select(r =>
            {
                var siteDefinition = r.GetValue<int>("sitedefinition");
                var sapDefinition = r.GetValue<int>("sapdefinition");
                var itemDefinition = r.GetValue<int>("itemdefinition");
                var quantity = new IntRange(r.GetValue<int>("minquantity"), r.GetValue<int>("maxquantity"));
                var stabilityThreshold = new IntRange(r.GetValue<int>("minstabilitythreshold"), r.GetValue<int>("maxstabilitythreshold"));
                var probability = r.GetValue<double>("probability");

                return new IntrusionLootInfo(siteDefinition,sapDefinition,itemDefinition,quantity,stabilityThreshold,probability);
            }).ToArray();

            return x;
        }
    }
}