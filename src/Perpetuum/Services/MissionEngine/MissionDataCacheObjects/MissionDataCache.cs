using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.Items;
using Perpetuum.Log;
using Perpetuum.Services.ExtensionService;
using Perpetuum.Services.MissionEngine.MissionBonusObjects;
using Perpetuum.Services.MissionEngine.Missions;
using Perpetuum.Services.MissionEngine.MissionStructures;
using Perpetuum.Services.MissionEngine.MissionTargets;
using Perpetuum.Units;
using Perpetuum.Zones;
using Perpetuum.Zones.Artifacts;

namespace Perpetuum.Services.MissionEngine.MissionDataCacheObjects
{
    /// <summary>
    /// Read only mission data cache
    /// </summary>
    public class MissionDataCache
    {
        private IDictionary<string, object> _dataCache; //prepared client data

        private IDictionary<int, Mission> _missions;
        private IDictionary<int, MissionTarget> _targets;
        private ILookup<int, Extension> _requiredExtensions;
        private ILookup<int, ItemInfo> _startItems;
        private ILookup<int, int> _requiredMissions;
        private ILookup<int, MissionStandingRequirement> _requiredStandings;
        private ILookup<int, int> _agentToMissions;
        private ILookup<int, MissionReward> _rewards;
        private IDictionary<int, MissionAgent> _agents;
        private IDictionary<int, MissionIssuer> _issuers;
        private ILookup<int, MissionStandingChange> _standingChange;
        private IDictionary<int, MissionLocation> _missionLocations;
        private IDictionary<int, int> _mineralDefinitionToAmountPerCycle;
        private IDictionary<MissionTargetType, int> _rewardByType;
        private IDictionary<int, int> _levelToGrind; 

        private List<MissionResolveInfo> _missionResolveInfos;
        private Dictionary<string, double> _missionConstants;
       
        private readonly List<ArtifactInfo> _missionArtifactInfos = new List<ArtifactInfo>();
        private readonly Dictionary<int, int> _mineralDefinitionToGeoscanDocumentDefinition = new Dictionary<int, int>();

        private Lazy<TargetSelectionValidator> _targetSeletionValidator;

        public int LookUpGrindAmount(int missionLevel)
        {
            int amount;
            if (!_levelToGrind.TryGetValue(missionLevel, out amount))
            {
                Logger.Error("wtf LookUpGrindAmount " + missionLevel);
                return 1000;
            }

            return amount;
        }

       
        public int ScaleMaxGangMembers
        {
            get { return (int) GetMissionConstantValue("ScaleMaxGangMembers"); }
        }

        public double ScaleMineralLevelFractionForGangMember
        {
            get { return GetMissionConstantValue("ScaleMineralLevelFractionForGangMember"); }
        }
        
        public double ScaleArtifactLevelFractionForGangMember
        {
            get { return GetMissionConstantValue("ScaleArtifactLevelFractionForGangMember"); }
        }

        public double RewardPerKilometer
        {
            get { return GetMissionConstantValue("RewardPerKilometer"); }
        }

        private readonly IExtensionReader _extensionReader;
        private readonly IZoneManager _zoneManager;
        private readonly IEntityDefaultReader _entityDefaultReader;

        public MissionDataCache(IExtensionReader extensionReader,IZoneManager zoneManager,IEntityDefaultReader entityDefaultReader)
        {
            _extensionReader = extensionReader;
            _zoneManager = zoneManager;
            _entityDefaultReader = entityDefaultReader;
        }

        #region cache mission

        /// <summary>
        /// 
        /// Resets/Clears the mission data cache
        /// 
        /// This can be called runtime, BUT works only on a single host server. 
        /// Workaround: inform all hosts and reset missiondatacache locally in every host.
        /// 
        /// </summary>
        private void ResetMissionDataCache()
        {
            InitPossibleMinerals();
            InitGeoscanDocuments();
            InitArtifactInfos();
            InitMissionResolveInfos();
            InitMissionConstants();
            
            _missionLocations = Database.CreateCache<int, MissionLocation>("missionlocations", "id", MissionLocation.FromRecord);

            _targets = Database.CreateCache<int, MissionTarget>("missiontargets", "id", MissionTargetFactory.GenerateMissionTargetFromConfigRecord, MissionTarget.Filter);

            _requiredExtensions = Database.CreateLookupCache<int, Extension>("missionrequiredextensions", "missionid", r => new Extension(r.GetValue<int>(k.extensionID.ToLower()), r.GetValue<int>(k.extensionLevel.ToLower())), FilterRequiredExtensions);

            _startItems = Database.CreateLookupCache<int, ItemInfo>("missionstartitem", "missionid", r => new ItemInfo(r.GetValue<int>(k.definition), r.GetValue<int>(k.quantity)), FilterStartItems);

            _requiredMissions = Database.CreateLookupCache<int, int>("missionrequiredmissions", "mission", r => r.GetValue<int>(k.requiredMission.ToLower()));

            _agentToMissions = Database.CreateLookupCache<int, int>("missiontoagent", "agentid", r => r.GetValue<int>("missionid"));

            _requiredStandings = Database.CreateLookupCache<int, MissionStandingRequirement>("missionrequiredstanding", "missionid", r => new MissionStandingRequirement(r));

            _missions = Database.CreateCache<int, Mission>("missions", "id", Mission.GenerateMissionFromRecord, Mission.Filter);

            _rewards = Database.CreateLookupCache<int, MissionReward>("missionrewards", "missionid", MissionReward.FromRecord);

            _agents = Database.CreateCache<int, MissionAgent>("missionagents", "id", MissionAgent.FromRecord);
            _issuers = Database.CreateCache<int, MissionIssuer>("missionissuer", "id", r => new MissionIssuer(r));
            _standingChange = Database.CreateLookupCache<int, MissionStandingChange>("missionstandingchange", "missionid", MissionStandingChange.FromRecord);

            _mineralDefinitionToAmountPerCycle = Database.CreateCache<int, int>("minerals", "definition", r => r.GetValue<int>("amount"));

            _rewardByType = Database.CreateCache<MissionTargetType, int>("missiontargettypes", "id", r => r.GetValue<int>("reward"));

            _levelToGrind = Database.CreateCache<int, int>("missiongrind", "missionlevel", r => r.GetValue<int>("amount"));

            _targetSeletionValidator = new Lazy<TargetSelectionValidator>(() => TargetSelectionValidator.CreateValidator(_zoneManager));
           
            _dataCache = null;
        }

       

        private void InitMissionConstants()
        {
            var records = Db.Query().CommandText("select * from missionconstants").Execute();

            _missionConstants = new Dictionary<string, double>(records.Count);

            foreach (var record in records)
            {
                var name = record.GetValue<string>("name");
                var value = record.GetValue<double>("value");
                _missionConstants.Add(name, value);
            }
        }

        private void InitMissionResolveInfos()
        {
            _missionResolveInfos = Db.Query().CommandText("select * from missiontolocation").Execute().Select(MissionResolveInfo.FromRecord).ToList();
        }

        private void InitArtifactInfos()
        {
            _missionArtifactInfos.Clear();

            _missionArtifactInfos.AddRange(Db.Query().CommandText("select * from artifacttypes where dynamic=1").Execute().Select(ArtifactInfo.GenerateArtifactInfo).ToList());
        }

        [CanBeNull]
        public MissionLocation GetLocationByEid(long locationEid)
        {
            return _missionLocations.Values.FirstOrDefault(l => l.LocationEid == locationEid);
        }

        public bool IsTargetSelectionValid(IZone zone, Position source, Position target)
        {
            return _targetSeletionValidator.Value.IsTargetSelectionValid(zone, source, target);
        }

        private double GetMissionConstantValue(string name)
        {
            double value;
            if (!_missionConstants.TryGetValue(name, out value))
            {
                Logger.Error("mission constant not found:" + name);
                throw new PerpetuumException(ErrorCodes.ConsistencyError);
            }

            return value;
        }

        public IEnumerable<Mission> GetAllMissions
        {
            get { return _missions.Values; }
        }

        /// <summary>
        /// No test missions, only handcrafted random templates
        /// </summary>
        public IEnumerable<Mission> GetAllLiveRandomMissionTemplates
        {
            get
            {
                return  _missions.Values.Where(m => m.behaviourType == MissionBehaviourType.Random && m.title.Contains("missiontemplate")).ToList();
            }
        }



        private bool FilterRequiredExtensions(IDataRecord record)
        {
            var extensionId = record.GetValue<int>(k.extensionID.ToLower());
            if (!_extensionReader.GetExtensions().ContainsKey(extensionId))
            {
                Logger.Error("consistency error in mission required extensions! ID:" + record.GetValue<int>(k.ID.ToLower()));
                return false;
            }
            return true;
        }

        private bool FilterStartItems(IDataRecord record)
        {
            var definition = record.GetValue<int>(k.definition);
            var quantity = record.GetValue<int>(k.quantity);

            if (!_entityDefaultReader.Exists(definition) || quantity <= 0)
            {
                Logger.Error("consistency error in mission start items. ID:" + record.GetValue<int>(k.ID.ToLower()));
                return false;
            }

            return true;
        }

        public void CacheMissionData()
        {
            Logger.Info("--consistency test starting--");

            try
            {
                ResetMissionDataCache();

                SetTriggeredMissions();
                CheckSuccessAndFailMissions();
                ChechBranchingMissionTargets();

                var t = GetDataForClient();
                Logger.Info(t.Count + " mission keys generated for client");
            }
            catch (Exception ex)
            {
                Logger.Exception(ex);
            }

            Logger.Info("--consistency test ending--");
        }

        private void SetTriggeredMissions()
        {
            foreach (var mission in _missions.Values)
            {
                if (mission.ValidSuccessMissionIdSet)
                {
                    _missions[mission.MissionIdOnSuccess].isTriggered = true;
                }

                foreach (var target in _targets.Values.Where(t => t.IsBranching))
                {
                    _missions[target.BranchMissionId].isTriggered = true;
                }
            }
        }

        private void CheckSuccessAndFailMissions()
        {
            foreach (var mission in _missions.Values)
            {
                if (mission.ValidSuccessMissionIdSet)
                {
                    if (!_missions.ContainsKey(mission.MissionIdOnSuccess))
                    {
                        Logger.Error("consistency error in mission. missionid on success not found. ID:" + mission.id + " success id:" + mission.MissionIdOnSuccess);
                    }
                }
            }
        }


        /// <summary>
        /// Mission branching config check.
        /// </summary>
        private void ChechBranchingMissionTargets()
        {
            foreach (var mission in _missions.Values)
            {
                var target = mission.Targets.FirstOrDefault(t => t.IsBranching);

                if (target != null)
                {
                    if (mission.Targets.Any(t => t.targetOrder == target.targetOrder && t.IsBranching))
                    {
                        Logger.Error("not all targets are branching in target group " + target + " " + mission);
                    }

                    if (mission.Targets.Any(t => t.targetOrder != target.targetOrder && target.IsBranching))
                    {
                        Logger.Error("branching target was found in different target group " + target + " " + mission);
                    }
                }
            }
        }

        #endregion

        public bool GetTargetById(int targetId, out MissionTarget missionTarget)
        {
            return _targets.TryGetValue(targetId, out missionTarget);
        }

        public bool TryGetAgent(int id, out MissionAgent missionAgent)
        {
            return _agents.TryGetValue(id, out missionAgent);
        }

        [CanBeNull]
        public MissionAgent GetAgent(int id)
        {
            return _agents.GetOrDefault(id);
        }

        public bool GetMissionById(int id, out Mission mission)
        {
            return _missions.TryGetValue(id, out mission);
        }

        public Mission GetMissionById(int id)
        {
            return _missions.GetOrDefault(id);
        }

        #region client data



        private static Dictionary<string, object> ListMissionTypes()
        {
            var counter = 0;
            return (from t in Db.Query().CommandText("select id,name,category from missiontypes").Execute() select (object) new Dictionary<string, object> {{k.key, t.GetValue<int>(0)}, {k.name, t.GetValue<string>(1)}, {k.category, t.GetValue<string>(2)}}).ToDictionary(d => "t" + counter++);
        }


        public IDictionary<string, object> GetDataForClient()
        {
            return LazyInitializer.EnsureInitialized(ref _dataCache,
                () =>
                    new Dictionary<string, object>
                    {
                        {k.missionType, ListMissionTypes()},
                        {"bonusMultipliers", GetBonusMultiplierDictionary()},
                        {"locationInfo", CreateLocationInfo()},
                        {"locations", CreateLocationsDictionary()},
                        {k.missions, CreateSlimMissionInfo()}
                    });
        }

        private Dictionary<string, object> CreateSlimMissionInfo()
        {
            var count = 0;
            var result = new Dictionary<string, object>();
            foreach (var mission in _missions.Values)
            {
                result.Add("m" + count++, mission.GetSlimInfo());
            }

            return result;
        }

        #endregion

        public List<int> GetCompleteMissionLine(int startMissionId)
        {
            var lineIds = new List<int>();
            var processQueue = new Queue<int>();

            Mission mission;
            if (!_missions.TryGetValue(startMissionId, out mission))
            {
                Logger.Error("mission not found, missionID: " + startMissionId);
                return lineIds;
            }

            if (mission.ValidSuccessMissionIdSet)
            {
                //not part of a missionline
                return lineIds;
            }

            foreach (var successorMissionId in mission.GetPossibleSuccessorMissionIds())
            {
                if (successorMissionId == 0) continue;
                processQueue.Enqueue(successorMissionId);
                lineIds.Add(successorMissionId);
            }

            while (processQueue.Count > 0)
            {
                var missionId = processQueue.Dequeue();

                if (!_missions.TryGetValue(missionId, out mission))
                {
                    Logger.Error("mission not found, missionID: " + missionId);
                    continue;
                }

                if (mission.ValidSuccessMissionIdSet)
                {
                    //end of a missionline
                    continue;
                }

                foreach (var successorMissionId in mission.GetPossibleSuccessorMissionIds())
                {
                    if (successorMissionId == 0) continue;

                    processQueue.Enqueue(successorMissionId);
                    lineIds.Add(successorMissionId);
                }
            }

            return lineIds;
        }


        private IEnumerable<Mission> GetPeriodicMissions()
        {
            return _missions.Values.Where(m => m.IsPeriodic);
        }

        public IEnumerable<int> GetPeriodicMissionIds()
        {
            return GetPeriodicMissions().Select(m => m.id);
        }

        public int GetMaximumPeriod()
        {
            var periodicMissions = GetPeriodicMissions().ToArray();

            if (periodicMissions.Length>0)
            {
                return  periodicMissions.Max(m => m.IsPeriodic ? m.PeriodMinutes : 0);
            }

            return 0;
        }


        /// <summary>
        /// Returns info about all the locations
        /// </summary>
        /// <returns></returns>
        private Dictionary<string, object> CreateLocationInfo()
        {
            var result = new Dictionary<string, object>();

            var counter = 0;
            foreach (var location in _missionLocations.Values)
            {
                var solvableMissions = location.GetSolvableRandomMissionsAtLocation();

                if (solvableMissions.Count > 0)
                {
                    //level -1 + random template

                    CollectConfigMissionByLocation(location,result,ref counter);
                    CollectRandomMissionsByLocation(location,result,ref counter);

                }
                else
                {
                    //oldschool
                    CollectConfigMissionByLocation(location,result,ref counter);

                }

            }

            return result;
        }


        private void CollectConfigMissionByLocation(MissionLocation location, Dictionary<string,object> result, ref int counter )
        {
            var maxLevel = 10; //
            for (var i = -1; i < maxLevel; i++)
            {
                foreach (var category in Enum.GetValues(typeof(MissionCategory)).Cast<MissionCategory>())
                {
                    var categoryCount = location.Agent.MyConfigMissions.Count(m => m.MissionLevel == i && m.missionCategory == category && location.id == m.LocationId);

                    if (categoryCount > 0)
                    {
                        var myDict = new Dictionary<string, object>
                        {
                            { k.missionCategory, (int)category },
                            { k.count, categoryCount },
                            { k.missionLevel, i },
                            { k.location, location.id },
                        };

                        result.Add("c" + counter++, myDict);
                    }
                }
            }
        }

        private void CollectRandomMissionsByLocation(MissionLocation location, Dictionary<string, object> result, ref int counter)
        {
            for (var i = 0; i <= location.maxMissionLevel; i++)
            {
                foreach (var category in Enum.GetValues(typeof(MissionCategory)).Cast<MissionCategory>())
                {
                    var categoryCount = location.Agent.MyRandomMissions.Count(m => m.missionCategory == category);
                    if (categoryCount > 0)
                    {
                        var myDict = new Dictionary<string, object>
                        {
                            { k.missionCategory, (int)category },
                            { k.count, categoryCount },
                            { k.missionLevel, i },
                            { k.location, location.id },
                        };

                        result.Add("r" + counter++, myDict);
                    }
                }
            }
        }




        private Dictionary<string, object> CreateLocationsDictionary()
        {
            var result = new Dictionary<string, object>();
            var counter = 0;

            foreach (var location in _missionLocations.Values)
            {
                result.Add("l" + counter++, location.ToDictionary());
            }

            return result;
        }


        private static Dictionary<string, object> GetBonusMultiplierDictionary()
        {
            var counter = 0;
            var result = new Dictionary<string, object>();

            foreach (var pair in MissionBonus.BonusMultipliers)
            {
                var oneEntry = new Dictionary<string, object>() {{k.bonus, pair.Key}, {k.multiplier, pair.Value}};

                result.Add("b" + counter++, oneEntry);
            }

            return result;
        }


        private readonly Dictionary<int, List<int>> _zoneIdToPossibleMineralDefinitions = new Dictionary<int, List<int>>();


        /// <summary>
        /// NOT gravel, gammaterial, energymineral
        /// </summary>
        private void InitPossibleMinerals()
        {
            _zoneIdToPossibleMineralDefinitions.Clear();

            const string query = @"

SELECT m.definition FROM dbo.mineralconfigs mc 
JOIN dbo.minerals m ON m.idx = mc.materialtype 
 WHERE mc.zoneid=@zoneId
  AND mc.materialtype !=10 and mc.materialtype != 15 and mc.materialtype != 13
";
            foreach (var zone in _zoneManager.Zones)
            {
                var mineralDefinitions = Db.Query().CommandText(query)
                                                .SetParameter("@zoneId", zone.Id)
                                                .Execute()
                                                .Select(r => r.GetValue<int>(0)).ToList();

                _zoneIdToPossibleMineralDefinitions[zone.Id] = mineralDefinitions;
                Logger.Info(mineralDefinitions.Count + " possible mineral definitions cached for zone:" + zone.Id);
            }
        }

        public List<int> GetPossibleMineralDefinitions(int zoneId)
        {
            List<int> list;
            if (!_zoneIdToPossibleMineralDefinitions.TryGetValue(zoneId, out list))
            {
                list = new List<int>();
            }

            return list;
        }


        public int GetGeoscanDocumentByMineral(int mineralDefinition)
        {
            int documentDefinition;
            if (!_mineralDefinitionToGeoscanDocumentDefinition.TryGetValue(mineralDefinition, out documentDefinition))
            {
                Logger.Error(EntityDefault.Get(mineralDefinition).Name + " " + mineralDefinition + " in, but no related geoscan document.");
                throw new PerpetuumException(ErrorCodes.ConsistencyError);
            }

            return documentDefinition;
        }

        private void InitGeoscanDocuments()
        {
            _mineralDefinitionToGeoscanDocumentDefinition.Clear();

            var records = Db.Query().CommandText("SELECT definition,geoscandocument FROM minerals WHERE geoscandocument IS NOT NULL").Execute();

            foreach (var record in records)
            {
                var mineralDefinition = record.GetValue<int>("definition");
                var geoscanDocumentDefinition = record.GetValue<int>("geoscandocument");

                _mineralDefinitionToGeoscanDocumentDefinition[mineralDefinition] = geoscanDocumentDefinition;
            }
        }


        /// <summary>
        /// Returns the raw amount of minerals that can be extracted with a driller module in one cycle
        /// </summary>
        /// <param name="mineralDefinition"></param>
        /// <returns></returns>
        public int GetAmountPerCycleByMineralDefinition(int mineralDefinition)
        {
            int amount;
            if (!_mineralDefinitionToAmountPerCycle.TryGetValue(mineralDefinition, out amount))
            {
                Logger.Error("no per cycle amount was found for definition:" + mineralDefinition);
                throw new PerpetuumException(ErrorCodes.ConsistencyError);
            }

            return amount;
        }


       

        public static string GetCorporationPostFixByMissionCategory(MissionCategory missionCategory)
        {
            var exs = GetExtensionSelectorByCategory(missionCategory);

            switch (exs)
            {
                case MissionExtensionSelector.fieldcraft:
                    return "_iw";
                case MissionExtensionSelector.production:
                    return "_ii";
                case MissionExtensionSelector.combat:
                    return "_ww";
                case MissionExtensionSelector.transport:
                    return "_ss";
                default:
                    return "_ss";
            }

        }

        public bool TryGetMissionById(int missionId, out Mission mission)
        {
            return _missions.TryGetValue(missionId, out mission);
        }

        public IEnumerable<MissionTarget> GetAllMissionTargets
        {
            get { return _targets.Values; }
        }

        //mission extension requirements - these are needed to take the mission
        public IEnumerable<Extension> GetRequiredExtensions(int missionId)
        {
            return _requiredExtensions.GetOrEmpty(missionId);
        }

        public IEnumerable<ItemInfo> GetStartItems(int missionId)
        {
            return _startItems.GetOrEmpty(missionId);
        }

        public IEnumerable<MissionStandingChange> GetStandingChanges(int missionId)
        {
            return _standingChange.GetOrEmpty(missionId);
        }


        public IEnumerable<MissionReward> GetRewardItems(int missionId)
        {
            return _rewards.GetOrEmpty(missionId);
        }

        public IEnumerable<MissionStandingRequirement> GetRequiredStandings(int missionId)
        {
            return _requiredStandings.GetOrEmpty(missionId);
        }


        public IEnumerable<int> GetRequiredMissions(int missionId)
        {
            return _requiredMissions.GetOrEmpty(missionId);
        }

        public bool GetMissionIdsByAgent(MissionAgent agent, out int[] missionIdsByAgent)
        {
            return _agentToMissions.TryGetValue(agent.id, out missionIdsByAgent);
        }

        public IEnumerable<MissionAgent> GetAllAgents
        {
            get { return _agents.Values; }
        }

        public bool GetIssuerById(int id, out MissionIssuer issuer)
        {
            return _issuers.TryGetValue(id, out issuer);
        }

        public bool GetLocationById(int id, out MissionLocation location)
        {
            return _missionLocations.TryGetValue(id, out location);
        }

        public IEnumerable<MissionLocation> GetAllLocations
        {
            get { return _missionLocations.Values; }
        }


        public MissionLocation GetLocation(int id)
        {
            return _missionLocations.GetOrDefault(id);
        }

        public bool GetAmountPerCycleByMineralDefinition(int mineralDefinition, out int amount)
        {
            return _mineralDefinitionToAmountPerCycle.TryGetValue(mineralDefinition, out amount);
        }

        public int GetRewardByType(MissionTargetType targetType)
        {
            return _rewardByType.GetOrDefault(targetType, 1);
        }


        public List<MissionResolveInfo> GetAllResolveInfos
        {
            get { return _missionResolveInfos; }
        }

        public List<ArtifactInfo> GetAllArtifactInfos
        {
            get { return _missionArtifactInfos; }
        }



        /*
0	missiontype_harvesting	missioncategory_harvesting
1	missiontype_mining	missioncategory_mining
2	missiontype_killandfetch	missioncategory_combat
3	missiontype_courier	missioncategory_transport
5	missiontype_onlykill	missioncategory_combat
6	missiontype_scan	missioncategory_exploration
7	missiontype_scan_robot	missioncategory_combat_exploration
8	missiontype_huntthescout	missioncategory_combat
9	missiontype_defendandmine	missioncategory_industrial
10	missiontype_retrieve	missioncategory_combat
11	missiontype_scanandloot	missioncategory_combat_exploration
12	missiontype_storyline	missioncategory_special
13	missiontype_production	missioncategory_production
14	missiontype_general_training	missioncategory_general_training
15	missiontype_combat_training	missioncategory_combat_training
16	missiontype_industrial_training	missioncategory_industrial_training
17	missiontype_industrial_courier	missioncategory_industrial
18	missiontype_complex_production	missioncategory_complex_production
19	missiontype_artifact	missioncategory_exploration
20  missiontype_combat_exploration missioncategory_combat_exploration
         */

        public static MissionCategory GetMissionCategoryByType(int missionTypeInt)
        {
            switch (missionTypeInt)
            {
                case 11:
                case 7:
                case 20:
                    return MissionCategory.CombatExploration;

                case 2:
                case 5:
                case 8:
                case 10:

                    return MissionCategory.Combat;

                case 15:
                    return MissionCategory.combat_training;

                case 6:
                case 19:
                    return MissionCategory.Exploration;

                case 14:
                    return MissionCategory.general_training;

                case 0:
                    return MissionCategory.Harvesting;

                case 9:
                case 17:
                    return MissionCategory.Industrial;

                case 16:
                    return MissionCategory.industrial_training;

                case 1:
                    return MissionCategory.Mining;

                case 13:
                    return MissionCategory.Production;

                case 12:
                    return MissionCategory.Special;

                case 3:
                    return MissionCategory.Transport;

                case 18:
                    return MissionCategory.ComplexProduction;

                default:
                    Logger.Error("noncategorized missiontype!!! missiontype:" + missionTypeInt + " falling back to " + MissionCategory.Transport);
                    return MissionCategory.Transport;
            }
        }

        public static MissionExtensionSelector GetExtensionSelectorByCategory(MissionCategory category)
        {
            switch (category)
            {

                case MissionCategory.Combat:
                case MissionCategory.CombatExploration:
                    return MissionExtensionSelector.combat;

                case MissionCategory.Harvesting:
                case MissionCategory.Mining:
                case MissionCategory.Exploration:
                case MissionCategory.Industrial:
                    return MissionExtensionSelector.fieldcraft;


                case MissionCategory.Production:
                case MissionCategory.ComplexProduction:
                    return MissionExtensionSelector.production;

                case MissionCategory.Transport:
                    return MissionExtensionSelector.transport;

                default:
                    return MissionExtensionSelector.none;
                

            }
        }

      

        [CanBeNull]
        public MissionTarget GetTargetByStructureUnit(Unit structureUnit)
        {

            var strucureTarget = GetAllMissionTargets.FirstOrDefault(t => t.MissionStructureEid == structureUnit.Eid);
            if (strucureTarget == null)
            {
                Logger.Error("no target found for " + structureUnit);
                throw new PerpetuumException(ErrorCodes.ItemNotFound);
            }

            return strucureTarget;
        }
    }
}