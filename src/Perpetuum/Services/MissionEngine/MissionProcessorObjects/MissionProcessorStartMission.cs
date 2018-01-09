using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Perpetuum.Accounting.Characters;
using Perpetuum.Data;
using Perpetuum.Log;
using Perpetuum.Services.MissionEngine.Missions;
using Perpetuum.Services.MissionEngine.MissionStructures;

namespace Perpetuum.Services.MissionEngine.MissionProcessorObjects
{
    public partial class MissionProcessor
    {
        /// <summary>
        /// Starts a mission by client request
        /// </summary>
        /// <param name="character"></param>
        /// <param name="category"></param>
        /// <param name="level"></param>
        /// <param name="spreadInGang"></param>
        /// <param name="location"></param>
        /// <returns></returns>
        public Dictionary<string, object> MissionStartForRequest(Character character, MissionCategory category, int level, MissionLocation location)
        {
            var gang = character.GetGang();

            //fix spreading:
            //if in gang = always spreading
            //else = not spreading
            var spreadInGang = gang != null;

            var agent = location.Agent;

            var safeToResolve = location.GetSolvableRandomMissionsAtLocation();

            if (level >= 0 && safeToResolve.Count > 0 && category != MissionCategory.Special)
            {
                return SelectAndStartRandomMission(character, spreadInGang, location, level, category);
            }

            //old missions 
            return SelectAndStartConfigMission(character, agent, category, level, spreadInGang, location);
        }


        /// <summary>
        /// This is the actual function that starts a mission, OLD SCHOOL
        /// </summary>
        /// <param name="character"></param>
        /// <param name="agent"></param>
        /// <param name="category"></param>
        /// <param name="level"></param>
        /// <param name="spreadInGang"></param>
        /// <param name="location"></param>
        /// <returns></returns>
        public Dictionary<string, object> SelectAndStartConfigMission(Character character, MissionAgent agent, MissionCategory category, int level, bool spreadInGang, MissionLocation location)
        {
            List<int> missionsDone;
            Dictionary<int, DateTime> periodicMissions;
            GetFinishedAndLastMissions(character, out missionsDone, out periodicMissions).ThrowIfError();

            var missionFilter = new MissionFilter(character, periodicMissions, missionsDone, this, location,_missionDataCache);

            missionFilter.IsMissionRunningWithThisCategoryAndLevel(category, level, agent).ThrowIfTrue(ErrorCodes.MissionRunningWithCategoryAndLevel);

            var missions =
                missionFilter.GetConfigMissionsByCategoryAndLevel(category, level).Where(m => missionFilter.IsConfigMissionAvailable(m,_standingHandler)).ToArray();

            missions.Any().ThrowIfFalse(ErrorCodes.NoMissionAvailableWithConditions);

            var mission = missions.RandomElement();

            Dictionary<string, object> info;
            var resolved = StartMission(character, spreadInGang, mission, location, level, out info);

            if (!resolved)
            {
                Logger.Error("WTF? in config mission start " + location + " " + mission);
            }

            return info;
        }


        /// <summary>
        /// Selects a template and starts it... Random mission start 
        /// </summary>
        /// <param name="character"></param>
        /// <param name="spreadInGang"></param>
        /// <param name="location"></param>
        /// <param name="level"></param>
        /// <param name="category"></param>
        /// <returns></returns>
        public Dictionary<string, object> SelectAndStartRandomMission(Character character, bool spreadInGang, MissionLocation location, int level, MissionCategory category)
        {
            if (location.Zone == null)
            {
                Logger.Error("zone config was not found for zoneId:" + location.zoneId);
                throw new PerpetuumException(ErrorCodes.ZoneNotFound);
            }
            
            var missionAgent = location.Agent;

            GetFinishedAndLastMissions(character, out List<int> missionsDone, out Dictionary<int, DateTime> periodicMissions).ThrowIfError();

            var missionFilter = new MissionFilter(character, periodicMissions, missionsDone, this, location,_missionDataCache);

            missionFilter.IsMissionRunningWithThisCategoryAndLevel(category, level, missionAgent).ThrowIfTrue(ErrorCodes.MissionRunningWithCategoryAndLevel);

            MissionHelper.GetStandingData(character, missionAgent.OwnerAlliance.Eid, out double standing, out int playerLevel);

            //at this location this is the maximum level
            playerLevel = playerLevel.Clamp(0, location.maxMissionLevel);

            //not higher pls
            level = level.Clamp(0, playerLevel);

            var solvableMissions = location.GetSolvableRandomMissionsAtLocation();

            var rndMissionsHere = solvableMissions
                .Where(m =>
                    m.missionCategory == category &&
                    missionFilter.IsRandomMissionAvailable(m)
                ).ToList();

            if (rndMissionsHere.Count == 0)
            {
                character.CreateErrorMessage(Commands.MissionStart, ErrorCodes.NoMissionAvailableWithConditions)
                    .SetExtraInfo(d => d[k.message] = "no mission was found")
                    .Send();

                return null;
            }

            var mission = rndMissionsHere.RandomElement();

            mission.ThrowIfNull(ErrorCodes.WTFErrorMedicalAttentionSuggested);

            var resolveInfo = _missionDataCache.GetAllResolveInfos.FirstOrDefault(ri => ri.missionId == mission.id && ri.locationId == location.id);

            resolveInfo.ThrowIfNull(ErrorCodes.ConsistencyError);

            var attempts = resolveInfo.attempts;

            Dictionary<string, object> info = new Dictionary<string, object>();
            var resolved = false;
            while (attempts-- > 0 && !resolved)
            {
                resolved = StartMission(character, spreadInGang, mission, location, level, out info);

                if (!resolved)
                {
                    Logger.Warning("mission resolve attempts left " + attempts + "      " + mission);
                }
            }

            resolved.ThrowIfFalse(ErrorCodes.WTFErrorMedicalAttentionSuggested);

            return info;
        }


        public bool StartMission(Character character, bool spreadInGang, Mission mission, MissionLocation location, int level, out Dictionary<string, object> info)
        {
            //add the new mission in progress, set things in motion
            MissionInProgress missionInProgress;
            var resolved = MissionAdministrator.StartMission(character, spreadInGang, mission, location, level, out missionInProgress);

            if (!resolved)
            {
                info = new Dictionary<string, object>();
                return false;
            }

            Transaction.Current.OnCommited(() => GetOptionsByRequest(character, missionInProgress.myLocation));

            info = new Dictionary<string, object>
            {
                {k.mission, missionInProgress.ToDictionary()},
            };

            return true;
        }


        // the system triggers a mission
        public bool TriggeredMissionStart(Character character, bool spreadInGang, int missionId, MissionLocation location, int level, out MissionInProgress missionInProgress)
        {
            Logger.Info("trying to trigger missionId: " + missionId + " characterId:" + character.Id);

            Mission mission;
            _missionDataCache.GetMissionById(missionId, out mission).ThrowIfFalse(ErrorCodes.ItemNotFound);

            var resolved = MissionAdministrator.StartMission(character, spreadInGang, mission, location, level, out missionInProgress);

            return resolved;
        }

        // an admin starts a mission
        public Dictionary<string, object> AdminMissionStartByRequest(Character character, bool spreadInGang, int missionId, MissionLocation location, int level)
        {
            Mission mission;
            _missionDataCache.GetMissionById(missionId, out mission).ThrowIfFalse(ErrorCodes.ItemNotFound);

            int missionLevel;
            if (mission.behaviourType == MissionBehaviourType.Random)
            {
                missionLevel = level; //submitted from gui
            }
            else
            {
                //use the mission's level
                missionLevel = mission.MissionLevel;
            }

            MissionInProgress missionInProgress;
            var result = TriggeredMissionStart(character, spreadInGang, missionId, location, missionLevel, out missionInProgress);

            if (result)
            {
                Transaction.Current.OnCommited(() => GetOptionsByRequest(character, location));
                Transaction.Current.OnCommited(() => SendRunningMissionList(character));
            }
            else
            {
                throw new PerpetuumException(ErrorCodes.UnableToResolveMission);
            }

            return new Dictionary<string, object>
            {
                {k.mission, missionInProgress.ToDictionary()},
            };
        }


        private void StartAsync(Character character, bool spreading, int missionId, MissionLocation location, int level)
        {
            Task.Run(() =>
            {
                using (var scope = Db.CreateTransaction())
                {
                    Logger.Info("starting a new mission:" + missionId + " " + location);
                    MissionInProgress triggeredMission;
                    TriggeredMissionStart(character, spreading, missionId, location, level, out triggeredMission);

                    Transaction.Current.OnCommited(() => SendRunningMissionList(character));

                    scope.Complete();
                }
            }).LogExceptions();
        }
    }
}
