using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Perpetuum.Services.MissionEngine.Missions;

namespace Perpetuum.Services.MissionEngine.AdministratorObjects
{
    public class MissionInProgressCollector
    {
        private readonly ConcurrentDictionary<int, MissionInProgress> _missionInProgress = new ConcurrentDictionary<int, MissionInProgress>();


        public IEnumerable<MissionInProgress> GetMissionsInProgress()
        {
            return _missionInProgress.Values;
        }

        public void AddMissionInProgress(MissionInProgress missionInProgress)
        {
            _missionInProgress.Add(missionInProgress.MissionId, missionInProgress);
        }

        public bool RemoveMissionInProgress(MissionInProgress missionInProgress)
        {
            return _missionInProgress.Remove(missionInProgress.MissionId);
        }

        public int NofRunningMissions()
        {
            return _missionInProgress.Count;
        }

        public void Reset()
        {
            _missionInProgress.Clear();
        }


        public bool IsMissionCurrentlyRunning(int missionId)
        {
            return _missionInProgress.Values.Any(p => p.MissionId == missionId);
        }

        public bool AnyMissionFromTheListRunning(List<int> checkList)
        {
            return _missionInProgress.Values.Any
                (
                    p => checkList.Contains(p.MissionId)
                );
        }

        public bool AnyMissionOfCategotryAndLevelRunning(MissionCategory missionCategory, int missionLevel, MissionAgent agent)
        {
            return _missionInProgress.Values.Any
                (
                    p =>
                        (
                            p.MissionLevel == missionLevel &&
                            p.myMission.missionCategory == missionCategory &&
                            p.myLocation.Agent.id == agent.id
                            )
                );
        }

        public bool AnyMissionOfCategotryRunning(MissionCategory missionCategory)
        {
            return _missionInProgress.Values.Any(p => p.myMission.missionCategory == missionCategory);

        }
    }
}
