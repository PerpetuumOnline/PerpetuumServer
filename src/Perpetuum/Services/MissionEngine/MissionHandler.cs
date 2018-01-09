using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Perpetuum.Accounting.Characters;
using Perpetuum.Items;
using Perpetuum.Log;
using Perpetuum.Players;
using Perpetuum.Services.MissionEngine.MissionProcessorObjects;
using Perpetuum.Services.MissionEngine.Missions;
using Perpetuum.Services.MissionEngine.MissionStructures;
using Perpetuum.Services.MissionEngine.MissionTargets;
using Perpetuum.Timers;
using Perpetuum.Zones;

namespace Perpetuum.Services.MissionEngine
{
    /// <summary>
    /// Runs in the zone plugin, handles the mission events
    /// 
    /// The concept is to do everything purely in the zone ram. 
    /// Gangmembers got from the zone -> they are online and on the same zone as me
    /// </summary>
    public class MissionHandler
    {
        private readonly ConcurrentDictionary<int,IZoneMissionTarget> _cachedMissionTargets = new ConcurrentDictionary<int,IZoneMissionTarget>();
        private readonly ConcurrentQueue<MissionEventInfo> _enqueuedMissionEventInfos = new ConcurrentQueue<MissionEventInfo>();
        private readonly ConcurrentDictionary<int,ZoneMissionInProgress> _runningMissions = new ConcurrentDictionary<int,ZoneMissionInProgress>();

        private readonly IZone _zone;
        private readonly Player _player;
        private readonly MissionProcessor _missionProcessor;

        public delegate MissionHandler Factory(IZone zone, Player player);

        public MissionProcessor MissionProcessor => _missionProcessor;

        public MissionHandler(IZone zone, Player player,MissionProcessor missionProcessor)
        {
            _zone = zone;
            _player = player;
            _missionProcessor = missionProcessor;
        }

        public bool AnyTargetForEventInfo(MissionEventInfo eventInfo)
        {
            foreach (var target in _cachedMissionTargets.Values)
            {
                if (target.IsMyTurn && !target.IsCompleted && target.MyTarget.Type == eventInfo.MissionTargetType && eventInfo.IsDefinitionMatching(target))
                {
                    return true;
                }
            }

            return false;
        }


        private IList<IZoneMissionTarget> CurrentTargets
        {
            get { return _cachedMissionTargets.Values.Where(t => t.IsMyTurn && !t.IsCompleted).ToList(); }
        }

        private IList<IZoneMissionTarget> CurrentTargetsByType(MissionTargetType missionTargetType)
        {
            return CurrentTargets.Where(t => t.MyTarget.Type == missionTargetType).ToList();
        }

        /// <summary>
        /// Looks for the displayOrder of a linked kill or lockunit target to use it as a mark
        /// The eventsource is a popNpc or a findArtifact
        /// 
        /// NOTE: lockUnit then kill... wont work properly. the mark will stay
        /// 
        /// </summary>
        /// <param name="eventSourceTarget"></param>
        /// <param name="targetOrder"></param>
        /// <returns></returns>
        public bool TryGetLinkedTargetOrder(IZoneMissionTarget eventSourceTarget, out int targetOrder)
        {
            IZoneMissionTarget linkedTarget = null;

            var sourceTargeType = eventSourceTarget.MyTarget.Type;

#if DEBUG
            Logger.Info("trying to get linket target's order for " + sourceTargeType + " " + eventSourceTarget.MyTarget);
#endif
            
            //do search based on assumptions

            if (eventSourceTarget.MyTarget.ValidPrimaryLinkSet)
            {
                //    source -> sometarget
                //    pop_npc/find_artifact ==> kill/lock_unit
                linkedTarget = _cachedMissionTargets.Values.FirstOrDefault(zt => eventSourceTarget.MyTarget.PrimaryDefinitionLinkId == zt.MyTarget.id && !zt.IsCompleted);
#if DEBUG
                if (linkedTarget != null)
                    Logger.Info("event source was linked to: " + linkedTarget.MyTarget);
#endif

            }

            if (linkedTarget == null)
            {
                //    sometarget -> source
                //    kill/lock_unit ==> pop_npc/find_artifact
                linkedTarget = _cachedMissionTargets.Values.FirstOrDefault(zt => zt.MyTarget.PrimaryDefinitionLinkId == eventSourceTarget.MyTarget.id && !zt.IsCompleted);

#if DEBUG
                if (linkedTarget != null)
                    Logger.Info("target was linked to event source: " + linkedTarget.MyTarget);
#endif
            }



            if (linkedTarget != null)
            {
                targetOrder = linkedTarget.MyTarget.displayOrder;
                return true;
            }

            targetOrder = 0;
#if DEBUG
            Logger.Warning("no linked target was found for: " + eventSourceTarget.MyTarget);
#endif

            return false;
        }



        public bool TryGetLinkedTargetOrderForContainer(IZoneMissionTarget eventSourceTarget, out int targetOrder)
        {
            IZoneMissionTarget linkedTarget = null;

#if DEBUG
            Logger.Info("trying to get linket target's order for " + eventSourceTarget.MyTarget.Type + " " + eventSourceTarget.MyTarget);
#endif
            
                //look for a loot item target which is primary linket to the event source
                linkedTarget = _cachedMissionTargets.Values.FirstOrDefault(zt =>
                    !zt.IsCompleted &&
                    zt.MyTarget.targetSecondaryAsMyPrimary &&
                    zt.MyTarget.Type == MissionTargetType.loot_item &&
                    zt.MyTarget.ValidPrimaryLinkSet &&
                    zt.MyTarget.PrimaryDefinitionLinkId == eventSourceTarget.MyTarget.id
                    );
#if DEBUG
            if (linkedTarget != null)
            {
                Logger.Info("the linked loot is: " + linkedTarget.MyTarget);
            }
#endif

            if (linkedTarget != null)
            {
                targetOrder = linkedTarget.MyTarget.displayOrder;
                return true;
            }

            targetOrder = -1;
#if DEBUG
            Logger.Warning("no linked target was found for: " + eventSourceTarget.MyTarget);
#endif

            return false;
        }




        /// <summary>
        /// Inits the missions.
        /// </summary>
        public void InitMissions()
        {
            foreach (var missionZoneInProgress in ZoneMissionInProgress.GetRunningMissionsSql(_zone, _player.Character.Id))
            {
                _runningMissions.Add(missionZoneInProgress.missionId, missionZoneInProgress);
                
                var targets = missionZoneInProgress.LoadZoneTargets(_zone.PresenceManager,_player).ToArray();

                foreach (var missionTarget in targets)
                {
                    _cachedMissionTargets.Add(missionTarget.Id, missionTarget);
                }
                
            }

            Logger.Info(_cachedMissionTargets.Count + " mission targets loaded for player:" + _player.Character.Id);
        }

        public void ResetMissionHandler()
        {
            _cachedMissionTargets.Clear();
            _enqueuedMissionEventInfos.Clear();
            _runningMissions.Clear();

            //itt kell szolni a playernek, hogy resetelve lettek a missionjei, kerjen uj datat
            Message.Builder.SetCommand(Commands.ZoneResetMissions).Send();

            Logger.Info("zone missions reset for " + _player.Character.Id);
        }



        /// <summary>
        /// Filters and enqueues a mission event info
        /// 
        /// This is where all zone targers pushed in for proccessing
        /// </summary>
        /// <param name="missionEventInfo"></param>
        public void EnqueueMissionEventInfo(MissionEventInfo missionEventInfo)
        {
            var mycharacter = _player.Character;
            var gang = _player.Gang;

            if (gang == null)
            {
                //enqueue for myself
                EnqueueMissionEventInfoLocally(missionEventInfo);
                return;
            }

            var membersOnZone = _zone.GetGangMembers(gang).ToList();

/*
            if (missionEventInfo.MissionTargetType == MissionTargetType.drill_mineral ||
                missionEventInfo.MissionTargetType == MissionTargetType.harvest_plant ||
                missionEventInfo.MissionTargetType == MissionTargetType.scan_mineral
                )
            
 */

            // check myself first
            if (AnyTargetForEventInfo(missionEventInfo))
            {

#if DEBUG                
                Logger.Info("   >>>> doer has the target, he comes first. " + _player.Character.Id + " " + missionEventInfo.MissionTargetType);
#endif
                EnqueueMissionEventInfoLocally(missionEventInfo);
                return;
            }


            foreach (var member in membersOnZone)
            {
                if (member.Character.Equals(mycharacter)) continue; //except for myself

                if (member.MissionHandler.AnyTargetForEventInfo(missionEventInfo))
                {
#if DEBUG
                    Logger.Info("   >>>> gm has it! " + member.Character.Id + " " + missionEventInfo.MissionTargetType);
#endif
                    member.MissionHandler.EnqueueMissionEventInfoLocally(missionEventInfo);
                    return;
                }
            }



#if DEBUG
            // pop_npc and reach position is always submitted to every gang member - don't log it
            if (!(missionEventInfo.MissionTargetType == MissionTargetType.pop_npc || missionEventInfo.MissionTargetType == MissionTargetType.reach_position))
                Logger.Warning("   >>>> ALL. " + _player.Character.Id + " " + missionEventInfo.MissionTargetType);
#endif

            //members including me
            foreach (var member in _zone.GetGangMembers(gang))
            {
                //ok, enqueue locally so he wont's spread it again to his gang members
                member.MissionHandler.EnqueueMissionEventInfoLocally(missionEventInfo);
            }

        }

        public void EnqueueMissionEventInfoLocally(MissionEventInfo missionEventInfo)
        {
            //has any mission?
            if (!HasAnyCachedTarget()) return;

            _enqueuedMissionEventInfos.Enqueue(missionEventInfo);
        }


        // ennyi idonkent nezi meg a mission queuet
        private readonly IntervalTimer _timerUpdateMissions = new IntervalTimer(TimeSpan.FromSeconds(1));

        public void Update(TimeSpan time)
        {
            if (_cachedMissionTargets.Count == 0 || _enqueuedMissionEventInfos.Count == 0)
                return;

            _timerUpdateMissions.Update(time);

            if (!_timerUpdateMissions.Passed) return;
            _timerUpdateMissions.Reset();

            MissionEventInfo missionEventInfo;

            while (_enqueuedMissionEventInfos.TryDequeue(out missionEventInfo))
            {
                foreach (var missionTarget in _cachedMissionTargets.Values)
                {
                    if (missionTarget == null) continue;

                    if (!missionTarget.HandleMissionEvent(missionEventInfo))
                        continue;

                    if (missionTarget.IsCompleted)
                    {
                        //the dequeued item finished a target
                        _cachedMissionTargets.Remove(missionTarget.Id);
                    }

                    //the event got used up
                    break;
                }
            }
        }

        public List<FindArtifactZoneTarget> GetArtifactTargets()
        {
            //from gang and me
            return CollectTargetsFromAllGangMembers(MissionTargetType.find_artifact).Where(t => !t.IsCompleted && t.IsMyTurn).OfType<FindArtifactZoneTarget>().ToList();

            //only mine
            //return _cachedMissionTargets.Values.OfType<FindArtifactZoneTarget>().Where(t => !t.IsCompleted && t.IsMyTurn).ToList();
        }

        

        public bool HasAnyCachedTarget()
        {
            return _cachedMissionTargets.Count > 0;
        }


        /// <summary>
        /// The mission engine sent a progress update to the zone. This function handles it.
        /// </summary>
        /// <param name="missionProgressUpdate"></param>
        /// <returns></returns>
        public void MissionAdvanceGroupOrder(MissionProgressUpdate missionProgressUpdate)
        {
            Logger.DebugInfo("advancing mission target group on zone: " + missionProgressUpdate);

            if (missionProgressUpdate.isFinished)
            {
                _runningMissions.Remove(missionProgressUpdate.missionId);

                var targetsToRemove = GetTargetsByProgressUpdate(missionProgressUpdate);

                foreach (var cachedMissionTarget in targetsToRemove)
                {
                    _cachedMissionTargets.Remove(cachedMissionTarget.Id);
                }
                Logger.DebugInfo("mission got finished and removed! " + missionProgressUpdate);
            }
            else
            {
                Logger.DebugInfo("mission targetorder advancing" + missionProgressUpdate);

                if (_runningMissions.TryGetValue(missionProgressUpdate.missionId,out ZoneMissionInProgress mzp))
                {
                    mzp.SetCurrentTargetOrder(missionProgressUpdate);
                }
            }
        }

        private IList<IZoneMissionTarget> GetTargetsByProgressUpdate(MissionProgressUpdate missionProgressUpdate)
        {
            var list = new List<IZoneMissionTarget>();
            foreach (var cachedMissionTarget in _cachedMissionTargets.Values)
            {
                if (cachedMissionTarget.MyZoneMissionInProgress.missionId == missionProgressUpdate.missionId)
                {
                    list.Add(cachedMissionTarget);
                }
            }

            return list;
        }

        /// <summary>
        /// The mission engine pushes a new mission
        /// </summary>
        /// <param name="missionProgressUpdate"></param>
        /// <returns></returns>
        public void MissionNew(MissionProgressUpdate missionProgressUpdate)
        {
            var missionZoneProgress = ZoneMissionInProgress.CreateFromProgressUpdate(_zone, missionProgressUpdate);
            
            _runningMissions[missionProgressUpdate.missionId] = missionZoneProgress;

            var targets = missionZoneProgress.LoadZoneTargets(_zone.PresenceManager,_player);

            foreach (var missionTarget in targets)
            {
                _cachedMissionTargets.Add(missionTarget.Id, missionTarget);
            }

            
        }


        /// <summary>
        /// This runs from within a request
        /// 
        /// It must be in a transaction since the process has to delete the submitted item
        /// </summary>
        /// <param name="kiosk"></param>
        /// <param name="itemToSubmit"></param>
        /// <param name="guid"></param>
        /// <returns></returns>
        public bool SubmitItemToKiosk(Kiosk kiosk, Item itemToSubmit, Guid guid)
        {
            var success = false;

            var submitTargets = CollectTargetsFromAllGangMembers(MissionTargetType.submit_item)
                .Cast<SubmitItemZoneTarget>().Where(t=>t.MyZoneMissionInProgress.missionGuid == guid);

            foreach (var targetSubmitItem in submitTargets)
            {
                if (!targetSubmitItem.IsMyTurn)
                    continue;

                if (targetSubmitItem.MyTarget.ValidMissionStructureEidSet)
                {
                    if (targetSubmitItem.MyTarget.MissionStructureEid != kiosk.Eid)
                    {
                        //not in this kiosk
                        continue;
                    }
                }

                var missionEvent = targetSubmitItem.CreateSubmitItemEventInfo(_player, kiosk, itemToSubmit);

                if (missionEvent != null)
                {
                    //progression happened
                    EnqueueMissionEventInfo(missionEvent);
                    success = true;
                    break;
                }
                
            }

            return success;
        }

        public List<IZoneMissionTarget> CollectTargetsFromAllGangMembers(MissionTargetType missionTargetType)
        {
            var targets = new List<IZoneMissionTarget>(CurrentTargetsByType(missionTargetType));

            var gang = _player.Gang;
            if (gang == null) 
                return targets;

            var affectedPlayers = _zone.GetGangMembers(gang).Where(p => p != _player).ToList();

            foreach (var targetsAtCurrentPlayer in affectedPlayers.Select(affectedPlayer => affectedPlayer.MissionHandler.CurrentTargetsByType(missionTargetType)))
            {
                targets.AddRange(targetsAtCurrentPlayer);
            }

            return targets;

        }


        public Dictionary<string, object> GetKioskMissionInfo(Kiosk kiosk, Guid guid)
        {
            var info = new Dictionary<string, object>();

            var targets = CollectTargetsFromAllGangMembers(MissionTargetType.submit_item);

            var counter = 0;
            foreach (var loadedTarget in targets)
            {
                if (loadedTarget.MyZoneMissionInProgress.missionGuid != guid)
                    continue;

                if (loadedTarget.MyTarget.ValidMissionStructureEidSet && loadedTarget.MyTarget.MissionStructureEid == kiosk.Eid)
                {
                    info.Add("m" + counter++, GetDefinitionAndQuantityInfo(loadedTarget));
                }
            }

            return info;
        }

        private static Dictionary<string,object> GetDefinitionAndQuantityInfo(IZoneMissionTarget target)
        {
            var info = new Dictionary<string,object>
            {
                {k.definition, target.MyTarget.Definition},
                {k.quantity, target.MyTarget.Quantity},
                {k.missionID, target.MyZoneMissionInProgress.missionId},
                {k.targetID, target.MyTarget.id},
                {k.guid, target.MyZoneMissionInProgress.missionGuid.ToString()}
            };

            return info;
        }


        /// <summary>
        /// Currently used by the NpcEgg
        /// 
        /// Makes sure that the NpcEgg is only deployed at the target's location
        /// </summary>
        /// <param name="sourcePosition"></param>
        /// <param name="definition"></param>
        /// <param name="missionTargetType"></param>
        /// <returns></returns>
        public bool IsMissionTargetLoadedForPositionAndDefinition(Position sourcePosition, int definition, MissionTargetType missionTargetType)
        {
            return _cachedMissionTargets.Values.Any(t =>
                t.MyTarget.Type == missionTargetType && t.IsMyTurn &&
                t.MyTarget.Definition == definition &&
                !t.IsCompleted &&
                t.MyTarget.CheckPosition &&
                sourcePosition.IsInRangeOf2D(t.MyTarget.targetPosition, t.MyTarget.TargetPositionRange)
                );
        }

        
        /// <summary>
        /// Compares by mission structure eid 
        /// "Is it the proper moment mission wise to use this structure?"
        /// </summary>
        /// <param name="missionStructure"></param>
        /// <returns></returns>
        public List<IZoneMissionTarget> GetTargetsForMissionStructure(MissionStructure missionStructure)
        {
            return 
            CollectTargetsFromAllGangMembers(missionStructure.TargetType)
                .Where(t => t.MyTarget.ValidMissionStructureEidSet && t.MyTarget.MissionStructureEid == missionStructure.Eid).ToList();


        }

        


        private int _enqueMissionTargetCounter;

        public void MissionUpdateOnTileChange()
        {
#if !DEBUG
            if (_zone.Configuration.Terraformable)
                return;
#endif

            if (_enqueMissionTargetCounter > 1)
            {
                _enqueMissionTargetCounter = 0;

                EnqueueMissionEventInfo(new ReachPositionEventInfo(_player, _player.CurrentPosition));
                EnqueueMissionEventInfo(new PopNpcEventInfo(_player, _player.CurrentPosition));

            }
            else
            {
                _enqueMissionTargetCounter++;
            }
        }

        public void SignalParticipationByLocking(Guid missionGuid)
        {
            //not in gang -> nothing to signal
            var gang = _player.Gang;
            if (gang == null)
                return;

            SignalParticipationAsync(_player.Character,missionGuid);
        }

        public void SignalParticipationAsync(Character doerCharacter, Guid missionGuid)
        {
            if (missionGuid == Guid.Empty)
                return;
            if (doerCharacter == Character.None)
                return;

            Task.Run(() =>
            {
                _missionProcessor.ServerAddsParticipant(missionGuid, doerCharacter);
            });
        }
    }
}