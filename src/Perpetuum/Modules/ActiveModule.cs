using System;
using System.Collections.Generic;
using System.Diagnostics;
using Perpetuum.Containers;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Items;
using Perpetuum.Modules.Weapons;
using Perpetuum.Players;
using Perpetuum.Robots;
using Perpetuum.Units;
using Perpetuum.Zones;
using Perpetuum.Zones.Beams;
using Perpetuum.Zones.Locking;
using Perpetuum.Zones.Locking.Locks;
using Perpetuum.Zones.Terrains;

namespace Perpetuum.Modules
{
    public class ModuleProperty : ItemProperty
    {
        protected readonly Module module;
        private List<AggregateField> _effectModifiers;

        public ModuleProperty(Module module, AggregateField field) : base(field)
        {
            this.module = module;
        }

        public void AddEffectModifier(AggregateField field)
        {
            if ( _effectModifiers == null )
                _effectModifiers = new List<AggregateField>();

            _effectModifiers.Add(field);
        }

        protected override double CalculateValue()
        {
            var m = module.GetPropertyModifier(Field);
            ApplyEffectModifiers(ref m);
            return m.Value;
        }

        protected void ApplyEffectModifiers(ref ItemPropertyModifier m)
        {
            if (_effectModifiers == null)
                return;

            foreach (var effectModifier in _effectModifiers)
            {
                module.ParentRobot?.ApplyEffectPropertyModifiers(effectModifier,ref m);
            }
        }

        protected override bool IsRelated(AggregateField field)
        {
            if (_effectModifiers != null)
            {
                if (_effectModifiers.Contains(field))
                    return true;
            }

            return base.IsRelated(field);
        }
    }

    public class CycleTimeProperty : ModuleProperty
    {
        private readonly ActiveModule _module;

        public CycleTimeProperty(ActiveModule module) : base(module, AggregateField.cycle_time)
        {
            _module = module;
        }

        protected override double CalculateValue()
        {
            var cycleTime = _module.GetPropertyModifier(AggregateField.cycle_time);

            var ammo = _module.GetAmmo();
            ammo?.ModifyCycleTime(ref cycleTime);

            var driller = _module as DrillerModule;
            if (driller != null)
            {
                var miningAmmo = ammo as MiningAmmo;
                if (miningAmmo != null)
                {
                    miningAmmo.miningCycleTimeModifier.Update();
                    var miningCycleTimeMod = miningAmmo.miningCycleTimeModifier.ToPropertyModifier();
                    miningCycleTimeMod.Modify(ref cycleTime);
                }
            }

            ApplyEffectModifiers(ref cycleTime);
            return cycleTime.Value;
        }
    }

    public class OptimalRangeProperty : ModuleProperty
    {
        private readonly ActiveModule _module;

        public OptimalRangeProperty(ActiveModule module) : base(module,AggregateField.optimal_range)
        {
            _module = module;
            AddEffectModifier(AggregateField.effect_optimal_range_modifier);
        }

        protected override double CalculateValue()
        {
            var optimalRange = ItemPropertyModifier.Create(AggregateField.optimal_range);

            var ammo = _module.GetAmmo();
            if (module is MissileWeaponModule m)
            {
                if (ammo != null)
                {
                    optimalRange = ammo.OptimalRangePropertyModifier;
                    var missileRangeMod = m.MissileRangeModifier.ToPropertyModifier();
                    missileRangeMod.Modify(ref optimalRange);
                    module.ApplyRobotPropertyModifiers(ref optimalRange);
                }
            }
            else
            {
                optimalRange = module.GetPropertyModifier(AggregateField.optimal_range);
                ammo?.ModifyOptimalRange(ref optimalRange);
            }

            ApplyEffectModifiers(ref optimalRange);
            return optimalRange.Value;
        }
    }


    public class FalloffProperty : ModuleProperty
    {
        public FalloffProperty(ActiveModule module) : base(module, AggregateField.falloff)
        {
        }

        protected override double CalculateValue()
        {
            var falloff = ItemPropertyModifier.Create(AggregateField.falloff);
            var ammo = ((ActiveModule)module).GetAmmo();
            if (module is MissileWeaponModule m)
            {
                if (ammo != null)
                {
                    falloff = ammo.FalloffRangePropertyModifier;
                    var missileRangeMod = m.MissileFalloffModifier.ToPropertyModifier();
                    missileRangeMod.Modify(ref falloff);
                    module.ApplyRobotPropertyModifiers(ref falloff);
                }
            }
            else
            {
                falloff = module.GetPropertyModifier(AggregateField.falloff);
                ammo?.ModifyFalloff(ref falloff);
            }
            ApplyEffectModifiers(ref falloff);
            return falloff.Value;
        }
    }


    public abstract partial class ActiveModule : Module
    {
        private Lock _lock;
        protected readonly ModuleProperty coreUsage;
        protected readonly CycleTimeProperty cycleTime;
        protected readonly ItemProperty falloff = ItemProperty.None;
        protected readonly ModuleProperty optimalRange;

        private readonly CategoryFlags _ammoCategoryFlags;

        protected ActiveModule(CategoryFlags ammoCategoryFlags,bool ranged = false)
        {
            IsRanged = ranged;
            coreUsage = new ModuleProperty(this,AggregateField.core_usage);
            AddProperty(coreUsage);
            cycleTime = new CycleTimeProperty(this);
            AddProperty(cycleTime);

            if (ranged)
            {
                optimalRange = new OptimalRangeProperty(this);
                AddProperty(optimalRange);
                falloff = new FalloffProperty(this);
                AddProperty(falloff);
            }

            _ammoCategoryFlags = ammoCategoryFlags;
        }

        public override void Initialize()
        {
            InitState();
            InitAmmo();
            base.Initialize();
        }


        protected ActiveModule(bool ranged) : this(CategoryFlags.undefined,ranged)
        {
        }

        public TimeSpan CycleTime => TimeSpan.FromMilliseconds(cycleTime.Value);

        public double CoreUsage => coreUsage.Value;

        public double OptimalRange => optimalRange.Value;

        public double Falloff => falloff.Value;

        public Lock Lock
        {
            [CanBeNull] private get { return _lock; }
            set
            {
                if (_lock != null)
                    _lock.Changed -= LockChangedHandler;

                _lock = value;

                if (_lock != null)
                    _lock.Changed += LockChangedHandler;
            }
        }

        public bool IsRanged { get; private set; }

        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
                base.AcceptVisitor(visitor);
        }

        private void LockChangedHandler(Lock @lock)
        {
            if (State.Type == ModuleStateType.Idle || State.Type == ModuleStateType.AmmoLoad)
                return;

            var shutdown = @lock.State == LockState.Disabled || (ED.AttributeFlags.PrimaryLockedTarget && !@lock.Primary);
            if (!shutdown) 
                return;

            State.SwitchTo(ModuleStateType.Shutdown);
            _lock = null;
        }

        private bool IsInRange(Position position)
        {
            Debug.Assert(ParentRobot != null, "ParentRobot != null");
            return !IsRanged || ParentRobot.IsInRangeOf3D(position,OptimalRange + Falloff);
        }

        public void ForceUpdate()
        {
            SendModuleStateToPlayer();
            SendAmmoUpdatePacketToPlayer();
        }

        private BeamType GetBeamType()
        {
            var ammo = GetAmmo();
            return ammo != null ? BeamHelper.GetBeamByDefinition(ammo.Definition) : BeamHelper.GetBeamByDefinition(Definition);
        }

        private void SendModuleStateToPlayer()
        {
            var state = State;

            var player = ParentRobot as Player;
            if (player == null)
                return;

            var packet = new Packet(ZoneCommand.ModuleChangeState);
            Debug.Assert(ParentComponent != null, "ParentComponent != null");
            packet.AppendByte((byte)ParentComponent.Type);
            packet.AppendByte((byte) Slot);
            packet.AppendByte((byte)state.Type);

            var timed = state as ITimedModuleState;
            if (timed == null)
            {
                packet.AppendInt(0);
                packet.AppendInt(0);
            }
            else
            {
                packet.AppendInt((int)timed.Timer.Interval.TotalMilliseconds);
                packet.AppendInt((int)timed.Timer.Elapsed.TotalMilliseconds);
            }

            player.Session.SendPacket(packet);
        }

        public void Update(TimeSpan time)
        {
            _states.Update(time);
        }

        protected abstract void OnAction();

        protected virtual void HandleOffensivePVPCheck(Player parentPlayer, UnitLock unitLockTarget)
        {
            if (parentPlayer != null)
            {
                // pvp ellenorzes
                (unitLockTarget.Target as Player)?.CheckPvp().ThrowIfError();
            }
        }

        protected Lock GetLock()
        {
            var currentLock = Lock.ThrowIfNull(ErrorCodes.LockTargetNotFound);
            currentLock.State.ThrowIfNotEqual(LockState.Locked, ErrorCodes.LockIsInProgress);

            var unitLockTarget = currentLock as UnitLock;
            if (unitLockTarget != null)
            {
                IsInRange(unitLockTarget.Target.CurrentPosition).ThrowIfFalse(ErrorCodes.TargetOutOfRange);

                var parentPlayer = ParentRobot as Player;

                if (ED.AttributeFlags.OffensiveModule)
                {
                    HandleOffensivePVPCheck(parentPlayer, unitLockTarget);
                    Debug.Assert(ParentRobot != null, "ParentRobot != null");
                    ParentRobot.OnAggression(unitLockTarget.Target);
                }
                else if ((parentPlayer != null) && (unitLockTarget.Target is Player) && ED.AttributeFlags.PvpSupport)
                {
                    parentPlayer.OnPvpSupport(unitLockTarget.Target);
                }
            }
            else
            {
                var terrainLockTarget = currentLock as TerrainLock;
                if (terrainLockTarget != null)
                {
                    IsInRange(terrainLockTarget.Location).ThrowIfFalse(ErrorCodes.TargetOutOfRange);
                }
            }

            return currentLock;
        }

        public override void UpdateAllProperties()
        {
            base.UpdateAllProperties();

            var ammo = GetAmmo();
            ammo?.UpdateAllProperties();
        }

        protected void CreateBeam(Unit target, BeamState beamState)
        {
            CreateBeam(target, beamState, 0, 0, 0);
        }

        protected int CreateBeam(Unit target, BeamState beamState, int duration, double bulletTime)
        {
            return CreateBeam(target, beamState, duration, bulletTime, 0);
        }

        private int CreateBeam(Unit target, BeamState beamState, int duration, double bulletTime,int visibility)
        {
            var delay = 0;
            var beamType = GetBeamType();

            if (beamType <= 0)
                return delay;

            delay = BeamHelper.GetBeamDelay(beamType);

            if (duration == 0)
                duration = (int)CycleTime.TotalMilliseconds;

            Debug.Assert(ParentComponent != null, "ParentComponent != null");
            var slot = ParentComponent.Type == RobotComponentType.Chassis ? Slot : 0xff; // -1

            var builder = Beam.NewBuilder().WithType(beamType)
                .WithSlot(slot)
                .WithSource(ParentRobot)
                .WithState(beamState)
                .WithBulletTime(bulletTime)
                .WithDuration(duration)
                .WithTarget(target)
                .WithVisibility(visibility);

            Zone.CreateBeam(builder);
            return delay;
        }

        protected void CreateBeam(Position location, BeamState beamState)
        {
            CreateBeam(location, beamState, 0, 0, 0);
        }

        protected int CreateBeam(Position location, BeamState beamState, int duration, double bulletTime)
        {
            return CreateBeam(location, beamState, duration, bulletTime,0);
        }

        private int CreateBeam(Position location, BeamState beamState, int duration, double bulletTime,int visibility)
        {
            var delay = 0;
            var beamType = GetBeamType();

            if (beamType <= 0) 
                return delay;

            delay = BeamHelper.GetBeamDelay(beamType);

            if (duration == 0)
                duration = (int) CycleTime.TotalMilliseconds;

            Debug.Assert(ParentComponent != null, "ParentComponent != null");
            var slot = ParentComponent.Type == RobotComponentType.Chassis ? Slot : 0xff; // -1

            var builder = Beam.NewBuilder().WithType(beamType)
                .WithSlot(slot)
                .WithSource(ParentRobot)
                .WithState(beamState)
                .WithBulletTime(bulletTime)
                .WithDuration(duration)
                .WithTargetPosition(location)
                .WithVisibility(visibility);

            Zone.CreateBeam(builder);
            return delay;
        }

        protected override void OnUpdateProperty(AggregateField field)
        {
            switch (field)
            {
                case AggregateField.core_usage:
                case AggregateField.effect_core_usage_gathering_modifier:
                {
                    coreUsage.Update();
                    break;
                }
                case AggregateField.cycle_time:
                case AggregateField.effect_weapon_cycle_time_modifier:
                case AggregateField.effect_gathering_cycle_time_modifier:
                {
                    cycleTime.Update();
                    break;
                }
                case AggregateField.optimal_range:
                case AggregateField.effect_optimal_range_modifier:
                case AggregateField.effect_ew_optimal_range_modifier:
                case AggregateField.module_missile_range_modifier:
                case AggregateField.effect_missile_range_modifier:
                {
                    optimalRange.Update();
                    break;
                }
                case AggregateField.falloff:
                {
                    falloff.Update();
                    break;
                }
            }

            base.OnUpdateProperty(field);
        }

        public override void Unequip(Container container)
        {
            UnequipAmmoToContainer(container);
            base.Unequip(container);
        }

        protected double ModifyValueByOptimalRange(Unit target,double value)
        {
            Debug.Assert(ParentRobot != null, "ParentRobot != null");

            var distance = ParentRobot.GetDistance(target);
            if (distance <= OptimalRange)
                return value;

            if (distance > OptimalRange + Falloff)
                return 0.0;

            var x = (distance - OptimalRange) / Falloff;
            var m = Math.Cos(x * Math.PI) / 2 + 0.5;
            return value * m;
        }

        protected LOSResult GetLineOfSight(Unit target)
        {
            Debug.Assert(ParentRobot != null, "ParentRobot != null");
            var visibility = ParentRobot.GetVisibility(target);
            return visibility?.GetLineOfSight(IsCategory(CategoryFlags.cf_missiles)) ?? LOSResult.None;
        }

        protected LOSResult GetLineOfSight(Position location)
        {
            Debug.Assert(ParentRobot != null, "ParentRobot != null");
            var losResult = ParentRobot.Zone.IsInLineOfSight(ParentRobot,location,IsCategory(CategoryFlags.cf_missiles));
            return losResult;
        }

        protected bool LOSCheckAndCreateBeam(Unit target)
        {
            var result = GetLineOfSight(target);
            if (result.hit)
            {
                var beamState = (result.blockingFlags != BlockingFlags.Undefined) ? BeamState.AlignToTerrain : BeamState.Hit;
                CreateBeam(result.position, beamState);
                return false;
            }

            CreateBeam(target, BeamState.Hit);
            return true;
        }

        protected void OnError(ErrorCodes error)
        {
            SendModuleErrorToPlayer(error);
        }

        private void SendModuleErrorToPlayer(ErrorCodes error)
        {
            var player = ParentRobot as Player;
            if (player == null)
                return;

            var packet = new CombatLogPacket(error, this, _lock);
            player.Session.SendPacket(packet);
        }

        public override Dictionary<string, object> ToDictionary()
        {
            var result = base.ToDictionary();

            result.Add(k.ammoCategoryFlags, (long)_ammoCategoryFlags);

            var ammo = GetAmmo();
            if (ammo == null)
                return result;

            result.Add(k.ammo, ammo.ToDictionary());
            result.Add(k.ammoQuantity, ammo.Quantity);

            return result;
        }

        public override double Volume
        {
            get
            {
                var volume = base.Volume;

                var ammo = GetAmmo();
                if (ammo != null)
                    volume += ammo.Volume;

                return volume;
            }
        }

        public override void UpdateRelatedProperties(AggregateField field)
        {
            var ammo = GetAmmo();
            ammo?.UpdateRelatedProperties(field);

            base.UpdateRelatedProperties(field);
        }
    }
}