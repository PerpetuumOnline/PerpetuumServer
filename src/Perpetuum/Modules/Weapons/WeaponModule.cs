using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Units;
using Perpetuum.Zones;
using Perpetuum.Zones.Beams;
using Perpetuum.Zones.Locking;
using Perpetuum.Zones.Locking.Locks;
using Perpetuum.Zones.Terrains;

namespace Perpetuum.Modules.Weapons
{
    public class WeaponModule : ActiveModule
    {
        private readonly ModuleAction _action;

        private readonly ModuleProperty _damageModifier;
        private readonly ModuleProperty _accuracy;

        public ModuleProperty DamageModifier
        {
            get { return _damageModifier; }
        }

        public WeaponModule(CategoryFlags ammoCategoryFlags) : base(ammoCategoryFlags, true)
        {
            _action = new ModuleAction(this);
            _damageModifier = new ModuleProperty(this,AggregateField.damage_modifier);
            AddProperty(_damageModifier);
            _accuracy = new ModuleProperty(this, AggregateField.accuracy);
            AddProperty(_accuracy);

            cycleTime.AddEffectModifier(AggregateField.effect_weapon_cycle_time_modifier);
        }

        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
                base.AcceptVisitor(visitor);
        }

        private class ModuleAction : ILockVisitor
        {
            private readonly WeaponModule _weapon;

            public ModuleAction(WeaponModule weapon)
            {
                _weapon = weapon;
            }

            public void DoAction()
            {
                _weapon.ParentRobot.HasShieldEffect.ThrowIfTrue(ErrorCodes.ShieldIsActive);

                var currentLock = _weapon.GetLock();
                currentLock?.AcceptVisitor(this);
            }

            public void VisitLock(Lock @lock)
            {

            }

            public void VisitUnitLock(UnitLock unitLock)
            {
                var victim = unitLock.Target;

                victim.InZone.ThrowIfFalse(ErrorCodes.TargetNotFound);
                victim.States.Dead.ThrowIfTrue(ErrorCodes.TargetIsDead);

                var err = victim.IsAttackable;
                if (err != ErrorCodes.NoError)
                    throw new PerpetuumException(err);

                victim.IsInvulnerable.ThrowIfTrue(ErrorCodes.TargetIsInvulnerable);

                _weapon.ConsumeAmmo();

                var result = _weapon.GetLineOfSight(victim);
                if (result.hit)
                {
                    DoDamageToPosition(result.position);
                    _weapon.OnError(ErrorCodes.LOSFailed);
                    return;
                }

                var distance = _weapon.ParentRobot.GetDistance(victim);
                var bulletTime = _weapon.GetAmmo().BulletTime;
                var flyTime = (int)((distance / bulletTime) * 1000);
                var beamTime = (int)Math.Max(flyTime, _weapon.CycleTime.TotalMilliseconds);
                
                var miss = _weapon.CheckAccuracy(victim);
                if (miss)
                {
                    _weapon.CreateBeam(victim, BeamState.Miss,beamTime,bulletTime);
                    _weapon.OnError(ErrorCodes.AccuracyCheckFailed);
                    return;
                }

                var delay = _weapon.CreateBeam(victim, BeamState.Hit, beamTime, bulletTime);
                flyTime += delay;

                var builder = _weapon.GetDamageBuilder();
                Task.Delay(flyTime).ContinueWith(t => victim.TakeDamage(builder.Build()));
            }

            public void VisitTerrainLock(TerrainLock terrainLock)
            {
                var location = terrainLock.Location;

                _weapon.ConsumeAmmo();

                var blockingInfo = _weapon?.ParentRobot?.Zone?.Terrain.Blocks.GetValue(terrainLock.Location) ?? BlockingInfo.None;
                location = location.AddToZ(Math.Min(blockingInfo.Height,20));

                var losResult = _weapon.GetLineOfSight(location);
                if (losResult.hit && !location.IsEqual2D(losResult.position))
                {
                    location = losResult.position;
                    _weapon.OnError(ErrorCodes.LOSFailed);
                }

                DoDamageToPosition(location);
            }

            private void DoDamageToPosition(Position location)
            {
                var distance = _weapon.ParentRobot.CurrentPosition.TotalDistance3D(location);
                var bulletTime = _weapon.GetAmmo().BulletTime;
                var flyTime = (int) ((distance/bulletTime)*1000);

                var beamTime = (int) Math.Max(flyTime, _weapon.CycleTime.TotalMilliseconds);
                flyTime += _weapon.CreateBeam(location, BeamState.Hit, beamTime, bulletTime);

                var damage = _weapon.GetCleanDamages().Sum(d => d.value);
                if (damage <= 0.0)
                    return;

                var zone = _weapon.Zone;
                if (zone == null)
                    return;

                Task.Delay(flyTime).ContinueWith(t => DealDamageToPosition(zone,location,damage));
            }

            private static void DealDamageToPosition(IZone zone,Position location,double damage)
            {
                using (new TerrainUpdateMonitor(zone))
                {
                    zone.DamageToPlantOnArea(Area.FromRadius(location,1),damage/2);
                }
            }
        }

        protected override void OnAction()
        {
            _action.DoAction();
        }

        protected virtual bool CheckAccuracy(Unit victim)
        {
            var rnd = FastRandom.NextDouble();
            var isMiss = rnd * _accuracy.Value > victim.SignatureRadius;
            return isMiss;
        }

        protected virtual IDamageBuilder GetDamageBuilder()
        {
            return DamageInfo.Builder.WithAttacker(ParentRobot)
                                .WithOptimalRange(OptimalRange)
                                .WithFalloff(Falloff)
                                .WithDamages(GetCleanDamages());
        }

        private IEnumerable<Damage> GetCleanDamages()
        {
            var ammo = (WeaponAmmo)GetAmmo();
            return ammo != null ? ammo.GetCleanDamages() : new Damage[0];
        }
    }
}