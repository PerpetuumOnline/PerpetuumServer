using System.Collections.Generic;
using Perpetuum.Groups.Gangs;
using Perpetuum.Players;
using Perpetuum.Units;

namespace Perpetuum.Zones.Effects
{
    /// <summary>
    /// Gang based effect
    /// </summary>
    public class GangEffect : AuraEffect
    {
        protected override void OnTick()
        {
            if (Owner != Source)
            {
                var gang = ((Player)Owner).Gang;
                // ha nincs mar gangben vagy a forras mar nincs gangben
                if (gang == null || !gang.IsMember((Player)Source))
                {
                    OnRemoved();
                    return;
                }
            }

            base.OnTick();
        }

        protected override IEnumerable<Unit> GetTargets(IZone zone)
        {
            var player = (Player)Owner;
            return zone.GetGangMembers(player.Gang).WithinRange(Owner.CurrentPosition, Radius);
        }

    }
}