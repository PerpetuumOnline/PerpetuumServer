using System;
using System.Collections.Generic;
using System.Linq;
using Perpetuum.Accounting.Characters;
using Perpetuum.Services.MissionEngine.AdministratorObjects;
using Perpetuum.Services.MissionEngine.MissionDataCacheObjects;
using Perpetuum.Services.MissionEngine.MissionProcessorObjects;
using Perpetuum.Services.MissionEngine.Missions;
using Perpetuum.Services.MissionEngine.MissionStructures;
using Perpetuum.Services.Standing;

namespace Perpetuum.Services.MissionEngine
{
    public class MissionFilter
    {
        private readonly MissionAgent _agent;
        private readonly Dictionary<int, DateTime> _periodicMissionTimes;
        private readonly ICollection<int> _finishedMissionIds;
        private readonly MissionProcessor _missionProcessor;
        private readonly MissionLocation _location;
        private readonly MissionDataCache _missionDataCache;
        private readonly Character _character;


        public MissionFilter(Character character, Dictionary<int, DateTime> periodicMissionTimes, ICollection<int> finishedMissionsIds, MissionProcessor missionProcessor, MissionLocation location,MissionDataCache missionDataCache)
        {
            _character = character;
            _agent = location.Agent;
            _periodicMissionTimes = periodicMissionTimes;
            _finishedMissionIds = finishedMissionsIds;
            _missionProcessor = missionProcessor;
            _location = location;
            _missionDataCache = missionDataCache;
        }

        private MissionInProgressCollector _collector;
        private bool _skipFind;

        private bool IsCollectorAvailable()
        {
            if (_skipFind)
            {
                return false;
            }

            if (_collector == null)
            {
                if (!_missionProcessor.MissionAdministrator.GetMissionInProgressCollector(_character, out _collector))
                {
                    _skipFind = true;
                    return false;
                }
            }

            return true;
        }


        public IEnumerable<Mission> GetConfigMissionsByCategoryAndLevel(MissionCategory missionCategory, int missionLevel)
        {
            return _agent.GetConfigMissionsByCategoryAndLevel(missionCategory, missionLevel);
        }

        public bool IsConfigMissionAvailable(Mission mission,IStandingHandler standingHandler, bool ignoreTriggerCheck = false)
        {
            //not listable
            if (!mission.listable)
            {
                return false;
            }

            if (mission.LocationId != _location.id)
            {
                //not at the requested location
                return false;
            }

            if (!ignoreTriggerCheck)
            {
                //the mission is member of a chain mission
                if (mission.isTriggered) return false;
            }

            if (mission.MissionLevel > 0)
            {
                if (!mission.MatchStandingToLevel(_character,standingHandler))
                {
                    //standing VS megacorp
                    return false;
                }
            }

            if (!mission.CheckPeriodicMissions(_periodicMissionTimes))
            {
                //mission was done within the period
                return false;
            }

            if (!mission.CheckRequiredStandingsToOtherAlliances(_character))
            {
                //standing check failed
                return false;
            }

            //a finished unique mission can't be listed any more
            if (mission.isUnique)
            {
                if (_finishedMissionIds.Contains(mission.id))
                {
                    return false;
                }
            }

            //required mission check
            if (mission.RequiredMissions.Any(missionId => !_finishedMissionIds.Contains(missionId)))
            {
                return false;
            }

            if (IsCollectorAvailable())
            {
                //currently this mission
                if (_collector.IsMissionCurrentlyRunning(mission.id))
                {
                    return false;
                }

                //if the current mission is not triggered BUT starter of a chain of missions
                if (!mission.isTriggered)
                {
                    //get all members of the chain
                    var storyLineMissionMembers = _missionDataCache.GetCompleteMissionLine(mission.id);

                    //check if any of the chain members are running
                    if (storyLineMissionMembers.Count > 0)
                    {
                        if (_collector.AnyMissionFromTheListRunning(storyLineMissionMembers))
                        {
                            return false;
                        }
                    }
                }
            }

            if (mission.RequiredExtensions.Any())
            {
                foreach (var requiredExtension in mission.RequiredExtensions)
                {
                    if (!_character.CheckLearnedExtension(requiredExtension))
                    {
                        return false;
                    }
                }
            }

            if (!CheckTutorialMissions(mission))
            {
                return false;
            }

            return true;
        }


        //
        //          TUTORIAL MISSION CHECK
        //

        private bool CheckTutorialMissions(Mission mission)
        {
            //GET BY NAME %%%

            /*
             
514 	
515
516 	

mission_tutorial_tm_stage_01 	
mission_tutorial_ics_stage_01 	
mission_tutorial_asi_stage_01

             */

            //the mission is a tutorial one
            if (mission.id == 915 || mission.id == 916 || mission.id == 917)
            {
                if (!_finishedMissionIds.IsNullOrEmpty())
                {
                    //and he already finished a tutorial
                    if (_finishedMissionIds.Contains(915) || _finishedMissionIds.Contains(916) || _finishedMissionIds.Contains(917))
                    {
                        return false;
                    }
                }

                if (_collector != null)
                {
                    if (_collector.IsMissionCurrentlyRunning(915) || _collector.IsMissionCurrentlyRunning(916) || _collector.IsMissionCurrentlyRunning(916))
                    {
                        return false;
                    }
                }
            }

            return true;
        }


        public bool IsMissionRunningWithThisCategoryAndLevel(MissionCategory category, int level, MissionAgent agent)
        {
            if (IsCollectorAvailable())
            {
                return _collector.AnyMissionOfCategotryAndLevelRunning(category, level, agent);
            }

            return false;
        }

        public bool IsRandomMissionAvailable(Mission mission, bool ignoreTriggerCheck = false)
        {
            //not listable
            if (!mission.listable)
            {
                return false;
            }

            if (!ignoreTriggerCheck)
            {
                //the mission is member of a chain mission
                if (mission.isTriggered) return false;
            }

           
            if (!mission.CheckPeriodicMissions(_periodicMissionTimes))
            {
                //mission was done within the period
                return false;
            }

            if (!mission.CheckRequiredStandingsToOtherAlliances(_character))
            {
                //standing check failed
                return false;
            }

            //a finished unique mission can't be listed any more
            if (mission.isUnique)
            {
                if (_finishedMissionIds.Contains(mission.id))
                {
                    return false;
                }
            }

            //required mission check
            if (mission.RequiredMissions.Any(missionId => !_finishedMissionIds.Contains(missionId)))
            {
                return false;
            }

            if (IsCollectorAvailable())
            {
                //currently this mission
                if (_collector.IsMissionCurrentlyRunning(mission.id))
                {
                    return false;
                }

                //if the current mission is not triggered BUT starter of a chain of missions
                if (!mission.isTriggered)
                {
                    //get all members of the chain
                    var storyLineMissionMembers = _missionDataCache.GetCompleteMissionLine(mission.id);

                    //check if any of the chain members are running
                    if (storyLineMissionMembers.Count > 0)
                    {
                        if (_collector.AnyMissionFromTheListRunning(storyLineMissionMembers))
                        {
                            return false;
                        }
                    }
                }
            }

            if (mission.RequiredExtensions.Any())
            {
                foreach (var requiredExtension in mission.RequiredExtensions)
                {
                    if (!_character.CheckLearnedExtension(requiredExtension))
                    {
                        return false;
                    }
                }
            }

            return true;

        }

        public bool IsMissionRunningWithThisCategory(MissionCategory missionCategory)
        {
            if (IsCollectorAvailable())
            {
                return _collector.AnyMissionOfCategotryRunning(missionCategory);
            }

            return false;

        }
    }
}
