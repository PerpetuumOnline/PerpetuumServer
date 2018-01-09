using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Perpetuum.Accounting.Characters;
using Perpetuum.Data;
using Perpetuum.Items;
using Perpetuum.Log;
using Perpetuum.Services.MissionEngine.AdministratorObjects;
using Perpetuum.Services.MissionEngine.Missions;
using Perpetuum.Services.MissionEngine.MissionStructures;
using Perpetuum.Services.MissionEngine.MissionTargets;
using Perpetuum.Zones.Artifacts.Repositories;

namespace Perpetuum.Services.MissionEngine.MissionProcessorObjects
{
    public partial class MissionProcessor
    {
        public Task EnqueueMissionTargetAsync(Character character,MissionTargetType targetType,Action<IDictionary<string,object>> dictionaryBuilder)
        {
            var dictionary = new Dictionary<string,object>
            {
                {k.characterID,character.Id},
                {k.type, (int) targetType}
            };

            dictionaryBuilder(dictionary);
            return EnqueueMissionTargetAsync(dictionary);
        }

        public Task EnqueueMissionTargetAsync(Dictionary<string,object> data)
        {
            return MissionTargetAdvancedAsync(data);
        }

        public Task MissionTargetAdvancedAsync(Dictionary<string,object> data)
        {
            return Task.Run(() => MissionTargetAdvanced(data));
        }

        public void MissionTargetAdvanced(Dictionary<string,object> data)
        {
            var originalData = data;
            var character = Character.Get(data.GetOrDefault<int>(k.characterID));
            var explicitGangHandling = data.GetOrDefault<int>(k.useGang) == 1;
            var targetType = data.GetOrDefault<MissionTargetType>(k.type);

            MissionTargetAdvanced(character, targetType, explicitGangHandling, originalData);
        }

        private void MissionTargetAdvanced(Character character, MissionTargetType targetType, bool explicitGangHandling, Dictionary<string, object> originalData)
        {
            //explicit gang handling not set
            if (!explicitGangHandling)
            {
                MissionTargetAdvancedSingle(originalData);

                if (WasProgress(originalData))
                {
                    SendRunningMissionListAsync(character);
                }

                return;
            }

            //ok, lets get deeper, we must use the gang 

            //first enqueue for myself, we are the priority here
            MissionTargetAdvancedSingle(originalData);

            if (WasProgress(originalData))
            {
                //yes, this request advanced one of my targets, we are done here, send running list if all done.
                SendRunningMissionListAsync(character);
                return;
            }

            //do gang

            var others = GetGangMembersCached(character).Where(m => m.Id != character.Id);
            foreach (var other in others)
            {
                //any mission running?
                if (!MissionAdministrator.GetMissionInProgressCollector(other, out MissionInProgressCollector collector))
                    continue;

                //any targets waiting?
                if (!collector.GetMissionsInProgress().SelectMany(m => m.CollectIncompleteTargetsByType(targetType)).Any(t => t.IsMyTurn))
                    continue;

                var info = originalData.Clone();

                //fake the source character
                info[k.characterID] = other.Id;

                //force add participation info
                info[k.assistingCharacterID] = character.Id;

                MissionTargetAdvancedSingle(info);

                //if event got used up => end of its life
                if (WasProgress(originalData))
                    return;
            }
        }

        private bool WasProgress(Dictionary<string, object> originalData)
        {
            return originalData.ContainsKey(k.wasProgress);
        }

        /// <summary>
        /// Zone or other module informs the mission engine that a certain target has advanced
        /// </summary>
        public void MissionTargetAdvancedSingle(IDictionary<string, object> infoDictionary)
        {
            var info = (Dictionary<string, object>) infoDictionary;

            var missionId = info.GetOrDefault<int>(k.missionID);
            var targetType = info.GetOrDefault<MissionTargetType>(k.type);
            var isComplete = info.GetOrDefault<bool>(k.completed);
            var character = Character.Get(info.GetOrDefault<int>(k.characterID));
            var targetId = info.GetOrDefault<int>(k.targetID);
            var missionGuid = info.GetOrDefault<Guid>(k.guid);

            var processById =
                !(
                    targetType == MissionTargetType.dock_in ||
                    targetType == MissionTargetType.prototype ||
                    targetType == MissionTargetType.massproduce ||
                    targetType == MissionTargetType.research ||
                    targetType == MissionTargetType.teleport
                    );

            try
            {
                if (processById)
                {
                    //new search mechanism, guid is needed
                    if (missionGuid.Equals(Guid.Empty))
                    {
                        Logger.Error("no mission guid!!! missionId:" + missionId + " " + targetType + " targetId:" + targetId);
                        return;
                    }

                    MissionTargetAdvanceById(character, targetId, missionId, isComplete, info, missionGuid);
                }
                else
                {
                    MissionTargetAdvanceByType(character, targetType, isComplete, info);
                }
            }
            catch (Exception ex)
            {
                Logger.Exception(ex);
            }
        }


        private void MissionTargetAdvanceById(Character character, int targetId, int missionId, bool isComplete, IDictionary<string, object> data, Guid missionGuid)
        {
            Logger.DebugInfo("processing target by id: " + targetId + " missionId: " + missionId);
            if (!GetTargetInProgress_and_missionInProgress(character, targetId, missionId, missionGuid, out MissionTargetInProgress missionTargetInProgress, out MissionInProgress missionInProgress))
            {
                Logger.Error("targetinprogress was not found. targetId: " + targetId + " missionId: " + missionId);
                return;
            }

            //do the work
            ProcessTargetAdvancementLocked(missionInProgress, missionTargetInProgress, isComplete, data);
        }

        private void MissionTargetAdvanceByType(Character character, MissionTargetType targetType, bool isComplete, Dictionary<string, object> data)
        {
            Logger.DebugInfo("process target by type: " + targetType);

            var kvpList = GetTargetInProgress_and_missionInProgressByTargetType(character, targetType);

            if (kvpList.IsNullOrEmpty())
                return;

            Logger.Info("processing " + kvpList.Count + " running targets for type: " + targetType);

            foreach (var pair in kvpList)
            {
                var missionTargetInProgress = pair.Key;
                var missionInProgress = pair.Value;

                ProcessTargetAdvancementLocked(missionInProgress, missionTargetInProgress, isComplete, data);

                if (WasProgress(data))
                    return;
            }
        }
      

        private void ProcessTargetAdvancementLocked(MissionInProgress missionInProgress, MissionTargetInProgress missionTargetInProgress, bool isComplete, IDictionary<string, object> data)
        {
            //pre filter
            if (!missionTargetInProgress.IsMyTurn) return;
            if (missionTargetInProgress.completed) return;
            if (missionInProgress.IsMissionFinished) return;

            lock (missionInProgress.lockObject)
            {
                //finally we got in, in the meanwhile the target and the mission might got done, progressed, etc
                if (!missionTargetInProgress.IsMyTurn) return;
                if (missionTargetInProgress.completed) return;
                if (missionInProgress.IsMissionFinished) return;
                

                ProcessAdvamcementForOneTarget(missionTargetInProgress, missionInProgress, data, isComplete);
            }
        }


        private void ProcessAdvamcementForOneTarget(MissionTargetInProgress missionTargetInProgress, MissionInProgress missionInProgress, IDictionary<string, object> info, bool isComplete)
        {
            Logger.Info("processing " + missionTargetInProgress + " for " + missionInProgress);

            var ec = ErrorCodes.NoError;
            var targetType = missionTargetInProgress.TargetType;
            var character = missionInProgress.character;

            //this character completed the target. 
            //note: Partial progress doesn't trigger this
            //for the toast in the client

            if (missionInProgress.ExtractDoerCharacter(info,out Character doerCharacter))
            {
                Logger.Info("    >>>> ASSISTED COMPLETE. characterId:" + doerCharacter.Id + " is helped characterID:" + character.Id + " " + missionTargetInProgress.TargetType + " " + missionInProgress.missionGuid);
            }

            switch (targetType)
            {
                case MissionTargetType.loot_item:

                    var lootItemsCount = info.GetOrDefault<int>(k.progressCount);
                    ec = missionTargetInProgress.AdvanceTarget_LootItem(isComplete, lootItemsCount);
                    break;

                case MissionTargetType.kill_definition:

                    if (info.ContainsKey(k.increase))
                    {
                        ec = missionTargetInProgress.AdvanceTarget_KillDefinition_IncreaseOnly();
                        break;
                    }

                    var killDefinitionCount = info.GetOrDefault<int>(k.progressCount);
                    ec = missionTargetInProgress.AdvanceTarget_KillDefinition(isComplete, killDefinitionCount);
                    break;

                case MissionTargetType.reach_position:

                    ec = missionTargetInProgress.AdvanceTarget_ReachPosition();
                    break;

                case MissionTargetType.scan_mineral:

                    ec = missionTargetInProgress.AdvanceTarget_ScanMineral();
                    break;

                case MissionTargetType.scan_unit:

                    var scanCount = info.GetOrDefault<int>(k.progressCount);
                    ec = missionTargetInProgress.AdvanceTarget_ScanUnit(isComplete, scanCount);
                    break;

                case MissionTargetType.scan_container:
                    var foundInContainer = info.GetOrDefault<int>(k.progressCount);
                    ec = missionTargetInProgress.AdvanceTarget_ScanContainer(isComplete, foundInContainer);
                    break;

                case MissionTargetType.drill_mineral:
                    var drilledAmount = info.GetOrDefault<int>(k.progressCount);
                    ec = missionTargetInProgress.AdvanceTarget_MineralDrilled(isComplete, drilledAmount);
                    break;

                case MissionTargetType.submit_item:
                    var wasInContainer = info.GetOrDefault<int>(k.progressCount);
                    ec = missionTargetInProgress.AdvanceTarget_SubmitItem(isComplete, wasInContainer);
                    break;

                case MissionTargetType.use_switch:
                    ec = missionTargetInProgress.AdvanceTarget_Alarm();
                    break;

                case MissionTargetType.find_artifact:
                    ec = missionTargetInProgress.AdvanceTarget_FindArtifact();
                    break;

                case MissionTargetType.dock_in:
                    var zoneId = info.GetOrDefault<int>(k.zoneID);
                    var position = info.GetOrDefault<Position>(k.position);
                    ec = missionTargetInProgress.AdvanceTarget_DockIn(position, zoneId);
                    break;

                case MissionTargetType.use_itemsupply:
                    var itemCount = info.GetOrDefault<int>(k.progressCount);
                    ec = missionTargetInProgress.AdvanceTarget_ItemSupply(isComplete, itemCount);
                    break;

                case MissionTargetType.massproduce:

                    //needs ct and goes for the produced definition

                    var producedDefinition = info.GetOrDefault<int>(k.definition);
                    var quantity = info.GetValue<int>(k.quantity);
                    ec = missionTargetInProgress.AdvanceTarget_MassProduce(producedDefinition, quantity, info);
                    break;

                case MissionTargetType.prototype:

                    //not used

                    var protoDefinition = info.GetOrDefault<int>(k.definition);
                    ec = missionTargetInProgress.AdvanceTarget_Prototype(protoDefinition);
                    break;

                case MissionTargetType.research:

                    //needs decoder and goes for the resulting definition -> ct definition

                    var researchedDefinition = info.GetOrDefault<int>(k.definition);
                    ec = missionTargetInProgress.AdvanceTarget_Research(researchedDefinition, info);
                    break;

                case MissionTargetType.teleport:
                    var channelId = info.GetOrDefault<int>(k.channel);
                    ec = missionTargetInProgress.AdvanceTarget_Teleport(channelId);
                    break;

                case MissionTargetType.harvest_plant:
                    var harvestedAmount = info.GetOrDefault<int>(k.progressCount);
                    ec = missionTargetInProgress.AdvanceTarget_MineralHarvested(isComplete, harvestedAmount);
                    break;

                case MissionTargetType.summon_npc_egg:
                    var currentProgress = info.GetOrDefault<int>(k.progressCount);
                    ec = missionTargetInProgress.AdvanceTarget_SummonNPCEgg(isComplete, currentProgress);
                    break;

                case MissionTargetType.pop_npc:
                    ec = missionTargetInProgress.AdvanceTarget_PopNpc();
                    break;

                case MissionTargetType.fetch_item:

                    var quantityFetched = info.GetOrDefault<int>(k.progressCount);
                    var location = info.GetOrDefault<int>(k.location);
                    ec = missionTargetInProgress.AdvanceTarget_FetchItem(isComplete, quantityFetched, location);
                    break;

                case MissionTargetType.lock_unit:
                    var lockedUnitCount = info.GetOrDefault<int>(k.progressCount);
                    var lockedNpcEids = info.GetOrDefault<long[]>("lockedUnits");
                    ec = missionTargetInProgress.AdvanceTarget_LockUnit(isComplete, lockedUnitCount, lockedNpcEids);
                    break;
            }

            if (ec == ErrorCodes.NothingToDo)
            {
                Logger.Info("Nothing to do exiting " + missionTargetInProgress);
                return;
            }

            ec.ThrowIfError();

            using (var scope = Db.CreateTransaction())
            {
                try
                {
                    var wasGroupFinished = false;
                    ProcessOneAdvancedTarget(character, missionTargetInProgress, missionInProgress, ref wasGroupFinished, info);
                    info[k.wasProgress] = 1; //signal progress to out functions
                    scope.Complete();
                }
                catch (Exception ex)
                {
                    Logger.Exception(ex);
                }
            }
        }


        private IList<Item> ProcessOneAdvancedTarget(Character character, MissionTargetInProgress missionTargetInProgress, MissionInProgress missionInProgress, ref bool wasGroupFinished, IDictionary<string, object> info)
        {
            var gangResultCommand = Commands.MissionTargetUpdate;
            string gangResultMessage = null;


            if (missionTargetInProgress.completed)
            {
                //extract success location
                missionTargetInProgress.GetSuccessInfo(info);
                missionTargetInProgress.WriteSuccessInfo();
                gangResultCommand = Commands.MissionTargetCompleted;
                gangResultMessage = missionTargetInProgress.myTarget.completedMessage;
            }
            
            //progress occured => save target anyway
            missionTargetInProgress.WriteMissionTargetToSql().ThrowIfError();
            
            if (missionTargetInProgress.IsBranchNeeded())
            {
                Logger.Info(">>> target is branching " + missionTargetInProgress);

                //ha lefutott, akkor kell ennek taskban elindulnia egy masik tranzakcioban
                Transaction.Current.OnCommited(() => { StartAsync(character, missionInProgress.spreadInGang, missionTargetInProgress.myTarget.BranchMissionId, missionInProgress.myLocation, missionInProgress.MissionLevel); });

                wasGroupFinished = true;
            }
            else
            {
                // Default behaviour
                missionInProgress.TryAdvanceTargetGroup(ref wasGroupFinished).ThrowIfError();
            }

            if (missionInProgress.ExtractDoerCharacter(info,out Character doerCharacter))
            {
                missionInProgress.AddParticipant(doerCharacter);
            }
            else
            {
                doerCharacter = character; //proper eventsource
            }

            var rewarditems = TryFinishMission(missionInProgress);

            Logger.Info("mission advanced! characterID:" + character.Id + " missionID:" + missionInProgress.MissionId + " targetId:" + missionTargetInProgress.MissionTargetId + " completed:" + missionTargetInProgress.completed);

            Transaction.Current.OnCommited(() =>
            {
                var statusInfo = missionInProgress.PrepareInfoDictionary(message: gangResultMessage, sourceCharacterId: doerCharacter.Id);
                missionTargetInProgress.PrepareInfoDictionary(statusInfo);

                missionTargetInProgress.SendTargetStatusToGangAsync(gangResultCommand, statusInfo);
               
            });

            return rewarditems;
        }

        private static readonly object LockObject = new object();

        [CanBeNull]
        private IList<Item> TryFinishMission(MissionInProgress missionInProgress)
        {
            if (!missionInProgress.IsMissionFinished)
                return null;

            lock (LockObject)
            {
                //mission is finished
                var mission = missionInProgress.myMission;

                var missionBonus = MissionAdministrator.GetOrAddMissionBonus(missionInProgress.character, mission.missionCategory, missionInProgress.MissionLevel, missionInProgress.myLocation.Agent);

                Logger.Info("++ Mission is finished " + missionInProgress);

                var successData = new Dictionary<string, object>
                {
                    {k.bonusMultiplier, missionBonus.BonusMultiplier},
                    {"rawBonusMultiplier", (int) Math.Round(missionBonus.RawMultiplier * 100.0)},
                    {"bonusLevel", missionBonus.Bonus}
                };

                var rewardItems = missionInProgress.SpawnRewardItems(out MissionLocation locationUsed);

                //online gang members
                var charactersToInform = missionInProgress.GetAffectedCharacters();

                //mission participants
                var participants = missionInProgress.GetParticipantsVerbose().ToList();

                participants = missionInProgress.FilterParticipants(participants);

                var ep = missionInProgress.CalculateEp();

                var epDict = new Dictionary<string, object>();
                var count = 0;
                foreach (var participant in participants)
                {
                    var oneMansEp = 0;
                    if (ep > 0)
                        oneMansEp = participant.AddExtensionPointsBoostAndLog(EpForActivityType.Mission, ep);

                    var oneEpEntry = new Dictionary<string, object>
                    {
                        {k.characterID, participant.Id},
                        {k.points, oneMansEp}
                    };

                    epDict.Add("ep" + count++, oneEpEntry);
                }

                missionInProgress.GetRewardDivider(participants, out double rewardFraction, out int rewardDivider);

                successData.Add(k.nofGangMembers, participants.Count);

                missionInProgress.IncreaseStanding(rewardFraction, participants, charactersToInform, successData);

                //pay the fee
                missionInProgress.PayOutMission(rewardFraction, participants, charactersToInform, successData);

                var isTempTeleportEnabled = false;
                if (mission.MissionLevel == -1 || mission.behaviourType == MissionBehaviourType.Random)
                {
                    //set teleport timer
                    missionInProgress.EnableTeleportOnSuccess(participants, missionInProgress.myLocation.ZoneConfig.Id);
                    isTempTeleportEnabled = true;
                }

                missionInProgress.WriteSuccessLogAllTargets();
                missionInProgress.CleanUpAllTargets();

                ArtifactRepository.DeleteArtifactsByMissionGuid(missionInProgress.missionGuid);

                //write mission log - mission completed
                missionInProgress.SetSuccessToMissionLog(true).ThrowIfError();

                AdvanceBonusInGang(participants, missionInProgress);

                //remove mission
                MissionAdministrator.RemoveMissionInProgress(missionInProgress);

                if (mission.ValidSuccessMissionIdSet)
                {
                    Logger.Info("++ Begin triggering a mission on success. triggered missionId:" + mission.MissionIdOnSuccess);
                    Transaction.Current.OnCommited(() =>
                    {
                        //trigger a new mission with the current location
                        StartAsync(missionInProgress.character, missionInProgress.spreadInGang, mission.MissionIdOnSuccess, missionInProgress.myLocation, missionInProgress.MissionLevel);
                    });
                }

                //add to ram cache
                AddToFinishedMissions(missionInProgress.character, missionInProgress.MissionId);

                var resultDict = new Dictionary<string, object>
                {
                    {k.mission, missionInProgress.ToDictionary()},
                    {"successData", successData},
                    {"teleportEnabled", (isTempTeleportEnabled ? 1 : 0)},
                    {k.extensionPoints, epDict},
                };

                //reply mission status
                if (rewardItems.Any())
                {
                    resultDict.Add(k.reward, rewardItems.ToDictionary("r", r => r.BaseInfoToDictionary()));

                    if (locationUsed != null)
                        resultDict.Add("rewardLocation", locationUsed.id);
                }

                missionInProgress.DeleteParticipants();

                Transaction.Current.OnCommited(() => Message.Builder.SetCommand(Commands.MissionDone).WithData(resultDict).ToCharacters(charactersToInform).Send());

                Logger.Info("++ mission is finished. " + missionInProgress);

                return rewardItems;
            }
        }


        private void AdvanceBonusInGang(List<Character> charactersToSpread, MissionInProgress missionInProgress)
        {
            foreach (var character in charactersToSpread)
            {
                var missionBonus = MissionAdministrator.GetOrAddMissionBonus(character, missionInProgress.myMission.missionCategory, missionInProgress.MissionLevel, missionInProgress.myLocation.Agent);
                missionBonus.AdvanceBonus();
                MissionAdministrator.SetMissionBonus(missionBonus);
            }
        }


        /// <summary>
        /// Get all targets with the given type in pairs to process advancement
        /// 
        /// Since the target is wrapped into mission and mission wrapped into collectors getting a target is always:
        /// get collector by character, get mission, get target 
        /// 
        /// </summary>
        /// <param name="character"></param>
        /// <param name="targetType"></param>
        /// <returns></returns>
        private List<KeyValuePair<MissionTargetInProgress, MissionInProgress>> GetTargetInProgress_and_missionInProgressByTargetType(Character character, MissionTargetType targetType)
        {
            var resultList = new List<KeyValuePair<MissionTargetInProgress, MissionInProgress>>();

            if (!MissionAdministrator.GetMissionInProgressCollector(character,out MissionInProgressCollector collector))
                return resultList;

            foreach (var missionInProgress in collector.GetMissionsInProgress())
            {
                var list = missionInProgress.CollectIncompleteTargetsByType(targetType).ToArray();

                if (list.IsNullOrEmpty())
                    continue;

                foreach (var missionTargetInProgress in list)
                {
                    resultList.Add(new KeyValuePair<MissionTargetInProgress, MissionInProgress>(missionTargetInProgress, missionInProgress));
                }
            }

            return resultList;
        }

        /// <summary>
        /// Retuns the requested target
        /// 
        /// Since the mission in progress is needed for the target we return that as well to avoid further lookups
        /// </summary>
        /// <returns></returns>
        private bool GetTargetInProgress_and_missionInProgress(Character character, int targetId, int missionId, Guid missionGuid, out MissionTargetInProgress targetInProgress, out MissionInProgress missionInProgress)
        {
            targetInProgress = null;

            // oldschool version
            // 
            //myMissionAdministrator.FindMissionInProgressByMissionId(character, missionId, out missionInProgress);

            MissionAdministrator.FindMissionInProgressByMissionGuid(character, missionGuid, out missionInProgress);

            if (missionInProgress == null)
            {
                Logger.Warning("mission was not found for character: " + character.Id + " missionID:" + missionId);
                return false;
            }

            if (missionInProgress.MissionId != missionId)
            {
                Logger.Warning("WTF??!!! " + missionInProgress);
            }

            if (!missionInProgress.GetTargetInProgress(targetId, out targetInProgress))
            {
                Logger.Error("mission target was not found for caharcter: " + character.Id + " missionID:" + missionId + " targetID:" + targetId);
                return false;
            }

            if (targetInProgress.completed)
            {
                Logger.Info("target already completed. missionID: " + missionId + " targetID:" + targetId + " characterID:" + character.Id);
                return false;
            }

            return true;
        }

        public void NpcGotKilledInAway(Character character, Guid guid, Dictionary<string, object> data)
        {
            if (!FindMissionInProgress(character, guid, out MissionInProgress missionInProgress))
                return;

            if (missionInProgress.myMission.behaviourType == MissionBehaviourType.Config)
                return;

            var targetInProgress = missionInProgress.CollectIncompleteTargetsByType(MissionTargetType.kill_definition).FirstOrDefault(t => t.IsMyTurn);

            if (targetInProgress == null)
                return;

            var isComplete = targetInProgress.progressCount + 1 == targetInProgress.myTarget.Quantity;

            ProcessTargetAdvancementLocked(missionInProgress, targetInProgress, isComplete, data);

            if (WasProgress(data))
            {
                SendRunningMissionListAsync(character);
            }
        }
    }
}
