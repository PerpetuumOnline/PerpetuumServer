using System;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;
using Perpetuum.Accounting.Characters;
using Perpetuum.Common.Loggers.Transaction;
using Perpetuum.Containers;
using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.Log;
using Perpetuum.Services.MissionEngine.AdministratorObjects;
using Perpetuum.Services.MissionEngine.Missions;
using Perpetuum.Services.MissionEngine.MissionStructures;
using Perpetuum.Services.MissionEngine.MissionTargets;

namespace Perpetuum.Services.MissionEngine.MissionProcessorObjects
{
    public partial class MissionProcessor
    {
        /// <summary>
        ///     Deliver items to a single mission
        /// </summary>
        public void DeliverSingleMission(Character character, Guid missionGuid, int locationId = 0)
        {
            Logger.Info($"++ Deliver starts for character:{character} missionGuid:{missionGuid}");

            var isDocked = character.IsDocked;

            MissionLocation location;
            if (isDocked) { location = _missionDataCache.GetLocationByEid(character.CurrentDockingBaseEid); }
            else
            {
                //ok, let's use the optional parameter
                locationId.ThrowIfEqual(0, ErrorCodes.WTFErrorMedicalAttentionSuggested);

                if (!_missionDataCache.GetLocationById(locationId, out location)) { throw new PerpetuumException(ErrorCodes.InvalidMissionLocation); }
            }

            //safety
            location.ThrowIfNull(ErrorCodes.WTFErrorMedicalAttentionSuggested);

            //get the affected characters from the gang
            var charactersToProcess = GetGangMembersCached(character);

            MissionInProgress missionInProgress = null;

            //let's collect the targets
            foreach (var processedCharacter in charactersToProcess)
            {
                if (MissionAdministrator.RunningMissionsCount(processedCharacter) == 0) continue;

                MissionInProgressCollector collector;
                if (MissionAdministrator.GetMissionInProgressCollector(processedCharacter, out collector))
                {
                    foreach (var mip in collector.GetMissionsInProgress())
                    {
                        if (mip.missionGuid == missionGuid)
                        {
                            missionInProgress = mip;
                            break;
                        }
                    }
                }
            }

            var result = new Dictionary<string,object>();
             
            if (missionInProgress == null)
            {
                // mission was not found
                Message.Builder.SetCommand(Commands.MissionDeliver)
                    .WithError(ErrorCodes.MissionNotFound)
                   .ToCharacter(character)
                   .Send();
                return;
            }

            if (missionInProgress.IsMissionFinished)
            {
                Message.Builder.SetCommand(Commands.MissionDeliver)
                   .WithError(ErrorCodes.MissionAlreadyDone)
                  .ToCharacter(character)
                  .Send();
                return;
            }

            var interestingTargets =  missionInProgress.CollectTargetsWithDefinitionsToDeliverInCurrentState(location).ToList();
            DeliverMissionByTargetList(interestingTargets, character, location);
        }


        private void DeliverMissionByTargetList(List<MissionTargetInProgress> interestingTargets, Character character, MissionLocation location)
        {
            var isDocked = character.IsDocked;
            var deliveryState = DeliverResult.nothingHappened;
            var result = new Dictionary<string, object>();

            if (interestingTargets.Count == 0)
            {
                //no targets were found among running targets / squad running targets
#if DEBUG
                Logger.Info("no targets to deliver in this round. ");
#endif

                result.Add(k.deliveryState, (int)deliveryState);

                Message.Builder.SetCommand(Commands.MissionDeliver)
                    .WithData(result)
                    .ToCharacter(character)
                    .Send();

                return;
            }

            Container robotInventory = null;
            var activeRobot = character.GetActiveRobot();
            if (activeRobot != null) { robotInventory = activeRobot.GetContainer(); }

            var publicContainer = location.GetContainer;
            if (publicContainer == null)
            {
                Logger.Error("WTF no public container on location " + location + " characterId:" + character + " isDocked:" + isDocked);
                throw new PerpetuumException(ErrorCodes.ContainerNotFound);
            }

            var itemsAffected = 0;
            var deliveryHelpers = GenerateDeliveryHelpers(interestingTargets, location, character);

            var itemsFromRobot = 0;
            var itemsFromPublicContainer = 0;

            if (isDocked)
            {
                //in docking base

                //robot inventory full tree
                if (robotInventory != null)
                    LookUpContainerForMissionDeliverItems(character, robotInventory, ref itemsAffected, deliveryHelpers, true);
                itemsFromRobot = itemsAffected;


                //first level
                LookUpContainerForMissionDeliverItems(character, publicContainer, ref itemsAffected, deliveryHelpers, false);
                itemsFromPublicContainer = itemsAffected - itemsFromRobot;
            }
            else
            {
                // at field terminal

                //full tree
                if (robotInventory != null)
                    LookUpContainerForMissionDeliverItems(character, robotInventory, ref itemsAffected, deliveryHelpers, true);
                itemsFromRobot = itemsAffected;

                //full tree
                LookUpContainerForMissionDeliverItems(character, publicContainer, ref itemsAffected, deliveryHelpers, true);
                itemsFromPublicContainer = itemsAffected - itemsFromRobot;
            }

            if (itemsAffected == 0)
            {
#if DEBUG
                Logger.Info("no items were found in containers for characterId:" + character);
#endif

                //deliveryState = DeliverResult.noDeliverableItemsFound;
                //no items were found in containers
                //result.Add(k.deliveryState, (int)deliveryState);

                Message.Builder.SetCommand(Commands.MissionDeliver)
                    .WithError(ErrorCodes.MissionNoItemsFoundToDeliver)
                    .ToCharacter(character)
                    .Send();

                return;
            }


            //these objects to progressed
            var changedDeliveryHelpers = deliveryHelpers.Keys.Where(dh => dh.wasChange).ToList();

            //collect information about the affected but not finished targets for the player who delivered
            var anyProgressItems = new Dictionary<string, object>();
            var completedItems = new Dictionary<string, object>();
            var itemsWithNoProgress = new Dictionary<string, object>();

            var counter = 0;
            //process delivery helpers
            foreach (var deliveryHelper in deliveryHelpers.Keys)
            {
                //fully completed targets
                if (deliveryHelper.IsCompleted)
                {
                    completedItems.Add("c" + counter++, deliveryHelper.CompletedInfo);
                    continue;
                }

                //it progressed, but not finished
                if (deliveryHelper.wasChange && deliveryHelper.IsQuantityMissing) { anyProgressItems.Add("i" + counter++, deliveryHelper.MissingInfo); }

                //unaffected
                if (!deliveryHelper.wasChange) { itemsWithNoProgress.Add("u" + counter++, deliveryHelper.MissingInfo); }
            }

            //completed info
            if (completedItems.Count > 0) { result.Add("completedItems", completedItems); }

            //any progress
            if (anyProgressItems.Count > 0) { result.Add("anyProgress", anyProgressItems); }

            //no progress
            if (itemsWithNoProgress.Count > 0) { result.Add("noProgress", itemsWithNoProgress); }

            //if any target got completed we send a completed
            if (completedItems.Count > 0) { deliveryState = DeliverResult.completed; }
            else
            {
                //no completed targets, was any progression?
                if (anyProgressItems.Count > 0)
                {
                    //some targets progressed
                    deliveryState = DeliverResult.partiallyDelivered;
                }
            }

            //info mission engine
            Transaction.Current.OnCommited(() =>
            {
                foreach (var changedDeliveryHelper in changedDeliveryHelpers)
                {
                    changedDeliveryHelper.EnqueueProgressInfo();
                }

                if (!isDocked)
                {
                    //container refresh on zone
                    character.ReloadContainerOnZoneAsync();
                }

                result.Add(k.items, publicContainer.ToDictionary());
                result.Add(k.deliveryState, (int)deliveryState);
                result.Add("nofItemsInRobotInventory", itemsFromRobot);
                result.Add("nofItemsInPublicContainer", itemsFromPublicContainer);

                Logger.Info("Mission delivery result. characterID:" + character.Id + " nofItemsInRobotInventory:" + itemsFromRobot + " nofItemsInPublicContainer:" + itemsFromPublicContainer + " completed targets:" + completedItems.Count + " wasProgress:" + anyProgressItems.Count + " unaffected:" + itemsWithNoProgress.Count + " out of targets:" + interestingTargets.Count + " total affected items:" + itemsAffected + " " + location);

                Message.Builder.SetCommand(Commands.MissionDeliver)
                    .WithData(result)
                    .ToCharacter(character)
                    .Send();
            });
        }


        private Dictionary<DeliveryHelper, MissionTargetInProgress> GenerateDeliveryHelpers(List<MissionTargetInProgress> targetsInProgress, MissionLocation location, Character delivererCharacter)
        {
            var deliveryHelpers = new Dictionary<DeliveryHelper, MissionTargetInProgress>(targetsInProgress.Count);
            foreach (var targetInProgress in targetsInProgress)
            {
                var dh = targetInProgress.CreateDeliveryHelper(
                    location.id,
                    targetInProgress.myMissionInProgress.character,
                    targetInProgress.myMissionInProgress.MissionId,
                    targetInProgress.MissionTargetId,
                    delivererCharacter,
                    targetInProgress.myMissionInProgress.missionGuid
                    );

                deliveryHelpers.Add(dh, targetInProgress);
            }

            return deliveryHelpers;
        }

        //nem rekurziv
        private static void LookUpContainerForMissionDeliverItems(Character delivererCharacter, Container container, ref int itemsAffected, Dictionary<DeliveryHelper, MissionTargetInProgress> deliveryHelpers, bool fullTree)
        {
#if DEBUG
            Logger.Info("++ Item lookup starts. container: " + container);
#endif
            container.ReloadItems(delivererCharacter);

            foreach (var kvp in deliveryHelpers)
            {
                var targetInProgress = kvp.Value;
                var dh = kvp.Key;

                if (targetInProgress.completed)
                    continue;

                if (dh.IsCompleted)
                    continue;

                //obtain data for checking

                var amountNeeded = dh.quantity;
                amountNeeded -= dh.ProgressCount;

                var loadedItems = container.GetItems(fullTree)
                    .NotOf().Type<VolumeWrapperContainer>()
                    .Where(i =>
                    {
                        if (i.Definition != dh.definition)
                            return false;

                        var volumeWrapper = i.ParentEntity as VolumeWrapperContainer;
                        if (volumeWrapper != null)
                            return false;

                        return true;
                    })
                    .ToArray();

                var usedQuantityFromType = 0;

                foreach (var loadedItem in loadedItems)
                {
                    itemsAffected++;

                    if (loadedItem.Quantity < amountNeeded)
                    {
                        Entity.Repository.Delete(loadedItem);
                        amountNeeded -= loadedItem.Quantity;
                        dh.ProgressCount += loadedItem.Quantity;
#if DEBUG
                        Logger.Info("++ Item partially fulfills target amount " + dh.definition);
#endif
                        usedQuantityFromType += loadedItem.Quantity;
                        continue;
                    }

                    if (loadedItem.Quantity == amountNeeded)
                    {
                        dh.ProgressCount += loadedItem.Quantity;
                        Entity.Repository.Delete(loadedItem);

#if DEBUG
                        Logger.Info("++ Target and item quantity exact match! " + dh.definition);
#endif
                        usedQuantityFromType += amountNeeded;
                        break;
                    }


                    //defition found, check quantity
                    if (loadedItem.Quantity > amountNeeded)
                    {
                        loadedItem.Quantity -= amountNeeded;
                        dh.ProgressCount += amountNeeded;

#if DEBUG
                        Logger.Info("++ More items found than needed in target " + dh.definition);
#endif
                        usedQuantityFromType += amountNeeded;
                        break;
                    }
                }

                if (usedQuantityFromType > 0)
                {
                    //do item log
                    var b = TransactionLogEvent.Builder().SetTransactionType(TransactionType.MissionItemDeliver)
                        .SetCharacter(delivererCharacter)
                        .SetItem(dh.definition, usedQuantityFromType)
                        .SetContainer(container);
                    delivererCharacter.LogTransaction(b);
                }
            }

            container.Save();

#if DEBUG
            Logger.Info("++ Item lookup ends container:" + container);
#endif
        }



        /// <summary>
        /// Multideliver
        ///     Collects all deliverable targets and tries to look up the items they require
        /// </summary>
        public void DeliverMissionByRequest(Character character, int locationId = 0)
        {
            Logger.Info("++ Deliver targets begins. characterId:" + character.Id);

            var deliveryState = DeliverResult.nothingHappened;

            //get the affected characters from the gang
            var charactersToProcess = GetGangMembersCached(character);

            var isDocked = character.IsDocked;

            MissionLocation location;
            if (isDocked) { location = _missionDataCache.GetLocationByEid(character.CurrentDockingBaseEid); }
            else
            {
                //ok, let's use the optional parameter
                locationId.ThrowIfEqual(0, ErrorCodes.WTFErrorMedicalAttentionSuggested);

                if (!_missionDataCache.GetLocationById(locationId, out location)) { throw new PerpetuumException(ErrorCodes.InvalidMissionLocation); }
            }

            //safety
            location.ThrowIfNull(ErrorCodes.WTFErrorMedicalAttentionSuggested);

            var interestingTargets = new List<MissionTargetInProgress>();

            //let's collect the targets
            foreach (var processedCharacter in charactersToProcess)
            {
                if (MissionAdministrator.RunningMissionsCount(processedCharacter) == 0) continue;

                MissionInProgressCollector collector;
                if (MissionAdministrator.GetMissionInProgressCollector(processedCharacter, out collector))
                {
                    foreach (var mip in collector.GetMissionsInProgress())
                    {
                        if (mip.IsMissionFinished) continue;

                        interestingTargets.AddRange(mip.CollectTargetsWithDefinitionsToDeliverInCurrentState(location));
                    }
                }
            }

            DeliverMissionByTargetList(interestingTargets, character, location);

        }
    }
}