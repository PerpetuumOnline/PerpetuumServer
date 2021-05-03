using System;
using Perpetuum.ExportedTypes;
using Perpetuum.Timers;
using Perpetuum.Zones.Effects;

namespace Perpetuum.Units
{
    public delegate bool UnitDespawnerCanApplyEffect(Unit unit);
    public delegate void UnitDespawnStrategy(Unit unit);

    public class UnitDespawnHelper 
    {
        protected readonly TimeSpan _despawnTime;
        protected readonly IntervalTimer _timer = new IntervalTimer(650);

        protected UnitDespawnHelper(TimeSpan despawnTime)
        {
            _despawnTime = despawnTime;
        }

        public UnitDespawnerCanApplyEffect CanApplyDespawnEffect { protected get; set; }

        public UnitDespawnStrategy DespawnStrategy { protected get; set; }

        public virtual void Update(TimeSpan time,Unit unit)
        {
            _timer.Update(time).IsPassed(() =>
            {
                TryReApplyDespawnEffect(unit);

                if (unit.HasDespawnEffect)
                    return;

                CanApplyDespawnEffect = null;
                if (DespawnStrategy != null)
                {
                    DespawnStrategy(unit);
                }
                else
                {
                    unit.States.Teleport = true; //kis villam visual amikor kiszedi
                    unit.RemoveFromZone();
                }
            });
        }

        protected readonly EffectToken _effectToken = EffectToken.NewToken();

        private void TryReApplyDespawnEffect(Unit unit)
        {
            var canApplyDespawnEffect = CanApplyDespawnEffect;
            if (canApplyDespawnEffect == null)
                return;

            var applyDespawnEffect = canApplyDespawnEffect(unit);
            if (!applyDespawnEffect)
                return;

            ApplyDespawnEffect(unit);
        }

        private void ApplyDespawnEffect(Unit unit)
        {
            var effectBuilder = unit.NewEffectBuilder().SetType(EffectType.effect_despawn_timer).WithDuration(_despawnTime).WithToken(_effectToken);
            unit.ApplyEffect(effectBuilder);
        }

        public override string ToString()
        {
            return $"DespawnTime: {_despawnTime}";
        }

        public static UnitDespawnHelper Create(Unit unit,TimeSpan despawnTime)
        {
            var helper = new UnitDespawnHelper(despawnTime);
            helper.ApplyDespawnEffect(unit);
            return helper;
        }

    }
}