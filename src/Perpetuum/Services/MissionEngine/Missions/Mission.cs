using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Perpetuum.Accounting.Characters;
using Perpetuum.Data;
using Perpetuum.ExportedTypes;
using Perpetuum.Items;
using Perpetuum.Log;
using Perpetuum.Services.MissionEngine.MissionDataCacheObjects;
using Perpetuum.Services.MissionEngine.MissionTargets;
using Perpetuum.Services.Standing;

namespace Perpetuum.Services.MissionEngine.Missions
{
    public class Mission
    {
        private readonly Lazy<Dictionary<string, object>> _myDictionary;
        private readonly Lazy<List<MissionTarget>> _myTargets;

        //SQLbol kiszedni %%%
        //sourceAgentID, -- ez meg hasznos lehet addig amig ki nem pucolom az osszes mocskos missiont
        //missionAgentDelete  hasznalja is

        //name,   -- ezt sracokkal megdumalni

        public readonly int id;
        internal long issuerCorporationEid; //fluff, tm_ss,tm_ii,ics_ww ... stb
        internal long issuerAllianceEid; //standing match VS level - config only
        public readonly bool isUnique;
        private readonly int? _missionIdOnSuccess;
        public bool isTriggered; //if this mission is participating as a fail or success mission
        private readonly int? _periodMinutes; //daily mission appereance 
        private readonly int? _missionLevel; //only in config missions
        public readonly string title;
        private readonly string _description;
        private readonly int _missionType; // client sorting and MissionCategory lookup
        public readonly int durationMinutes; //maximum length of the mission
        public readonly bool listable;
        public readonly double rewardFee;
        public readonly MissionCategory missionCategory;
        private readonly int? _locationId;
        private readonly int? _difficultyReward;
        private readonly double? _difficultymultiplier;

        public readonly MissionBehaviourType behaviourType; //config / random filter

        //missionDone, missionExpired
        private readonly string _successMessage; //a message on success
        public readonly string failMessage; //a message on failure/abort

        public override string ToString()
        {
            var info = $"id:{id} title:{title} {missionCategory}";

            return info;
        }

        protected Mission(IDataRecord record)
        {
            id = record.GetValue<int>(k.ID.ToLower());
            _missionIdOnSuccess = record.GetValue<int?>(k.missionIDOnSuccess.ToLower());
            isUnique = record.GetValue<bool>(k.isUnique.ToLower());
            _periodMinutes = record.GetValue<int?>(k.periodMinutes.ToLower());
            _missionLevel = record.GetValue<int?>(k.missionLevel.ToLower());
            title = record.GetValue<string>(k.title);
            _description = record.GetValue<string>(k.description);
            _missionType = record.GetValue<int>(k.missionType.ToLower());
            durationMinutes = record.GetValue<int>(k.durationMinutes.ToLower());
            _successMessage = record.GetValue<string>(k.successMessage.ToLower());
            failMessage = record.GetValue<string>(k.failMessage.ToLower());
            listable = record.GetValue<bool>(k.listable);
            rewardFee = record.GetValue<double>(k.rewardFee.ToLower());
            _locationId = record.GetValue<int?>("locationid");
            behaviourType = (MissionBehaviourType) record.GetValue<int>("behaviourtype");
            _difficultyReward = record.GetValue<int?>("difficultyreward");
            _difficultymultiplier = record.GetValue<double?>("difficultymultiplier");

            missionCategory = MissionDataCache.GetMissionCategoryByType(_missionType);

            InitIssuer(record);

            _myTargets = new Lazy<List<MissionTarget>>(CollectMyTargets);
            _myDictionary = new Lazy<Dictionary<string, object>>(GenerateMyDictionary);
        }

        private static MissionDataCache _missionDataCache;

        public static void Init(MissionDataCache missionDataCache)
        {
            _missionDataCache = missionDataCache;
        }

        private void InitIssuer(IDataRecord record)
        {
            LoadIssuer(record);
        }
        
        /// <summary>
        /// Config missions load their issuer from the cached issuers
        /// </summary>
        /// <param name="record"></param>
        protected virtual void LoadIssuer(IDataRecord record)
        {
            var issuerId = record.GetValue<int>(k.issuerID.ToLower());

            MissionIssuer missionIssuer;
            if (!_missionDataCache.GetIssuerById(issuerId, out missionIssuer))
            {
                Logger.Error("invalid issuer is defined, falling back to default: 252 tm_ww ");
                _missionDataCache.GetIssuerById(252, out missionIssuer); //safe, liveon is ennyi az eid
            }

            issuerCorporationEid = missionIssuer.corporationEid;
            issuerAllianceEid = missionIssuer.allianceEid;
        }

        public bool ValidDifficultyMultiplierSet
        {
            get { return _difficultymultiplier != null && _difficultymultiplier > 0; }
        }

        public double DifficultyMultiplier
        {
            get { return _difficultymultiplier ?? 1; }
        }
        
       
        public bool ValidDifficultyRewardSet
        {
            get { return _difficultyReward != null && _difficultyReward > 0; }
        }

        public int DifficultyReward
        {
            get { return _difficultyReward ?? 10; }
        }

        public bool ValidSuccessMissionIdSet
        {
            get { return _missionIdOnSuccess != null && _missionIdOnSuccess > 0; }
        }

        public bool IsPeriodic
        {
            get { return _periodMinutes != null && _periodMinutes > 0; }
        }

        public bool ValidMissionLevelSet
        {
            get { return _missionLevel != null; }
        }

        public int MissionIdOnSuccess
        {
            get { return _missionIdOnSuccess ?? 0; }
        }

        public int PeriodMinutes
        {
            get { return _periodMinutes ?? 5; }
        }

        public int MissionLevel
        {
            get { return _missionLevel ?? 0; }
        }


        public Dictionary<string, object> ToDictionary()
        {
            return _myDictionary.Value;
        }

      
        private Dictionary<string, object> GenerateMyDictionary()
        {
            return new Dictionary<string, object>
            {
                {k.ID, id},
                {k.rewardFee, rewardFee},
                {k.missionCategory, (int) missionCategory},
                {k.title, title},
                {k.description, _description},
                {k.type, _missionType},
                {k.durationMinutes, durationMinutes},
                {k.startItems, StartItems.ToDictionary("c", se => se.ToDictionary())},
                {k.listable, listable},
                {k.location, _locationId},
                {k.successMessage, _successMessage},
                {k.failMessage, failMessage},
                {"behaviourType", (int) behaviourType}
            };
        }


        private IEnumerable<MissionStandingRequirement> RequiredStandings
        {
            get { return _missionDataCache.GetRequiredStandings(id); }
        }


        public IEnumerable<int> RequiredMissions
        {
            get { return _missionDataCache.GetRequiredMissions(id); }
        }

        private List<MissionTarget> CollectMyTargets()
        {
            return _missionDataCache.GetAllMissionTargets.Where(t => t.ValidMissionIdSet && t.MissionId == id).ToList();
        }


        /// <summary>
        /// mission targets in their original form
        /// </summary>
        public List<MissionTarget> Targets
        {
            get { return _myTargets.Value; }
        }


        

        //mission extension requirements
        public IEnumerable<Extension> RequiredExtensions
        {
            get { return _missionDataCache.GetRequiredExtensions(id); }
        }

        public IEnumerable<ItemInfo> StartItems
        {
            get { return _missionDataCache.GetStartItems(id); }
        }

        public IEnumerable<MissionStandingChange> StandingChanges
        {
            get { return _missionDataCache.GetStandingChanges(id); }
        }

        public IEnumerable<MissionReward> RewardItems
        {
            get { return _missionDataCache.GetRewardItems(id); }
        }

        public int LocationId
        {
            get { return _locationId ?? 0; }
        }


        public static bool Filter(IDataRecord record)
        {
            var mission = GenerateMissionFromRecord(record);

            if (!mission.CheckConsistency())
            {
                Logger.Error("consistency error in mission! ID: " + record.GetValue<int>(k.ID.ToLower()));
                return false;
            }

            return true;
        }


        protected virtual bool CheckConsistency()
        {
            //itt lehet meg varazsolni... 

            return true;
        }

        //ezt kell x ido lejarosra kessbe rakni %%% mi a fasz ez?
        public static TimeSpan GetMissionAverageSeconds(int missionId)
        {
            var seconds = Db.Query().CommandText("getMissionAverageTime").SetParameter("@missionID", missionId).ExecuteScalar<int>();
            return TimeSpan.FromSeconds(seconds);
        }


        public double GetTypeRelatedExtensionBonus(Character character)
        {
            var extensionSelector = MissionDataCache.GetExtensionSelectorByCategory(missionCategory);
            var specializedBonus = 0.0;

            switch (extensionSelector)
            {
                case MissionExtensionSelector.combat:
                    specializedBonus = character.GetExtensionBonusByName(ExtensionNames.COMBAT_MISSION_SPECIALIST);
                    break;

                case MissionExtensionSelector.fieldcraft:
                    specializedBonus = character.GetExtensionBonusByName(ExtensionNames.INDUSTRY_MISSION_SPECIALIST);
                    break;

                case MissionExtensionSelector.production:
                    specializedBonus = character.GetExtensionBonusByName(ExtensionNames.PRODUCTION_MISSION_SPECIALIST);
                    break;

                case MissionExtensionSelector.transport:
                    specializedBonus = character.GetExtensionBonusByName(ExtensionNames.LOGISTIC_MISSION_SPECIALIST);
                    break;

                default:
                    return specializedBonus;

            }

            return specializedBonus;
        }

        public double GetHitMitigationBonus(Character character)
        {
            return character.GetExtensionBonusByName(ExtensionNames.STANDING_MANAGEMENT);

        }


        /// <summary>
        /// Matches level with standing
        /// Used in config mission start
        /// </summary>
        /// <param name="character"></param>
        /// <param name="standingHandler"></param>
        /// <returns></returns>
        public bool MatchStandingToLevel(Character character,IStandingHandler standingHandler)
        {
            var standing = standingHandler.GetStanding(issuerAllianceEid, character.Eid);
            return standing >= MissionLevel;
        }

        public bool CheckRequiredStandingsToOtherAlliances(Character character)
        {
            foreach (var missionStandingRequirement in RequiredStandings)
            {
                if (!missionStandingRequirement.CheckStanding(character))
                {
                    return false;
                }
            }

            return true;
        }

        public bool CheckPeriodicMissions(Dictionary<int, DateTime> periodicMissions)
        {
            //daily mission check
            if (IsPeriodic)
            {
                if (periodicMissions.ContainsKey(id))
                {
                    if (periodicMissions[id].AddMinutes(PeriodMinutes) > DateTime.Now)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public IEnumerable<int> GetPossibleSuccessorMissionIds()
        {
            var list = new List<int>();

            if (ValidSuccessMissionIdSet)
            {
                list.Add(MissionIdOnSuccess);
            }

            list.AddMany(Targets.Where(t => t.IsBranching).Select(t => t.BranchMissionId));

            return list;
        }

        public static Mission GenerateMissionFromRecord(IDataRecord record)
        {
            var behaviour = (MissionBehaviourType) record.GetValue<int>("behaviourtype");

            switch (behaviour)
            {
                case MissionBehaviourType.Config:
                    return new Mission(record);

                case MissionBehaviourType.Random:
                    return new RandomMission(record);

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }


        public virtual void AcceptVisitor(MissionVisitor visitor)
        {
            visitor.VisitMission(this);
        }


        public Dictionary<string,object> GetSlimInfo()
        {
            return new Dictionary<string, object> {{k.ID, id}, {k.title, title}, {k.type, _missionType}};

        }
    }
}