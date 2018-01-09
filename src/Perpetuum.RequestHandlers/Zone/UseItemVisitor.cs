using Perpetuum.Accounting.Characters;
using Perpetuum.EntityFramework;
using Perpetuum.Players;
using Perpetuum.Units;
using Perpetuum.Zones;
using Perpetuum.Zones.Gates;

namespace Perpetuum.RequestHandlers.Zone
{
    public class UseItemVisitor : IEntityVisitor<Unit>,IEntityVisitor<Gate>
    {
        private readonly IZone _zone;
        private readonly Character _character;

        public UseItemVisitor(IZone zone, Character character)
        {
            _zone = zone;
            _character = character;
        }

        public void Visit(Unit unit)
        {
            var usable = unit as IUsableItem;
            if (usable != null)
            {
                //fallback, can only be used if the player is on the zone
                //zone packet used in this case

                Player player;
                if (_zone.TryGetPlayer(_character, out player))
                {
                    usable.UseItem(player);
                }
            }
        }

        /// <summary>
        /// Gate can be used from anywhere
        /// </summary>
        /// <param name="gate"></param>
        public void Visit(Gate gate)
        {
            gate.UseGateWithCharacter(_character, _character.CorporationEid);
        }
    }
}