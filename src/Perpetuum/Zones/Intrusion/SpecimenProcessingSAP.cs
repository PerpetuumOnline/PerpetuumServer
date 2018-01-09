using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Transactions;
using Perpetuum.Accounting.Characters;
using Perpetuum.Data;
using Perpetuum.ExportedTypes;
using Perpetuum.Items;
using Perpetuum.Players;
using Perpetuum.Robots;

namespace Perpetuum.Zones.Intrusion
{
    /// <summary>
    /// Intrusion target which can be completed by submitting items to the SAP
    /// </summary>
    public class SpecimenProcessingSAP : SAP
    {
        private class SiegeItem
        {
            public readonly int definition;
            public readonly IntRange quantity;

            public SiegeItem(int definition, IntRange quantity)
            {
                this.definition = definition;
                this.quantity = quantity;
            }
        }

        private static readonly IDictionary<int,SiegeItem> _specimenProcessingItems; //the items the specimen processing might require


        private const int SUBMIT_ITEM_RANGE = 7;
        private static readonly TimeSpan _submitItemCooldown = TimeSpan.FromMinutes(1);

        private static readonly IntRange _requiredItems = new IntRange(3, 4);

        private readonly IList<ItemInfo> _itemInfos;
        private readonly ConcurrentDictionary<int, PlayerItemProgress> _playerItemProgresses = new ConcurrentDictionary<int, PlayerItemProgress>();

        static SpecimenProcessingSAP()
        {
            _specimenProcessingItems = Database.CreateCache<int, SiegeItem>("siegeitems", "id", r =>
            {
                var definition = r.GetValue<int>("definition");
                var minQty = r.GetValue<int>("minquantity");
                var maxQty = r.GetValue<int>("maxquantity");
                return new SiegeItem(definition, new IntRange(minQty, maxQty));
            });
           
        }

        public SpecimenProcessingSAP() : base(BeamType.attackpoint_item_enter, BeamType.attackpoint_item_out)
        {
            var itemsCount = FastRandom.NextInt(_requiredItems);
            _itemInfos = GenerateSpecimenProcessingItemList(itemsCount);
        }

        /// <summary>
        /// Generates required item's list for the specimen processing SAP
        /// </summary>
        private static IList<ItemInfo> GenerateSpecimenProcessingItemList(int count = 5)
        {
            var result = new Dictionary<int, ItemInfo>();

            while (count > 0)
            {
                var randomItemInfo = _specimenProcessingItems.Where(d => !result.ContainsKey(d.Key)).RandomElement();

                var siegeItem = randomItemInfo.Value;
                var randomQty = FastRandom.NextInt(siegeItem.quantity);

                var itemInfo = new ItemInfo(siegeItem.definition, randomQty);
                result.Add(randomItemInfo.Key, itemInfo);
                count--;
            }

            return result.Values.ToArray();
        }


        protected override int MaxScore => _itemInfos.Count;


        private ItemInfo GetItemInfo(int index)
        {
            if (index >= _itemInfos.Count)
                return default(ItemInfo);

            return _itemInfos[index];
        }

        public void SubmitItem(Player player,long itemEid)
        {
            IsInRangeOf3D(player, SUBMIT_ITEM_RANGE).ThrowIfFalse(ErrorCodes.AttackPointIsOutOfRange);

            Site.IntrusionInProgress.ThrowIfFalse(ErrorCodes.SiegeAlreadyExpired);

            var progress = _playerItemProgresses.GetOrAdd(player.Character.Id, new PlayerItemProgress());

            progress.nextSubmitTime.ThrowIfGreater(DateTime.Now,ErrorCodes.SiegeSubmitItemOverload);

            var container = player.GetContainer();
            Debug.Assert(container != null, "container != null");
            container.EnlistTransaction();

            var item = container.GetItemOrThrow(itemEid);

            var requestedItemInfo = GetItemInfo(progress.index);

            requestedItemInfo.Definition.ThrowIfNotEqual(item.Definition,ErrorCodes.SiegeDefinitionNotSupported);

            var neededQty = requestedItemInfo.Quantity - progress.quantity;

            var submittedQty = UpdateOrDeleteItem(item, neededQty);

            container.Save();

            if ( submittedQty > 0 )
            {
                Transaction.Current.OnCompleted(c => UpdateProgess(player, container, submittedQty, requestedItemInfo, progress));
            }
        }

        private void UpdateProgess(Player player, RobotInventory container, int submittedQty, ItemInfo requestedItemInfo, PlayerItemProgress progress)
        {
            progress.quantity += submittedQty;

            if (progress.quantity >= requestedItemInfo.Quantity)
            {
                progress.index++;
                progress.quantity = 0;

                IncrementPlayerScore(player, 1);
            }

            progress.nextSubmitTime = DateTime.Now + _submitItemCooldown;

            container.SendUpdateToOwnerAsync();

            if (progress.index >= _itemInfos.Count) 
                return;

            SendProgressToPlayer(player.Character);
        }

        public void SendProgressToPlayer(Character character)
        {
            var currentIndex = 0;
            var submittedQty = 0;
            var nextSubmitTime = default(DateTime);
           
            PlayerItemProgress itemProgress;
            if ( _playerItemProgresses.TryGetValue(character.Id,out itemProgress))
            {
                currentIndex = itemProgress.index;
                submittedQty = itemProgress.quantity;
                nextSubmitTime = itemProgress.nextSubmitTime;
            }

            var itemInfo = GetItemInfo(currentIndex);
            var maxScore = MaxScore;
            var currentScore = GetPlayerScore(character);
            
            var data = new Dictionary<string, object>
                           {
                               {k.eid,Eid},
                               {k.definition, itemInfo.Definition},
                               {k.quantity, itemInfo.Quantity},
                               {k.current, submittedQty},
                               {k.submitInterval,(int)_submitItemCooldown.TotalMinutes},
                               {k.nextSubmitTime,nextSubmitTime},
                               {k.maxScore,maxScore},
                               {k.currentScore,currentScore}
                           };

            Message.Builder.SetCommand(Commands.IntrusionSapItemInfo).WithData(data).WrapToResult().ToCharacter(character).Send();
        }


        private static int UpdateOrDeleteItem(Item item, int neededQty)
        {
            var itemQty = item.Quantity;
            var submittedQty = 0;

            if (itemQty > neededQty)
            {
                item.Quantity -= neededQty;
                submittedQty = neededQty;
            }
            else
            {
                Repository.Delete(item);
                submittedQty = itemQty;
            }

            return submittedQty;
        }

        private class PlayerItemProgress
        {
            public int index;
            public int quantity;
            public DateTime nextSubmitTime;
        }

        protected override void AppendTopScoresToPacket(Packet packet,int count)
        {
            AppendPlayerTopScoresToPacket(this, packet,count);
        }

    }
}