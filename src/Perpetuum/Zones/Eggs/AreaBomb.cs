using System.Threading.Tasks;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Modules.Weapons;
using Perpetuum.Players;
using Perpetuum.Units;
using Perpetuum.Zones.Beams;

namespace Perpetuum.Zones.Eggs
{
    /// <summary>
    /// Area of effect emitter zone object
    /// </summary>
    public class AreaBomb : Egg
    {
        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
                base.AcceptVisitor(visitor);
        }

        protected override void OnSummonSuccess(IZone zone, Player[] summoners)
        {
            const int beamDistance = 600; //messzirol latszik

            foreach (var summoner in summoners)
            {
                summoner.ApplyPvPEffect();
            }

            //warning gfx beam
            //ojan beam ami egy ideig csak pulzal, ezalatt lehet elrohanni
            zone.CreateBeam(BeamType.timebomb_activation,builder => builder.WithPosition(CurrentPosition).WithVisibility(beamDistance).WithDuration(100));

            Task.Delay(ED.Config.ActionDelay).ContinueWith(t =>
            {
                //explosion gfx beam
                zone.CreateBeam(BeamType.timebomb_explosion, builder => builder.WithPosition(CurrentPosition)
                                                                               .WithState(BeamState.Hit)
                                                                               .WithVisibility(beamDistance)
                                                                               .WithDuration(15000));

                var damageBuilder = DamageInfo.Builder.WithAttacker(this)
                                     .WithDamage(DamageType.Chemical, ED.Config.damage_chemical ?? 0.0)
                                     .WithDamage(DamageType.Explosive, ED.Config.damage_explosive ?? 0.0)
                                     .WithDamage(DamageType.Kinetic, ED.Config.damage_kinetic ?? 0.0)
                                     .WithDamage(DamageType.Thermal, ED.Config.damage_thermal ?? 0.0)
                                     .WithDamage(DamageType.Toxic, ED.Config.damage_toxic ?? 0.0)
                                     .WithOptimalRange(2)
                                     .WithFalloff(ED.Config.item_work_range ?? 0.0)
                                     .WithExplosionRadius(ED.Config.explosion_radius ?? 0.0);

                zone.DoAoeDamageAsync(damageBuilder);
            });
        }

        protected override bool IsHostileFor(Unit unit)
        {
            return unit.IsHostile(this);
        }
    }
}
