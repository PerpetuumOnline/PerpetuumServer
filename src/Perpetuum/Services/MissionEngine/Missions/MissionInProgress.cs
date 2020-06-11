using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Transactions;
using Perpetuum.Accounting.Characters;
using Perpetuum.Common.Loggers.Transaction;
using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Groups.Corporations;
using Perpetuum.Items;
using Perpetuum.Log;
using Perpetuum.Services.MissionEngine.MissionDataCacheObjects;
using Perpetuum.Services.MissionEngine.MissionProcessorObjects;
using Perpetuum.Services.MissionEngine.MissionStructures;
using Perpetuum.Services.MissionEngine.MissionTargets;
using Perpetuum.Services.ProductionEngine;
using Perpetuum.Services.ProductionEngine.CalibrationPrograms;
using Perpetuum.Services.Standing;
using Perpetuum.Zones;
using Perpetuum.Zones.Artifacts;
using Perpetuum.Zones.Artifacts.Repositories;

namespace Perpetuum.Services.MissionEngine.Missions
{
    /// <summary>
    /// Running instance of a mission - runs in mission engine.
    /// </summary>
    public class MissionInProgress
    {
        //current mission targets state
        private Dictionary<int, MissionTargetInProgress> _targetsInProgress;

        private List<MissionTarget> _selectedMissionTargets;
        private List<int> _selectedMineralDefinitions;
        private List<int> _selectedPlantMineralDefinitions;
        private List<int> _selectedItemDefinitions;
        private List<MissionLocation> _selectedLocations;
        private List<ArtifactInfo> _selectedArtifactInfos;

        private Position _searchOrigin;
        
        public readonly object lockObject = new object();

        public Guid missionGuid;
        public Character character;
        public DateTime started;
        public DateTime expire;
        public int currentTargetOrder;
        public bool spreadInGang;
        public double bonusMultiplier;
        public readonly Mission myMission;
        private readonly IStandingHandler _standingHandler;
        private readonly IProductionDataAccess _productionDataAccess;
        private readonly IZoneManager _zoneManager;
        public MissionLocation myLocation;
        private long _issuerCorporationEid;
        private long _issuerAllianceEid;

        private int? _selectedRace;
        private int _rewardDivider;

        public int MissionLevel { get; set; }

        public bool isTestMode;


        //cache stuff
        private Dictionary<string, object> _standingInfo;
        private Dictionary<string, object> _rewardItemsInfo;
        private Dictionary<string, object> _miscInfo;
        private Dictionary<string, object> _startItemsInfo;

        private static MissionDataCache _missionDataCache;

        public static Factory MissionInProgressFactory { get; set; }
        public static MissionProcessor MissionProcessor { get; set; }

        public static void Init(MissionDataCache missionDataCache)
        {
            _missionDataCache = missionDataCache;
        }

        public delegate MissionInProgress Factory(Mission mission);

        public MissionInProgress(Mission mission,IStandingHandler standingHandler,IProductionDataAccess productionDataAccess,IZoneManager zoneManager)
        {
            myMission = mission;
            _standingHandler = standingHandler;
            _productionDataAccess = productionDataAccess;
            _zoneManager = zoneManager;
        }

        public static MissionInProgress CreateFromRecord(IDataRecord record, Mission mission)
        {
            var missionInProgress = MissionInProgressFactory(mission);
            missionInProgress.character = Character.Get(record.GetValue<int>("characterid"));
            missionInProgress.started = record.GetValue<DateTime>("started");
            missionInProgress.expire = record.GetValue<DateTime>("expire");
            missionInProgress.missionGuid = record.GetValue<Guid>("missionguid");
            missionInProgress.currentTargetOrder = record.GetValue<int>("grouporder");
            missionInProgress.spreadInGang = record.GetValue<bool>("spreadingang");
            missionInProgress.bonusMultiplier = record.GetValue<double>("bonusmultiplier");
            missionInProgress._issuerCorporationEid = (record.GetValue<long?>("issuercorporationeid") ?? 0);
            missionInProgress._issuerAllianceEid = (record.GetValue<long?>("issuerallianceeid") ?? 0);
            missionInProgress._selectedRace = record.GetValue<int?>("selectedrace");
            missionInProgress._rewardDivider = record.GetValue<int>("rewarddivider");

            //ez mindig ki van toltve, csak fallback ami itt jon
            var locationId = record.GetValue<int?>("locationid") ?? mission.LocationId;

            if (_missionDataCache.GetLocationById(locationId, out MissionLocation location))
            {
                missionInProgress.myLocation = location;
            }
            else
            {
                Logger.Error("invalid mission location:" + locationId + " " + mission);
                throw new PerpetuumException(ErrorCodes.ConsistencyError);
            }

            int finalLevel;

            var levelFromSql = record.GetValue<int?>("missionlevel");
            if (levelFromSql == null)
            {
                //oldschool safety
                finalLevel = mission.MissionLevel;
            }
            else
            {
                //newschool dynamic
                finalLevel = (int) levelFromSql;
            }

            missionInProgress.MissionLevel = finalLevel;
            return missionInProgress;
        }

        [CanBeNull]
        private MissionTargetInProgress GetTargetInProgressByDisplayOrder(int displayOrder)
        {
            var result = _targetsInProgress.Values.FirstOrDefault(t => t.DisplayOrder == displayOrder);

            if (result == null)
            {
                //no target found on this displayOrder
                Logger.Error("target missing at displayOrder: " + displayOrder + " " + myMission);
            }

            return result;
        }


        public int MissionId
        {
            get { return myMission.id; }
        }


        public override string ToString()
        {
            return "guid:" + missionGuid + " characterID:" + character.Id + " missionID:" + MissionId + " expire:" + expire + " current targetOrder:" + currentTargetOrder + " title:" + myMission.title;
        }

        private string WhereThis
        {
            get { return $"missionguid='{missionGuid}' and characterid={character.Id} and missionid={MissionId}"; }
        }

        public Dictionary<string, object> ToDictionary()
        {
            var tempDict = new Dictionary<string, object>
            {
                {k.missionCategory, (int) myMission.missionCategory},
                {k.missionID, MissionId},
                {k.guid, missionGuid.ToString()},
                {k.characterID, character.Id},
                {k.startTime, started},
                {k.targetOrder, currentTargetOrder},
                {k.spreading, spreadInGang},
                {k.bonusMultiplier, bonusMultiplier},
                {k.expire, expire},
                {k.missionLevel, MissionLevel},
                {k.location, myLocation.id},
                {k.issuerCorporationEID, _issuerCorporationEid}, //fluff
                {k.issuerAllianceEID, _issuerAllianceEid}, //standing
                {"missionInfo", myMission.ToDictionary()},
                {"rewardDivider", _rewardDivider},
            };

            AddParticipantsToInfo(tempDict);
            InitStaticInfos();
            AddStaticInfoDictionary(tempDict);

            //target states
            var counter = 0;
            tempDict.Add(k.missionTargetState, (from t in _targetsInProgress.Values
                select (object) t.ToDictionary()).ToDictionary(d => "t" + counter++));

            return tempDict;
        }

        private void AddParticipantsToInfo(Dictionary<string, object> info)
        {
            var participants = GetParticipantsVerbose().Select(c=>c.Id).ToArray();
            info["participants"] = participants;
        }

        public void InitStaticInfos()
        {
            if (_miscInfo == null || _standingInfo == null || _rewardItemsInfo == null || _startItemsInfo == null)
            {
                _miscInfo = GetMiscInfo();
                _standingInfo = GenerateStandingChangeDictionary();
                _rewardItemsInfo = GenerateRewardsDictionary();
                _startItemsInfo = GenerateStartItemsDictionary();
            }
        }

        private Dictionary<string, object> GenerateStartItemsDictionary()
        {
            //skip this part at random missions
            if (myMission.behaviourType == MissionBehaviourType.Random) return new Dictionary<string, object>();

            var count = 0;
            var startItemsDict = new Dictionary<string, object>();
            foreach (var startItem in myMission.StartItems)
            {
                var oneEntry = startItem.ToDictionary();
                startItemsDict.Add("v" + count++, oneEntry);
            }

            return startItemsDict;
        }

        private void AddStaticInfoDictionary(Dictionary<string, object> data)
        {
            data.Add("standingChanges", _standingInfo);
            data.Add(k.reward, _rewardItemsInfo);
            data.AddMany(_miscInfo);
            data.Add(k.startItems, _startItemsInfo);
        }

        private Dictionary<string, object> GenerateStandingChangeDictionary()
        {
            var dict = new Dictionary<string, object>();
            var counter = 0;

            var extensionBonus = myMission.GetTypeRelatedExtensionBonus(character);
            var hitMitigationBonus = myMission.GetHitMitigationBonus(character);

            var mscc = new MissionStandingChangeCalculator(this, myLocation);
            var standingChanges = mscc.CollectStandingChanges(myMission).ToList();

            foreach (var msc in standingChanges)
            {
                var oneEntry = new Dictionary<string, object>()
                {
                    {k.allianceEID, msc.allianceEid},

                };

                double sValue;
                if (msc.change > 0)
                {
                    sValue = msc.change*(1.0 + extensionBonus)*bonusMultiplier;

                }
                else
                {
                    sValue = msc.change*(1.0 - hitMitigationBonus);

                }

                oneEntry[k.standingChange] = sValue;

                dict.Add("sc" + counter++, oneEntry);
            }

#if DEBUG
            Logger.Info(dict.Count + " standing info generated >>>>>>>>>>>>>>>>>>>>> " + missionGuid);
#endif
            return dict;
        }






        private Dictionary<string, object> GenerateRewardsDictionary()
        {
            var ris = new MissionRewardItemSelector(this);
            var rewardItems = ris.SelectRewards(myMission).ToList();

            var rewardCounter = 0;
            var rewDict = new Dictionary<string, object>();
            if (rewardItems.Count > 0)
            {
                rewDict = (from r in rewardItems
                    select (object) r.ToDictionary()).ToDictionary(d => "r" + rewardCounter++);
            }

            return rewDict;
        }


        private IEnumerable<MissionTargetInProgress> CollectCompletedTargets()
        {
            return _targetsInProgress.Values.Where(t => t.completed);
        }

        private MissionTargetInProgress[] UnfinishedTargetsAtCurrentOrder
        {
            get { return _targetsInProgress.Values.Where(t => t.IsMyTurn && !t.completed).ToArray(); }
        }

        public IEnumerable<MissionTargetInProgress> CollectIncompleteTargetsByType(MissionTargetType missionTargetType)
        {
            return _targetsInProgress.Values.Where(t => t.TargetType == missionTargetType && !t.completed);
        }

        private IEnumerable<MissionTargetInProgress> CollectTargetsByType(MissionTargetType missionTargetType)
        {
            return _targetsInProgress.Values.Where(t => t.TargetType == missionTargetType);
        }


        public bool GetTargetInProgress(int targetId, out MissionTargetInProgress missionTargetInProgress)
        {
            return _targetsInProgress.TryGetValue(targetId, out missionTargetInProgress);
        }

        public IEnumerable<MissionTargetInProgress> CollectTargetsWithDefinitionsToDeliverInCurrentState(MissionLocation location)
        {
            return
                CollectIncompleteTargetsByType(MissionTargetType.fetch_item)
                    .Where(
                        t => t.IsMyTurn &&
                             !t.completed &&
                             (t.myTarget.MissionStructureEid == location.LocationEid || t.myTarget.deliverAtAnyLocation)
                    );
        }

        /// <summary>
        /// The heart of mission creation
        /// 
        /// fixed from config
        /// or
        /// the random target can spawn it's targets
        /// 
        /// </summary>
        /// <param name="mission"></param>
        /// <returns></returns>
        public bool CreateAndSolveTargets(Mission mission)
        {
            //init stuff 
            _targetsInProgress = new Dictionary<int, MissionTargetInProgress>();
            _selectedMissionTargets = new List<MissionTarget>();
            _selectedMineralDefinitions = new List<int>();
            _selectedPlantMineralDefinitions = new List<int>();
            _selectedItemDefinitions = new List<int>();
            _selectedLocations = new List<MissionLocation>();
            _selectedArtifactInfos = new List<ArtifactInfo>();

            ChooseNpcRace();

            //get config stuff
            CreateTargetsInProgress(mission);

            //resolve definition and other stuff
            ResolveLinksOnAllTargets();

            //we assume that a proper displayOrder is set at this point
            InitSearchOrigin();

            if (!ResolveLocationsOnAllTargets())
            {
                //location resolve failed
                return false;
            }

            //try snapping the pop npcs to structures
            SnapPopNpcTargetsToPreviousStructures();

            //scale mission by level and gang
            ScaleTargets();

            //spawn the items according to the spawn targets
            SpawnTargets();

            //generate info for the static targets
            ResetTargetDictionaries();

            return true;
        }

        private void ChooseNpcRace()
        {
            var races = new[] {1, 2, 3};

            if (myLocation.ZoneConfig.IsAlpha) //TODO: Fixme -- Alpha as neutral-assumption
            {
                _selectedRace = races.RandomElement();
            }
            else if (FastRandom.NextDouble() > 0.25)
            {
                _selectedRace = myLocation.RaceId;
            }
            else
            {
                //25% other from other two faction
                _selectedRace = races.Where(r => r != myLocation.RaceId).RandomElement();
            }

        }


        /// <summary>
        ///
        /// Maximum gang member count -> maximum amount of npcs
        /// 
        /// </summary>
        private static int MaxmimalGangParticipants
        {
            get { return _missionDataCache.ScaleMaxGangMembers; }
        }


        public int GangMemberCountMaximized
        {
            get { return ScaleGangMemberCount.Clamp(0, MaxmimalGangParticipants); }
        }

        private int _scaleGetMembersCount = -1;

        public int ScaleGangMemberCount
        {
            get
            {
                if (!spreadInGang)
                {
                    _scaleGetMembersCount = 0;
                    return _scaleGetMembersCount;
                }

                if (_scaleGetMembersCount == -1)
                {
                    //exclude myself : --

                    _scaleGetMembersCount = (MissionProcessor.GetGangMembersCached(character).Count - 1).Clamp(0, int.MaxValue);
                }

                return _scaleGetMembersCount;
            }
        }


        public int ScaleMissionLevel
        {
            get { return (MissionLevel).Clamp(0, int.MaxValue); }
        }



        private void ScaleTargets()
        {
            var counter = 0;

            while (_targetsInProgress.Values.Any(t => !t.myTarget.IsScaled))
            {
                foreach (var missionTargetInProgress in _targetsInProgress.Values)
                {
                    if (!missionTargetInProgress.myTarget.IsScaled)
                    {
                        missionTargetInProgress.myTarget.Scale(this);
                    }
                }

                counter++;

                if (!MissionResolveTester.isTestMode)
                {
                    Logger.Info("scaled " + counter + " times.");
                }

                if (counter > 50)
                {
                    Logger.Error("possible infinite loop in scaletargets ");
                    throw new PerpetuumException(ErrorCodes.ConsistencyError);
                }

            }

        }

        private void SpawnTargets()
        {
            if (isTestMode)
                return;

            var container = myLocation.GetContainer;
            container.ReloadItems(character);

            foreach (var missionTargetInProgress in CollectTargetsByType(MissionTargetType.spawn_item).Where(t => t.myTarget.ValidItemInfo))
            {
                missionTargetInProgress.ForceComplete();

                if (isTestMode) continue;

                var spawnedItem =
                    container.CreateAndAddItem(missionTargetInProgress.myTarget.GetItemInfoFromPrimaryDefinition, false,
                        item =>
                        {
                            item.Owner = character.Eid;

                            var randomCalibrationProgram = item as RandomCalibrationProgram;
                            if (randomCalibrationProgram != null)
                            {
                                CollectComponentsForCPRG(randomCalibrationProgram);
                            }

                        });

                Logger.Info("item spawned for mission " + this + " ---> " + spawnedItem);

            }

            container.Save();

            Transaction.Current.OnCompleted(b =>
            {
                Message.Builder.SetCommand(Commands.ListContainer)
                    .WithData(container.ToDictionary())
                    .ToCharacter(character)
                    .Send();
            });
        }




        /// <summary>
        /// Create info for the freshly generated targets and cache it
        /// </summary>
        private void ResetTargetDictionaries()
        {
            foreach (var missionTargetInProgress in _targetsInProgress.Values)
            {
                missionTargetInProgress.myTarget.ResetMyDictionary();
            }

        }

        private void SnapPopNpcTargetsToPreviousStructures()
        {
            var popNpcs = CollectTargetsByType(MissionTargetType.pop_npc).Where(t => t.myTarget.isSnapToNextStructure).ToList();

            //pop npc position snapping
            foreach (var target in popNpcs)
            {
                TrySnapToPeviousMissionStructureTarget(target);
            }
        }


        private bool ResolveLocationsOnAllTargets()
        {
            var displayOrderMax = _targetsInProgress.Values.Select(t => t.DisplayOrder).DefaultIfEmpty().Max();

            for (var i = 0; i <= displayOrderMax; i++)
            {

#if DEBUG
                //check if more target has the same display order
                (_targetsInProgress.Values.Count(t => t.DisplayOrder == i) > 1).ThrowIfTrue(ErrorCodes.ConsistencyError);
#endif

                var target = GetTargetInProgressByDisplayOrder(i);

                if (target != null)
                {
                    if (!target.myTarget.ResolveLocation(this))
                    {
                        Logger.Warning("mission resolve failed: " + myMission + " " + this);
                        Logger.Warning("problem with target: " + target.myTarget + " zone:" + myLocation.ZoneConfig.Id + " x:y " + SearchOrigin.intX + " " + SearchOrigin.intY);
                        // location resolve failed,  na akkor itt j0n az okossag merugye nem tudta osszerakni a missiont
                        return false;
                    }

                    //target.MyTarget.Log("target resolved " + target.MyTarget);
                }
                else
                {
                    Logger.Error("consistency error! no target for displayOrder:" + i);
                }
            }

            return true;
        }


        private void CreateTargetsInProgress(Mission mission)
        {
            //cycle through all targets from the config and spawn the running target
            foreach (var target in mission.Targets)
            {
                var targetInProgress = target.CreateTargetInProgress(this);

                _targetsInProgress[targetInProgress.MissionTargetId] = targetInProgress;
            }
        }


        private void ResolveLinksOnAllTargets()
        {
            foreach (var missionTargetInProgress in _targetsInProgress.Values.OrderBy(t => t.DisplayOrder))
            {
                if (!missionTargetInProgress.myTarget.IsResolved)
                {
                    missionTargetInProgress.myTarget.ResolveLinks(this);
                }
            }
        }

        private void TrySnapToPeviousMissionStructureTarget(MissionTargetInProgress popNpcTargetInProgress)
        {
            for (var i = popNpcTargetInProgress.DisplayOrder - 1; i >= 0; i--)
            {
                var previousTarget = GetTargetInProgressByDisplayOrder(i);

                if (previousTarget != null)
                {
                    if (previousTarget.TargetType == MissionTargetType.submit_item || previousTarget.TargetType == MissionTargetType.use_itemsupply || previousTarget.TargetType == MissionTargetType.use_switch)
                    {
                        //yes this one is a structure
                        //snap to this
                        var snappedPosition = previousTarget.myTarget.targetPosition;
                        //yes, snap success
                        popNpcTargetInProgress.myTarget.SetPositionComponents(snappedPosition.intX, snappedPosition.intY);
                        popNpcTargetInProgress.myTarget.Log(" Snapped to " + previousTarget.TargetType + " " + previousTarget.MissionTargetId);
                        return;
                    }
                }
                else
                {
                    Logger.Error("WTF? snapping turned on, but no structure was found. " + popNpcTargetInProgress.myTarget + " " + this);
                }
            }
        }




        public bool IsMissionFinished
        {
            get
            {
                //is any branching target finished or all non-optional targets finished
                return
                    _targetsInProgress.Values
                        .Any(t => t.IsTargetBranching && t.completed)
                    ||
                    _targetsInProgress.Values
                        .Where(t => !t.myTarget.isOptional)
                        .All(t => t.completed);
            }
        }


        private bool IsAllTargetsCompletedAtCurrentOrder()
        {
#if DEBUG

            Logger.Info("targets at current targetOrder");

            foreach (var m in _targetsInProgress.Values.Where(t => t.TargetOrder == currentTargetOrder))
            {
                Logger.Info("> " + m);
            }

#endif

            //is all targets finished?
            var list = _targetsInProgress.Values.Where(t => t.TargetOrder == currentTargetOrder && !t.myTarget.isOptional).ToArray();

            var result = list.All(t => t.completed);

            Logger.Info("IsAllTargetsCompletedAtCurrentOrder result: " + result);
            return result;
        }

        private ErrorCodes AdvanceTargetOrder()
        {
            ErrorCodes ec;

            currentTargetOrder++; //next target group

            Logger.Info("+++ Group was finished. Begin WriteTargetOrder.");

            if ((ec = WriteTargetOrder()) != ErrorCodes.NoError)
            {
                Logger.Error("error occured in WriteTargetOrder " + ec);
                return ec;
            }

            Logger.Info("End WriteTargetOrder.");

            var zoneConfig = character.GetCurrentZoneConfiguration();

            var isInZone = (zoneConfig != ZoneConfiguration.None);

            if (isInZone)
            {
                Logger.Info("Begin SendZoneUpdate. characterID:" + character.Id + " zoneId:" + zoneConfig.Id + " new targetOrder:" + currentTargetOrder + " missionID:" + MissionId);

                //update the zone
                SendZoneAdvanceGroupOrder(zoneConfig, IsMissionFinished);

                Logger.Info("End SendZoneUpdate. cid" + character.Id + " isInZone:" + true + " zoneId:" + zoneConfig.Id);
            }
            else
            {
                Logger.Info("Player not on zone " + character.Id);
            }

            var infoDict = PrepareInfoDictionary();
            foreach (var missionTargetInProgress in UnfinishedTargetsAtCurrentOrder)
            {
                //Each new target in mission engine
                missionTargetInProgress.PrepareInfoDictionary(infoDict, message: missionTargetInProgress.myTarget.activatedMessage);

                missionTargetInProgress.SendTargetStatusToGangOnCommitted(Commands.MissionTargetActivated, infoDict);
            }

            return ec;
        }

        public Dictionary<string, object> PrepareInfoDictionary(string message = null, int sourceCharacterId = 0)
        {
            var info = new Dictionary<string, object>();

            if (message != null)
            {
                info[k.message] = message;
            }

            if (sourceCharacterId != 0)
            {
                //assisting info
                info[k.sourceAgent] = sourceCharacterId;
            }

            var missionInProgressInfo = ToDictionary();
            info.Add("missionStatus", missionInProgressInfo);

            return info;
        }




        public ErrorCodes TryAdvanceTargetGroup(ref bool groupFinished)
        {
            Logger.Info("Begin advancing targetGroup. characterID:" + character.Id + " missionID:" + MissionId + " pre groupOrder:" + currentTargetOrder);

            try
            {
                var ec = ErrorCodes.NoError;

                if (IsAllTargetsCompletedAtCurrentOrder())
                {
                    groupFinished = true;

                    if ((ec = AdvanceTargetOrder()) != ErrorCodes.NoError)
                    {
                        Logger.Error("error in AdvanceTargetOrder " + ec + " " + this);
                        return ec;
                    }

                    return ec; //exit with group finished OK
                }

                Logger.Info("Group not finished yet.");

                // 8.(

                groupFinished = false;
                return ec;
            }
            finally
            {
                Logger.Info("End advancing targetGroup. characterID:" + character.Id + " missionID:" + MissionId + " pre groupOrder:" + currentTargetOrder + " groupFinished: " + groupFinished);
            }
        }

        private void SendZoneAdvanceGroupOrder(ZoneConfiguration zoneConfiguration, bool isFinished)
        {
            var zone = _zoneManager.GetZone(zoneConfiguration.Id);
            if (zone == null)
                return;

            Logger.Info("sending mission update to zone:" + zoneConfiguration.Id + " isFinished:" + isFinished);

            Transaction.Current.OnCommited(() =>
            {
                var data = new MissionProgressUpdate
                {
                    character = character,
                    targetOrder = currentTargetOrder,
                    isFinished = isFinished,
                    missionId = MissionId,
                    missionGuid = missionGuid,
                    missionLevel = MissionLevel,
                    locationId = myLocation.id,
                    selectedRace = _selectedRace ?? 1,
                    spreadInGang = spreadInGang,
                };

                var player = zone.GetPlayer(character);
                player?.MissionHandler.MissionAdvanceGroupOrder(data);
                Logger.Info("OnCommitted - sending zoneMissionAdvanceTargetOrder to zone:" + zoneConfiguration.Id + " characterID:" + character.Id + " groupOrder:" + currentTargetOrder + " finished:" + isFinished + " missionID:" + MissionId);
            });
        }

        public void SendZoneNewMission()
        {
            var zone = character.GetCurrentZone();
            if (zone != null)
            {
                Transaction.Current.OnCommited(() =>
                {
                    {
                        var updateData = new MissionProgressUpdate
                        {
                            character = character,
                            missionId = MissionId,
                            missionGuid = missionGuid,
                            targetOrder = 0,
                            missionLevel = MissionLevel,
                            locationId = myLocation.id,
                            selectedRace = _selectedRace ?? 1,
                            spreadInGang = spreadInGang
                        };

                        var player = zone.GetPlayer(character);
                        player?.MissionHandler.MissionNew(updateData);
                    }
                });

                Logger.Info("Sending zoneMissionNew zone:" + zone.Id + " characterID:" + character.Id + " missionID:" + MissionId);
            }

            //put the toast up in the air!
            var data = ToDictionary();
            var affected = GetAffectedCharacters();
            Message.Builder.SetCommand(Commands.ZoneMissionNew).WithData(data).ToCharacters(affected).Send();
        }



        /// <summary>
        /// Granulated version to obtain the gang members
        /// </summary>
        /// <returns></returns>
        public List<Character> GetAffectedCharacters()
        {
            if (!spreadInGang)
            {
                return new List<Character> {character};
            }

            return MissionProcessor.GetGangMembersCached(character);
        }



        public ErrorCodes InsertMissionLog()
        {
            var res = Db.Query().CommandText("insert missionlog (missionid,characterid,expire,missionguid,spreadingang,bonusmultiplier,locationid,missionlevel,issuerallianceeid,issuercorporationeid,selectedrace,rewarddivider) values (@missionID,@characterID,@expire,@missionGuid,@spreadInGang,@bonusmultiplier,@locationid,@missionlevel,@issuerAlliance,@issuerCorporation,@selectedrace,@rewardDivider)")
                .SetParameter("@characterID", character.Id)
                .SetParameter("@missionID", MissionId)
                .SetParameter("@expire", expire)
                .SetParameter("@missionGuid", missionGuid)
                .SetParameter("@spreadInGang", spreadInGang)
                .SetParameter("@bonusmultiplier", bonusMultiplier)
                .SetParameter("@locationid", myLocation.id)
                .SetParameter("@missionlevel", MissionLevel)
                .SetParameter("@issuerCorporation", _issuerCorporationEid)
                .SetParameter("@issuerAlliance", _issuerAllianceEid)
                .SetParameter("@selectedRace", _selectedRace)
                .SetParameter("@rewardDivider", _rewardDivider)
                .ExecuteNonQuery();

            return (res != 1) ? ErrorCodes.SQLInsertError : ErrorCodes.NoError;
        }


        public ErrorCodes WriteAllTargetsToSql()
        {
            var ec = ErrorCodes.NoError;
            foreach (var targetInProgress in _targetsInProgress.Values)
            {
                if ((ec = targetInProgress.WriteMissionTargetToSql()) != ErrorCodes.NoError)
                {
                    return ec;
                }
            }
            return ec;
        }


        /// <summary>
        /// 
        /// First it gets the mission target from the cache by id
        /// then it modifies it with a running target's record - made mission creation time
        /// and thus making it unique, 
        /// no more read from cache is valid, all code must use the running version of the target!
        /// 
        /// </summary>
        /// <returns></returns>
        public ErrorCodes ReadTargetsInProgressSql()
        {
            var records = Db.Query().CommandText("select * from missiontargetsarchive where " + WhereThis)
                .Execute().ToArray();

            _targetsInProgress = new Dictionary<int, MissionTargetInProgress>(records.Length);

            foreach (var record in records)
            {
                var targetId = record.GetValue<int>("targetid");

                MissionTarget target;
                if (!_missionDataCache.GetTargetById(targetId, out target))
                {
                    Logger.Warning("targetid invalid:" + targetId + " " + this);
                    continue;
                }

                var currentTarget = target.GetClone();

                //set parameters from running version of this target
                currentTarget.ModifyWithRecord(record);

                //make a progress wrapper
                var targetInProgress = currentTarget.CreateTargetInProgress(this);

                //set the progress in the running wrapper
                targetInProgress.progressCount = record.GetValue<int>("progresscount");
                targetInProgress.completed = record.GetValue<bool>("completed");

                //set success info
                targetInProgress.ReadSuccessInfo(record);

                _targetsInProgress.Add(targetInProgress.MissionTargetId, targetInProgress);
            }

            return ErrorCodes.NoError;
        }


        public ErrorCodes SetSuccessToMissionLog(bool success)
        {
            var res =
                Db.Query().CommandText("update missionlog set succeeded=@success, finished=@now where " + WhereThis)
                    .SetParameter("@success", success)
                    .SetParameter("@now", DateTime.Now)
                    .ExecuteNonQuery();

            return (res != 1) ? ErrorCodes.SQLUpdateError : ErrorCodes.NoError;
        }

        /// <summary>
        /// Saves the progress of the mission
        /// 
        /// %%% change column name to target order
        /// </summary>
        /// <returns></returns>
        private ErrorCodes WriteTargetOrder()
        {
            var res = Db.Query().CommandText("update missionlog set grouporder=@go where " + WhereThis)
                .SetParameter("@go", currentTargetOrder)
                .ExecuteNonQuery();

            return (res != 1) ? ErrorCodes.SQLUpdateError : ErrorCodes.NoError;
        }

        public void CleanUpAllTargets()
        {
            Db.Query().CommandText("delete missiontargetsarchive where " + WhereThis)
                .ExecuteNonQuery();
        }




        private void OnMissionFailure(Command command, MissionProcessor missionProcessor, ErrorCodes errorToInfo = ErrorCodes.NoError)
        {
            WriteFailure();

            var data = new Dictionary<string, object>
            {
                {k.mission, ToDictionary()},
                {k.message, myMission.failMessage}
            };

            //if (!character.AccessLevel.IsAdminOrGm())
            missionProcessor.MissionAdministrator.DecreaseBonus(character, myMission.missionCategory, MissionLevel, myLocation.Agent);

            DeleteParticipants();

            Transaction.Current.OnCommited(() =>
            {
                //remove from ram
                missionProcessor.MissionAdministrator.RemoveMissionInProgress(this);

                character.ReloadContainerOnZoneAsync();
                missionProcessor.GetOptionsByRequest(character, myLocation);
                missionProcessor.SendRunningMissionList(character);

                var gangMembers = GetAffectedCharacters();


                if (errorToInfo != ErrorCodes.NoError)
                {
                    foreach (var gangMember in gangMembers)
                    {
                        gangMember.CreateErrorMessage(Commands.MissionError, errorToInfo).Send();
                    }

                }

                Message.Builder.SetCommand(command).WithData(data).ToCharacters(gangMembers).Send();
            });


        }



        public void OnMissionExpired(MissionProcessor missionProcessor)
        {
            Logger.Info("mission expired. " + this);

            OnMissionFailure(Commands.MissionExpired, missionProcessor, ErrorCodes.MissionExpired);
        }

        public void OnMissionAbort(MissionProcessor missionProcessor, ErrorCodes errorToInfo)
        {
            Logger.Info("mission aborted. " + this);

            OnMissionFailure(Commands.MissionAbort, missionProcessor, errorToInfo);

        }


        private void WriteFailure()
        {
            //write fail to log
            SetSuccessToMissionLog(false).ThrowIfError();

            // delete targets from sql
            CleanUpAllTargets();
            ArtifactRepository.DeleteArtifactsByMissionGuid(missionGuid);

            var config = character.GetCurrentZoneConfiguration();
            //update the zone
            SendZoneAdvanceGroupOrder(config, true);

            if (MissionLevel < 0)
                return;

            //extension bonus to decrease penalty
            var hitMitigationMultiplier = (1 - myMission.GetHitMitigationBonus(character)).Clamp();

            var mscc = new MissionStandingChangeCalculator(this, myLocation);
            var changes = mscc.CollectStandingChanges(myMission);

            foreach (var missionStandingChange in changes.Where(s => s.change > 0))
            {
                var standingChange = -0.2*missionStandingChange.change*hitMitigationMultiplier;
                var origStanding = _standingHandler.GetStanding(missionStandingChange.allianceEid, character.Eid);
                var newStanding = (origStanding + standingChange).Clamp(0, 10);

                _standingHandler.SetStanding(missionStandingChange.allianceEid, character.Eid, newStanding);
                var logEntry = new StandingLogEntry
                {
                    characterID = character.Id,
                    allianceEID = missionStandingChange.allianceEid,
                    actual = newStanding,
                    change = standingChange,
                    missionID = MissionId
                };
                _standingHandler.WriteStandingLog(logEntry);
            }

            Transaction.Current.OnCommited(() => { _standingHandler.SendStandingToDefaultCorps(character); });
        }



        private void InitSearchOrigin()
        {
            //mission location, technically the location of the field terminal
            _searchOrigin = myLocation.MyPosition;
        }

        public Position SearchOrigin
        {
            get { return _searchOrigin; }
            set
            {
                {
                    var freshOrigin = value;

                    if (!MissionResolveTester.isTestMode)
                        Logger.Info("new search origin arrived " + Math.Round(freshOrigin.TotalDistance2D(_searchOrigin), 1) + " tiles from current " + freshOrigin);

                    _searchOrigin = freshOrigin;
                }
            }
        }



        public List<MissionLocation> SelectedLocations
        {
            get { return _selectedLocations; }
        }

        public List<int> SelectedMinerals
        {
            get { return _selectedMineralDefinitions; }
        }

        public List<MissionTarget> SelectedTargets
        {
            get { return _selectedMissionTargets; }
        }

        public List<int> SelectedPlantMinerals
        {
            get { return _selectedPlantMineralDefinitions; }
        }

        public List<int> SelectedItemDefinitions
        {
            get { return _selectedItemDefinitions; }
        }

        public List<ArtifactInfo> SelectedArtifactInfos
        {
            get { return _selectedArtifactInfos; }
        }

        public void AddToSelectedArtifacts(ArtifactInfo artifactInfo)
        {
            _selectedArtifactInfos.Add(artifactInfo);
        }

        public void AddToSelectedMinerals(int mineraldefinition)
        {
            _selectedMineralDefinitions.Add(mineraldefinition);
        }

        public void AddToSelectedTargets(MissionTarget missionTarget)
        {
            _selectedMissionTargets.Add(missionTarget);
        }

        public void AddToSelectedPlantMinerals(int plantMineralDefinition)
        {
            _selectedPlantMineralDefinitions.Add(plantMineralDefinition);
        }

        public void AddToSelectedItems(int choosenDefinition)
        {
            _selectedItemDefinitions.Add(choosenDefinition);
        }

        public void AddToSelectedLocations(MissionLocation location)
        {
            _selectedLocations.Add(location);
        }


        /// <summary>
        /// Old and new mission system standing change
        /// </summary>
        public void IncreaseStanding(double rewardFraction, List<Character> participants, List<Character> onlineGangMembers, Dictionary<string, object> successData)
        {
            var mscc = new MissionStandingChangeCalculator(this, myLocation);
            var standingChanges = mscc.CollectStandingChanges(myMission).ToList();

            if (standingChanges.Count == 0)
            {
                Logger.Error("no standing changes collected. " + this);
                return;
            }

            var count = 0;
            var allStandingInfo = new Dictionary<string, object>();

            var netStandings = new Dictionary<string, object>();
            foreach (var missionStandingChange in standingChanges)
            {
                netStandings.Add("n" + count++, missionStandingChange.ToDictionary());
            }
            allStandingInfo.Add("grossStandings", netStandings);
            Logger.Info("++ rewardDivider:" + Math.Round(1/rewardFraction) +  " rewardFraction: " + rewardFraction + " participants:" + participants.Count + "  " + this);
            
            var infoByCharacters = new Dictionary<string, object>(participants.Count);

            foreach (var currentCharacter in participants)
            {
                var extensionBonus = myMission.GetTypeRelatedExtensionBonus(currentCharacter);
                var hitMitigationBonus = myMission.GetHitMitigationBonus(currentCharacter);

                var oneCharacterInfo = new Dictionary<string, object>
                {
                    {k.characterID, currentCharacter.Id},
                    {k.extensionMultiplier, 1 + extensionBonus}
                };
#if DEBUG
                Logger.Info("characterid is: " + currentCharacter + " extensionbonus for missions related to standing: " + extensionBonus);
#endif

                var changesPerCharacter = new Dictionary<string, object>(standingChanges.Count);

                foreach (var standingChange in standingChanges)
                {
                    var originalStanding = _standingHandler.GetStanding(standingChange.allianceEid, currentCharacter.Eid);

                    var oneChange = new Dictionary<string, object> {{k.allianceEID, standingChange.allianceEid}, {"fromStanding", originalStanding}};

#if DEBUG
                    Logger.Info("original standing: " + originalStanding);
#endif

                    if (standingChange.change >= 0)
                    {
                        var levelToMatch = MissionLevel + 1;
                        if (MissionLevel == -1)
                        {
                            levelToMatch = 1;
                        }

                        if (originalStanding >= levelToMatch)
                        {
                            //hard limit
#if DEBUG
                            Logger.Info("no standing increase for characterID:" + currentCharacter.Id + " standing is above missionlevel+1. standing:" + originalStanding + " lvlThreshold:" + MissionLevel + 1);
#endif
                            oneChange.Add("toStanding", originalStanding); //marad a regi
                            oneChange.Add("limitReached", 1); //jelezzuk h limit miatt maradt a regi
                            oneChange.Add("netChange", 0.0);
                            changesPerCharacter.Add("c" + count++, oneChange);

                            continue;
                        }
                    }


                    double standingChangeValue;

                    if (standingChange.change > 0.0)
                    {
                        standingChangeValue = standingChange.change * (1 + extensionBonus) * rewardFraction * bonusMultiplier;
                    }
                    else
                    {
                        standingChangeValue = standingChange.change * (1 - hitMitigationBonus) * rewardFraction;
                    }

                    oneChange.Add("netChange", standingChangeValue);

                    var newStanding = (originalStanding + standingChangeValue).Clamp(0, 10);

                    oneChange.Add("toStanding", newStanding);
                    changesPerCharacter.Add("c" + count++, oneChange);

#if(DEBUG)
                    Logger.Info("standing change value: " + standingChangeValue + "  extb:" + extensionBonus + " rewardFraction:" + rewardFraction + " new standing:" + newStanding);
#endif
                    _standingHandler.SetStanding(standingChange.allianceEid, currentCharacter.Eid, newStanding);
                    var logEntry = new StandingLogEntry
                    {
                        characterID = currentCharacter.Id,
                        allianceEID = standingChange.allianceEid,
                        actual = newStanding,
                        change = standingChangeValue,
                        missionID = MissionId
                    };
                    _standingHandler.WriteStandingLog(logEntry);
                }

                oneCharacterInfo.Add("changes", changesPerCharacter);
                infoByCharacters.Add("i" + count++, oneCharacterInfo);

                var character1 = currentCharacter; //closure workaround
                Transaction.Current.OnCommited(() => _standingHandler.SendStandingToDefaultAlliances(character1));
            }

            if (infoByCharacters.Count > 0)
                allStandingInfo.Add("byCharacters", infoByCharacters);

            successData.Add("standingInfo", allStandingInfo);
        }

        //completely runtime
        public MissionLocation deliveryWorkLocation;


        public IList<Item> SpawnRewardItems(out MissionLocation locationUsed)
        {
            locationUsed = null;
            Logger.Info("++ Spawning reward items for characterId:" + character.Id);

            var ris = new MissionRewardItemSelector(this);
            var rewardItems = ris.SelectRewards(myMission).ToList();

            var spawnedItems = new List<Item>();

            if (rewardItems.Count > 0)
            {
                if (deliveryWorkLocation == null)
                {
                    deliveryWorkLocation = myLocation;
                }

                locationUsed = deliveryWorkLocation;
                Logger.Info("++ Spawning reward using location: " + deliveryWorkLocation + "  --  " + this);

                var publicContainer = deliveryWorkLocation.GetContainer;

                foreach (var reward in rewardItems)
                {
                    if (reward.Probability > 0)
                    {
                        //decide random
                        var chance = FastRandom.NextInt(100);

                        if (chance > reward.Probability)
                        {
                            continue;
                        }
                    }

                    //or spawn

                    //If item is tokens, split amoung participants
                    if (myLocation.GetRaceSpecificCoinDefinition() == reward.ItemInfo.Definition)
                    {
                        var participants = GetParticipants();
                        if (participants.Count > 0 && myLocation.GetRaceSpecificCoinDefinition() == reward.ItemInfo.Definition)
                        {
                            var tokenSplitQuantity = (int)Math.Ceiling((double)reward.ItemInfo.Quantity / (double)participants.Count);
                            var splitTokens = new MissionReward(new ItemInfo(reward.ItemInfo.Definition, tokenSplitQuantity));
                            Item tokenItem = null;
                            foreach (var participant in participants)
                            {
                                var rewardItem = publicContainer.CreateAndAddItem(splitTokens.ItemInfo, false, item => { item.Owner = participant.Eid; });

                                var b = TransactionLogEvent.Builder().SetTransactionType(TransactionType.MissionRewardTake)
                                    .SetCharacter(character)
                                    .SetItem(rewardItem)
                                    .SetContainer(publicContainer);
                                character.LogTransaction(b);
                                tokenItem = rewardItem;
                            }
                            if (tokenItem != null)
                            {
                                spawnedItems.Add(tokenItem);
                            }
                        }
                    }
                    else
                    {
                        var rewardItem = publicContainer.CreateAndAddItem(reward.ItemInfo, false, item => { item.Owner = character.Eid; });

                        var b = TransactionLogEvent.Builder().SetTransactionType(TransactionType.MissionRewardTake)
                            .SetCharacter(character)
                            .SetItem(rewardItem)
                            .SetContainer(publicContainer);
                        character.LogTransaction(b);

                        spawnedItems.Add(rewardItem);
                    }
                }
            }

            Logger.Info(spawnedItems.Count + " reward items were created for character: " + character.Id + " owner:" + character.Eid);
            return spawnedItems;
        }

        public void GetFinalReward(out double rewardSum, out double distanceReward, out double difficultyReward, out double rewardByTargets, out double riskCompensation, out double zoneFactor)
        {
            var rfc = new MissionRewardCalculator(this, false);
            rfc.CalculateAllRewards(myMission, out rewardSum, out distanceReward, out difficultyReward, out rewardByTargets, out riskCompensation, out zoneFactor);

        }

        private double GetEstimatedReward()
        {
            var rfc = new MissionRewardCalculator(this, true);
            var reward = rfc.CalculateReward(myMission);
            return reward;
        }

        private Dictionary<string, object> GetMiscInfo()
        {
            var extensionBonus = myMission.GetTypeRelatedExtensionBonus(character) + 1;

            var allPositiveBonus = bonusMultiplier*extensionBonus;

            var rawReward = GetEstimatedReward();
            var fineReward = extensionBonus*bonusMultiplier*rawReward;
            var hitMitigationBonus = myMission.GetHitMitigationBonus(character);

            var result = new Dictionary<string, object>()
            {
                {k.extensionMultiplier, extensionBonus},
                {"priceExpected", fineReward},
                {"positiveExtensionPercent", (int) (allPositiveBonus*100)},
                {"negativeExtensionPercent", (int) ((1 - hitMitigationBonus)*100)}
            };

            if (myMission.behaviourType == MissionBehaviourType.Random)
            {
                //only when meaningful
                if (_targetsInProgress.Values.Any(t => t.TargetType == MissionTargetType.pop_npc || t.myTarget.FindArtifactSpawnsNpcs))
                {
                    result.Add(k.raceID, _selectedRace);
                }
            }

            if (myMission.missionCategory == MissionCategory.Transport)
            {
                var sumVolume = _targetsInProgress.Values.Where(t => t.myTarget.ValidItemInfo && (t.myTarget.Type == MissionTargetType.use_itemsupply)).Sum(t => t.myTarget.Quantity*t.myTarget.PrimaryEntityDefault.Volume);
                result.Add("transportVolume", sumVolume);

            }


            return result;

        }

        public double GetDifficultyReward
        {
            get
            {
                var level = MissionLevel + 1;
                return level*myMission.DifficultyReward;

            }
        }

        public double GetParticipantBonusModifier()
        {
            return ComputeParticipantBonusMultiplier(GetParticipants().Count);
        }

        private double ComputeParticipantBonusMultiplier(int paricipantCount)
        {
            //Solo or squad of 1 and initial estimate
            if (paricipantCount < 2)
            {
                return 1.0;
            }
            //Modify total reward by participants
            //Clamp participant count to [1,MaxmimalGangParticipants]
            double participantBonus = 0.05;  //TODO expose parameter in DB
            int participantCount = Math.Min(MaxmimalGangParticipants, Math.Max(1, paricipantCount));
            double participantModifier = 1 + participantCount * participantBonus;
            return  participantModifier;
        }

        /// <summary>
        /// Both old and new tech handled
        /// </summary>
        public void PayOutMission(double rewardFraction, List<Character> participants, List<Character> onlineGangMembers, Dictionary<string, object> successData)
        {
            var paymentData = new Dictionary<string, object>();
            
            double rewardSum;
            double distanceReward;
            double difficultyReward;
            double rewardByTargets;
            double riskCompensation;
            double zoneFactor;
            GetFinalReward(out rewardSum, out distanceReward, out difficultyReward, out rewardByTargets, out riskCompensation, out zoneFactor);

            rewardSum *= GetParticipantBonusModifier();

            paymentData.Add("totalReward", Math.Round(rewardSum));
            paymentData.Add("distanceReward", distanceReward);
            paymentData.Add("difficultyReward", difficultyReward);
            paymentData.Add("rewardByTargets", rewardByTargets);
            paymentData.Add("riskCompensation", riskCompensation > 0 ? 1 : 0);
            paymentData.Add("zoneFactor", zoneFactor);


            Logger.Info("-- mission payout starts. reward:" + rewardSum + " rewardFraction:" + rewardFraction + " participants:" + participants.Count + " bonusMult:" + bonusMultiplier + " for " + this);


            if (rewardSum <= 0)
                return;

            var paymentsPerCharacter = new Dictionary<string, object>(participants.Count);
            var payoutLogEntries = new List<MissionPayOutLogEntry>();

            var count = 0;
            foreach (var currentCharacter in participants)
            {
                var extBonus = currentCharacter.GetExtensionBonusByName(ExtensionNames.MISSION_BONUS_LEVEL_MOD) + 1;
                var typeRelatedBonus = myMission.GetTypeRelatedExtensionBonus(currentCharacter) + 1;
                var oneCharacterPayOut = new Dictionary<string, object>
                {
                    {k.characterID, currentCharacter.Id},
                    {k.extensionMultiplier, typeRelatedBonus}
                };

                var realRewardFeePerCharacter = rewardSum*rewardFraction*typeRelatedBonus*bonusMultiplier;
                if (realRewardFeePerCharacter < 1)
                    continue;

                var feeWithExtension = Math.Round(rewardSum*rewardFraction*typeRelatedBonus*extBonus);
                var yourShare = Math.Round(rewardSum*rewardFraction);

                oneCharacterPayOut.Add("grossForCharacter", realRewardFeePerCharacter);
                oneCharacterPayOut.Add("feeWithExtensions", feeWithExtension);
                oneCharacterPayOut.Add("yourShare", yourShare);
                oneCharacterPayOut.Add("allExtensionsBonus", (int) Math.Ceiling(extBonus*typeRelatedBonus*100.0));

                var corporation = currentCharacter.GetCorporation();

                var taxRatio = 0.0;

                if (corporation is PrivateCorporation)
                {
                    //the corp is private
                    taxRatio = corporation.TaxRate/100.0;

                    if (taxRatio > 0)
                    {
                        //if corptax set
                        var corporationWallet = new CorporationWallet(corporation);
                        var amountToCorp = Math.Round(taxRatio*realRewardFeePerCharacter);
                        corporationWallet.Balance += amountToCorp;

                        oneCharacterPayOut.Add(k.tax, corporation.TaxRate);
                        oneCharacterPayOut.Add("corporationFee", amountToCorp);

                        Logger.Info("reward transferred to corp " + corporation.CorporationName + " tax:" + corporation.TaxRate + " amount:" + amountToCorp);

                        corporation.LogTransaction(TransactionLogEvent.Builder()
                            .SetCorporation(corporation)
                            .SetTransactionType(TransactionType.MissionTax)
                            .SetCreditBalance(corporationWallet.Balance)
                            .SetCreditChange(amountToCorp)
                            .SetCharacter(currentCharacter));

                        payoutLogEntries.Add(new MissionPayOutLogEntry(missionGuid, MissionId, myMission.missionCategory, MissionLevel, corporation.Eid, null, onlineGangMembers.Count, amountToCorp, rewardSum));
                    }
                }

                double amountToCharacter;
                //if the complete reward is paid to the corporation
                if (Math.Abs(taxRatio - 1.0) < double.Epsilon)
                    amountToCharacter = 0d;
                else
                    amountToCharacter = Math.Round((1 - taxRatio)*realRewardFeePerCharacter);

                oneCharacterPayOut.Add(k.fee, amountToCharacter);
                paymentsPerCharacter.Add("p" + count++, oneCharacterPayOut);

                Logger.Info("reward transferred to char " + currentCharacter.Nick + " amount:" + amountToCharacter);

                currentCharacter.AddToWallet(TransactionType.missionPayOut, amountToCharacter);
                payoutLogEntries.Add(new MissionPayOutLogEntry(missionGuid, MissionId, myMission.missionCategory, MissionLevel, null, currentCharacter.Id, onlineGangMembers.Count, amountToCharacter, rewardSum));
            }

            paymentData.Add("paidCharacters", paymentsPerCharacter);
            successData.Add("paymentInfo", paymentData);

            MissionPayOutLogEntry.SaveLog(payoutLogEntries);

            Logger.Info("-- mission payout finished.   " + this);

        }


        /// <summary>
        /// Collects the components from the loaded targets
        /// Note: this will result raw_minerals, random_items
        /// </summary>
        /// <param name="randomCalibrationProgram"></param>
        public void CollectComponentsForCPRG(RandomCalibrationProgram randomCalibrationProgram)
        {
            Logger.Info("start collecting components for random cprg. " + randomCalibrationProgram);

            var researchTarget = CollectTargetsByType(MissionTargetType.research).FirstOrDefault();
            var massproduceTarget = CollectTargetsByType(MissionTargetType.massproduce).FirstOrDefault();

            if (researchTarget == null && massproduceTarget == null)
            {
                Logger.Error("no proper mission target was found for " + randomCalibrationProgram + " " + this);
                throw new PerpetuumException(ErrorCodes.ConsistencyError);
            }

            var possibleComponents = CollectPossibleComponents();

            Logger.Info("possible components: " + possibleComponents.Count);
            DisplayComponents(possibleComponents);

            var itemsToFetch = CollectFetchSummary();

            Logger.Info("items to fetch: " + itemsToFetch.Count);

            SubtractFetchItemsFromComponents(possibleComponents, itemsToFetch);

            Logger.Info("except fetch items: " + possibleComponents.Count);
            DisplayComponents(possibleComponents);

            possibleComponents = FilterUnwantedComponents(possibleComponents);

            Logger.Info("except unwanted items: " + possibleComponents.Count);

            var productionComponents = new List<ProductionComponent>();

            foreach (var possibleItem in possibleComponents.Values)
            {
                var ed = EntityDefault.Get(possibleItem.Definition);
                var quantity = possibleItem.Quantity;

                //only include minerals or other missionitems, component items

                var pc = new ProductionComponent(ed, quantity);

                Logger.Info("component added to cprg. component:" + ed.Name + " q:" + quantity);

                productionComponents.Add(pc);

            }


            if (researchTarget != null)
            {
                var cprgDefinition = researchTarget.myTarget.Definition;

                //deal with calibration program
                if (cprgDefinition == randomCalibrationProgram.Definition)
                {
                    if (!researchTarget.myTarget.ValidSecondaryDefinitionSet)
                    {
                        Logger.Error("no valid secondary definition is set on research target. ct creation fails. " + researchTarget);
                        throw new PerpetuumException(ErrorCodes.ConsistencyError);
                    }

                    //this is going to be the item the cprg will produce
                    randomCalibrationProgram.SetTargetDefinition(researchTarget.myTarget.SecondaryDefinition);
                    randomCalibrationProgram.TargetQuantity = MissionLevel + 1;
                }

            }

            if (massproduceTarget != null)
            {
                if (!massproduceTarget.myTarget.ValidItemInfo)
                {
                    Logger.Error("no valid iteminfo was set in " + massproduceTarget);
                    throw new PerpetuumException(ErrorCodes.ConsistencyError);
                }

                if (randomCalibrationProgram.OriginalTargetDefinition == massproduceTarget.myTarget.Definition)
                {
                    randomCalibrationProgram.TargetQuantity = massproduceTarget.myTarget.Quantity;
                }
                else
                {
                    Logger.Error("CPG mismatch in " + massproduceTarget + "   CPRG  " + randomCalibrationProgram);
                    throw new PerpetuumException(ErrorCodes.ConsistencyError);
                }
            }

            if (productionComponents.Count == 0)
            {
                Logger.Info("no component was found in mision. fallback will happen " + randomCalibrationProgram);
                return;
            }

            //and finally set the components, have fun at massproduce
            randomCalibrationProgram.SetComponents(productionComponents);

        }

        private Dictionary<int, ItemInfo> FilterUnwantedComponents(Dictionary<int, ItemInfo> possibleComponents)
        {
            var result = new Dictionary<int, ItemInfo>();

            foreach (var possibleComponent in possibleComponents.Values)
            {
                var ed = EntityDefault.Get(possibleComponent.Definition);

                if (ed.CategoryFlags.IsCategory(CategoryFlags.cf_random_calibration_programs) ||
                    ed.CategoryFlags.IsCategory(CategoryFlags.cf_random_research_kits))
                {
                    //cprg: just dont include, it cannot be a component
                    //research kit: only could exists for this research, cannot be a component

                    continue;

                }

                result.Add(possibleComponent.Definition, possibleComponent);
            }

            return result;
        }

        /// <summary>
        /// These targets can spawn items that may be used as production component
        /// </summary>
        /// <returns></returns>
        private Dictionary<int, ItemInfo> CollectPossibleComponents()
        {
            var possibleComponents = new Dictionary<int, ItemInfo>();

            foreach (var missionTargetInProgress in _targetsInProgress.Values.Where(t => t.myTarget.ValidItemInfo))
            {
                if (missionTargetInProgress.TargetType == MissionTargetType.drill_mineral ||
                    missionTargetInProgress.TargetType == MissionTargetType.use_itemsupply ||
                    missionTargetInProgress.TargetType == MissionTargetType.spawn_item)
                {
                    var possibleComponent = missionTargetInProgress.myTarget.GetItemInfoFromPrimaryDefinition;

                    if (possibleComponent.EntityDefault.CategoryFlags.IsCategory(CategoryFlags.cf_component_items) ||
                        possibleComponent.EntityDefault.CategoryFlags.IsCategory(CategoryFlags.cf_raw_material))
                    {

                        ItemInfo existingInfo;
                        if (possibleComponents.TryGetValue(possibleComponent.Definition, out existingInfo))
                        {
                            existingInfo.Quantity += possibleComponent.Quantity;
                            possibleComponents[existingInfo.Definition] = existingInfo;
                        }
                        else
                        {
                            possibleComponents.Add(possibleComponent.Definition, possibleComponent);
                        }
                    }
                }

            }

            return possibleComponents;

        }


        /// <summary>
        /// Collect all items that has to be delivered in this mission
        /// </summary>
        /// <returns></returns>
        private Dictionary<int, ItemInfo> CollectFetchSummary()
        {
            var targetWithItems = _targetsInProgress.Values.Where(t => (t.TargetType == MissionTargetType.fetch_item || t.TargetType == MissionTargetType.submit_item) && t.myTarget.ValidItemInfo);

            var itemsToFetch = new Dictionary<int, ItemInfo>();
            foreach (var fetchTarget in targetWithItems)
            {
                var fetchItemInfo = fetchTarget.myTarget.GetItemInfoFromPrimaryDefinition;

                ItemInfo existingInfo;
                if (itemsToFetch.TryGetValue(fetchItemInfo.Definition, out existingInfo))
                {
                    existingInfo.Quantity += fetchItemInfo.Quantity;
                    itemsToFetch[existingInfo.Definition] = existingInfo;
                }
                else
                {
                    itemsToFetch.Add(fetchItemInfo.Definition, fetchItemInfo);
                }

            }

            return itemsToFetch;
        }


        private void SubtractFetchItemsFromComponents(Dictionary<int, ItemInfo> possibleComponents, Dictionary<int, ItemInfo> fetchItemInfos)
        {
            foreach (var fetchItemInfo in fetchItemInfos.Values)
            {
                ItemInfo component;
                if (possibleComponents.TryGetValue(fetchItemInfo.Definition, out component))
                {
                    //this has to be delivered, cannot be used as component
                    component.Quantity -= fetchItemInfo.Quantity;

                    if (component.Quantity < 0)
                    {
                        Logger.Error("fetch is asking too much " + fetchItemInfo + " " + this);
                        throw new PerpetuumException(ErrorCodes.ConsistencyError);
                    }

                    if (component.Quantity == 0)
                    {
                        possibleComponents.Remove(component.Definition);
                        continue;
                    }

                    possibleComponents[component.Definition] = component;

                }
            }
        }

        /// <summary>
        /// Collects all item definitions, and translates them into their CPRG pair for later operations.
        /// Used to make the cprg vs item definitions unique. 
        /// Technically when an item is choosen the pair CPRG is choosen.
        /// </summary>
        /// <returns></returns>
        public List<int> CollectCPRGDefinitionsFromItems()
        {
            var possibleCPRGDefinitions = new List<int>();

            foreach (var target in _targetsInProgress.Values)
            {
                if (target.myTarget.ValidDefinitionSet)
                {
                    var definitionFromTarget = target.myTarget.Definition;
                    var entityDefaultFromTarget = EntityDefault.Get(definitionFromTarget);

                    if (entityDefaultFromTarget.CategoryFlags.IsCategory(CategoryFlags.cf_generic_random_items))
                    {
                        if (_productionDataAccess.ResearchLevels.TryGetValue(definitionFromTarget, out ItemResearchLevel researchLevel))
                        {
                            if (researchLevel.calibrationProgramDefinition != null)
                            {
                                possibleCPRGDefinitions.Add((int)researchLevel.calibrationProgramDefinition);
                            }
                        }
                    }
                }

            }

            return possibleCPRGDefinitions;
        }


        private List<MissionTargetInProgress> GetTargetsInOrderOfCompletion()
        {
            var targetsThatMatter = CollectCompletedTargets().Where(t => t.TargetType != MissionTargetType.spawn_item);

            var orderedList = targetsThatMatter.OrderBy(t => t.SuccessTime).ToList();

            return orderedList;


        }


        public double CollectDistanceReward()
        {
            var rewardSum = 0.0;

            var orderedList = GetTargetsInOrderOfCompletion();

            var currentPosition = myLocation.MyPosition;

            foreach (var targetInProgress in orderedList)
            {
                if (!targetInProgress.ValidSuccessInfoSet)
                {
                    Logger.Error("no proper successinfo set in " + targetInProgress + " " + this);
                    throw new PerpetuumException(ErrorCodes.ConsistencyError);
                }

                var dist = targetInProgress.SuccessPosition.TotalDistance2D(currentPosition);

                var kilometers = dist/100.0;

                var rewardForTarget = kilometers* _missionDataCache.RewardPerKilometer;

                rewardSum += rewardForTarget;

                currentPosition = targetInProgress.SuccessPosition;

                if (!MissionResolveTester.isTestMode)
                {
                    Logger.Info("distance: " + Math.Round(dist, 3) + " km:" + Math.Round(kilometers, 3) + " reward:" + Math.Round(rewardForTarget, 3));
                }

            }

            var pMult = MissionTargetRewardCalculator.PayOutMultipliers[MissionLevel];

            rewardSum *= pMult;

            rewardSum = Math.Pow(rewardSum, 0.7);

            if (!MissionResolveTester.isTestMode)
            {
                Logger.Info("distance reward sum:" + Math.Round(rewardSum, 3));
            }

            return rewardSum;
        }


        public double CollectRewardByTargets()
        {
            var targetsThatMatter = GetTargetsInOrderOfCompletion();
            return SumRewardsFromTargets(targetsThatMatter);

        }

        public double EstimateRewardByTargets()
        {
            var targetsThatMatter = _targetsInProgress.Values.Where(t => t.TargetType != MissionTargetType.spawn_item).ToList();
            return SumRewardsFromTargets(targetsThatMatter, true);
        }

        private double SumRewardsFromTargets(List<MissionTargetInProgress> targets, bool estimation = false)
        {
            var rewardSum = 0.0;

            foreach (var targetInProgress in targets)
            {
                rewardSum += targetInProgress.GetRewardFee(estimation);
            }

            return rewardSum;

        }

        public void WriteSuccessLogAllTargets()
        {
            if (isTestMode)
            {
                //blocking operation, on purpose
                WriteSuccessLogToSqlAllTargets();
            }
            else
            {
                WriteSuccessLogToSqlAllTargets();
            }
        }

        public void WriteSuccessLogFirstEntry()
        {
            if (isTestMode)
            {
                var si = new SuccessLogInfo()
                {
                    guid = missionGuid,
                    locationEid = myLocation.LocationEid,
                    missionCategory = myMission.missionCategory,
                    targetType = MissionTargetType.rnd_point,
                    x = (int) myLocation.X,
                    y = (int) myLocation.Y,
                    zoneId = myLocation.ZoneConfig.Id,
                    eventTime = DateTime.Now,
                };

                MissionResolveTester.EnqueSuccesLogInfo(si);
                return;
            }


            MissionTargetInProgress.WriteSuccessLog(myLocation.ZoneConfig.Id, (int) myLocation.X, (int) myLocation.Y, missionGuid, MissionTargetType.rnd_point, myLocation.LocationEid, myMission.missionCategory);

        }

        private void WriteSuccessLogToSqlAllTargets()
        {
            //starting point is the missionlocation itself as a random point
            WriteSuccessLogFirstEntry();

            var orderedList = GetTargetsInOrderOfCompletion();

            foreach (var missionTargetInProgress in orderedList)
            {
                missionTargetInProgress.WriteSuccessLog();
            }

            if (MissionResolveTester.skipLog) return;
            Logger.Info("success log written. " + this);
        }

        public void GenerateSuccessInfoForTest(List<Position> terminalsOnZones)
        {
            SearchOrigin = myLocation.MyPosition;

            foreach (var missionTargetInProgress in _targetsInProgress.Values.OrderBy(t => t.DisplayOrder))
            {
                if (!missionTargetInProgress.completed)
                    missionTargetInProgress.ForceComplete();

                var mtsig = new MissionTargetSuccessInfoGenerator(this, missionTargetInProgress, terminalsOnZones);

                missionTargetInProgress.myTarget.AcceptVisitor(mtsig);

                SearchOrigin = missionTargetInProgress.SuccessPosition;
            }

        }

        public long GenerateStructureHash()
        {
            var result = 0L;
            foreach (var missionTargetInProgress in _targetsInProgress.Values)
            {
                if (missionTargetInProgress.myTarget.ValidMissionStructureEidSet && (missionTargetInProgress.myTarget.Type == MissionTargetType.use_switch || missionTargetInProgress.myTarget.Type == MissionTargetType.submit_item || missionTargetInProgress.myTarget.Type == MissionTargetType.rnd_use_itemsupply))
                {
                    result = result | missionTargetInProgress.myTarget.MissionStructureEid;
                }
            }

            return result;

        }




        public ErrorCodes OnMissionStarting()
        {
            //calculate estimations and other stuff 
            //this will run all the lazy inits
            var temp = ToDictionary();

            Logger.Info("mission info is ready " + temp.Count);

            return ErrorCodes.NoError;
        }


        public void SetIssuerCorporationAndAllianceByConfigMission()
        {
            _issuerAllianceEid = myMission.issuerCorporationEid;
            _issuerAllianceEid = myMission.issuerAllianceEid;

        }


        public void SetIssuerCorporationAndAllianceByLocation()
        {
            long issuerCorporationEid;
            long issuerAllianceEid;
            myLocation.GetIssuerCorporationByCategory(myMission.missionCategory, out issuerCorporationEid, out issuerAllianceEid);

            _issuerCorporationEid = issuerCorporationEid;
            _issuerAllianceEid = issuerAllianceEid;

        }

        [Conditional("DEBUG")]
        private void DisplayComponents(Dictionary<int, ItemInfo> componentsDictionary)
        {

            foreach (var itemInfo in componentsDictionary.Values)
            {
                Logger.Info("cprg component " + EntityDefault.Get(itemInfo.Definition).Name + " " + itemInfo);
            }



        }


        public List<int> CollectActiveCPRGDefinitions()
        {
            if (isTestMode)
            {
                return new List<int>();
            }


            var members = GetAffectedCharacters();

            var queryStr = "SELECT distinct definition FROM dbo.missiontargetsarchive WHERE definition IN (SELECT definition FROM dbo.getDefinitionByCFString('cf_random_calibration_programs'))";

            queryStr += " AND characterid in (" + members.ArrayToString() + ")";

            return
                Db.Query().CommandText(queryStr)
                    .Execute()
                    .Select(r => r.GetValue<int>(0))
                    .ToList();

        }

        public const int MISSION_SUCCESS_TELEPORT_MINUTES = 5;
        public const double MISSION_SUCCESS_TELEPORT_MINUTES_BETA = 2.5;


        public void EnableTeleportOnSuccess(IEnumerable<Character> characters, int zoneId)
        {
            var zone = _zoneManager.GetZone(myLocation.ZoneConfig.Id);
            if (zone.Configuration.IsBeta)
            {
                //on beta, only the owner, shorter timer
                zone.GetPlayer(character)?.EnableSelfTeleport(TimeSpan.FromMinutes(MISSION_SUCCESS_TELEPORT_MINUTES_BETA),zoneId);
                return;
            }

            foreach (var currentCharacter in characters)
            {
                //gangmember only on the zone where the mission was started
                zone.GetPlayer(currentCharacter)?.EnableSelfTeleport(TimeSpan.FromMinutes(MISSION_SUCCESS_TELEPORT_MINUTES),zoneId);
            }
        }


        public bool IsTargetLinked(int targetId)
        {
            MissionTargetInProgress target;

            if (!GetTargetInProgress(targetId, out target))
            {
                return false;
            }

            Logger.Info("checking target for links: " + target.myTarget);

            //any target linked to this target?
            return _targetsInProgress.Values.Any(t =>
                t.myTarget.ValidPrimaryLinkSet &&
                t.myTarget.PrimaryDefinitionLinkId == targetId
                );

        }


        public void AddParticipant(Character doerCharacter)
        {
            if (!spreadInGang) return;
            
            if (!IsParticipant(doerCharacter))
            {
                WriteParticipant(missionGuid, doerCharacter);
                _participants = null;
            }
            else
            {
#if DEBUG
                Logger.Info("    >>>> already participant: " + doerCharacter.Id);
#endif
            }
        }

        public void ForceAddParticipant()
        {
            WriteParticipant(missionGuid, character);
            _participants = null;
        }


        public static void WriteParticipant(Guid missionGuid, Character doerCharacter)
        {

            if (missionGuid == Guid.Empty || doerCharacter == Character.None) return;

#if DEBUG
            Logger.Info("    >>>> write mission participant: " + doerCharacter.Id);
#endif

            Db.Query().CommandText("missionAddParticipant")
                .SetParameter("@missionGuid", missionGuid)
                .SetParameter("@characterId", doerCharacter.Id)
                .Execute();
            
        }


        public void DeleteParticipants()
        {
            var res =
                Db.Query().CommandText("delete missionparticipants where missionguid=@missionGuid")
                    .SetParameter("@missionGuid", missionGuid)
                    .ExecuteNonQuery();

            Logger.Info(res + " missionparticipants were deleted. " + missionGuid);
        }

        public HashSet<Character> GetParticipantsVerbose()
        {
            var participants = GetParticipants();

            var logMessage = participants.Count + " characters participated in " + this.ToString();
            foreach (var pCharacter in participants)
            {
#if DEBUG
                logMessage += "\ncharacterId:" + pCharacter.Id + " nick:" + pCharacter.Nick;
#endif
            }

#if DEBUG
            Logger.Info(logMessage);
#endif

            return participants;
        }


        private HashSet<Character> LoadParticipantsFromDb()
        {
            return
                Db.Query().CommandText("select characterid from missionparticipants where missionguid=@missionGuid")
                    .SetParameter("@missionGuid", missionGuid)
                    .Execute()
                    .Select(r => Character.Get(r.GetValue<int>(0)))
                    .ToHashSet();
        }

        private HashSet<Character> _participants;

        private HashSet<Character> GetParticipants()
        {
            return LazyInitializer.EnsureInitialized(ref _participants, LoadParticipantsFromDb);
        }


        private bool IsParticipant(Character doerCharacter)
        {
            var result = GetParticipants().Contains(doerCharacter);
#if DEBUG
            Logger.Info("    >>>> is mission participant: " + (result ? "YES" : "NO" )  + doerCharacter.Id);    
#endif
            return result;
        }

        public bool ExtractDoerCharacter(IDictionary<string, object> info, out Character doerCharacter)
        {
            var assistingCharacterId = info.GetOrDefault<int>(k.assistingCharacterID);

            if (assistingCharacterId == 0 || assistingCharacterId == character.Id)
            {
                doerCharacter = null;
                return false;
            }

            doerCharacter = Character.Get(assistingCharacterId);
            return true;
        }

        public List<Character> FilterParticipants( List<Character> participants)
        {
            var ownersGang = character.GetGang();

            var filteredList = new List<Character> { character };

            if (spreadInGang && ownersGang == null)
            {
                Logger.Info("++ mission started in a gang, but owner is gangless now. " + this);
                return filteredList;
            }

            if (!spreadInGang || ownersGang == null)
            {
                Logger.Info("++ mission was not started in a gang. going solo. " + this);
                return filteredList;
            }

            foreach (var participant in participants)
            {
                // the owner is already on the list
                if (participant == character) continue;

                if (!ownersGang.IsMember(participant)) continue;

                filteredList.Add(participant);
            }

            Logger.Info("++ " + participants.Count + " were processed. " + filteredList.Count + " was found in the owner's gang. " + this);

            return filteredList;

        }

        public void SetRewardDivider()
        {
            if (!spreadInGang)
            {
                //solo, no gang
                _rewardDivider = 1;
            }

            var ownersGang = character.GetGang();
            if (ownersGang == null)
            {
                //not in gang
                _rewardDivider = 1;
            }

            // that counter is starting from 0 which means no other gangmembers online
            _rewardDivider = GangMemberCountMaximized +1;

        }

        public void GetRewardDivider(List<Character> participants, out double rewardFraction, out int rewardDivider)
        {
            if (!spreadInGang)
            {
                rewardDivider = 1;
                rewardFraction = 1;
                Logger.Info("++ solo divider  " + this);
                return;
            }

            rewardDivider = Math.Max(participants.Count(), _rewardDivider);
            if (rewardDivider <= 0) rewardDivider = 1;
            rewardFraction = 1.0 / rewardDivider;

            Logger.Info("++ rewardDivider:"  + rewardDivider + " rewardFraction:" + rewardFraction + "  " + this );

        }

        public int CalculateEp()
        {
            if (myLocation.ZoneConfig.Type == ZoneType.Training) return 0;
            var missionLevel = MissionLevel;
            if (missionLevel == 0) return 1; //Change: level 0 -> 1ep
            if (missionLevel == -1) { missionLevel = 1; } //10 training missions

            var result = myMission.Targets.Count * Math.Pow(missionLevel, 0.5);

            result = Math.Ceiling(result);
            result = Math.Max(result, 1);

            if (myLocation.ZoneConfig.IsBeta)
                result *= 2;

            return (int) result;
        }
    }
}
