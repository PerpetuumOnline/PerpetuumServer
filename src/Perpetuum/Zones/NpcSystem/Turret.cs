using System;
using System.Collections.Generic;
using System.Linq;
using Perpetuum.EntityFramework;
using Perpetuum.Modules;
using Perpetuum.Modules.EffectModules;
using Perpetuum.Modules.Weapons;
using Perpetuum.Players;
using Perpetuum.StateMachines;
using Perpetuum.Timers;
using Perpetuum.Units;
using Perpetuum.Zones.Effects;
using Perpetuum.Zones.Locking;
using Perpetuum.Zones.Locking.Locks;
using Perpetuum.Zones.Terrains;

namespace Perpetuum.Zones.NpcSystem
{
    public class ModuleActivator : IEntityVisitor<WeaponModule>,
                                   IEntityVisitor<MissileWeaponModule>,
                                   IEntityVisitor<ArmorRepairModule>,
                                   IEntityVisitor<ShieldGeneratorModule>,
                                   IEntityVisitor<SensorJammerModule>,
                                   IEntityVisitor<SensorDampenerModule>,
                                   IEntityVisitor<WebberModule>,
                                   IEntityVisitor<EnergyNeutralizerModule>,
                                   IEntityVisitor<EnergyVampireModule>,
                                   IEntityVisitor<SensorBoosterModule>,
                                   IEntityVisitor<ArmorHardenerModule>,
                                   IEntityVisitor<BlobEmissionModulatorModule>,
                                   IEntityVisitor<TargetBlinderModule>,
                                   IEntityVisitor<CoreBoosterModule>
    {
        private readonly IntervalTimer _timer;
        private readonly ActiveModule _module;

        public ModuleActivator(ActiveModule module)
        {
            _module = module;
            _timer = new IntervalTimer(TimeSpan.FromSeconds(1),true);
        }

        public void Update(TimeSpan time)
        {
            _timer.Update(time);

            if (!_timer.Passed)
                return;

            _timer.Reset();

            if (_module.State.Type != ModuleStateType.Idle)
                return;

            _module.AcceptVisitor(this);
        }

        private void TryActiveModule(LOSResult result, UnitLock primaryLock)
        {
            if (result.hit && !result.blockingFlags.HasFlag(BlockingFlags.Plant))
                return;

            _module.Lock = primaryLock;
            _module.State.SwitchTo(ModuleStateType.Oneshot);
        }

        public void Visit(MissileWeaponModule module)
        {
            var hasShieldEffect = module.ParentRobot.HasShieldEffect;
            if (hasShieldEffect)
                return;

            var primaryLock = module.ParentRobot.GetFinishedPrimaryLock();
            if (primaryLock == null)
                return;

            var visibility = module.ParentRobot.GetVisibility(primaryLock.Target);
            if (visibility == null)
                return;

            var result = visibility.GetLineOfSight(true);
            TryActiveModule(result, primaryLock);
        }

        public void Visit(WeaponModule module)
        {
            var hasShieldEffect = module.ParentRobot.HasShieldEffect;
            if (hasShieldEffect)
                return;

            var primaryLock = module.ParentRobot.GetFinishedPrimaryLock();
            if (primaryLock == null)
                return;

            var visibility = module.ParentRobot.GetVisibility(primaryLock.Target);
            if (visibility == null)
                return;

            var result = visibility.GetLineOfSight(false);
            TryActiveModule(result,primaryLock);
        }

        private const double ARMOR_REPAIR_THRESHOLD = 0.95;
        private const double ARMOR_REPAIR_CORE_THRESHOLD = 0.35;

        public void Visit(ArmorRepairModule module)
        {
            if (module.ParentRobot.ArmorPercentage >= ARMOR_REPAIR_THRESHOLD)
                return;

            if (module.ParentRobot.CorePercentage < ARMOR_REPAIR_CORE_THRESHOLD)
                return;

            module.State.SwitchTo(ModuleStateType.Oneshot);
        }

        private const double SHIELD_ARMOR_THRESHOLD = 0.35;

        public void Visit(ShieldGeneratorModule module)
        {
            if (module.ParentRobot.ArmorPercentage >= SHIELD_ARMOR_THRESHOLD)
                return;

            module.State.SwitchTo(ModuleStateType.Oneshot);
        }

        private const double SENSOR_JAMMER_CORE_THRESHOLD = 0.55;
        
        public void Visit(SensorJammerModule module)
        {
            if ( module.ParentRobot.CorePercentage < SENSOR_JAMMER_CORE_THRESHOLD )
                return;

            var lockTarget = ((Creature)module.ParentRobot).SelectOptimalLockTargetFor(module);
            if (lockTarget == null)
                return;

            module.Lock = lockTarget;
            module.State.SwitchTo(ModuleStateType.Oneshot);
        }

        private const double SENSOR_DAMPENER_CORE_THRESHOLD = 0.55;
        
        public void Visit(SensorDampenerModule module)
        {
            if (module.ParentRobot.CorePercentage < SENSOR_DAMPENER_CORE_THRESHOLD)
                return;

            var lockTarget = ((Creature)module.ParentRobot).SelectOptimalLockTargetFor(module);
            if (lockTarget == null)
                return;

            module.Lock = lockTarget;
            module.State.SwitchTo(ModuleStateType.Oneshot);
        }

        private const double WEBBER_CORE_THRESHOLD = 0.55;

        public void Visit(WebberModule module)
        {
            if (module.ParentRobot.CorePercentage < WEBBER_CORE_THRESHOLD)
                return;

            var lockTarget = ((Creature)module.ParentRobot).SelectOptimalLockTargetFor(module);
            if (lockTarget == null)
                return;

            module.Lock = lockTarget;
            module.State.SwitchTo(ModuleStateType.Oneshot);
        }

        private const double BLOBBER_CORE_THRESHOLD = 0.55;

        public void Visit(BlobEmissionModulatorModule module)
        {
            if (module.ParentRobot.Zone.Configuration.Protected)
                return;

            if (module.ParentRobot.CorePercentage < BLOBBER_CORE_THRESHOLD)
                return;

            var lockTarget = ((Creature)module.ParentRobot).SelectOptimalLockTargetFor(module);
            if (lockTarget == null)
                return;

            module.Lock = lockTarget;
            module.State.SwitchTo(ModuleStateType.Oneshot);
        }


        private const double BLINDER_CORE_THRESHOLD = 0.55;

        public void Visit(TargetBlinderModule module)
        {
            if (module.ParentRobot.CorePercentage < BLINDER_CORE_THRESHOLD)
                return;

            var lockTarget = ((Creature)module.ParentRobot).SelectOptimalLockTargetFor(module);
            if (lockTarget == null)
                return;

            module.Lock = lockTarget;
            module.State.SwitchTo(ModuleStateType.Oneshot);
        }


        private const double ENERGY_NEUTRALIZER_CORE_THRESHOLD = 0.55;

        public void Visit(EnergyNeutralizerModule module)
        {
            if (module.ParentRobot.CorePercentage < ENERGY_NEUTRALIZER_CORE_THRESHOLD)
                return;

            var lockTarget = ((Creature)module.ParentRobot).SelectOptimalLockTargetFor(module);
            if ( lockTarget == null )
                return;

            var visibility = module.ParentRobot.GetVisibility(lockTarget.Target);
            if (visibility == null)
                return;

            var r = visibility.GetLineOfSight(false);
            if (r != null)
            {
                if (r.hit)
                    return;
            }

            module.Lock = lockTarget;
            module.State.SwitchTo(ModuleStateType.Oneshot);
        }

        private const double ENERGY_VAMPIRE_CORE_THRESHOLD = 0.05;

        public void Visit(EnergyVampireModule module)
        {
            if ( module.ParentRobot.CorePercentage < ENERGY_VAMPIRE_CORE_THRESHOLD )
                return;

            var lockTarget = ((Creature)module.ParentRobot).SelectOptimalLockTargetFor(module);
            if ( lockTarget == null )
                return;

            var visibility = module.ParentRobot.GetVisibility(lockTarget.Target);
            if (visibility == null)
                return;

            var r = visibility.GetLineOfSight(false);
            if (r != null)
            {
                if (r.hit)
                    return;
            }

            module.Lock = lockTarget;
            module.State.SwitchTo(ModuleStateType.Oneshot);
        }

        public void Visit(SensorBoosterModule module)
        {
            if (module.State.Type == ModuleStateType.Idle)
            {
                module.State.SwitchTo(ModuleStateType.AutoRepeat);
            }
        }

        public void Visit(ArmorHardenerModule module)
        {
            if (module.State.Type == ModuleStateType.Idle)
            {
                module.State.SwitchTo(ModuleStateType.AutoRepeat);
            }
        }

        private const double ENERGY_INJECTOR_THRESHOLD = 0.65;

        public void Visit(CoreBoosterModule module)
        {
            if (module.ParentRobot.CorePercentage > ENERGY_INJECTOR_THRESHOLD)
                return;

            module.State.SwitchTo(ModuleStateType.Oneshot);
        }
    }



    public class Turret : Creature
    {
        private readonly NullAI _nullAI;
        private readonly FiniteStateMachine<TurretAI> _ai;

        protected Turret()
        {
            _nullAI = new NullAI(this);
            _ai = new FiniteStateMachine<TurretAI>();
        }

        protected TurretAI AI
        {
            get { return _ai.Current ?? _nullAI; }
        }

        protected override void OnEnterZone(IZone zone, ZoneEnterType enterType)
        {
            AI.ToActiveAI();
            base.OnEnterZone(zone, enterType);
        }

        protected override void OnUnitTileChanged(Unit unit)
        {
            AI.AttackHostile(unit);
        }

        protected override void OnUnitEffectChanged(Unit unit, Effect effect, bool apply)
        {
            if (effect is InvulnerableEffect && !apply)
            {
                AI.AttackHostile(unit);
            }
            
            base.OnUnitEffectChanged(unit, effect, apply);
        }

        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
                base.AcceptVisitor(visitor);
        }

        protected override void OnUpdate(TimeSpan time)
        {
            _ai.Update(time);
            base.OnUpdate(time);
        }

        protected void LockHostile(Unit unit,bool force = false)
        {
            if (IsLocked(unit))
                return;

            if (!force && !IsHostile(unit))
                return;

            AddLock(unit, false);
        }

        internal override bool IsHostile(Player player)
        {
            return true;
        }

        protected abstract class TurretAI : IState
        {
            private readonly Turret _turret;

            protected TurretAI(Turret turret)
            {
                _turret = turret;
            }

            public virtual void Enter()
            {
                WriteLog("Turret AI:" + GetType().Name);
            }

            public virtual void Exit()
            {
                WriteLog("Turret AI:" + GetType().Name);
            }

            public virtual void Update(TimeSpan time)
            {

            }

            protected void WriteLog(string message)
            {
//                Logger.Info(message);
            }

            public virtual void ToInactiveAI()
            {
                _turret._ai.ChangeState(new InactiveAI(_turret));
            }

            public virtual void ToActiveAI()
            {
                _turret._ai.ChangeState(new ActiveAI(_turret));
            }

            public virtual void AttackHostile(Unit unit)
            {
                
            }
        }

        private class NullAI : TurretAI
        {
            public NullAI(Turret turret) : base(turret)
            {
            }
        }

        private class InactiveAI : TurretAI
        {
            private readonly Turret _turret;

            public InactiveAI(Turret turret) : base(turret)
            {
                _turret = turret;
            }

            public override void Enter()
            {
                _turret.StopAllModules();
                _turret.ResetLocks();
                base.Enter();
            }

            public override void ToInactiveAI()
            {
                // nem csinal semmit   
            }
        }

        private class ActiveAI : TurretAI
        {
            private readonly Turret _turret;

            private readonly List<ModuleActivator> _moduleActivators;

            public ActiveAI(Turret turret) : base(turret)
            {
                _turret = turret;

                _moduleActivators = new List<ModuleActivator>();

                _minCycleTime = TimeSpan.FromSeconds(5);

                foreach (var module in _turret.ActiveModules)
                {
                    _moduleActivators.Add(new ModuleActivator(module));
                    _minCycleTime = _minCycleTime.Min(module.CycleTime);
                }
            }

            public override void Enter()
            {
                foreach (var unitVisibility in _turret.GetVisibleUnits())
                {
                    _turret.LockHostile(unitVisibility.Target);
                }

                base.Enter();
            }

            private readonly TimeSpan _minCycleTime;

            public override void Exit()
            {
                _turret.StopAllModules();
                _turret.ResetLocks();
                base.Exit();
            }

            public override void AttackHostile(Unit unit)
            {
                if (!_turret.IsHostile(unit))
                    return;

                _turret.LockHostile(unit);
                base.AttackHostile(unit);
            }

            private readonly IntervalTimer _primarySelectTimer = new IntervalTimer(0);

            public override void Update(TimeSpan time)
            {
                if ( !SelectPrimaryTarget(time) )
                    return;

                foreach (var activator in _moduleActivators)
                {
                    activator.Update(time);
                }

                base.Update(time);
            }

            private bool SelectPrimaryTarget(TimeSpan time)
            {
                var locks = _turret.GetLocks().Where(l => l.State == LockState.Locked).ToArray();
                if (locks.Length <= 0)
                    return false;

                _primarySelectTimer.Update(time);

                if (_primarySelectTimer.Passed)
                {
                    _primarySelectTimer.Interval = FastRandom.NextTimeSpan(_minCycleTime);

                    var validLocks = new List<UnitLock>();

                    foreach (var l in locks)
                    {
                        var unitLock = (UnitLock)l;

                        if (unitLock.Primary)
                            continue;

                        var visibility = _turret.GetVisibility(unitLock.Target);
                        if (visibility == null)
                            continue;

                        var r = visibility.GetLineOfSight(false);
                        if (r != null)
                        {
                            if (r.hit && (r.blockingFlags & BlockingFlags.Plant) == 0)
                                continue;
                        }

                        validLocks.Add(unitLock);
                    }

                    if (validLocks.Count > 0)
                    {
                        var newPrimary = validLocks.RandomElement();
                        _turret.SetPrimaryLock(newPrimary);
                        return true;
                    }
                }

                return locks.Any(l => l.Primary);
            }

            public override void ToActiveAI()
            {
                // nem csinal semmit
            }
        }
    }
}