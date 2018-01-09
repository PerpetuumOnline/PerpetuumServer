using System;
using Perpetuum.ExportedTypes;
using Perpetuum.Timers;
using Perpetuum.Zones.Effects;

namespace Perpetuum.Units
{
    public delegate bool UnitDespawnerCanApplyEffect(Unit unit);

    public class UnitDespawnHelper 
    {
        private readonly TimeSpan _despawnTime;
        private readonly IntervalTimer _timer = new IntervalTimer(500);

        private UnitDespawnHelper(TimeSpan despawnTime)
        {
            _despawnTime = despawnTime;
        }

        public UnitDespawnerCanApplyEffect CanApplyDespawnEffect { private get; set; }

        public void Update(TimeSpan time,Unit unit)
        {
            _timer.Update(time).IsPassed(() =>
            {
                TryReApplyDespawnEffect(unit);

                if (unit.HasDespawnEffect)
                    return;

                CanApplyDespawnEffect = null;

                unit.States.Teleport = true; //kis villam visual amikor kiszedi
                unit.RemoveFromZone();
            });
        }

        private readonly EffectToken _effectToken = EffectToken.NewToken();

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