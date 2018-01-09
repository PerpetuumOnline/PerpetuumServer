using System;
using Perpetuum.Accounting.Characters;
using Perpetuum.Log;
using Perpetuum.Services.MissionEngine.Missions;

namespace Perpetuum.Services.MissionEngine.MissionProcessorObjects
{
    public partial class MissionProcessor
    {

        public void AbortMissionByRequest(Character character, Guid missionGuid, ErrorCodes errorToInfo)
        {
            Logger.Info("Aborting mission " + missionGuid + " character: " + character.Id);

            (MissionAdministrator.RunningMissionsCount(character) == 0).ThrowIfTrue(ErrorCodes.ItemNotFound);

            MissionInProgress missionInProgress;
            MissionAdministrator.FindMissionInProgressByMissionGuid(character, missionGuid, out missionInProgress).ThrowIfFalse(ErrorCodes.ItemNotFound);

            missionInProgress?.OnMissionAbort(this,errorToInfo);
        }


    }
}
