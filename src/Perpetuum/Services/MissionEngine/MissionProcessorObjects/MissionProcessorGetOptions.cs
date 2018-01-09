using System;
using System.Collections.Generic;
using System.Linq;
using Perpetuum.Accounting.Characters;
using Perpetuum.Services.MissionEngine.Missions;
using Perpetuum.Services.MissionEngine.MissionStructures;
using Perpetuum.Services.Standing;

namespace Perpetuum.Services.MissionEngine.MissionProcessorObjects
{
    public partial class MissionProcessor
    {
        public void GetOptionsByRequest(Character character, MissionLocation location)
        {
            var result = new Dictionary<string, object>()
            {
                {k.locationID, location.id}
            };

            List<int> missionsDone;
            Dictionary<int, DateTime> periodicMissions;
            GetFinishedAndLastMissions(character, out missionsDone, out periodicMissions).ThrowIfError();
            var solvableMissions = location.GetSolvableRandomMissionsAtLocation();

            var configMissionsAvailabilityDict = GenerateConfigMissionOptions(location, character, missionsDone, periodicMissions, solvableMissions);

            result.Add(k.options, configMissionsAvailabilityDict);

            var randomMissionAvailabilityDict = GenerateRandomMissionOptions(location, character, missionsDone, periodicMissions, solvableMissions);

            result.Add("randomMissions", randomMissionAvailabilityDict);

            Message.Builder.WithData(result).ToCharacter(character).SetCommand(Commands.MissionGetOptions).Send();
        }


        private Dictionary<string, object> GenerateRandomMissionOptions(MissionLocation location, Character character, List<int> missionsDone, Dictionary<int, DateTime> periodicMissions, List<Mission> solvableMissions)
        {
            var availabilityDict = new Dictionary<string, object>();
            var missionAgent = location.Agent;

            var missionFilter = new MissionFilter(character, periodicMissions, missionsDone, this, location,_missionDataCache);

            int[] missionIdsByAgent;
            if (!_missionDataCache.GetMissionIdsByAgent(missionAgent, out missionIdsByAgent))
            {
                //no random mission defined for this agent
                return availabilityDict;
            }

            var rndMissionsHere = solvableMissions.Where(m => missionFilter.IsRandomMissionAvailable(m)).ToList();

            var counter = 0;

            foreach (var missionCategory in Enum.GetValues(typeof (MissionCategory)).Cast<MissionCategory>())
            {
                if (rndMissionsHere.All(m => m.missionCategory != missionCategory))
                    continue;

                //one mission per category
                if (missionFilter.IsMissionRunningWithThisCategory(missionCategory))
                    continue;

                for (var missionLevel = 0; missionLevel <= location.maxMissionLevel; missionLevel++)
                {
                    var oneEntry = new Dictionary<string, object>()
                    {
                        {k.missionCategory, (int) missionCategory},
                        {k.missionLevel, missionLevel},
                        {k.oke, 1},
                    };

                    availabilityDict.Add("r" + counter++, oneEntry);
                }
            }

            return availabilityDict;
        }

        /// <summary>
        /// Oldschool mission options.
        /// </summary>
        /// <param name="location"></param>
        /// <param name="character"></param>
        /// <param name="missionsDone"></param>
        /// <param name="periodicMissions"></param>
        /// <param name="solvableMissions"></param>
        /// <returns></returns>
        private Dictionary<string, object> GenerateConfigMissionOptions(MissionLocation location, Character character, List<int> missionsDone, Dictionary<int, DateTime> periodicMissions, List<Mission> solvableMissions)
        {
            var missionAgent = location.Agent;

            var missionFilter = new MissionFilter(character, periodicMissions, missionsDone, this, location,_missionDataCache);

            var standing = _standingHandler.GetStanding(missionAgent.OwnerAlliance.Eid, character.Eid);

            //if the agent has random missions then we skip the levels from 0 ... 9
            //otherwise we include all config levels
            //var topLevel = solvableMissions.Count > 0 ? -1 : 9;

            var topLevel = 9;

            var counter = 0;
            var availabilityDict = new Dictionary<string, object>();

            foreach (var missionCategory in Enum.GetValues(typeof (MissionCategory)).Cast<MissionCategory>())
            {
                for (var missionLevel = -1; missionLevel <= topLevel; missionLevel++)
                {
                    var missionsWithCategoryAndLevel = missionFilter.GetConfigMissionsByCategoryAndLevel(missionCategory, missionLevel).ToArray();
                    var totalCount = missionsWithCategoryAndLevel.Count(m => !m.isTriggered && m.listable);

                    var standingBlocked = standing < missionLevel;

                    var availableCount = missionsWithCategoryAndLevel.Count(m => missionFilter.IsConfigMissionAvailable(m,_standingHandler));

                    var oneEntry = new Dictionary<string, object>()
                    {
                        {k.missionCategory, (int) missionCategory},
                        {k.missionLevel, missionLevel},
                        {"totalCount", totalCount},
                        {"availableCount", availableCount},
                        {k.oke, (availableCount > 0 ? 1 : 0)},
                        {"standingBlocked", standingBlocked},
                    };

                    availabilityDict.Add("c" + counter++, oneEntry);
                }
            }

            return availabilityDict;
        }
    }
}
