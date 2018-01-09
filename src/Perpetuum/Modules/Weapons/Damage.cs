using System;
using System.Collections.Generic;
using System.Diagnostics;
using Perpetuum.Builders;
using Perpetuum.Units;

namespace Perpetuum.Modules.Weapons
{
    public struct Damage
    {
        public readonly DamageType type;
        public readonly double value;

        public Damage(DamageType type, double value)
        {
            this.type = type;
            this.value = value;
        }
    }

    public interface IDamageBuilder : IBuilder<DamageInfo>
    {
        IDamageBuilder WithAttacker(Unit attacker);
        IDamageBuilder WithExplosionRadius(double explosionRadius);
        IDamageBuilder WithOptimalRange(double optimalRange);
        IDamageBuilder WithFalloff(double falloff);
        IDamageBuilder WithDamage(Damage damage);
    }

    public static class DamageBuilderExtensions
    {
        public static IDamageBuilder WithAllDamageTypes(this IDamageBuilder builder,double damage)
        {
            return builder.WithDamage(DamageType.Chemical, damage)
                          .WithDamage(DamageType.Explosive, damage)
                          .WithDamage(DamageType.Kinetic, damage)
                          .WithDamage(DamageType.Thermal, damage);
        }

        public static IDamageBuilder WithDamages(this IDamageBuilder builder,IEnumerable<Damage> damages)
        {
            foreach (var damage in damages)
                builder.WithDamage(damage);

            return builder;
        }

        public static IDamageBuilder WithDamage(this IDamageBuilder builder,DamageType type, double damage)
        {
            Debug.Assert(!double.IsNaN(damage));
            return Math.Abs(damage - 0.0) < double.Epsilon ? builder : builder.WithDamage(new Damage(type, damage));
        }
    }


    public sealed class DamageInfo
    {
        private const double CRITICALHIT_MOD = 1.75;
        public Unit attacker;
        public Position sourcePosition;
        public bool IsCritical { get; private set; }
        public IList<Damage> damages = new List<Damage>();
        private double _optimalRange;
        private double _falloff;
        private double _explosionRadius;

        private DamageInfo() {}

        public double Range
        {
            get { return _optimalRange + _falloff; }
        }

        public IList<Damage> CalculateDamages(Unit target)
        {
            var result = new List<Damage>();

            var zone = target.Zone;

            if (zone != null && damages != null )
            {
                var criticalHitChance = 0.0;

                if (attacker != null)
                    criticalHitChance = attacker.CriticalHitChance;

                var random = FastRandom.NextDouble();
                IsCritical = random <= criticalHitChance;
               
                var damageModifier = IsCritical ? CRITICALHIT_MOD :  1.0;

                damageModifier *= FastRandom.NextDouble(0.9, 1.1);

                // csak akkor szamolunk vele ha mindkettonek van erteke
                if (_optimalRange > 0.0 && _falloff > 0.0)
                {
                    var distance = sourcePosition.TotalDistance2D(target.CurrentPosition);
                    var range = Range;

                    if (distance > range)
                    {
                        damageModifier = 0.0;
                    }
                    else if (_falloff > 0.0 && distance > _optimalRange && distance <= range)
                    {
                        var x = (distance - _optimalRange) / _falloff;
                        damageModifier *= Math.Cos(x * Math.PI) / 2 + 0.5;
                    }
                }

                if (damageModifier > 0.0)
                {
                    if (_explosionRadius > 0.0)
                    {
                        var tmpDamageMod = target.SignatureRadius / _explosionRadius;

                        if (tmpDamageMod <= 0.0 || tmpDamageMod >= 1.0)
                        {
                            tmpDamageMod = 1.0;
                        }

                        damageModifier *= tmpDamageMod;
                    }

                    foreach (var cleanDamage in damages)
                    {
                        var damageValue = cleanDamage.value * damageModifier;

                        Debug.Assert(!double.IsNaN(damageValue));

                        result.Add(new Damage(cleanDamage.type, damageValue));
                    }
                }
            }

            return result;
        }

        public static IDamageBuilder Builder
        {
            get { return new DamageBuilder(); }
        }

        private class DamageBuilder : IDamageBuilder
        {
            private Unit _attacker;
            private Position _sourcePosition;
            private readonly IList<Damage> _damages = new List<Damage>();
            private double _optimalRange;
            private double _falloff;
            private double _explosionRadius;

            public IDamageBuilder WithAttacker(Unit attacker)
            {
                _attacker = attacker;

                if (attacker != null)
                {
                    WithSourcePosition(attacker.PositionWithHeight);
                }

                return this;
            }

            private DamageBuilder WithSourcePosition(Position position)
            {
                _sourcePosition = position;
                return this;
            }

            public IDamageBuilder WithOptimalRange(double optimalRange)
            {
                _optimalRange = optimalRange;
                return this;
            }

            public IDamageBuilder WithFalloff(double falloff)
            {
                _falloff = falloff;
                return this;
            }

            public IDamageBuilder WithExplosionRadius(double explosionRadius)
            {
                _explosionRadius = explosionRadius;
                return this;
            }

            public IDamageBuilder WithDamage(Damage damage)
            {
                _damages.Add(damage);
                return this;
            }

            public DamageInfo Build()
            {
                var damageInfo = new DamageInfo
                {
                    attacker = _attacker,
                    sourcePosition = _sourcePosition,
                    damages = _damages,
                    _optimalRange = _optimalRange,
                    _falloff = _falloff,
                    _explosionRadius = _explosionRadius
                };

                return damageInfo;
            }
        }
    }
}