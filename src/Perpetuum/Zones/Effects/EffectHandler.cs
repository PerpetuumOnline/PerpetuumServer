using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Perpetuum.ExportedTypes;
using Perpetuum.Units;

namespace Perpetuum.Zones.Effects
{
    /// <summary>
    /// This handler takes care of every effect operation on the zone
    /// </summary>
    public class EffectHandler
    {
        private readonly Unit _unit;
        private List<Effect> _effects = new List<Effect>();
        private readonly ConcurrentQueue<EffectBuilder> _newEffects = new ConcurrentQueue<EffectBuilder>();
        private readonly ConcurrentQueue<Effect> _expiredEffects = new ConcurrentQueue<Effect>();
        private int _dirty;

        public EffectHandler(Unit unit)
        {
            _unit = unit;
        }

        public IEnumerable<Effect> Effects => _effects;

        public event EffectEventHandler<bool> EffectChanged;

        private void OnEffectChanged(Effect effect, bool apply)
        {
            EffectChanged?.Invoke(effect, apply);
        }

        public void Apply(EffectBuilder effectBuilder)
        {
            _newEffects.Enqueue(effectBuilder);
            Interlocked.Exchange(ref _dirty, 1);
        }

        public void Remove(Effect effect)
        {
            _expiredEffects.Enqueue(effect);
            Interlocked.Exchange(ref _dirty, 1);
        }

        public void Update(TimeSpan time)
        {
            if (Interlocked.CompareExchange(ref _dirty, 0, 1) == 1)
            {
                var effects = new List<Effect>(_effects);
                var helper = new EffectPropertyUpdateHelper();
                var updated = false;

                try
                {
                    foreach (var removedEffect in ProcessExpiredEffects(effects))
                    {
                        updated = true;
                        helper.AddEffect(removedEffect);
                        OnEffectChanged(removedEffect,false);
                    }

                    foreach (var newEffect in ProcessNewEffects(effects))
                    {
                        updated = true;
                        helper.AddEffect(newEffect);
                        OnEffectChanged(newEffect,true);
                    }
                }
                finally
                {
                    if (updated)
                    {
                        _effects = effects;
                        helper.Update(_unit);
                    }
                }
            }

            foreach (var effect in _effects)
            {
                effect.Update(time);
            }
        }

        private IEnumerable<Effect> ProcessNewEffects(List<Effect> effects)
        {
            while (_newEffects.TryDequeue(out EffectBuilder effectBuilder))
            {
                var effect = GetEffectByToken(effectBuilder.Token);
                if (effect != null)
                {
                    if (effect.IsAura)
                        continue;

                    var effectTimer = effect.Timer;
                    if (effectTimer == null)
                        continue;

                    effectTimer.Reset();
                    yield return effect;
                }
                else
                {
                    effect = effectBuilder.Build();

                    if (!CanApplyEffect(effects, effect))
                        continue;

                    effect.Removed += Remove;
                    effects.Add(effect);
                    yield return effect;
                }
            }
        }

        private static bool CanApplyEffect(List<Effect> effects,Effect effect)
        {
            var mask = 0x8000000000000000;
            do
            {
                var flag = (ulong)effect.Category & mask;
                if (flag > 0)
                {
                    var category = (EffectCategory)flag;
                    var maxLevel = EffectHelper.EffectCategoryLevels[category];
                    if (maxLevel > 0)
                    {
                        var currentLevel = effects.Count(e => e.Category.HasFlag(category));
                        if (currentLevel >= maxLevel)
                            return false;
                    }
                }

                mask >>= 1;
            } while (mask > 0);

            return true;
        }

        private IEnumerable<Effect> ProcessExpiredEffects(List<Effect> effects)
        {
            while (_expiredEffects.TryDequeue(out Effect expiredEffect))
            {
                if (!effects.Remove(expiredEffect))
                    continue;

                yield return expiredEffect;
            }
        }

        public void RemoveEffectsByType(EffectType type)
        {
            RemoveEffects(GetEffectsByType(type));
        }

        public void RemoveEffectsByCategory(EffectCategory category)
        {
            RemoveEffects(GetEffectsByCategory(category));
        }

        private void RemoveEffects(IEnumerable<Effect> effects)
        {
            foreach (var effect in effects)
            {
                Remove(effect);
            }
        }

        public void RemoveEffectByToken(EffectToken token)
        {
            var effect = GetEffectByToken(token);
            if ( effect == null )
                return;

            Remove(effect);
        }

        public bool ContainsToken(EffectToken token)
        {
            return Effects.Any(e => e.Token == token);
        }

        public bool ContainsEffect(EffectType type)
        {
            return Effects.Any(e => e.Type == type);
        }

        public bool ContainsEffect(EffectCategory category)
        {
            return Effects.Any(e => e.Category.HasFlag(category));
        }

        public IEnumerable<Effect> GetEffectsByType(EffectType type)
        {
            return _effects.Where(e => e.Type == type);
        }

        public IEnumerable<Effect> GetEffectsByCategory(EffectCategory category)
        {
            return _effects.Where(e => e.Category.HasFlag(category));
        }

        [CanBeNull]
        public Effect GetEffectByToken(EffectToken token)
        {
            return _effects.FirstOrDefault(e => e.Token == token);
        }

        private class EffectPropertyUpdateHelper
        {
            private readonly HashSet<AggregateField> _relatedFields = new HashSet<AggregateField>();

            public void AddEffect(Effect effect)
            {
                foreach (var modifier in effect.PropertyModifiers)
                {
                    _relatedFields.Add(modifier.Field);
                }
            }

            public void Update(Unit unit)
            {
                if ( _relatedFields.Count == 0 )
                    return;

                foreach (var modifier in _relatedFields)
                {
                    unit.UpdateRelatedProperties(modifier);
                }
            }
        }
    }
}