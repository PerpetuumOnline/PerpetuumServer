using System.Collections.Generic;
using Perpetuum.Units;

namespace Perpetuum.Zones.Effects
{
    public delegate IEnumerable<Unit> EffectTargetSelector(IZone zone);
    
    /// <summary>
    /// Radius based effect
    /// </summary>
    public class AuraEffect : Effect
    {
        public double Radius { protected get; set; }

        public EffectTargetSelector TargetSelector { private get; set; }

        protected override void OnTick()
        {
            var remove = false;

            try
            {
                var zone = Owner.Zone;
                var sourceIsInZone = Source.InZone;
                var sourceIsDead = Source.States.Dead;
                var containsAuraEffect = Source.EffectHandler.ContainsToken(Token);

                // nincsen a terepen vagy nincs mar rajta az effect
                if (zone == null || !sourceIsInZone || sourceIsDead || !containsAuraEffect)
                {
                    remove = true;
                    return;
                }

                // ha ez az eredeti effect akkor keres targeteket
                if (Owner == Source)
                {
                    ApplyEffectToTargets(zone);
                }
                else
                {
                    // ha van radius akkor megnezzuk,h benne van-e
                    if (Radius <= 0.0)
                        return;

                    var isInRadius = Owner.IsInRangeOf3D(Source, Radius);
                    if (!isInRadius)
                    {
                        remove = true;
                    }
                }
            }
            finally
            {
                if (remove)
                {
                    OnRemoved();
                }
            }
        }

        private void ApplyEffectToTargets(IZone zone)
        {
            var units = GetTargets(zone);

            foreach (var unit in units)
            {
                if ( unit == Owner )
                    continue;

                if ( unit.EffectHandler.ContainsToken(Token) )
                    continue;

                if (Radius > 0.0)
                {
                    if ( !unit.IsInRangeOf3D(Source.CurrentPosition,Radius) )
                        continue;
                }

                var effectBuilder = unit.NewEffectBuilder();
                SetupEffect(effectBuilder);
                unit.ApplyEffect(effectBuilder);
            }
        }

        protected virtual IEnumerable<Unit> GetTargets(IZone zone)
        {
            var selector = TargetSelector;
            return selector == null ? new Unit[0] : selector(zone);
        }

        protected virtual void SetupEffect(EffectBuilder effectBuilder)
        {
            effectBuilder.SetType(Type)
                         .WithToken(Token)
                         .SetSource(Source)
                         .WithPropertyModifiers(propertyModifiers)
                         .EnableModifiers(true)
                         .WithRadius(Radius);
        }

        public override string ToString()
        {
            return $"Radius: {Radius}";
        }
    }
}