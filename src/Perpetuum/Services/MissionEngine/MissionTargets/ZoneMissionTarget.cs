using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Perpetuum.Accounting.Characters;
using Perpetuum.Data;
using Perpetuum.ExportedTypes;
using Perpetuum.IDGenerators;
using Perpetuum.Log;
using Perpetuum.Players;
using Perpetuum.Services.Looting;
using Perpetuum.Services.MissionEngine.MissionProcessorObjects;
using Perpetuum.Services.MissionEngine.Missions;
using Perpetuum.Units;
using Perpetuum.Zones;
using Perpetuum.Zones.NpcSystem;
using Perpetuum.Zones.NpcSystem.Flocks;
using Perpetuum.Zones.NpcSystem.Presences;

namespace Perpetuum.Services.MissionEngine.MissionTargets
{
    public interface IZoneMissionTarget
    {
        [NotNull]
        MissionTarget MyTarget { get; }

        ZoneMissionInProgress MyZoneMissionInProgress { get; }
        bool IsMyTurn { get; }
        bool IsCompleted { get; }
        int Id { get; }
        bool HandleMissionEvent(MissionEventInfo missionEventInfo);
    }

    public abstract class ZoneMissionTarget
    {
        private static readonly IIDGenerator<int> _idGenerator = IDGenerator.CreateIntIDGenerator();

        public static MissionProcessor MissionProcessor { get; set; }
        public static PresenceFactory PresenceFactory { get; set; }

        public int Id { get; }

        protected ZoneMissionTarget()
        {
            Id = _idGenerator.GetNextID();
        }

        /// <summary>
        ///     Inform the mission engine that a target has been advanced
        /// </summary>
        protected void SendReportToMissionEngine()
        {
            MissionProcessor.MissionTargetAdvancedAsync(ToDictionary());
        }

        protected abstract Dictionary<string, object> ToDictionary();
    }

    public class ProgressCounter
    {
        private int _current;
        private int _updateCounter;

        public ProgressCounter(int current, int max)
        {
            _current = current;
            MaxValue = max;
        }

        public ProgressCounter(int max) : this(0, max)
        {
        }

        public int Current
        {
            get { return _current; }
            set
            {
                _current = value.Clamp(0,MaxValue);
                _updateCounter++;
            }
        }

        public int MaxValue { get; private set; }

        public bool IsCompleted
        {
            get { return Current >= MaxValue; }
        }

        public void AddToDictionary(Dictionary<string, object> info)
        {
            info.Add(k.progressCount, _current);
        }

        public bool IsEveryNTurn(int n = 1)
        {
            return _updateCounter%n == 1;
        }

        public override string ToString()
        {
            return $"Current: {Current}, MaxValue: {MaxValue}, UpdateCounter: {_updateCounter}";
        }
    }


    /// <summary>
    ///     Abstract class to handle a mission target on zone
    /// </summary>
    public abstract class ZoneMissionTarget<T> : ZoneMissionTarget, IZoneMissionTarget where T : MissionEventInfo
    {
        public Player Player { get; }

        //current progress of the target

        private T _lastEventInfo;
        protected T successEventInfo;

        protected ZoneMissionTarget(IZone zone, Player player, MissionTarget target, ZoneMissionInProgress zoneMissionInProgress)
        {
            Zone = zone;

            MyZoneMissionInProgress = zoneMissionInProgress;
            Player = player;
            MyTarget = target;
        }

        [NotNull]
        public IZone Zone { get; }

        public ZoneMissionInProgress MyZoneMissionInProgress { get; private set; }
        public bool IsCompleted { get; private set; }
        public MissionTarget MyTarget { get; set; }

        public bool HandleMissionEvent(MissionEventInfo eventInfo)
        {
            var e = eventInfo as T;
            if (e == null)
                return false;

            if (eventInfo.MissionTargetType != MyTarget.Type)
                return false;

            if (IsCompleted)
                return false;

            if (!IsMyTurn)
                return false;

            _lastEventInfo = e;

            if (!CanHandleMissionEvent(e))
                return false;

            OnHandleMissionEvent(e);
            return true;
        }

        public bool IsMyTurn => MyZoneMissionInProgress.currentTargetOrder == MyTarget.targetOrder;

        protected override Dictionary<string, object> ToDictionary()
        {
            var dict = new Dictionary<string, object>
            {
                {k.missionID, MyZoneMissionInProgress.missionId},
                {k.type, MyTarget.Type},
                {k.completed, IsCompleted},
                {k.targetID, MyTarget.id},
                {k.characterID, Player.Character.Id},
                {k.guid,MyZoneMissionInProgress.missionGuid},
                
            };
            
            if (_lastEventInfo?.Player != null)
            {
                var doerCharacter = _lastEventInfo.Player.Character;

                if (!doerCharacter.Equals(Player.Character))
                {
                    if (IsInSameGang(Player, _lastEventInfo.Player))
                    {
                        //assisting \o/
                        dict[k.assistingCharacterID] = doerCharacter.Id;
                    }
                }
            }
            
            if (successEventInfo != null)
            {
                if (successEventInfo.Player.Character != Character.None && !successEventInfo.Player.Character.Equals(Player.Character))
                {
                    if (IsInSameGang(Player, successEventInfo.Player))
                    {
                        dict[k.assistingCharacterID] = successEventInfo.Player.Character.Id;    
                    }
                }

                //it was a success. lets send the success position and zoneid
                dict.Add(k.position, successEventInfo.Position);
                dict.Add(k.zoneID, Zone.Id);
            }

#if DEBUG
            if (dict.ContainsKey(k.assistingCharacterID))
            {
                Logger.Info("    >>>> -assist- " + MyTarget.Type);
            }
            else
            {
                Logger.Info("    >>>> -normal- " + MyTarget.Type);
            }
#endif

            return dict;
        }

        private bool IsInSameGang(Player missionOwner, Player doerPlayer)
        {
            var gang = missionOwner.Gang;
            if (gang == null)
                return false;

            return gang.IsMember(doerPlayer.Character);
        }

        protected abstract bool CanHandleMissionEvent(T eventInfo);

        protected virtual void OnTargetComplete()
        {
            IsCompleted = true;
            successEventInfo = _lastEventInfo;
        }

        protected abstract void OnHandleMissionEvent(T missionEventInfo);

        public override string ToString()
        {
            return $"id: {Id} characterID: {Player.Character.Id} " +
                   $"missionID: {MyZoneMissionInProgress.missionId} " +
                   $"targetOrder: {MyTarget.targetOrder} " +
                   $"type: {MyTarget.Type} ";
        }

        /// <summary>
        ///     Is the caller mission event is valid against the set parameters
        /// </summary>
        /// <param name="currentP"></param>
        /// <returns></returns>
        protected bool IsZoneOrPositionValid(Position currentP)
        {
            if (!MyTarget.ValidZoneSet)
                return true;
            //zone set

            if (!MyTarget.ValidPositionSet)
                return true;

            //position specified, it has to happen on the current zone
            if (Zone.Id != MyTarget.ZoneId)
            {
                return false; //happened on a different zone, sorry
            }

            if (MyTarget.CheckPosition)
            {
                //range check finally
                return MyTarget.targetPosition.IsInRangeOf2D(currentP, MyTarget.TargetPositionRange);
            }

            return true;
        }

        public void DropLootFromSecondaryDefinition(IZone zone, Position position)
        {
            //mission loot needed?
            if (!MyTarget.IsSecondaryItemSet)
                return;

            var linkedDisplayOrder = GetAttachedDisplayOrderForContainer();

            Db.CreateTransactionAsync(scope =>
            {
                DropLootFromSecondaryDefinitionToZone(zone, position, linkedDisplayOrder);
            });
        }
        
        private int GetAttachedDisplayOrderForContainer()
        {
            if (Player.MissionHandler.TryGetLinkedTargetOrderForContainer(this, out int linkedDisplayOrder))
            {
                return linkedDisplayOrder;
            }

            return -1;
        }

        
        private int GetAttachedDisplayOrderForNpc()
        {
            if (Player.MissionHandler.TryGetLinkedTargetOrder(this, out int linkedDisplayOrder))
            {
                return linkedDisplayOrder;
            }

            return -1;
        }

        private void DropLootFromSecondaryDefinitionToZone(IZone zone, Position position, int linkedDisplayOrder)
        {
            var lootDefinition = MyTarget.SecondaryDefinition;
            var lootQuantity = MyTarget.SecondaryQuantity;

            var container = (MissionContainer)LootContainer.Create()
                .SetType(LootContainerType.Mission)
                .SetOwner(Player)
                .AddLoot(lootDefinition, lootQuantity)
                .BuildAndAddToZone(zone, position);
            Debug.Assert(container != null, "container != null");

            AttachToUnit(container, MyZoneMissionInProgress.missionGuid,linkedDisplayOrder);
        }

        private void AttachToNpc(Npc npc)
        {
            AttachToUnit(npc, MyZoneMissionInProgress.missionGuid, _npcLinkedDisplayOrder);
        }

        private void AttachToUnit(Unit unit, Guid missionGuid, int linkedDisplayOrder)
        {
            if (linkedDisplayOrder < 0) return;

            unit.OptionalProperties.Add(new ReadOnlyOptionalProperty<Guid>(UnitDataType.MissionGuid, missionGuid));
            unit.OptionalProperties.Add(new ReadOnlyOptionalProperty<int>(UnitDataType.MissionDisplayOrder, linkedDisplayOrder));
        }

        private int _npcLinkedDisplayOrder;

        /// <summary>
        ///     Spawns npcs from the primary definition/quantity
        /// </summary>
        /// <param name="presenceManager"></param>
        /// <param name="position"></param>
        protected void SpawnNpcOnSuccess(IPresenceManager presenceManager,Position position)
        {
            if (!MyTarget.ValidQuantitySet)
                return;

            _npcLinkedDisplayOrder = GetAttachedDisplayOrderForNpc();

            Task.Run(() =>
            {
                var p = AddDirectPresenceToPosition(presenceManager,position);

                var playersToThreat = new List<Player>() {Player};

                var gang = Player.Gang;
                if (gang != null)
                {
                    playersToThreat.AddRange(Zone.GetGangMembers(gang));
                }
                foreach (var npc in p.Flocks.GetMembers())
                {
                    npc.Tag(Player,TimeSpan.FromHours(1));//mission presence-ben hosszu idore vannak taggelve

                    foreach (var threatPlayer in playersToThreat)
                    {
                        npc.AddDirectThreat(threatPlayer, 10 + FastRandom.NextDouble(0, 30));    
                    }
                    
                }

                Zone.CreateBeam(BeamType.teleport_storm, builder => builder.WithPosition(position).WithDuration(100000));
            }).LogExceptions();
        }

        public const int RANDOM_POP_NPC_LIFETIME_MINUTES = 25;

        private DirectPresence AddDirectPresenceToPosition(IPresenceManager presenceManager,Position successPosition)
        {
            //kamu presence config
            var configuration = new DirectPresenceConfiguration(Zone);

            //ez meg majd felepiti a flockokat ami kell, a popNpc alapjan
            var directPresence = (DirectPresence)PresenceFactory(Zone, configuration);
            directPresence.MissionTarget = this;
            directPresence.DynamicPosition = successPosition;
            directPresence.LifeTime = TimeSpan.FromMinutes(RANDOM_POP_NPC_LIFETIME_MINUTES);
            directPresence.LoadFlocks(); //ez csinalja meg a flockokat tenylegesen

            foreach (var flock in directPresence.Flocks)
            {
                flock.NpcCreated += AttachToNpc;
            }

            directPresence.Flocks.SpawnAllMembers();
            presenceManager.AddPresence(directPresence);
            directPresence.PresenceExpired += OnPresenceExpired;
            return directPresence;
        }

        private void OnPresenceExpired(Presence presence)
        {
            Db.CreateTransactionAsync(scope =>
            {
                Player.MissionHandler.MissionProcessor.NpcPresenceExpired(Player.Character,MyZoneMissionInProgress.missionGuid,MyZoneMissionInProgress.missionId,MyTarget.id);
                Logger.Info("Mission NPC presence expired " + MyZoneMissionInProgress + " onwer character:" + Player.Character.Id);
            });
        }

        [Conditional("DEBUG")]
        public void Log(string message)
        {
            Logger.DebugInfo("--- zt -> " + message);
        }
    }
}