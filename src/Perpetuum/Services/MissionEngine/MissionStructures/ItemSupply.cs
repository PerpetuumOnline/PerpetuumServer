using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Transactions;
using Perpetuum.Accounting.Characters;
using Perpetuum.Common.Loggers.Transaction;
using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.Items;
using Perpetuum.Log;
using Perpetuum.Players;
using Perpetuum.Services.MissionEngine.MissionTargets;
using Perpetuum.Services.ProductionEngine.CalibrationPrograms;

namespace Perpetuum.Services.MissionEngine.MissionStructures
{


    /// <summary>
    /// 
    /// On period over spawns an item into the player's container
    /// 
    /// </summary>
    public class ItemSupply : AlarmSwitch
    {
        public ItemSupply() : base(MissionTargetType.use_itemsupply) {}

        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
                base.AcceptVisitor(visitor);
        }

        protected override void OnPeriodOver(Player player)
        {
            var info = BaseInfoToDictionary();
            info.Add(k.success, true);
            Message.Builder.SetCommand(Commands.AlarmOver).WithData(info).ToCharacter(player.Character).Send();
            GetSuppliedItem(player);
        }

        private void GetSuppliedItem(Player player)
        {
            if (!player.InZone)
                return;

            //csak mission felveve
            
            var supplyTargets = player.MissionHandler.GetTargetsForMissionStructure(this);
            if (supplyTargets.Count == 0) 
                return;

            using (var scope = Db.CreateTransaction())
            {
                try
                {
                var container = player.GetContainer();
                Debug.Assert(container != null, "container != null");
                container.EnlistTransaction();
                //spawn item to player

                var spawnedItems = new List<Item>();

                foreach (var targetBase in supplyTargets.Cast<ItemSupplyZoneTarget>())
                {
                    var goalQuantity = targetBase.MyTarget.ValidQuantitySet ? targetBase.MyTarget.Quantity : 1;
                    var alreadyGiven = targetBase.GetCurrentProgress();
                    var quantityNeeded = (goalQuantity - alreadyGiven).Clamp(0, goalQuantity);
                    if (quantityNeeded == 0)
                    {
                        Logger.Error("WTF in GetSuppliedItem " + targetBase.MyTarget);
                        continue;
                    }

                    var itemEd = EntityDefault.Get(targetBase.MyTarget.Definition);
                    var quantity = container.GetMaximalQuantity(itemEd, quantityNeeded);

                    if (quantity <= 0)
                    {
                        // clarify gameplay 
                        Message.Builder.SetCommand(Commands.MissionError).WithData(container.GetCapacityInfo()).ToCharacter(player.Character).WithError(ErrorCodes.ContainerIsFull).Send();
                        continue;
                    }

                    var item = (Item)Factory.CreateWithRandomEID(itemEd);
                    item.Owner = player.Character.Eid;
                    item.Quantity = quantity;

                    // ha itt megall akkor rosszul van kiszamolva a quantity
                    Debug.Assert(container.IsEnoughCapacity(item), "not enough capacity!");

                    //this is the point when the item supply spawns a cprg
                    //collect components here from the mission
                    var randomCalibrationProgram = item as RandomCalibrationProgram;
                    randomCalibrationProgram?.SetComponentsFromRunningTargets(player.Character);

                    container.AddItem(item, false);
                    spawnedItems.Add(item);

                    var b = TransactionLogEvent.Builder().SetTransactionType(TransactionType.ItemSupply).SetCharacter(player.Character).SetItem(item);
                    player.Character.LogTransaction(b);
                }

                    container.Save();

                    Transaction.Current.OnCommited(() =>
                {
                    foreach (var item in spawnedItems)
                    {
                        player.MissionHandler.EnqueueMissionEventInfo(new ItemSupplyEventInfo(player, item, this, CurrentPosition));
                    }

                    //success beam kirajzolo
                    CreateSuccessBeam(player);

                    container.SendUpdateToOwner();
                    CreateInteractionBeam(player);
                });

                scope.Complete();
            }
                catch (Exception ex)
                {
                    var err = ErrorCodes.ServerError;
                    var gex = ex as PerpetuumException;
                    if (gex != null)
                    {
                        err = gex.error;
                        Logger.Exception(ex);
                    }
                    else
                    {
                        Logger.Exception(ex);
                    }

                    player.Character.SendErrorMessage(Commands.MissionError,err);
                }
            }
        }
    }


}
