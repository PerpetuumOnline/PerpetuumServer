using System.Collections.Generic;
using System.Linq;
using Perpetuum.Players;
using Perpetuum.Units;

namespace Perpetuum.Zones.Effects
{
    
    /// <summary>
    /// Corporation based effect
    /// </summary>
    public class CorporationEffect : AuraEffect
    {
        private long _corporationEid;

        protected override void SetupEffect(EffectBuilder effectBuilder)
        {
            effectBuilder.WithCorporationEid(_corporationEid);
            base.SetupEffect(effectBuilder);
        }

        public void SetCorporationEid(long corporationEid)
        {
            _corporationEid = corporationEid;
        }

        protected override void OnTick()
        {
            if (Owner is Player player)
            {
                // ha megvaltozott a corpeid akkor nem kell mar az effect
                if (player.CorporationEid != _corporationEid)
                {
                    OnRemoved();
                    return;
                }
            }

            base.OnTick();
        }

        protected override IEnumerable<Unit> GetTargets(IZone zone)
        {
            return zone.Players.Where(p => p.CorporationEid == _corporationEid);
        }

    }
}