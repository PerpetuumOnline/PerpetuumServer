using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Perpetuum.Accounting.Characters;
using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Host;
using Perpetuum.Items;
using Perpetuum.Log;
using Perpetuum.Services.MissionEngine.MissionBonusObjects;
using Perpetuum.Services.MissionEngine.MissionDataCacheObjects;
using Perpetuum.Services.MissionEngine.MissionProcessorObjects;
using Perpetuum.Services.MissionEngine.Missions;
using Perpetuum.Services.MissionEngine.MissionStructures;
using Perpetuum.Services.Standing;
using Perpetuum.Timers;
using Perpetuum.Zones;

namespace Perpetuum.Services.MissionEngine.AdministratorObjects
{
    public class MissionAdministrator
    {
        public delegate MissionAdministrator Factory(MissionProcessor missionProcessor);

        private readonly ConcurrentDictionary<int, MissionInProgressCollector> _missionInProgress = new ConcurrentDictionary<int, MissionInProgressCollector>();
        private readonly ConcurrentDictionary<int, MissionBonusCollector> _missionBonuses = new ConcurrentDictionary<int, MissionBonusCollector>();

        private readonly MissionProcessor _missionProcessor;
        private readonly MissionDataCache _missionDataCache;
        private readonly IStandingHandler _standingHandler;
        private readonly MissionInProgress.Factory _missionInProgressFactory;

        public MissionAdministrator(MissionProcessor missionProcessor,MissionDataCache missionDataCache,IStandingHandler standingHandler,MissionInProgress.Factory missionInProgressFactory)
        {
            _missionProcessor = missionProcessor;
            _missionDataCache = missionDataCache;
            _standingHandler = standingHandler;
            _missionInProgressFactory = missionInProgressFactory;
        }

        public void Initialize()
        {
            CacheRunningMissions();
            LoadMissionBonuses();
        }

        public void ResetMissionInProgressCollector()
        {
            _missionInProgress.Clear();
        }


        public bool GetMissionInProgressCollector(Character character, out MissionInProgressCollector missionInProgressCollector)
        {
            return _missionInProgress.TryGetValue(character.Id, out missionInProgressCollector);
        }

        private MissionInProgressCollector GetOrCreateMissionInProgressCollector(Character character)
        {
            MissionInProgressCollector missionInProgressCollector;
            if (!_missionInProgress.TryGetValue(character.Id, out missionInProgressCollector))
            {
                return new MissionInProgressCollector();
            }

            return missionInProgressCollector;
        }

        private void AddNewMissionInProgress(MissionInProgress missionInProgress)
        {
            var collector = _missionInProgress.GetOrAdd(missionInProgress.character.Id, z => new MissionInProgressCollector());
            collector.AddMissionInProgress(missionInProgress);
        }


        private bool HasCharacterRunningMissions(Character character)
        {
            if (character == Character.None)
                return false;
            return _missionInProgress.Keys.Contains(character.Id);
        }

        private void CacheRunningMissions()
        {
            //running missions for active characters
            var queryStr = @"
select 
ml.*
from missionlog ml 
JOIN characters c ON c.characterID=ml.characterID
where ml.finished is NULL and c.active=1";

            try
            {
                var lastOnline = HostInfo.GetLastOnline();
                var offlinePeriod = DateTime.Now.Subtract(lastOnline);

                Logger.Info("the server was " + offlinePeriod + " offline.");


                var shiftedExpiry =
                    //offset exiry dates
                    Db.Query().CommandText("UPDATE missionlog SET expire=DATEADD(MINUTE,@offlineMinutes , expire ) WHERE finished IS NULL")
                        .SetParameter("@offlineMinutes", (int) offlinePeriod.TotalMinutes)
                        .ExecuteNonQuery();

                Logger.Info( shiftedExpiry + " missions' expiry date got offseted with " + offlinePeriod);


                var missionRecords = Db.Query().CommandText(queryStr).Execute().ToList();

                foreach (var record in missionRecords)
                {
                    var missionId = record.GetValue<int>("missionid");

                    Mission mission;
                    if (!_missionDataCache.GetMissionById(missionId, out mission))
                    {
                        //skip mission load
                        continue;
                    }

                    var missionInProgress = MissionHelper.ReadMissionInProgressByRecord(record, mission);

                    if (missionInProgress == null) continue;

                    // add to ram 
                    AddMissionInProgress(missionInProgress);
                }

                Logger.Info(_missionInProgress.Count + " running missions cached.");
            }
            catch (Exception ex)
            {
                Logger.Error("error occured in caching the running missions." + ex.Message);
            }
        }

        public void Update(TimeSpan time)
        {
            UpdateExpiredMissions(time);
            UpdateExpiredBonus(time);
        }


        private bool _bonusInProgress;
        private readonly IntervalTimer _missionBonusExpireTimer = new IntervalTimer(TimeSpan.FromSeconds(60.43));

        private void UpdateExpiredBonus(TimeSpan time)
        {
            _missionBonusExpireTimer.Update(time);

            if (!_missionBonusExpireTimer.Passed)
                return;

            _missionBonusExpireTimer.Reset();

            if (!_bonusInProgress)
            {
                _bonusInProgress = true;

                Task.Run(() =>
                {
                    CheckBonusExpire();
                    _bonusInProgress = false;
                }).LogExceptions();
            }
        }


        private void CheckBonusExpire()
        {
            foreach (var collector in _missionBonuses.Values)
            {
                foreach (var missionBonus in collector.ActiveBonuses())
                {
                    var seconsPast = DateTime.Now.Subtract(missionBonus.lastModified).TotalSeconds;
                    var expiryPeriod = missionBonus.GetBonusTimeSeconds();

#if DEBUG
                    Logger.Info(" >>>> " + missionBonus + " secondsPast:" + seconsPast + " expiryPeriod:" + expiryPeriod);
#endif

                    if (DateTime.Now.Subtract(missionBonus.lastModified).TotalSeconds > missionBonus.GetBonusTimeSeconds())
                    {
                        using (var scope = Db.CreateTransaction())
                        {
                            try
                            {
                                missionBonus.Timeout();
                                SetMissionBonus(missionBonus);

                                scope.Complete();
                            }
                            catch (Exception ex)
                            {
                                Logger.Exception(ex);
                            }
                        }
                    }
                }
            }
        }

        private bool _expireInProgress;
        private readonly IntervalTimer _missionExpireTimer = new IntervalTimer(TimeSpan.FromSeconds(5));

        private void UpdateExpiredMissions(TimeSpan time)
        {
            _missionExpireTimer.Update(time);

            if (!_missionExpireTimer.Passed)
                return;

            _missionExpireTimer.Reset();

            if (!_expireInProgress)
            {
                _expireInProgress = true;

                Task.Run(() =>
                {
                    MissionExpireCycle();
                    _expireInProgress = false;
                }).LogExceptions();
            }
        }

        private void MissionExpireCycle()
        {
            if (_missionInProgress.IsNullOrEmpty()) return;

            var expiredOnes = new List<MissionInProgress>();
            var now = DateTime.Now;

            try
            {
                foreach (var list in _missionInProgress.Values)
                {
                    expiredOnes.AddRange(list.GetMissionsInProgress().Where(m => m.expire < now));
                }
            }
            catch (Exception ex)
            {
                Logger.Error("error in collecting the expried missions. ");
                Logger.Exception(ex);
            }

            if (!expiredOnes.IsNullOrEmpty())
            {
                //... itt jarnak le

                foreach (var expiredMission in expiredOnes)
                {
                    using (var scope = Db.CreateTransaction())
                    {
                        try
                        {
                            if (expiredMission != null)
                            {
                                Logger.Info("mission expired: " + expiredMission);

                                expiredMission.OnMissionExpired(_missionProcessor);
                                
                            }

                            scope.Complete();
                        }
                        catch (Exception ex)
                        {
                            Logger.Exception(ex);
                        }
                    }
                }
            }
        }

        public void DecreaseBonus(Character character, MissionCategory missionCategory, int missionLevel, MissionAgent agent)
        {
            var missionBonus = GetOrAddMissionBonus(character, missionCategory, missionLevel, agent);

            if (missionBonus.DecreaseBonus())
            {
                SetMissionBonus(missionBonus);
            }
        }

        private Dictionary<string, object> GetRunningMissionsInfo(Character character, bool excludeSoloMissions = false)
        {
            var tempDict = new Dictionary<string, object>();
            if (HasCharacterRunningMissions(character))
            {
                var missionsInPrgs = GetOrCreateMissionInProgressCollector(character).GetMissionsInProgress().ToList();
                
                var result = new Dictionary<string,object>(missionsInPrgs.Count);

                var counter = 0;
                foreach (var missionInProgress in missionsInPrgs)
                {
                    if (excludeSoloMissions && !missionInProgress.spreadInGang) 
                        continue;

                    var oneEntry = missionInProgress.ToDictionary();

                    result.Add("c" + counter++, oneEntry);
                }


                tempDict = result;
            }

            return tempDict;

        }

        private Dictionary<string, object> CollectRunningGangMissions(Character character)
        {
            var gangDict = new Dictionary<string, object>();

            //exclude myself
            var members = _missionProcessor.GetGangMembersCached(character).Where(c => c.Id != character.Id).ToList();

            var counter = 0;
            foreach (var member in members)
            {
                var missionInfo = GetRunningMissionsInfo(member, true);
                gangDict.Add("g" + counter++, missionInfo);
            }

            return gangDict;
        }


        public Dictionary<string, object> RunningMissionList(Character character)
        {
            //obtain my info
            var missionsInfo = GetRunningMissionsInfo(character);
            
            var bonusDict = new Dictionary<string, object>();
            MissionBonusCollector missionBonusCollector;
            if (_missionBonuses.TryGetValue(character.Id, out missionBonusCollector))
            {
                bonusDict =
                    missionBonusCollector.ToDictionary();
            }

            //gang members' running missions
            var gangMissions = CollectRunningGangMissions(character);

            var totalDict = new Dictionary<string, object>
            {
                {k.missions, missionsInfo},
                {"gangMissions", gangMissions},
                {k.count, GetMissionCountData(character)},
                {k.bonus, bonusDict},
                {k.standing,_standingHandler.GetStandingForDefaultAlliances(character)},
            };

            return totalDict;
        }


        public void RemoveMissionInProgress(MissionInProgress missionInProgress)
        {
            if (HasCharacterRunningMissions(missionInProgress.character))
            {
                GetOrCreateMissionInProgressCollector(missionInProgress.character).RemoveMissionInProgress(missionInProgress);
            }
        }


        private void AddMissionInProgress(MissionInProgress newMission)
        {
            //add to running missions
            AddNewMissionInProgress(newMission);
        }

        public int RunningMissionsCount(Character character)
        {
            return HasCharacterRunningMissions(character) ? GetOrCreateMissionInProgressCollector(character).NofRunningMissions() : 0;
        }

        public Dictionary<string, object> GetMissionCountData(Character character)
        {
            var runningMissionsCount = RunningMissionsCount(character);

            return new Dictionary<string, object>
            {
                {k.current, runningMissionsCount},
            };
        }

        [CanBeNull]
        public bool StartMission(Character character, bool spreadInGang, Mission mission, MissionLocation location, int level, out MissionInProgress missionInProgress)
        {
            Logger.Info("Attempting to start mission. " + mission + " lvl:" + level + " location:" + location + " spreading:" + spreadInGang + " characterID:" + character.Id);

            if (!TryCreateMission(character, spreadInGang, mission, location,level, out missionInProgress))
            {
                if (mission.behaviourType == MissionBehaviourType.Config)
                {
                    throw new PerpetuumException(ErrorCodes.WTFErrorMedicalAttentionSuggested);
                }

                //random missiont failed to resolve
                Logger.Warning("random mission failed to resolve. " + mission + " characterID:" + character.Id);
                return false;
            }
       
            AdministrateNewMission(missionInProgress);

            return true;

        }

        public bool TryCreateMission(Character character, bool spreadInGang, Mission mission, MissionLocation location, int level, out MissionInProgress missionInProgress, bool isTestMode = false)
        {
            if (location.ZoneConfig == ZoneConfiguration.None)
            {
                Logger.Error("zone config was not found for zoneId:" + location.ZoneConfig.Id);
                missionInProgress = null;
                return false;
            }

            missionInProgress = _missionInProgressFactory(mission);
            missionInProgress.missionGuid = Guid.NewGuid();
            missionInProgress.character = character;
            missionInProgress.started = DateTime.Now;
            missionInProgress.expire = DateTime.Now.AddMinutes(mission.durationMinutes);
            missionInProgress.spreadInGang = spreadInGang;
            missionInProgress.bonusMultiplier = GetBonusMultiplierForMissionStart(character, mission, location, level);
            missionInProgress.myLocation = location;
            missionInProgress.MissionLevel = level;
            missionInProgress.isTestMode = isTestMode;

            if (mission.behaviourType == MissionBehaviourType.Config)
            {
                //get from cached data
                missionInProgress.SetIssuerCorporationAndAllianceByConfigMission();
            }
            else
            {
                //lookup based on location
                missionInProgress.SetIssuerCorporationAndAllianceByLocation();    
            }

            missionInProgress.SetRewardDivider();
            
            //add targets in progress
            var success = missionInProgress.CreateAndSolveTargets(mission);
            return success;
        }

        private void AdministrateNewMission(MissionInProgress missionInProgress)
        {
            //initialize rewards and other stuff
            missionInProgress.OnMissionStarting().ThrowIfError();


            //store running mission in sql
            missionInProgress.InsertMissionLog().ThrowIfError();

            //write mission targets to sql
            missionInProgress.WriteAllTargetsToSql().ThrowIfError();

            //spawn courier and other start related items
            SpawnStartItemsForConfigMissions(missionInProgress.myMission, missionInProgress);

            //add to ram cache
            AddMissionInProgress(missionInProgress);

            //add the owner for sure
            missionInProgress.ForceAddParticipant();
            
            missionInProgress.SendZoneNewMission();

        }



        private double GetBonusMultiplierForMissionStart(Character character, Mission mission, MissionLocation location, int level)
        {
            var result = 1.0;

            MissionBonusCollector collector;
            if (_missionBonuses.TryGetValue(character.Id, out collector))
            {
                MissionBonus bonus;
                if (collector.GetBonusWithConditions(mission.missionCategory, level, location.Agent, out bonus))
                {
                    result = bonus.BonusMultiplier;
                    return result;
                }

            }

            //on level 0 there is no bonus collector allocated
            var extBonus = character.GetExtensionBonusByName(ExtensionNames.MISSION_BONUS_LEVEL_MOD) +1;
            result *= extBonus;

            return result;
        }

        /// <summary>
        /// This spawns the reward items for the config missions
        /// </summary>
        /// <param name="mission"></param>
        /// <param name="missionInProgress"></param>
        private static void SpawnStartItemsForConfigMissions(Mission mission, MissionInProgress missionInProgress)
        {
            if (!mission.StartItems.Any())
                return;

            var container = missionInProgress.myLocation.GetContainer;
           
            var startItems = new List<Item>();

            foreach (var itemInfo in mission.StartItems)
            {
                var ed = EntityDefault.Get(itemInfo.Definition);

                if (ed.AttributeFlags.NonStackable)
                {
                    //give one by one the nonstackable

                    for (var i = 0; i < itemInfo.Quantity; i++)
                    {
                        var startItem = container.CreateAndAddItem(itemInfo.Definition, false, item =>
                        {
                            item.Owner = missionInProgress.character.Eid;
                            item.Quantity = 1;
                        });

                        startItems.Add(startItem);
                    }
                }
                else
                {
                    //these ones are also nonstacked just for the clarity, so the player sees the items separately
                    var info = itemInfo;
                    var startItem = container.CreateAndAddItem(itemInfo.Definition, false, item =>
                    {
                        item.Owner = missionInProgress.character.Eid;
                        item.Quantity = info.Quantity;
                    });

                    startItems.Add(startItem);
                }
            }

            container.Save();

            if (startItems.Count <= 0)
                return;

            
            var startItemsDict = startItems.ToDictionary("i", i => i.ToDictionary());
            var result = new Dictionary<string, object>(2)
            {
                {k.locationID, missionInProgress.myLocation.id}, 
                {k.startItems, startItemsDict},
            };

            Transaction.Current.OnCommited(() =>
            {
                Message.Builder.SetCommand(Commands.MissionStartItems)
                    .WithData(result)
                    .ToCharacter(missionInProgress.character)
                    .Send();
            });
        }

       

        public bool FindMissionInProgressByMissionId(Character character, int missionId, out MissionInProgress missionInProgress)
        {
            missionInProgress = null;

            MissionInProgressCollector collector;
            if (!GetMissionInProgressCollector(character, out collector))
            {
                return false;
            }

            if (collector.NofRunningMissions() == 0)
            {
                return false;
            }

            missionInProgress = collector.GetMissionsInProgress().FirstOrDefault(progress => progress.MissionId == missionId);

            return missionInProgress != null;
        }

        public bool FindMissionInProgressByMissionGuid(Character character, Guid missionGuid, out MissionInProgress missionInProgress)
        {
            missionInProgress = null;

            MissionInProgressCollector collector;
            if (!GetMissionInProgressCollector(character, out collector))
            {
                return false;
            }

            if (collector.NofRunningMissions() == 0)
            {
                return false;
            }

            missionInProgress = collector.GetMissionsInProgress().FirstOrDefault(progress => progress.missionGuid == missionGuid);

            return missionInProgress != null;
        }



        #region mission bonus

        public MissionBonusCollector GetOrAddBonusCollector(Character character)
        {
            return
                _missionBonuses.GetOrAdd(character.Id, c => new MissionBonusCollector());
        }

        public void RemoveCollector(Character character)
        {
            MissionBonusCollector collector;
            _missionBonuses.Remove(character.Id, out collector);
        }

        public void SetMissionBonus(MissionBonus missionBonus)
        {
            missionBonus.SaveToDb();

            Transaction.Current.OnCommited(() =>
            {
                if (missionBonus.Bonus == 0)
                {
                    MissionBonusCollector collector;
                    if (_missionBonuses.TryGetValue(missionBonus.character.Id, out collector))
                    {
                        collector.RemoveBonus(missionBonus);

                        if (collector.IsEmpty)
                        {
                            RemoveCollector(missionBonus.character);
                        }
                    }
                }
                else
                {
                    var collector =
                        GetOrAddBonusCollector(missionBonus.character);

                    collector.AddBonus(missionBonus);
                }

                missionBonus.SendUpdateToClient();
            });
        }

        public MissionBonus GetOrAddMissionBonus(Character character, MissionCategory missionCategory, int missionLevel, MissionAgent agent)
        {
            var collector =
                _missionBonuses.GetOrAdd(character.Id, c => new MissionBonusCollector());

            MissionBonus missionBonus;
            if (!collector.GetBonusWithConditions(missionCategory, missionLevel, agent, out missionBonus))
            {
                return new MissionBonus(character, missionCategory, missionLevel, agent, 0);
            }

            return missionBonus;
        }

        private void LoadMissionBonuses()
        {
            var records = Db.Query().CommandText("select characterid,missioncategory,missionlevel,agentid,bonus from missionbonus").Execute();

            foreach (var record in records)
            {
                var mb = MissionBonus.FromRecrod(record);

                var collector = GetOrAddBonusCollector(mb.character);

                collector.AddBonus(mb);
            }

            Logger.Info(_missionBonuses.Count + " mission bonuses loaded.");
        }

        #endregion
    }
}
