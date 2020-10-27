using Perpetuum.Log;
using Perpetuum.Players;
using Perpetuum.Units;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Perpetuum.Zones.Effects.ZoneEffects
{
    public class ZoneEffectHandler : IZoneEffectHandler
    {
        private readonly IZone _zone;
        private readonly ConcurrentDictionary<ZoneEffect, byte> _effects;

        public ZoneEffectHandler(IZone zone)
        {
            _zone = zone;
            _effects = new ConcurrentDictionary<ZoneEffect, byte>();
            InitCollection();
        }

        private void InitCollection()
        {
            foreach (var zoneEffect in ZoneEffectReader.GetStaticZoneEffects(_zone))
            {
                _effects.Add(zoneEffect, byte.MinValue);
            }
        }

        public void AddEffect(ZoneEffect zoneEffect)
        {
            _effects.TryAdd(zoneEffect, byte.MinValue);
            OnZoneEffectAdded(zoneEffect);
        }

        public void RemoveEffect(ZoneEffect zoneEffect)
        {
            _effects.TryRemove(zoneEffect, out byte b);
            OnZoneEffectRemoved(zoneEffect);
        }

        private void OnZoneEffectRemoved(ZoneEffect effect)
        {
            var eligibleUnits = _zone.Units.Where(u => u.EffectHandler.ContainsEffect(effect.Effect));
            foreach (var unit in eligibleUnits)
            {
                unit.EffectHandler.RemoveEffectsByType(effect.Effect);
                Logger.DebugInfo($"Removing {effect.Effect} to {unit} on zone:{_zone.Id}");
            }
        }

        private void OnZoneEffectAdded(ZoneEffect effect)
        {
            var eligibleUnits = _zone.Units.Where(u => CanApplyEffect(effect)(u));
            foreach (var unit in eligibleUnits)
            {
                var builder = unit.NewEffectBuilder().SetType(effect.Effect).SetOwnerToSource();
                unit.ApplyEffect(builder);
                Logger.DebugInfo($"Adding {effect.Effect} to {unit} on zone:{_zone.Id}");
            }
        }

        private IEnumerable<ZoneEffect> GetEffects()
        {
            return _effects.Keys;
        }

        private Func<Unit, bool> CanApplyEffect(ZoneEffect effect)
        {
            return u => !u.EffectHandler.ContainsEffect(effect.Effect) && (!effect.PlayerOnly || u is Player);
        }

        public void OnEnterZone(Unit unit)
        {
            foreach (var zoneEffect in GetEffects())
            {
                if (CanApplyEffect(zoneEffect)(unit))
                {
                    var builder = unit.NewEffectBuilder().SetType(zoneEffect.Effect).SetOwnerToSource();
                    unit.ApplyEffect(builder);
                }
            }
        }
    }
}
