using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Perpetuum.Builders;
using Perpetuum.Collections.Spatial;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Items;
using Perpetuum.Log;
using Perpetuum.Modules;
using Perpetuum.Modules.Weapons;
using Perpetuum.Players;
using Perpetuum.Robots;
using Perpetuum.Timers;
using Perpetuum.Zones;
using Perpetuum.Zones.Beams;
using Perpetuum.Zones.Blobs;
using Perpetuum.Zones.DamageProcessors;
using Perpetuum.Zones.Effects;
using Perpetuum.Zones.Eggs;
using Perpetuum.Zones.Gates;
using Perpetuum.Zones.Locking;
using Perpetuum.Zones.PBS;
using Perpetuum.Zones.PBS.DockingBases;
using Perpetuum.Zones.PBS.Turrets;

namespace Perpetuum.Units
{
    public class SpeedMaxProperty : ItemProperty
    {
        private readonly Unit _owner;

        public SpeedMaxProperty(Unit owner)
            : base(AggregateField.speed_max)
        {
            _owner = owner;
        }

        protected override double CalculateValue()
        {
            var speedMax = _owner.GetPropertyModifier(AggregateField.speed_max);
            var speedMaxMod = _owner.GetPropertyModifier(AggregateField.speed_max_modifier);
            speedMaxMod.Modify(ref speedMax);

            _owner.ApplyEffectPropertyModifiers(AggregateField.effect_speed_max_modifier,ref speedMax);
            _owner.ApplyEffectPropertyModifiers(AggregateField.effect_massivness_speed_max_modifier,ref speedMax);

            if (_owner.ActualMass > 0)
                speedMax.Multiply(_owner.Mass / _owner.ActualMass);

            _owner.ApplyEffectPropertyModifiers(AggregateField.effect_speed_highway_modifier,ref speedMax);
            return speedMax.Value;
        }

        protected override bool IsRelated(AggregateField field)
        {
            switch (field)
            {
                case AggregateField.speed_max:
                case AggregateField.speed_max_modifier:
                case AggregateField.effect_speed_max_modifier:
                case AggregateField.effect_massivness_speed_max_modifier:
                case AggregateField.effect_speed_highway_modifier:
                    return true;
            }

            return false;
        }
    }

    
    public delegate void UnitEventHandler(Unit unit);
    public delegate void UnitEventHandler<in T>(Unit unit, T args);
    public delegate void UnitEventHandler<in T1,in T2>(Unit unit, T1 args1, T2 args2);

    public abstract partial class Unit : Item
    {
        private ICoreRecharger _coreRecharger = CoreRecharger.None;

        private readonly DamageProcessor _damageProcessor;

        private readonly OptionalPropertyCollection _optionalProperties = new OptionalPropertyCollection();

        private readonly object _killSync = new object();

        private Position _currentPosition;
        private double _currentSpeed;
        private double _direction;
        private double _orientation;

        private ItemProperty _armorMax;
        private ItemProperty _armor;
        private ItemProperty _coreMax;
        private ItemProperty _core;
        private ItemProperty _actualMass;
        private ItemProperty _coreRechargeTime;
        private ItemProperty _resistChemical;
        private ItemProperty _resistExplosive;
        private ItemProperty _resistKinetic;
        private ItemProperty _resistThermal;
        private ItemProperty _kersChemical;
        private ItemProperty _kersExplosive;
        private ItemProperty _kersKinetic;
        private ItemProperty _kersThermal;
        private ItemProperty _speedMax;
        private ItemProperty _criticalHitChance;
        private ItemProperty _sensorStrength;
        private ItemProperty _detectionStrength;
        private ItemProperty _stealthStrength;
        private ItemProperty _massiveness;
        private ItemProperty _reactorRadiation;
        private ItemProperty _signatureRadius;
        private ItemProperty _slope;
        
        private readonly Lazy<double> _height;

        protected Unit()
        {
            _damageProcessor = new DamageProcessor(this) { DamageTaken = OnDamageTaken };

            var effectHandler = new EffectHandler(this);
            effectHandler.EffectChanged += OnEffectChanged;
            EffectHandler = effectHandler;

            _optionalProperties.PropertyChanged += property =>
            {
                UpdateTypes |= UnitUpdateTypes.OptionalProperty;
            };

            InitUnitProperties();

            States = new UnitStates(this);
            _height = new Lazy<double>(() => ComputeHeight() + 1.0);
        }

        public EffectBuilder.Factory EffectBuilderFactory { get; set; }

        public void SetCoreRecharger(ICoreRecharger recharger)
        {
            _coreRecharger = recharger;
        }

        public Guid GetMissionGuid()
        {
            var p = (ReadOnlyOptionalProperty<Guid>)_optionalProperties.Get(UnitDataType.MissionGuid);
            return p?.Value ?? Guid.Empty;
        }

        public int GetMissionDisplayOrder()
        {
            var p = (ReadOnlyOptionalProperty<int>)_optionalProperties.Get(UnitDataType.MissionDisplayOrder);
            if (p == null)
                return -1;

            return p.Value;
        }

        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
                base.AcceptVisitor(visitor);
        }

        private IZone _zone;

        [CanBeNull]
        public IZone Zone => _zone;

        public bool InZone => Zone != null;

        public OptionalPropertyCollection OptionalProperties => _optionalProperties;

        public EffectHandler EffectHandler { get; private set; }

        protected UnitUpdateTypes UpdateTypes { get; set; }

        public double CurrentSpeed
        {
            get => _currentSpeed;
            set
            {
                if ( Math.Abs(_currentSpeed - value) < double.Epsilon )
                    return;

                _currentSpeed = value;
                UpdateTypes |= UnitUpdateTypes.Speed;
            }
        }

        public double Direction
        {
            get => _direction;
            set
            {
                if ( Math.Abs(_direction - value) < double.Epsilon )
                    return;

                _direction = value;
                UpdateTypes |= UnitUpdateTypes.Direction;
            }
        }

        public double Orientation
        {
            get => _orientation;
            set
            {
                if ( Math.Abs(_orientation - value) < double.Epsilon )
                    return;

                _orientation = value;
                UpdateTypes |= UnitUpdateTypes.Orientation;
            }
        }

        public double Height => _height.Value;

        public virtual bool IsLockable => !ED.AttributeFlags.NonLockable && !States.Dead && !States.Unlockable;

        public virtual ErrorCodes IsAttackable
        {
            get
            {
                if ( !ED.AttributeFlags.NonAttackable && !States.Dead)
                    return ErrorCodes.NoError;

                return ErrorCodes.TargetIsNonAttackable;
            }
        }

        public bool IsInvulnerable
        {
            get { return ED.AttributeFlags.Invulnerable || EffectHandler.ContainsEffect(EffectType.effect_invulnerable); }
        }


        public bool HasShieldEffect => EffectHandler.ContainsEffect(EffectType.effect_shield);

        public bool HasPvpEffect => EffectHandler.ContainsEffect(EffectType.effect_pvp);

        public bool HasTeleportSicknessEffect => EffectHandler.ContainsEffect(EffectType.effect_teleport_sickness);

        public bool HasDespawnEffect => EffectHandler.ContainsEffect(EffectType.effect_despawn_timer);

        public Position WorldPosition
        {
            get { return Zone?.ToWorldPosition(CurrentPosition) ?? CurrentPosition; }
        }

        public Position PositionWithHeight
        {
            get { return CurrentPosition.AddToZ(Height); }
        }

        public Position CurrentPosition
        {
            get { return _currentPosition; }
            set
            {
                var lastPosition = _currentPosition;

                _currentPosition = Zone.FixZ(value);

                UpdateTypes |= UnitUpdateTypes.Position;

                if (!lastPosition.IsTileChange(_currentPosition))
                    return;

                UpdateTypes |= UnitUpdateTypes.TileChanged;

                OnTileChanged();

                var lastCellCoord = lastPosition.ToCellCoord();
                var currentCellCoord = _currentPosition.ToCellCoord();

                if (lastCellCoord == currentCellCoord)
                    return;

                OnCellChanged(lastCellCoord, currentCellCoord);
            }
        }

        public IBuilder<Packet> EnterPacketBuilder { get; private set; }

        public IBuilder<Packet> ExitPacketBuilder => new UnitExitPacketBuilder(this);

        /// <summary>
        /// Updates the specified time.
        /// ez hivodik meg minden 50ms alatt
        /// </summary>
        public void Update(TimeSpan time)
        {
            if ( !InZone || States.Dead )
                return;

            OnUpdate(time);
        }

        private readonly IntervalTimer _broadcastTimer = new IntervalTimer(200);

        protected virtual void OnUpdate(TimeSpan time)
        {
            _coreRecharger.RechargeCore(this,time);

            EffectHandler.Update(time);

            UnitUpdatedEventArgs e = null;

            _broadcastTimer.Update(time);

            if (_broadcastTimer.Passed)
            {
                _broadcastTimer.Reset();

                if (UpdateTypes > 0)
                {
                    e = new UnitUpdatedEventArgs { UpdateTypes = UpdateTypes };

                    if ((UpdateTypes & UnitUpdateTypes.Unit) > 0)
                    {
                        var packetBuilder = new UnitUpdatePacketBuilder(this);
                        OnBroadcastPacket(packetBuilder.ToProxy());
                    }

                    UpdateTypes = UnitUpdateTypes.None;
                }

                var changedProperties = GetChangedProperties();
                if (changedProperties != ImmutableHashSet<ItemProperty>.Empty)
                {
                    if (e == null)
                        e = new UnitUpdatedEventArgs();

                    e.UpdatedProperties = changedProperties;

                    var builder = new UnitPropertiesUpdatePacketBuilder(this, changedProperties);
                    OnBroadcastPacket(builder.ToProxy());
                }
            }

            if (e == null)
                return;

            OnUpdated(e);
        }

        public event UnitEventHandler<Packet> BroadcastPacket;
        public event UnitEventHandler<Effect, bool /* apply */> EffectChanged;

        protected virtual void OnBroadcastPacket(IBuilder<Packet> packetBuilder)
        {
            BroadcastPacket?.Invoke(this, packetBuilder.Build());
        }

        protected virtual void OnEffectChanged(Effect effect,bool apply)
        {
            EffectChanged?.Invoke(this, effect, apply);

            var canBroadcast = effect.Display;
            if (canBroadcast)
            {
                var packetBuilder = new EffectPacketBuilder(effect, apply);
                OnBroadcastPacket(packetBuilder.ToProxy());
            }
        }

        public void SendRefreshUnitPacket()
        {
            OnBroadcastPacket(UnitEnterPacketBuilder.Create(this,ZoneEnterType.Update).ToProxy());
        }

        public void AddToZone(IZone zone,Position position,ZoneEnterType enterType = ZoneEnterType.Default,IBeamBuilder enterBeamBuilder = null)
        {
            _zone = zone;
            CurrentPosition = zone.FixZ(position);

            OnEnterZone(zone,enterType);

            zone.AddUnit(this);

            if (enterBeamBuilder != null)
                zone.CreateBeam(enterBeamBuilder);

            EnterPacketBuilder = UnitEnterPacketBuilder.Create(this, enterType);
            zone.UpdateUnitRelations(this);
            EnterPacketBuilder = UnitEnterPacketBuilder.Create(this, ZoneEnterType.Default);
        }

        protected virtual void OnEnterZone(IZone zone, ZoneEnterType enterType) { }

        public event UnitEventHandler RemovedFromZone;

        public void RemoveFromZone(IBeamBuilder exitBeamBuilder = null)
        {
            IZone zone;
            if ((zone = Interlocked.CompareExchange(ref _zone,null,_zone)) == null)
                return;

            Debug.Assert(zone != null, "zone != null");

            if (exitBeamBuilder != null)
                zone.CreateBeam(exitBeamBuilder);

            zone.RemoveUnit(this);

            OnRemovedFromZone(zone);
            zone.UpdateUnitRelations(this);
            RemovedFromZone?.Invoke(this);
        }

        protected virtual void OnRemovedFromZone(IZone zone) { }

        public void TakeDamage(DamageInfo damageInfo)
        {
            _damageProcessor.TakeDamage(damageInfo);
        }

        public event UnitEventHandler<Unit, DamageTakenEventArgs> DamageTaken;

        protected virtual void OnDamageTaken(Unit source, DamageTakenEventArgs e)
        {
            DamageTaken?.Invoke(this,source, e);

            var packet = new CombatLogPacket(CombatLogType.Damage, this, source);
            packet.AppendByte((byte)(e.IsCritical ? 1 : 0));
            packet.AppendDouble(e.TotalDamage);
            packet.AppendDouble(e.TotalKers);
            packet.Send(this, source);

            if (!(e.TotalDamage >= 0.0))
                return;

            Armor -= e.TotalDamage;

            OnCombatEvent(source,e);

            if (Armor <= 0.0)
            {
                Kill(source);
            }
        }

        public virtual void OnCombatEvent(Unit source, CombatEventArgs e)
        {
            
        }


        public double GetDistance(Unit unit)
        {
            return GetDistance(unit.CurrentPosition);
        }

        private double GetDistance(Position targetPosition)
        {
            return CurrentPosition.TotalDistance3D(targetPosition);
        }

        public event UnitEventHandler<UnitUpdatedEventArgs> Updated;

        private void OnUpdated(UnitUpdatedEventArgs e)
        {
            Updated?.Invoke(this, e);
        }

        protected virtual void DoExplosion()
        {
            var zone = Zone;
            if ( zone == null )
                return;

            if ( zone.Configuration.Protected )
                return;

            var damageBuilder = GetExplosionDamageBuilder();
            Task.Delay(FastRandom.NextInt(0, 3000)).ContinueWith(t => zone.DoAoeDamage(damageBuilder));
        }

        private IDamageBuilder GetExplosionDamageBuilder()
        {
            var radius = SignatureRadius * 0.5; //Note: reduced for increased bot srf-areas
            var damageBuilder = DamageInfo.Builder.WithAttacker(this)
                                          .WithOptimalRange(1)
                                          .WithFalloff(radius)
                                          .WithExplosionRadius(radius);

            var armorMaxValue = ArmorMax;

            if (armorMaxValue.IsZero())
                armorMaxValue = 1.0;

            var coreMax = CoreMax;

            if (coreMax.IsZero())
                coreMax = 1.0;

            var damage = (Math.Sin( Core.Ratio(coreMax) * Math.PI) + 1)*(armorMaxValue*0.1);
            damageBuilder.WithAllDamageTypes(damage);
            return damageBuilder;
        }

        private class KillDetectorHelper : IEntityVisitor<PBSTurret>,IEntityVisitor<PBSDockingBase>,IEntityVisitor<PBSObject>
        {
            public KillDetectorHelper()
            {
                CanBeKilledResult = true;
            }

            public bool CanBeKilledResult { get; private set; }

            private bool CanBeKilled(IPBSObject pbsObject)
            {
                return pbsObject.ReinforceHandler.CurrentState.CanBeKilled;
            }

            public void Visit(PBSTurret turret)
            {
                CanBeKilledResult = CanBeKilled(turret);
            }

            public void Visit(PBSDockingBase dockingBase)
            {
                CanBeKilledResult = CanBeKilled(dockingBase);
            }

            public void Visit(PBSObject pbsObject)
            {
                CanBeKilledResult = CanBeKilled(pbsObject);
            }
        }


        public void Kill(Unit killer = null)
        {
            if (!Monitor.TryEnter(_killSync) )
                return;

            try
            {
                var detector = new KillDetectorHelper();

                AcceptVisitor(detector);

                if (!detector.CanBeKilledResult)
                    return;

                if (States.Dead || !InZone)
                    return;

                if (killer != null)
                {
                    var killingBlowPacket = new CombatLogPacket(CombatLogType.KillingBlow, this, killer);
                    killingBlowPacket.Send(this, killer);

                    OnCombatEvent(killer, new KillingBlowEventArgs());
                }

                States.Dead = true;
                OnDead(killer);
            }
            finally
            {
                Monitor.Exit(_killSync);
            }
        }

        protected virtual bool CanBeKilled()
        {
            return true;
        }

        public event Action<Unit /* killer */,Unit /* victim */> Dead;

        protected virtual void OnDead(Unit killer)
        {
            Dead?.Invoke(killer,this);

            DoExplosion();

            Logger.Info($"Unit died. Killer = {(killer != null ? killer.InfoString : "")} Victim = {InfoString}");
            RemoveFromZone(new WreckBeamBuilder(this));
        }

        public bool IsInRangeOf3D(Unit target, double range)
        {
            return IsInRangeOf3D(target.CurrentPosition, range);
        }

        public bool IsInRangeOf3D(Position targetPosition, double range)
        {
            return CurrentPosition.IsInRangeOf3D(targetPosition, range);
        }

        public event UnitEventHandler TileChanged;

        protected virtual void OnTileChanged()
        {
            TileChanged?.Invoke(this);
        }

        protected virtual void OnCellChanged(CellCoord lastCellCoord, CellCoord currentCellCoord) {}

        public override Dictionary<string, object> ToDictionary()
        {
            var result = base.ToDictionary();

            if (!InZone) 
                return result;

            result.Add(k.px, CurrentPosition.X);
            result.Add(k.py, CurrentPosition.Y);
            result.Add(k.pz, CurrentPosition.Z);
            result.Add(k.orientation, (byte)(_orientation * 255));

            var standingControlled = this as IStandingController;
            standingControlled?.AddStandingInfoToDictonary(result);

            return result;
        }

        public virtual string InfoString => $"Unit:{ED.Name}:{Definition}:{Eid}";

        public virtual void OnAggression(Unit victim)
        {
        }

        public double GetKersByDamageType(DamageType damageType)
        {
            switch (damageType)
            {
                case DamageType.Chemical: { return _kersChemical.Value; }
                case DamageType.Thermal: { return _kersThermal.Value; }
                case DamageType.Kinetic: { return _kersKinetic.Value; }
                case DamageType.Explosive: { return _kersExplosive.Value; }
            }
            return 1.0;
        }

        public double GetResistByDamageType(DamageType damageType)
        {
            var resist = 0.0;

            switch (damageType)
            {
                case DamageType.Chemical: { resist = _resistChemical.Value; break; }
                case DamageType.Thermal: { resist = _resistThermal.Value; break; }
                case DamageType.Kinetic: { resist = _resistKinetic.Value; break; }
                case DamageType.Explosive: { resist = _resistExplosive.Value; break; }
            }

            resist /= (resist + 100);
            return resist;
        }

        public EffectBuilder NewEffectBuilder()
        {
            var builder = EffectBuilderFactory();
            builder.WithOwner(this);
            return builder;
        }

        public void ApplyEffect(EffectBuilder builder)
        {
            EffectHandler.Apply(builder);
        }

        public void ApplyPvPEffect()
        {
            ApplyPvPEffect(TimeSpan.Zero);
        }

        public void ApplyPvPEffect(TimeSpan duration)
        {
            var effect = EffectHandler.GetEffectsByType(EffectType.effect_pvp).FirstOrDefault();
            var token = effect?.Token ?? EffectToken.NewToken();
            var builder = NewEffectBuilder();
            builder.SetType(EffectType.effect_pvp).WithDuration(duration).WithToken(token);
            ApplyEffect(builder);
        }

        public virtual bool IsWalkable(Vector2 position)
        {
            return Zone.IsWalkable((int) position.X, (int) position.Y, Slope);
        }

        public virtual bool IsWalkable(Position position)
        {
            return Zone.IsWalkable((int) position.X,(int)position.Y, Slope);
        }

        public virtual bool IsWalkable(int x,int y)
        {
            return Zone.IsWalkable(x, y,Slope);
        }

        public virtual IDictionary<string, object> GetDebugInfo()
        {
            var info = new Dictionary<string, object>
            {
                {k.eid, Eid},
                {k.definitionName, ED.Name},
                {k.owner, Owner},
                {k.state, States.ToString()},
                {"p", ItemProperty.ToDebugString(Properties)}
            };


            var counter = 0;
            foreach (var effect in EffectHandler.Effects)
            {
                info.Add("e" + counter++, effect.Type.ToString());
            }

            var standingControlled = this as IStandingController;
            standingControlled?.AddStandingInfoToDictonary(info);

            return info;
        }

        public void ApplyEffectPropertyModifiers(AggregateField modifierField, ref ItemPropertyModifier modifier)
        {
            foreach (var effect in EffectHandler.Effects)
            {
                effect.ApplyTo(ref modifier, modifierField);
            }
        }

        private class UnitEnterPacketBuilder : IBuilder<Packet>
        {
            private readonly ZoneEnterType _enterType;
            private readonly Unit _unit;

            private UnitEnterPacketBuilder(Unit unit,ZoneEnterType enterType)
            {
                _unit = unit;
                _enterType = enterType;
            }

            public Packet Build()
            {
                var packet = new Packet(ZoneCommand.EnterUnit);

                packet.AppendLong(_unit.Eid);
                var character = _unit.GetCharacter();
                packet.AppendInt(character.Id);

                packet.AppendPosition(_unit.CurrentPosition);
                packet.AppendByte((byte)(_unit.CurrentSpeed * 255));
                packet.AppendByte((byte)(_unit.Orientation * byte.MaxValue));
                packet.AppendByte((byte)(_unit.Direction * byte.MaxValue));

                var desc = GetDescription(_unit);
                packet.AppendByteArray(desc);
                packet.AppendByte((byte)_enterType);
                _unit.States.AppendToPacket(packet);
                packet.AppendDouble(_unit.ArmorMax);
                packet.AppendDouble(_unit.Armor);
                packet.AppendDouble(_unit._speedMax.Value);
                packet.AppendLong(_unit.Owner);

                var robot = _unit as Robot;
                if (robot == null)
                {
                    packet.AppendByte(0);
                }
                else
                {
                    var primaryLock = robot.GetPrimaryLock();
                    if (primaryLock != null)
                    {
                        packet.AppendByte(1);
                        LockPacketBuilder.AppendTo(primaryLock,packet);
                    }
                    else
                    {
                        packet.AppendByte(0);
                    }
                }

                // effektek
                var effects = _unit.EffectHandler.Effects.ToArray();
                packet.AppendInt(effects.Length);

                foreach (var effect in effects)
                {
                    effect.AppendToStream(packet);
                }

                _unit.OptionalProperties.WriteToStream(packet);
                return packet;
            }

            private static byte[] GetDescription(Unit unit)
            {
                using (var stream = new MemoryStream())
                {
                    using (var bw = new BinaryWriter(stream))
                    {
                        bw.Write(unit.Definition);

                        var robot = unit as Robot;
                        if (robot != null)
                        {
                            WriteRobotComponent(bw, robot.GetRobotComponent<RobotHead>());
                            WriteRobotComponent(bw, robot.GetRobotComponent<RobotChassis>());
                            WriteRobotComponent(bw, robot.GetRobotComponent<RobotLeg>());
                        }
                        else
                        {
                            bw.Write(new byte[15]);
                        }

                        return stream.ToArray();
                    }
                }
            }

            private static void WriteRobotComponent(BinaryWriter bw, RobotComponent component)
            {
                if (component == null)
                {
                    bw.Write(0);
                    bw.Write((byte) 0);
                    return;
                }

                bw.Write(component.Definition);
                bw.Write((byte) component.MaxSlots);

                for (var i = 0; i < component.MaxSlots; i++)
                {
                    var module = component.GetModule(i + 1);
                    WriteModule(bw, module);
                }
            }

            private static void WriteModule(BinaryWriter bw, Module module)
            {
                var moduleDefinition = module?.Definition ?? 0;
                bw.Write(moduleDefinition);
            }

            public static IBuilder<Packet> Create(Unit unit, ZoneEnterType enterType)
            {
                return new UnitEnterPacketBuilder(unit,enterType);
            }
        }

        private class UnitExitPacketBuilder : IBuilder<Packet>
        {
            private readonly Unit _unit;

            public UnitExitPacketBuilder(Unit unit)
            {
                _unit = unit;
            }

            private ZoneExitType ExitType
            {
                get
                {
                    if (_unit.States.Dead)
                        return ZoneExitType.Died;

                    if (_unit.States.Dock)
                        return ZoneExitType.Docked;

                    if (_unit.States.Teleport)
                        return ZoneExitType.Teleport;

                    if (_unit.States.LocalTeleport)
                        return ZoneExitType.LocalTeleport;

                    return ZoneExitType.LeftGrid;
                }
            }

            public Packet Build()
            {
                var packet = new Packet(ZoneCommand.ExitUnit);
                packet.AppendLong(_unit.Eid);
                packet.AppendByte((byte)ExitType);
                return packet;
            }
        }

        protected bool IsHostile(Unit unit)
        {
            return unit.IsHostileFor(this);
        }

        protected virtual bool IsHostileFor(Unit unit)  { return false; }
        internal  virtual bool IsHostile(Player player) { return false; }
        internal  virtual bool IsHostile(AreaBomb bomb) { return false; }
        internal  virtual bool IsHostile(Gate gate)     { return false; }

        public void StopMoving()
        {
            CurrentSpeed = 0;
        }

        public IEnumerable<T> GetUnitsWithinRange<T>(double distance) where T : Unit
        {
            var zone = Zone;
            if (zone == null)
                return Enumerable.Empty<T>();

            return zone.Units.OfType<T>().WithinRange(CurrentPosition, distance);
        }

        protected override void OnPropertyChanged(ItemProperty property)
        {
            base.OnPropertyChanged(property);

            if (property.Field == AggregateField.blob_effect)
            {
                _sensorStrength.Update();
                _detectionStrength.Update();
            }
        }

        public double ArmorPercentage
        {
            get { return Armor.Ratio(ArmorMax); }
        }

        public double ArmorMax
        {
            get { return _armorMax.Value; }
        }

        public double Armor
        {
            get { return _armor.Value; }
            set { _armor.SetValue(value); }
        }

        public double ActualMass
        {
            get { return _actualMass.Value; }
        }

        public double CorePercentage
        {
            get { return Core.Ratio(CoreMax); }
        }

        public double CoreMax
        {
            get { return _coreMax.Value; }
        }

        public double Core
        {
            get { return _core.Value; }
            set { _core.SetValue(value); }
        }

        public double CriticalHitChance
        {
            get { return _criticalHitChance.Value; }
        }

        public double SignatureRadius
        {
            get { return _signatureRadius.Value; }
        }

        public double SensorStrength
        {
            get { return _sensorStrength.Value; }
        }

        public double DetectionStrength
        {
            get { return _detectionStrength.Value; }
        }

        public double StealthStrength
        {
            get { return _stealthStrength.Value; }
        }

        public double Massiveness
        {
            get { return _massiveness.Value; }
        }

        public double ReactorRadiation
        {
            get { return _reactorRadiation.Value; }
        }

        public double Slope
        {
            get { return _slope.Value; }
        }

        public double Speed
        {
            get
            {
                var speedMax = _speedMax.Value;
                return speedMax * _currentSpeed;
            }
        }

        public TimeSpan CoreRechargeTime => TimeSpan.FromSeconds(_coreRechargeTime.Value);

        private void InitUnitProperties()
        {
            _armorMax = new UnitProperty(this, AggregateField.armor_max, AggregateField.armor_max_modifier, AggregateField.effect_armor_max_modifier);

            _armorMax.PropertyChanged += property =>
            {
                if (Armor > property.Value)
                {
                    Armor = property.Value;
                }
            };

            AddProperty(_armorMax);

            _armor = new ArmorProperty(this);

            AddProperty(_armor);

            _coreMax = new UnitProperty(this, AggregateField.core_max, AggregateField.core_max_modifier);
            AddProperty(_coreMax);

            _core = new CoreProperty(this);
            _core.PropertyChanged += property =>
            {
                if (property.Value > 1.0)
                    return;
                EffectHandler.RemoveEffectsByCategory(EffectCategory.effcat_zero_core_drop);
            };
            AddProperty(_core);

            _coreRechargeTime = new UnitProperty(this, AggregateField.core_recharge_time, AggregateField.core_recharge_time_modifier, AggregateField.effect_core_recharge_time_modifier);
            AddProperty(_coreRechargeTime);

            _actualMass = new ActualMassProperty(this);
            AddProperty(_actualMass);

            _speedMax = new SpeedMaxProperty(this);
            AddProperty(_speedMax);

            _resistChemical = new UnitProperty(this, AggregateField.resist_chemical, AggregateField.resist_chemical_modifier, AggregateField.effect_resist_chemical);
            AddProperty(_resistChemical);

            _resistThermal = new UnitProperty(this, AggregateField.resist_thermal, AggregateField.resist_thermal_modifier, AggregateField.effect_resist_thermal);
            AddProperty(_resistThermal);

            _resistKinetic = new UnitProperty(this, AggregateField.resist_kinetic, AggregateField.resist_kinetic_modifier, AggregateField.effect_resist_kinetic);
            AddProperty(_resistKinetic);

            _resistExplosive = new UnitProperty(this, AggregateField.resist_explosive, AggregateField.resist_explosive_modifier, AggregateField.effect_resist_explosive);
            AddProperty(_resistExplosive);

            _slope = new UnitProperty(this, AggregateField.slope, AggregateField.slope_modifier);
            AddProperty(_slope);

            _criticalHitChance = new UnitProperty(this, AggregateField.critical_hit_chance, AggregateField.critical_hit_chance_modifier, AggregateField.effect_critical_hit_chance_modifier);
            AddProperty(_criticalHitChance);

            _massiveness = new UnitProperty(this, AggregateField.massiveness, AggregateField.massiveness_modifier, AggregateField.effect_massiveness);
            AddProperty(_massiveness);

            _signatureRadius = new UnitProperty(this, AggregateField.signature_radius, AggregateField.signature_radius_modifier, AggregateField.effect_signature_radius_modifier);
            AddProperty(_signatureRadius);

            _sensorStrength = new SensorStrengthProperty(this);
            AddProperty(_sensorStrength);

            _stealthStrength = new UnitProperty(this, AggregateField.stealth_strength, AggregateField.stealth_strength_modifier, AggregateField.effect_stealth_strength_modifier);
            _stealthStrength.PropertyChanged += property =>
            {
                UpdateTypes |= UnitUpdateTypes.Stealth;
            };
            AddProperty(_stealthStrength);

            _detectionStrength = new DetectionStrengthProperty(this);
            _detectionStrength.PropertyChanged += property =>
            {
                UpdateTypes |= UnitUpdateTypes.Detection;
            };
            AddProperty(_detectionStrength);

            _kersChemical = new UnitProperty(this, AggregateField.chemical_damage_to_core_modifier);
            AddProperty(_kersChemical);

            _kersThermal = new UnitProperty(this, AggregateField.thermal_damage_to_core_modifier);
            AddProperty(_kersThermal);

            _kersKinetic = new UnitProperty(this, AggregateField.kinetic_damage_to_core_modifier);
            AddProperty(_kersKinetic);

            _kersExplosive = new UnitProperty(this, AggregateField.explosive_damage_to_core_modifier);
            AddProperty(_kersExplosive);

            _reactorRadiation = new UnitProperty(this, AggregateField.reactor_radiation, AggregateField.reactor_radiation_modifier);
            AddProperty(_reactorRadiation);
        }

        private class ArmorProperty : UnitProperty
        {
            public ArmorProperty(Unit owner) : base(owner, AggregateField.armor_current) { }

            protected override double CalculateValue()
            {
                var armor = owner.ArmorMax;

                if (owner.DynamicProperties.Contains(k.armor))
                {
                    var armorPercentage = owner.DynamicProperties.GetOrAdd<double>(k.armor);
                    armor = CalculateArmorByPercentage(armorPercentage);
                }

                return armor;
            }

            protected override void OnPropertyChanging(ref double newValue)
            {
                base.OnPropertyChanging(ref newValue);

                if (newValue < 0.0)
                {
                    newValue = 0.0;
                    return;
                }

                var armorMax = owner.ArmorMax;
                if (newValue >= armorMax)
                {
                    newValue = armorMax;
                }
            }

            private double CalculateArmorByPercentage(double percent)
            {
                if (double.IsNaN(percent))
                    percent = 0.0;

                // 0.0 - 1.0
                percent = percent.Clamp();

                var armorMax = owner.ArmorMax;

                if (double.IsNaN(armorMax))
                {
                    armorMax = 0.0;
                }

                var val = armorMax * percent;
                return val;
            }

        }

        private class CoreProperty : UnitProperty
        {
            public CoreProperty(Unit owner) : base(owner, AggregateField.core_current) { }

            protected override double CalculateValue()
            {
                var currentCore = owner.CoreMax;

                if (owner.DynamicProperties.Contains(k.currentCore))
                {
                    currentCore = owner.DynamicProperties.GetOrAdd<double>(k.currentCore);
                }

                return currentCore;
            }

            protected override void OnPropertyChanging(ref double newValue)
            {
                base.OnPropertyChanging(ref newValue);

                newValue = newValue.Clamp(1, owner.CoreMax);
            }
        }

        private class ActualMassProperty : UnitProperty
        {
            public ActualMassProperty(Unit owner) : base(owner, AggregateField.mass) { }

            protected override double CalculateValue()
            {
                var mass = owner.Mass;
                var massMod = owner.GetPropertyModifier(AggregateField.mass_modifier);
                massMod.Modify(ref mass);

                if (owner is Robot robot)
                {
                    mass += robot.Modules.Sum(m => m.Mass);
                }

                return mass;
            }
        }

        private class DetectionStrengthProperty : UnitProperty
        {
            public DetectionStrengthProperty(Unit owner)
                : base(owner, AggregateField.detection_strength, AggregateField.detection_strength_modifier, AggregateField.effect_detection_strength_modifier)
            {
            }

            protected override double CalculateValue()
            {
                var v = base.CalculateValue();

                var blobableUnit = owner as IBlobableUnit;
                blobableUnit?.BlobHandler.ApplyBlobPenalty(ref v, 0.75);

                return v;
            }
        }

        private class SensorStrengthProperty : UnitProperty
        {
            public SensorStrengthProperty(Unit owner)
                : base(owner, AggregateField.sensor_strength, AggregateField.sensor_strength_modifier, AggregateField.effect_sensor_strength_modifier)
            {
            }

            protected override double CalculateValue()
            {
                var v = base.CalculateValue();

                var blobableUnit = owner as IBlobableUnit;
                blobableUnit?.BlobHandler.ApplyBlobPenalty(ref v, 0.5);

                return v;
            }
        }

        public static Unit CreateUnitWithRandomEID(string definitionName)
        {
            return (Unit)Factory.Create(EntityDefault.GetByName(definitionName), EntityIDGenerator.Random);
        }

        public int BlockingRadius => ED.Config.blockingradius ?? 1;
        public double HitSize => ED.Config.HitSize;
    }
}