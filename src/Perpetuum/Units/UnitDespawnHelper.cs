using System;
using Perpetuum.ExportedTypes;
using Perpetuum.Timers;
using Perpetuum.Zones.Effects;

namespace Perpetuum.Units
{
    public interface IUnitDespawnHelper
    {
        UnitDespawnerCanApplyEffect CanApplyDespawnEffect { set; }
        UnitDespawnStrategy DespawnStrategy { set; }
        EffectType DespawnEffect { get; }
        void Update(TimeSpan time, Unit unit);
    }

    public delegate bool UnitDespawnerCanApplyEffect(Unit unit);
    public delegate void UnitDespawnStrategy(Unit unit);

    public class UnitDespawnHelper : IUnitDespawnHelper
    {
        public UnitDespawnerCanApplyEffect CanApplyDespawnEffect { protected get; set; }
        public UnitDespawnStrategy DespawnStrategy { protected get; set; }

        public virtual EffectType DespawnEffect
        {
            get { return EffectType.effect_despawn_timer; }
        }

        protected readonly TimeSpan _despawnTime;
        protected readonly EffectToken _effectToken = EffectToken.NewToken();
        protected readonly IntervalTimer _timer = new IntervalTimer(650);

        protected UnitDespawnHelper(TimeSpan despawnTime)
        {
            _despawnTime = despawnTime;
        }

        protected bool HasEffectOrPending(Unit unit)
        {
            return unit.EffectHandler.ContainsOrPending(_effectToken);
        }

        public virtual void Update(TimeSpan time, Unit unit)
        {
            _timer.Update(time).IsPassed(() =>
            {
                TryReApplyDespawnEffect(unit);

                if (HasEffectOrPending(unit))
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

        protected void ApplyDespawnEffect(Unit unit)
        {
            var effectBuilder = unit.NewEffectBuilder().SetType(DespawnEffect).WithDuration(_despawnTime).WithToken(_effectToken);
            unit.ApplyEffect(effectBuilder);
        }

        public override string ToString()
        {
            return $"DespawnTime: {_despawnTime}";
        }

        public static UnitDespawnHelper Create(Unit unit, TimeSpan despawnTime)
        {
            var helper = new UnitDespawnHelper(despawnTime);
            helper.ApplyDespawnEffect(unit);
            return helper;
        }
    }

    public class CancellableDespawnHelper : UnitDespawnHelper
    {
        public override EffectType DespawnEffect
        {
            get { return EffectType.effect_stronghold_despawn_timer; }
        }
        private CancellableDespawnHelper(TimeSpan despawnTime) : base(despawnTime) { }

        private bool _canceled = false;
        public void Cancel(Unit unit)
        {
            _canceled = true;
            RemoveDespawnEffect(unit);
        }

        private void RemoveDespawnEffect(Unit unit)
        {
            unit.EffectHandler.RemoveEffectByToken(_effectToken);
        }

        public override void Update(TimeSpan time, Unit unit)
        {
            _timer.Update(time).IsPassed(() =>
            {
                if (_canceled || HasEffectOrPending(unit))
                    return;

                DespawnStrategy?.Invoke(unit);
            });
        }

        public new static CancellableDespawnHelper Create(Unit unit, TimeSpan despawnTime)
        {
            var helper = new CancellableDespawnHelper(despawnTime);
            helper.ApplyDespawnEffect(unit);
            return helper;
        }
    }
}