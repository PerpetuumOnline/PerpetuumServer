using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Perpetuum.Accounting.Characters;
using Perpetuum.Builders;
using Perpetuum.Collections.Spatial;
using Perpetuum.Common.Loggers.Transaction;
using Perpetuum.Containers;
using Perpetuum.Containers.SystemContainers;
using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Groups.Corporations;
using Perpetuum.Groups.Gangs;
using Perpetuum.Items;
using Perpetuum.Log;
using Perpetuum.Modules;
using Perpetuum.Robots;
using Perpetuum.Services.ExtensionService;
using Perpetuum.Services.Looting;
using Perpetuum.Services.MissionEngine;
using Perpetuum.Services.MissionEngine.MissionTargets;
using Perpetuum.Services.MissionEngine.TransportAssignments;
using Perpetuum.Timers;
using Perpetuum.Units;
using Perpetuum.Units.DockingBases;
using Perpetuum.Zones;
using Perpetuum.Zones.Beams;
using Perpetuum.Zones.Blobs;
using Perpetuum.Zones.Blobs.BlobEmitters;
using Perpetuum.Zones.CombatLogs;
using Perpetuum.Zones.DamageProcessors;
using Perpetuum.Zones.Effects;
using Perpetuum.Zones.Finders;
using Perpetuum.Zones.Finders.PositionFinders;
using Perpetuum.Zones.Locking;
using Perpetuum.Zones.Locking.Locks;
using Perpetuum.Zones.NpcSystem;
using Perpetuum.Zones.PBS;
using Perpetuum.Zones.PlantTools;
using Perpetuum.Zones.ProximityProbes;
using Perpetuum.Zones.Teleporting;
using Perpetuum.Zones.Teleporting.Strategies;
using Perpetuum.Zones.Terrains;

namespace Perpetuum.Players
{
    public class PlayerMovement
    {
        private static readonly TimeSpan _minStepTime = TimeSpan.FromMilliseconds(50);
        private static readonly TimeSpan _maxElapsedTime = TimeSpan.FromSeconds(1);
        private readonly Player _player;

        public PlayerMovement(Player player)
        {
            _player = player;
        }

        public void Update(TimeSpan elapsed)
        {
            var speed = _player.Speed;
            if (speed <= 0.0)
                return;

            elapsed = elapsed.Min(_maxElapsedTime);

            var angle = _player.Direction * MathHelper.PI2;
            var vx = Math.Sin(angle) * speed;
            var vy = Math.Cos(angle) * speed;

            var px = _player.CurrentPosition.X;
            var py = _player.CurrentPosition.Y;

            while (elapsed > TimeSpan.Zero)
            {
                var time = _minStepTime;

                if (elapsed < _minStepTime)
                    time = elapsed;

                elapsed -= _minStepTime;

                var nx = px + (vx * time.TotalSeconds);
                var ny = py - (vy * time.TotalSeconds);

                var dx = (int)px - (int)nx;
                var dy = (int)py - (int)ny;

                if (dx != 0 || dy != 0)
                {
                    // csak akkor kell ha csempevaltas volt
                    if (!_player.IsWalkable((int)nx, (int)ny))
                    {
                        //_player.Zone.CreateAlignedDebugBeam(BeamType.red_10sec, new Position(nx, ny));
                        break;
                    }
                }

                px = nx;
                py = ny;
                //_player.Zone.CreateAlignedDebugBeam(BeamType.blue_10sec, new Position(px, py));
            }
            _player.TryMove(new Position(px, py));
            //_player.Zone.CreateAlignedDebugBeam(BeamType.orange_10sec, new Position(px, py));
        }
    }

    public class ErrorPacketBuilder : IBuilder<Packet>
    {
        private readonly ErrorCodes _error;

        public ErrorPacketBuilder(ErrorCodes error)
        {
            _error = error;
        }

        public Packet Build()
        {
            return new Packet(ZoneCommand.Error) {Error = _error};
        }
    }

    public sealed class Player : Robot,IBlobableUnit,IBlobEmitter
    {
        private readonly IExtensionReader _extensionReader;
        private readonly ICorporationManager _corporationManager;
        private readonly MissionHandler.Factory _missionHandlerFactory;
        private readonly ITeleportStrategyFactories _teleportStrategyFactories;
        private readonly DockingBaseHelper _dockingBaseHelper;
        private readonly CombatLogger.Factory _combatLoggerFactory;
        private readonly IBlobEmitter _blobEmitter;
        private readonly BlobHandler<Player> _blobHandler;
        private readonly PlayerMovement _movement;
        private CombatLogger _combatLogger;
        private PlayerMoveCheckQueue _check;

        public Player(IExtensionReader extensionReader,
            ICorporationManager corporationManager,
            MissionHandler.Factory missionHandlerFactory,
            ITeleportStrategyFactories teleportStrategyFactories,
            DockingBaseHelper dockingBaseHelper,
            CombatLogger.Factory combatLoggerFactory
            )
        {
            _extensionReader = extensionReader;
            _corporationManager = corporationManager;
            _missionHandlerFactory = missionHandlerFactory;
            _teleportStrategyFactories = teleportStrategyFactories;
            _dockingBaseHelper = dockingBaseHelper;
            _combatLoggerFactory = combatLoggerFactory;
            Session = ZoneSession.None;
            _movement = new PlayerMovement(this);

            _blobEmitter = new BlobEmitter(this);
            _blobHandler = new BlobHandler<Player>(this);
        }

        public long CorporationEid { get; set; }
        public IZoneSession Session { get; private set; }
        public Character Character { get; set; } = Character.None;
        public bool HasGMStealth { get; set; }

        public bool TryMove(Position position)
        {
            if (!IsWalkable(position))
                return false;

            _check.EnqueueMove(position);

            CurrentPosition = position;
            return true;
        }

        public void SetSession(IZoneSession session)
        {
            Session = session;
        }

        public MissionHandler MissionHandler { get; private set; }

        private bool HasAggressorEffect
        {
            get { return EffectHandler.ContainsEffect(EffectType.effect_aggressor); }
        }

        public bool HasSelfTeleportEnablerEffect
        {
            get { return EffectHandler.ContainsEffect(EffectType.effect_teleport_self_enabler); }
        }

        public Gang Gang { get; set; }

        public bool IsInSafeArea
        {
            get
            {
                var zone = Zone;
                if (zone == null)
                    return false;

                return zone.Configuration.Protected || EffectHandler.ContainsEffect(EffectType.effect_syndicate_area) || EffectHandler.ContainsEffect(EffectType.effect_safe_spot);
            }
        }

        public override bool IsLockable
        {
            get
            {
                var isInvulnerable = IsInvulnerable;

                if (isInvulnerable)
                    return false;

                return base.IsLockable;
            }
        }

        public IBlobHandler BlobHandler
        {
            get { return _blobHandler; }
        }

        public double BlobEmission
        {
            get { return _blobEmitter.BlobEmission; }
        }

        public double BlobEmissionRadius
        {
            get { return _blobEmitter.BlobEmissionRadius; }
        }


        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
                base.AcceptVisitor(visitor);
        }

        protected override void OnEnterZone(IZone zone, ZoneEnterType enterType)
        {
            base.OnEnterZone(zone, enterType); //aa
            _check = PlayerMoveCheckQueue.Create(this, CurrentPosition);

            zone.SendPacketToGang(Gang, new GangUpdatePacketBuilder(Visibility.Visible, this));
            
            MissionHandler = _missionHandlerFactory(zone, this);
            MissionHandler.InitMissions();

            Direction = FastRandom.NextDouble(); 

            var p = DynamicProperties.GetProperty<int>(k.pvpRemaining);
            if (!p.HasValue)
                return;

            ApplyPvPEffect(TimeSpan.FromMilliseconds(p.Value));
            p.Clear();
        }

        protected override void OnRemovedFromZone(IZone zone)
        {
            Session.SendPacket(ExitPacketBuilder);
            zone.SendPacketToGang(Gang, new GangUpdatePacketBuilder(Visibility.Invisible, this));

            _check.StopAndDispose();

            if (!States.LocalTeleport)
                Session.Stop();

            base.OnRemovedFromZone(zone);
        }

        public override void OnUpdateToDb()
        {
            try
            {
                if (!IsRepackaged)
                {
                    DynamicProperties.Update(k.armor, Armor.Ratio(ArmorMax));
                    DynamicProperties.Update(k.currentCore, Core);
                }

                var zone = Zone;
                if (zone == null || States.Dead)
                    return;

                var character = Character;
                character.ZoneId = zone.Id;
                character.ZonePosition = CurrentPosition;

                var p = DynamicProperties.GetProperty<int>(k.pvpRemaining);

                var pvpEffect = EffectHandler.GetEffectsByType(EffectType.effect_pvp).FirstOrDefault();
                if (pvpEffect == null)
                {
                    p.Clear();
                    return;
                }

                var effectTimer = pvpEffect.Timer;
                if (effectTimer != null)
                    p.Value = (int)effectTimer.Remaining.TotalMilliseconds;
            }
            finally
            {
                base.OnUpdateToDb();
            }
        }

        protected override void OnUpdate(TimeSpan time)
        {
            base.OnUpdate(time);
            UpdateCombat(time);
            _movement.Update(time);
            _blobHandler.Update(time);
            MissionHandler.Update(time);

            _combatLogger?.Update(time);
        }

        public void SendModuleProcessError(Module module, ErrorCodes error)
        {
            var packet = new Packet(ZoneCommand.ModuleEvaluateError);

            packet.AppendByte((byte)module.ParentComponent.Type);
            packet.AppendByte((byte) module.Slot);
            packet.AppendInt((int)error);

            Session.SendPacket(packet);
        }

        public void ApplyInvulnerableEffect()
        {
            RemoveInvulnerableEffect(); // Remove existing effect, set new
            var builder = NewEffectBuilder().SetType(EffectType.effect_invulnerable);
            builder.WithDurationModifier(0.75); //Reduce span of syndicate protection
            ApplyEffect(builder);
        }

        public void RemoveInvulnerableEffect()
        {
            EffectHandler.RemoveEffectsByType(EffectType.effect_invulnerable);
        }


        public void ApplyTeleportSicknessEffect()
        {
            var zone = Zone;
            if (zone == null || zone is TrainingZone)
                return;

            var effectBuilder = NewEffectBuilder().SetType(EffectType.effect_teleport_sickness);

            if (HasPvpEffect)
            {
                effectBuilder.WithDurationModifier(3.0);
            }

            ApplyEffect(effectBuilder);
        }

        private void ApplyAggressorEffect()
        {
            ApplyEffect(NewEffectBuilder().SetType(EffectType.effect_aggressor));
        }

        public void ApplySelfTeleportEnablerEffect(TimeSpan duration)
        {
            var effect = EffectHandler.GetEffectsByType(EffectType.effect_teleport_self_enabler).FirstOrDefault();
            if (effect != null)
                EffectHandler.Remove(effect);

            var builder = NewEffectBuilder().SetType(EffectType.effect_teleport_self_enabler).WithDuration(duration);
            ApplyEffect(builder);
        }

        public void RemoveSelfTeleportEnablerEffect()
        {
            EffectHandler.RemoveEffectsByType(EffectType.effect_teleport_self_enabler);
        }

        public void CheckDockingConditionsAndThrow(long baseEid, bool checkRange = true)
        {
            if ( !Session.AccessLevel.IsAdminOrGm())
            {
                HasAggressorEffect.ThrowIfTrue(ErrorCodes.NotAllowedForAggressors);
                HasPvpEffect.ThrowIfTrue(ErrorCodes.CantDockThisState);
                HasTeleportSicknessEffect.ThrowIfTrue(ErrorCodes.CantDockThisState);
            }

            var zone = Zone;
            if (zone == null)
                return;

            var dockingBase = _dockingBaseHelper.GetDockingBase(baseEid);
            if (dockingBase == null)
                return;

            if (dockingBase.Zone == zone)
            {
                // csak akkor van range check ha a terepen van
                if (checkRange)
                {
                    // alapbol van checkrange
                    dockingBase.IsInDockingRange(this).ThrowIfFalse(ErrorCodes.DockingOutOfRange);
                }
            }

            var currentAccess = Session.AccessLevel;
            if (!currentAccess.IsAdminOrGm())
            {
                dockingBase.IsDockingAllowed(Character).ThrowIfError();
            }

            DockToBase(zone,dockingBase);
        }

        /// <summary>
        /// Make the docking happen
        /// </summary>
        /// <param name="zone"></param>
        /// <param name="dockingBase"></param>
        public void DockToBase(IZone zone, DockingBase dockingBase)
        {
            States.Dock = true;

            var publicContainer = dockingBase.GetPublicContainer();

            FullArmorRepair();

            publicContainer.AddItem(this, false);
            publicContainer.Save();
            dockingBase.DockIn(Character,NormalUndockDelay,ZoneExitType.Docked);

            Transaction.Current.OnCommited(() =>
            {
                RemoveFromZone();
                MissionHelper.MissionAdvanceDockInTarget(Character.Id, zone.Id, CurrentPosition);
                TransportAssignment.DeliverTransportAssignmentAsync(Character);
            });
        }

        public static readonly TimeSpan NormalUndockDelay = TimeSpan.FromSeconds(7);


        protected override void OnDead(Unit killer)
        {
            HandlePlayerDeadAsync(Zone, killer).ContinueWith(t => base.OnDead(killer));
        }

        private Task HandlePlayerDeadAsync(IZone zone, Unit killer)
        {
            return Task.Run(() => HandlePlayerDead(zone, killer));
        }

        public const int ARKHE_REQUEST_TIMER_MINUTES_PVP = 3;
        public const int ARKHE_REQUEST_TIMER_MINUTES_NPC = 1;


        //ennek mindenkepp vegig kell futnia
        private void HandlePlayerDead(IZone zone, Unit killer)
        {
            using (var scope = Db.CreateTransaction())
            {
                EnlistTransaction();
                try
                {
                    killer = zone.ToPlayerOrGetOwnerPlayer(killer) ?? killer;

                    SaveCombatLog(zone, killer);

                    var character = Character;

                    var dockingBase = character.GetHomeBaseOrCurrentBase();
                    dockingBase.DockIn(character,NormalUndockDelay, ZoneExitType.Died);

                    PlayerDeathLogger.Log.Write(zone, this, killer);

                    //pay out insurance if needed
                    var wasInsured = InsuranceHelper.CheckInsuranceOnDeath(Eid, Definition);

                    if (!Session.AccessLevel.IsAdminOrGm())
                    {
                        var robotInventory = GetContainer();
                        Debug.Assert(robotInventory != null);

                        var lootItems = new List<LootItem>();

                        // minden fittelt modul
                        foreach (var module in Modules.Where(m => LootHelper.Roll()))
                        {
                            lootItems.Add(LootItemBuilder.Create(module).AsDamaged().Build());

                            var activeModule = module as ActiveModule;
                            var ammo = activeModule?.GetAmmo();
                            if (ammo != null)
                            {
                                if (LootHelper.Roll())
                                    lootItems.Add(LootItemBuilder.Create(ammo).Build());
                            }

                            // szedjuk le a robotrol
                            module.Parent = robotInventory.Eid; //in case the container is full

                            //toroljuk is le, nem kell ez sehova mar
                            Repository.Delete(module);
                        }

                        foreach (var item in robotInventory.GetItems(true).Where(i => i is VolumeWrapperContainer))
                        {
                            //Transport assignments
                            var wrapper = item as VolumeWrapperContainer;
                            if (wrapper == null)
                                continue;

                            lootItems.AddRange(wrapper.GetLootItems());
                            wrapper.SetAllowDelete();
                            Repository.Delete(wrapper);
                        }

                        // elkerunk minden itemet a kontenerbol es valogatunk belole 50% szerint
                        foreach (var item in robotInventory.GetItems().Where(i => LootHelper.Roll() && !i.ED.AttributeFlags.NonStackable))
                        {
                            var qtyMod = FastRandom.NextDouble();
                            item.Quantity = (int)(item.Quantity * qtyMod);

                            if (item.Quantity > 0)
                            {
                                lootItems.Add(LootItemBuilder.Create(item.Definition).SetQuantity(item.Quantity).SetRepackaged(item.ED.AttributeFlags.Repackable).Build());
                            }
                            else
                            {
                                robotInventory.RemoveItemOrThrow(item);

                                //toroljuk is le, nem kell ez mar
                                Repository.Delete(item);
                            }
                        }

                        //Check if paint was applied + paint drop chance
                        if (ED.Config.Tint != Tint && LootHelper.Roll(0.5))
                        {
                            //Paint Query
                            //TODO: performance => cache paints in static collection
                            //TODO: performance => cache if painted, and paint entitydef on undock
                            EntityDefault paint = EntityDefault.Reader.GetAll()
                                .Where(i => i.CategoryFlags == CategoryFlags.cf_paints)
                                .Where(i => i.Config.Tint == Tint).First();
                            if (paint != null)
                            {
                                lootItems.Add(LootItemBuilder.Create(paint.Definition).SetQuantity(1).SetDamaged(false).Build());
                            }
                        }

                        if (lootItems.Count > 0)
                        {
                            var lootContainer = LootContainer.Create().AddLoot(lootItems).BuildAndAddToZone(zone, CurrentPosition);

                            if (lootContainer != null)
                            {
                                var b = TransactionLogEvent.Builder().SetTransactionType(TransactionType.PutLoot).SetCharacter(character).SetContainer(lootContainer.Eid);
                                foreach (var lootItem in lootItems)
                                {
                                    b.SetItem(lootItem.ItemInfo.Definition,lootItem.ItemInfo.Quantity);
                                    Character.LogTransaction(b);
                                }
                            }
                        }

                        var killedByPlayer = (killer != null && killer.IsPlayer());

                        Trashcan.Get().MoveToTrash(this,Session.DisconnectTime, wasInsured, killedByPlayer,Session.InactiveTime);

                        character.NextAvailableRobotRequestTime = DateTime.Now.AddMinutes(killedByPlayer ? ARKHE_REQUEST_TIMER_MINUTES_PVP : ARKHE_REQUEST_TIMER_MINUTES_NPC);

                        Robot activeRobot = null;

                        if (!killedByPlayer)
                        {
                            activeRobot = dockingBase.CreateStarterRobotForCharacter(character);

                            if (activeRobot != null)
                            {
                                Transaction.Current.OnCommited(() =>
                                {
                                    var starterRobotInfo = new Dictionary<string, object>
                                    {
                                        {k.baseEID, Eid},
                                        {k.robotEID, activeRobot.Eid}
                                    };

                                    Message.Builder.SetCommand(Commands.StarterRobotCreated).WithData(starterRobotInfo).ToCharacter(character).Send();
                                });
                            }
                        }

                        character.SetActiveRobot(activeRobot);
                    }
                    else
                    {
                        // mert rendesek vagyunk
                        this.Repair();

                        // csak az adminok miatt kell
                        var container = dockingBase.GetPublicContainer();
                        container.AddItem(this, false);
                    }

                    this.Save();

                    scope.Complete();
                }
                catch (Exception ex)
                {
                    Logger.Exception(ex);
                }
            }
        }

        protected override void OnTileChanged()
        {
            base.OnTileChanged();

            var zone = Zone;
            if (zone == null)
                return;

            MissionHandler?.MissionUpdateOnTileChange();

            var controlInfo = zone.Terrain.Controls.GetValue(CurrentPosition);

            ApplyHighwayEffect(controlInfo.IsAnyHighway);

            if (zone.Configuration.Protected)
                return;

            //PVP zone
            ApplySyndicateAreaEffect(controlInfo.SyndicateArea);
        }

        protected override void OnCellChanged(CellCoord lastCellCoord, CellCoord currentCellCoord)
        {
            base.OnCellChanged(lastCellCoord,currentCellCoord);

            var zone = Zone;
            if ( zone == null )
                return;

            Task.Run(() =>
            {
                var district = currentCellCoord.ComputeDistrict(lastCellCoord);
                zone.SendBeamsToPlayer(this,district);
                Session.SendTerrainData();
            }).LogExceptions();
        }

        public ErrorCodes CheckPvp()
        {
            var zone = Zone;
            Debug.Assert(zone != null);

            if (!HasPvpEffect && (zone.Configuration.Protected || EffectHandler.ContainsEffect(EffectType.effect_syndicate_area)))
                return ErrorCodes.PvpIsNotAllowed;

            return ErrorCodes.NoError;
        }

        private void ApplySyndicateAreaEffect(bool apply)
        {
            if ( apply )
            {
                var effectBuilder = NewEffectBuilder().SetType(EffectType.effect_syndicate_area);
                ApplyEffect(effectBuilder);
            }
            else
            {
                EffectHandler.RemoveEffectsByType(EffectType.effect_syndicate_area);
            }
        }

        private void ApplyHighwayEffect(bool apply)
        {
            if (apply)
            {
                var effectBuilder = NewEffectBuilder().SetType(EffectType.effect_highway)
                                                      .WithPropertyModifier(ItemPropertyModifier.Create(AggregateField.effect_speed_highway_modifier, 1.0));
                ApplyEffect(effectBuilder);
            }
            else
            {
                EffectHandler.RemoveEffectsByType(EffectType.effect_highway);
            }
        }

        private bool IsInSameCorporation(Player player)
        {
            return (CorporationEid == player.CorporationEid) && !IsInDefaultCorporation();
        }

        public bool IsInDefaultCorporation()
        {
            return DefaultCorporationDataCache.IsCorporationDefault(CorporationEid);
        }

        public override ItemPropertyModifier GetPropertyModifier(AggregateField field)
        {
            var modifier = base.GetPropertyModifier(field);

            if (Character == Character.None)
                return modifier;

            var characterExtensions = Character.GetExtensions();
            var extensions = _extensionReader.GetExtensions();

            var extensionBonus = characterExtensions.Select(e => extensions[e.id])
                .Where(e => e.aggregateField == field)
                .Sum(e => characterExtensions.GetLevel(e.id) * e.bonus);

            extensionBonus += ExtensionBonuses.Where(e => e.aggregateField == field).Sum(e => characterExtensions.GetLevel(e.extensionId) * e.bonus);

            if (!extensionBonus.IsZero())
            {
                var m = ItemPropertyModifier.Create(field,extensionBonus);
                m.NormalizeExtensionBonus();
                m.Modify(ref modifier);
            }

            return modifier;
        }

        private bool IsUnitPVPAggro(Unit unit)
        {
            return unit is MobileTeleport
                || unit is IPBSObject
                || unit is WallHealer
                || unit is ProximityProbeBase
                || (unit is BlobEmitterUnit b && b.IsPlayerSpawned);
        }

        public override void OnAggression(Unit victim)
        {
            base.OnAggression(victim);

            AddInCombatWith(victim);

            if (victim is ITaggable taggable)
                taggable.Tag(this, TimeSpan.Zero);

            if (IsUnitPVPAggro(victim))
            {
                ApplyPvPEffect();
                return;
            }

            if (!(victim is Player victimPlayer))
                return;

            victimPlayer.Session.CancelLogout();

            ApplyPvPEffect();

            if (IsInSameCorporation(victimPlayer))
                return;

            if (HasPvpEffect && victimPlayer.HasPvpEffect)
                return;
        }

        public void OnPvpSupport(Unit target)
        {
            if (target is Player player && player.HasPvpEffect)
            {
                ApplyPvPEffect();
            }
        }

        public override string InfoString
        {
            get { return $"Player:{Character.Id}:{Definition}:{Eid}"; }
        }

        public void SendInitSelf()
        {
            var zone = Zone;
            Debug.Assert(zone != null, "zone != null");

            Session.SendPacket(EnterPacketBuilder);

            // terrain
            Session.SendTerrainData();

            // beams
            zone.SendBeamsToPlayer(this,GridDistricts.All);

            // elkuldjuk az osszes lockot
            var lockPackets = GetLockPackets();
            Session.SendPackets(lockPackets);

            foreach (var visibility in GetVisibleUnits())
            {
                Session.SendPacket(visibility.Target.EnterPacketBuilder);

                var robot = visibility.Target as Robot;
                if (robot == null)
                    continue;

                var unitLockPackets = robot.GetLockPackets();
                Session.SendPackets(unitLockPackets);
            }

            Session.SendPacket(new GangUpdatePacketBuilder(Visibility.Visible, zone.GetGangMembers(Gang)));
            Session.SendPacket(zone.Weather.GetCurrentWeather().CreateUpdatePacket());

            foreach (var effect in EffectHandler.Effects)
            {
                Session.SendPacket(new EffectPacketBuilder(effect, true));
            }

            foreach (var module in ActiveModules)
            {
                module.ForceUpdate();
            }
        }

        public void WriteFQLog(string message)
        {
            var e = new LogEvent
            {
                LogType = LogType.Info,
                Tag = "FQ",
                Message = $"{InfoString} - {message}"
            };

            Logger.Log(e);
        }

        private void SendError(ErrorCodes error)
        {
            Session.SendPacket(new ErrorPacketBuilder(error));
        }

        public void SendForceUpdate()
        {
            Session.SendPacket(new UnitUpdatePacketBuilder(this, UpdatePacketControl.ForceReposition));
        }


        protected override void OnBroadcastPacket(IBuilder<Packet> packetBuilder)
        {
            base.OnBroadcastPacket(packetBuilder);
            Session.SendPacket(packetBuilder.Build());
        }

        protected override void OnUnitVisibilityUpdated(Unit target,Visibility visibility)
        {
            switch (visibility)
            {
                case Visibility.Visible:
                {
                    target.BroadcastPacket += OnUnitBroadcastPacket;
                    target.Updated += OnUnitUpdated;
                    Session.SendPacket(target.EnterPacketBuilder);
                    break;
                }
                case Visibility.Invisible:
                {
                    target.BroadcastPacket -= OnUnitBroadcastPacket;
                    target.Updated -= OnUnitUpdated;
                    Session.SendPacket(target.ExitPacketBuilder);
                    break;
                }

            }

            if (Gang.IsMember(target))
            {
                Session.SendPacket(new GangUpdatePacketBuilder(visibility,(Player) target));
            }

            base.OnUnitVisibilityUpdated(target,visibility);
        }

        private void OnUnitUpdated(Unit unit, UnitUpdatedEventArgs e)
        {
            if (!Gang.IsMember(unit)) 
                return;

            var send = (e.UpdateTypes & UnitUpdateTypes.Visibility) > 0 || (e.UpdatedProperties != null && e.UpdatedProperties.Any(p => p.Field.IsPublic()));
            if (!send)
                return;

            var v = Visibility.Invisible;
            if (unit.InZone)
                v = Visibility.Visible;

            Session.SendPacket(new GangUpdatePacketBuilder(v,(Player) unit));
        }

        private void OnUnitBroadcastPacket(Unit sender, Packet packet)
        {
            Session.SendPacket(packet);
        }

        protected override void OnLockStateChanged(Lock @lock)
        {
            base.OnLockStateChanged(@lock);

            if (@lock is UnitLock u)
            {
                var player = u.Target as Player;
                player?.Session.CancelLogout();

                if (@lock.State == LockState.Locked)
                {
                    //  !!
                    //  this will enqueue primary, secondary, and will also fire if a primary is converted to secondary
                    //  !!

                    var npc = u.Target as Npc;

                    if (npc != null)
                    {
                        MissionHandler.EnqueueMissionEventInfo(new LockUnitEventInfo(this, npc, npc.CurrentPosition));
                        MissionHandler.SignalParticipationByLocking(npc.GetMissionGuid());
                    }
                }
            }

            if (@lock.State == LockState.Inprogress && EffectHandler.ContainsEffect(EffectType.effect_invulnerable))
            {
              EffectHandler.RemoveEffectsByType(EffectType.effect_invulnerable);
            }
        }

        protected override void OnEffectChanged(Effect effect, bool apply)
        {
            base.OnEffectChanged(effect,apply);

            if (!apply) 
                return;

            switch (effect.Type)
            {
                case EffectType.effect_demobilizer:
                    {
                        OnCombatEvent(effect.Source, new DemobilizerEventArgs());
                        break;
                    }

                case EffectType.effect_sensor_supress:
                    {
                        OnCombatEvent(effect.Source,new SensorDampenerEventArgs());
                        break;
                    }
            }
        }

        protected override void OnLockError(Lock @lock, ErrorCodes error)
        {
            SendError(error);
            base.OnLockError(@lock, error);
        }

        public override void OnCombatEvent(Unit source, CombatEventArgs e)
        {
            base.OnCombatEvent(source, e);

            var player = Zone.ToPlayerOrGetOwnerPlayer(source);
            if (player == null)
                return;

            var logger = LazyInitializer.EnsureInitialized(ref _combatLogger, CreateCombatLogger);
            logger.Log(player,e);
        }

        private CombatLogger CreateCombatLogger()
        {
            var logger = _combatLoggerFactory(this);
            logger.Expired = () =>
            {
                _combatLogger = null;
            };
            return logger;
        }

        private void SaveCombatLog(IZone zone, Unit killer)
        {
            _combatLogger?.Save(zone,killer);
        }

        public static Player LoadPlayerAndAddToZone(IZone zone,Character character)
        {
            using (var scope = Db.CreateTransaction())
            {
                var player = (Player)character.GetActiveRobot().ThrowIfNull(ErrorCodes.ARobotMustBeSelected);

                DockingBase dockingBase = null;

                ZoneEnterType zoneEnterType;
                Position spawnPosition;
                if (character.IsDocked)
                {
                    // ha bazisrol jott
                    zoneEnterType = ZoneEnterType.Undock;

                    dockingBase = character.GetCurrentDockingBase();
                    spawnPosition = UndockSpawnPositionSelector.SelectSpawnPosition(dockingBase);

                    character.ZoneId = zone.Id;
                    character.ZonePosition = spawnPosition;
                    character.IsDocked = false;
                }
                else
                {
                    // ha teleportalt
                    zoneEnterType = ZoneEnterType.Teleport;

                    zone.Id.ThrowIfNotEqual(character.ZoneId ?? -1, ErrorCodes.InvalidZoneId);

                    var zonePosition = character.ZonePosition.ThrowIfNull(ErrorCodes.InvalidPosition);
                    spawnPosition = (Position)zonePosition;
                }

                spawnPosition = zone.FixZ(spawnPosition);

                // keresunk neki valami jo poziciot
                var finder = new ClosestWalkablePositionFinder(zone, spawnPosition, player);
                var validPosition = finder.FindOrThrow();

                // parentoljuk a zonahoz <<< NAGYON FONTOS - TILOS MASHOGY kulonben bennmaradnak a (pbs) bazison a robotok, es pl letorlodnek amikor kilovik a bazist
                var zoneStorage = zone.Configuration.GetStorage();
                player.Parent = zoneStorage.Eid;
                player.FullCoreRecharge();

                // elmentjuk
                player.Save();

                // csak akkor rakjuk ki ha volt rendes commit
                Transaction.Current.OnCommited(() =>
                {
                    dockingBase?.LeaveChannel(character);

                    player.CorporationEid = character.CorporationEid;
                    zone.SetGang(player);

                    player.AddToZone(zone,validPosition,zoneEnterType);
                    player.ApplyInvulnerableEffect();
                });

                scope.Complete();
                return player;
            }
        }

        [CanBeNull]
        public Task TeleportToPositionAsync(Position target, bool applyTeleportSickness, bool applyInvulnerable)
        {
            var zone = Zone;
            if (zone == null)
                return null;

            var teleport = _teleportStrategyFactories.TeleportWithinZoneFactory();
            if (teleport == null)
                return null;

            teleport.TargetPosition = target;
            teleport.ApplyTeleportSickness = applyTeleportSickness;
            teleport.ApplyInvulnerable = applyInvulnerable;
            var task = teleport.DoTeleportAsync(this);
            return task?.LogExceptions();
        }

        public override void UpdateVisibilityOf(Unit target)
        {
            target.UpdatePlayerVisibility(this);
        }

        protected override void UpdateUnitVisibility(Unit target)
        {
            UpdateVisibility(target);
        }

        protected internal override void UpdatePlayerVisibility(Player player)
        {
            UpdateVisibility(player);
        }

        protected override bool IsDetected(Unit target)
        {
            if ( Gang.IsMember(target) )
                return true;

            return base.IsDetected(target);
        }

        protected override bool IsHostileFor(Unit unit)
        {
            return unit.IsHostile(this);
        }

        private void UpdateCombat(TimeSpan time)
        {
            if (!States.Combat)
                return;

            _combatTimer.Update(time);

            if ( !_combatTimer.Passed)
                return;

            SetCombatState(false);
        }

        private void SetCombatState(bool state)
        {
            States.Combat = state;
            _combatTimer.Reset();

            if ( state )
                Session.ResetLogoutTimer();
        }

        private readonly IntervalTimer _combatTimer = new IntervalTimer(TimeSpan.FromSeconds(10));

        private void AddInCombatWith(Unit enemy)
        {
            SetCombatState(true);

            var enemyPlayer = enemy as Player;
            enemyPlayer?.SetCombatState(true);
        }

        public void SendStartProgressBar(Unit unit, TimeSpan timeout, TimeSpan start)
        {
            var data = unit.BaseInfoToDictionary();

            data.Add(k.timeOut, (int)timeout.TotalMilliseconds);
            data.Add(k.started, (long)start.TotalMilliseconds);
            data.Add(k.now, (long)start.TotalMilliseconds);


            Message.Builder.SetCommand(Commands.AlarmStart).WithData(data).ToCharacter(Character).Send();
        }

        public  void SendEndProgressBar( Unit unit, bool success = true)
        {
            var info = unit.BaseInfoToDictionary();
            info.Add(k.success, success);
            Message.Builder.SetCommand(Commands.AlarmOver).WithData(info).ToCharacter(Character).Send();
        }

        public void SendArtifactRadarBeam(Position targetPosition)
        {
            var builder = Beam.NewBuilder()
                              .WithType(BeamType.artifact_radar)
                              .WithSourcePosition(targetPosition)
                              .WithTargetPosition(targetPosition)
                              .WithState(BeamState.AlignToTerrain)
                              .WithDuration(TimeSpan.FromSeconds(30));

            Session.SendBeam(builder);
        }

        public bool IsStandingMatch(long targetCorporationEid, double? standingLimit)
        {
            return _corporationManager.IsStandingMatch(targetCorporationEid, CorporationEid, standingLimit);
        }

        public void ReloadContainer()
        {
            void Reload()
            {
                var container = GetContainer();
                Debug.Assert(container != null,"container != null");

                container.EnlistTransaction();
                container.ReloadItems(Character);
                container.SendUpdateToOwner();
            }

            if (Transaction.Current != null)
                Reload();
            else
            {
                using (var scope = Db.CreateTransaction())
                {
                    Reload();
                    scope.Complete();
                }
            }
        }

        public void UpdateCorporationOnZone(long newCorporationEid)
        {
            CorporationEid = newCorporationEid;

            var playersOnZone = Zone.GetCharacters().ToArray();
            if (playersOnZone.Length <= 0)
                return;

            var result = new Dictionary<string,object>
            {
                {k.corporationEID, newCorporationEid},
                {k.characterID, Character.Id},
            };

            Message.Builder.SetCommand(Commands.CharacterUpdate).WithData(result).ToCharacters(playersOnZone).Send();
        }

        public void EnableSelfTeleport(TimeSpan duration, int affectedZoneId = -1)
        {
            if (affectedZoneId != -1 && Zone.Id != affectedZoneId)
                return;

            ApplySelfTeleportEnablerEffect(duration);
        }


    }
}
