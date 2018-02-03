using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Perpetuum.Deployers;
using Perpetuum.ExportedTypes;
using Perpetuum.Log;
using Perpetuum.Players;
using Perpetuum.Services.MissionEngine.MissionTargets;
using Perpetuum.Units;
using Perpetuum.Zones;
using Perpetuum.Zones.Blobs.BlobEmitters;
using Perpetuum.Zones.NpcSystem.Presences;
using Perpetuum.Zones.Teleporting.Strategies;

namespace Perpetuum.Services.RiftSystem
{
    public abstract class Portal : Unit,IUsableItem
    {
        private UnitDespawnHelper _despawnHelper;

        public void SetDespawnTime(TimeSpan despawnTime)
        {
            _despawnHelper = UnitDespawnHelper.Create(this, despawnTime);
        }

        protected override void OnUpdate(TimeSpan time)
        {
            _despawnHelper?.Update(time, this);
            base.OnUpdate(time);
        }

        public virtual void UseItem(Player player)
        {
            player.HasTeleportSicknessEffect.ThrowIfTrue(ErrorCodes.TeleportTimerStillRunning);
            player.HasPvpEffect.ThrowIfTrue(ErrorCodes.CantBeUsedInPvp);
            player.CurrentPosition.IsInRangeOf3D(CurrentPosition, 8).ThrowIfFalse(ErrorCodes.TeleportOutOfRange);
        }
    }

    public class RandomRiftPortal : Portal
    {
        private readonly ITeleportStrategyFactories _teleportStrategyFactories;
        private Position _targetPosition;

        public RandomRiftPortal(ITeleportStrategyFactories teleportStrategyFactories)
        {
            _teleportStrategyFactories = teleportStrategyFactories;
        }

        protected override void OnEnterZone(IZone zone, ZoneEnterType enterType)
        {
            var rift = Zone.Units.OfType<Rift>().RandomElement();
            if (rift != null)
            {
                _targetPosition = rift.CurrentPosition;
            }

            base.OnEnterZone(zone, enterType);
        }

        public override void UseItem(Player player)
        {
            base.UseItem(player);

            var teleport = _teleportStrategyFactories.TeleportWithinZoneFactory();
            teleport.TargetPosition = _targetPosition;
            teleport.ApplyTeleportSickness = true;
            teleport.ApplyInvulnerable = true;
            teleport.DoTeleportAsync(player);
        }
    }
    
    public class RiftNpcGroupInfo
    {
        public int presenceID;
        public TimeSpan presenceLifeTime;
        public int wavesCount;
        public Player ownerPlayer;
    }

    public class RiftActivator : ItemDeployerBase
    {
        public override void Deploy(IZone zone, Player player)
        {
            var rift = player.GetUnitsWithinRange<Rift>(5).FirstOrDefault();
            if (rift == null)
                throw new PerpetuumException(ErrorCodes.RiftOutOfRange);

            var maxRiftLevel = 1;

            if (zone.Configuration.IsAlpha)
                maxRiftLevel = 1;
            else if (zone.Configuration.IsBeta)
                maxRiftLevel = 2;
            else if (zone.Configuration.IsGamma)
                maxRiftLevel = 3;

            if (ED.Tier.level > maxRiftLevel)
                throw new PerpetuumException(ErrorCodes.RiftLevelMismatch);

            var info = new RiftNpcGroupInfo();
            Debug.Assert(DeployableItemEntityDefault.Config.npcPresenceId != null, "DeployableItemEntityDefault.Config.npcPresenceId != null");
            info.presenceID = (int)DeployableItemEntityDefault.Config.npcPresenceId;
            Debug.Assert(DeployableItemEntityDefault.Config.lifeTime != null, "DeployableItemEntityDefault.Config.lifeTime != null");
            info.presenceLifeTime = TimeSpan.FromMilliseconds((double)DeployableItemEntityDefault.Config.lifeTime);
            Debug.Assert(DeployableItemEntityDefault.Config.waves != null, "DeployableItemEntityDefault.Config.waves != null");
            info.wavesCount = (int) DeployableItemEntityDefault.Config.waves;
            info.ownerPlayer = player;

            if (!rift.TryActivate(info))
                throw new PerpetuumException(ErrorCodes.WTFErrorMedicalAttentionSuggested);

            LogTransaction(player,this);

            var seei = new SummonEggEventInfo(player,DeployableItemEntityDefault.Definition, player.CurrentPosition);
            player.MissionHandler.EnqueueMissionEventInfo(seei);
        }
    }

    public class Rift : Unit,IUsableItem,IBlobEmitter
    {
        private readonly ITeleportStrategyFactories _teleportStrategyFactories;
        private UnitDespawnHelper _despawnHelper;
        private readonly BlobEmitter _blobEmitter;

        public Rift(ITeleportStrategyFactories teleportStrategyFactories)
        {
            _teleportStrategyFactories = teleportStrategyFactories;
            _blobEmitter = new BlobEmitter(this);
        }

        public bool IsDestinationStronghold { get; set; } = false;

        public void SetDespawnTime(TimeSpan despawnTime)
        {
            _despawnHelper = UnitDespawnHelper.Create(this,despawnTime);
        }

        protected override void OnUpdate(TimeSpan time)
        {
            _despawnHelper?.Update(time, this);
            base.OnUpdate(time);
        }

        private int _activated;

        public bool TryActivate(RiftNpcGroupInfo info)
        {
            var zone = Zone;
            if (zone == null)
                return false;
            
            if (Interlocked.CompareExchange(ref _activated, 1, 0) == 1)
                return false;

            zone.CreateBeam(BeamType.npc_egg_beam, builder => builder.WithPosition(CurrentPosition).WithDuration(6000));

            Task.Delay(6000).ContinueWith(t =>
            {
                var presence = (DynamicPoolPresence)zone.PresenceManager.CreatePresence(info.presenceID);
                presence.DynamicPosition = CurrentPosition;
                presence.LifeTime = info.presenceLifeTime;
                presence.Summoner = info.ownerPlayer.Character;
                presence.Init(info.wavesCount);

                zone.PresenceManager.AddPresence(presence);
                RemoveFromZone();
            });

            Logger.Info("Rift activated.");
            return true;
        }

        public void UseItem(Player player)
        {
            player.HasTeleportSicknessEffect.ThrowIfTrue(ErrorCodes.TeleportTimerStillRunning);
            player.HasPvpEffect.ThrowIfTrue(ErrorCodes.CantBeUsedInPvp);

            IZone destination = player.Character.GetZone(16);

            // teleport player to stronghold
            // stronghold zone must be active.
            if (this.IsDestinationStronghold && destination != null)
            {
                var teleport = _teleportStrategyFactories.TeleportToAnotherZoneFactory(destination);
                teleport.TargetPosition = new Position(1120, 1039);
                teleport.DoTeleportAsync(player);
            }
            else
            {
                player.CurrentPosition.IsInRangeOf3D(CurrentPosition, 8).ThrowIfFalse(ErrorCodes.TeleportOutOfRange);

                var nearestRift = Zone.Units.OfType<Rift>().Where(rift => rift != this).GetNearestUnit(CurrentPosition);
                if (nearestRift == null)
                    throw new PerpetuumException(ErrorCodes.WTFErrorMedicalAttentionSuggested);

                var teleport = _teleportStrategyFactories.TeleportWithinZoneFactory();
                teleport.TargetPosition = nearestRift.CurrentPosition;
                teleport.ApplyTeleportSickness = true;
                teleport.ApplyInvulnerable = true;
                teleport.DoTeleportAsync(player);
            }
        }

        public double BlobEmission => _blobEmitter.BlobEmission;

        public double BlobEmissionRadius => _blobEmitter.BlobEmissionRadius;
    }
}