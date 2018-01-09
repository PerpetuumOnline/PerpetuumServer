using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Perpetuum.Accounting.Characters;
using Perpetuum.Log;
using Perpetuum.Services.MissionEngine.AdministratorObjects;
using Perpetuum.Services.MissionEngine.MissionDataCacheObjects;
using Perpetuum.Services.MissionEngine.Missions;
using Perpetuum.Services.MissionEngine.MissionStructures;
using Perpetuum.Services.MissionEngine.TransportAssignments;
using Perpetuum.Services.Standing;
using Perpetuum.Threading.Process;
using Perpetuum.Timers;

namespace Perpetuum.Services.MissionEngine.MissionProcessorObjects
{
    public partial class MissionProcessor : Process
    {
        private readonly MissionDataCache _missionDataCache;
        private readonly IStandingHandler _standingHandler;
        public MissionAdministrator MissionAdministrator { get; }

        public MissionProcessor(MissionDataCache missionDataCache,MissionAdministrator.Factory missionAdministratorFactory,IStandingHandler standingHandler)
        {
            MissionAdministrator = missionAdministratorFactory(this);
            _missionDataCache = missionDataCache;
            _standingHandler = standingHandler;
        }

        public override void Start()
        {
            MissionAdministrator.Initialize();
            base.Start();
        }

        private readonly IntervalTimer _transportAssignmentInterval = new IntervalTimer(TimeSpan.FromHours(1));

        public override void Update(TimeSpan time)
        {
            MissionAdministrator.Update(time);

            _transportAssignmentInterval.Update(time);

            if (!_transportAssignmentInterval.Passed)
                return;

            _transportAssignmentInterval.Reset();

            TransportAssignment.CleanUpExpiredAssignmentsAsync();
        }


        public Dictionary<string, object> RunningMissionList(Character character)
        {
            return MissionAdministrator.RunningMissionList(character);
        }

        public bool FindMissionInProgress(Character character, Guid missionGuid, out MissionInProgress missionInProgress)
        {
            return MissionAdministrator.FindMissionInProgressByMissionGuid(character, missionGuid, out missionInProgress);
        }


        public Task SendRunningMissionListAsync(Character character)
        {
            return Task.Run(() => SendRunningMissionList(character));
        }

        public void SendRunningMissionList(Character character)
        {
            Message.Builder.SetCommand(Commands.MissionListRunning)
                .WithData(RunningMissionList(character))
                .ToCharacter(character)
                .Send();
        }

        public void ServerAddsParticipant(Guid guid,Character doerCharacter)
        {
            var ownerCharacter = MissionHelper.FindMissionOwnerByGuid(guid);
            if (ownerCharacter == Character.None)
            {
                Logger.Error($" no mission owner character was found for guid:{guid.ToString()}");
                return;
            }

            if (doerCharacter == Character.None)
            {
                Logger.Error($" WTF? in MissionServerAddsParticipantHandler. doerCharacterId:{doerCharacter.Id} mission owner characterId:{ownerCharacter.Id}");
                return;
            }

            var gang = ownerCharacter.GetGang();
            if (gang == null)
                return;

            if (!gang.IsMember(doerCharacter))
                return;

            if (!FindMissionInProgress(ownerCharacter,guid,out MissionInProgress missionInProgress))
            {
                Logger.Error($" no mission was found for characterId:{ownerCharacter.Id} guid:{guid.ToString()}");
                return;
            }

            Logger.DebugInfo($"    >>>> missionGuid:{guid.ToString()} mission owner:{ownerCharacter.Id} new participant:{doerCharacter.Id}");
            missionInProgress.AddParticipant(doerCharacter);
        }

        public void NpcPresenceExpired(Character character,Guid missionGuid,int missionId,int targetId)
        {
            Logger.Info($"Npc presence expired, checking conditions {missionGuid.ToString()}");

            if (!FindMissionInProgress(character,missionGuid,out MissionInProgress missionInProgress))
            {
                Logger.Info($"MissionNpcPresenceExpired. Mission was not found, nothing to do. missionId: {missionId} character:{character.Id} guid:{missionGuid.ToString()}");
                return;
            }

            if (!missionInProgress.IsTargetLinked(targetId))
            {
                //target is not linked
                return;
            }

            AbortMissionByRequest(character,missionGuid,ErrorCodes.NPCsGoneMissionAborted);
        }

        public void MissionStartFromFieldTerminal(Character character,int locationId,MissionCategory missionCategory,int level)
        {
            try
            {
                _missionDataCache.GetLocationById(locationId,out MissionLocation location).ThrowIfNull(ErrorCodes.ItemNotFound);
                var result = MissionStartForRequest(character,missionCategory,level,location);
                Message.Builder.ToCharacter(character).WithData(result).SetCommand(Commands.MissionStart).Send();
            }
            catch (PerpetuumException exception)
            {
                Logger.Exception(exception);
                character.CreateErrorMessage(Commands.MissionStart,exception.error).Send();
            }
        }

    }
}
