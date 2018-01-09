using System.Collections.Generic;
using System.Linq;
using Perpetuum.Accounting.Characters;
using Perpetuum.Players;
using Perpetuum.Units;

namespace Perpetuum.Zones
{
    public interface IZoneManager
    {
        bool ContainsZone(int zoneID);

        IEnumerable<IZone> Zones { get; }

        [CanBeNull]
        IZone GetZone(int zoneID);
    }

    public static class ZoneManagerExtensions
    {
        [CanBeNull]
        public static T GetUnit<T>(this IZoneManager zoneManager, long eid) where T : Unit
        {
            foreach (var zone in zoneManager.Zones)
            {
                if (zone.GetUnit(eid) is T unit)
                    return unit;
            }

            return null;
       }

        public static bool TryGetPlayer(this IZoneManager zoneManager, Character character,out Player player)
        {
            foreach (var zone in zoneManager.Zones)
            {
                if (zone.TryGetPlayer(character, out player))
                    return true;

            }

            player = null;
            return false;
        }

        public static Player GetPlayer(this IZoneManager zoneManager, Character character)
        {
            if (zoneManager.TryGetPlayer(character, out Player player))
                return player;

            return null;
        }
    }

    public class ZoneManager : IZoneManager
    {
        public List<IZone> Zones { get; set; } = new List<IZone>();

        IEnumerable<IZone> IZoneManager.Zones => Zones;

        public IZone GetZone(int zoneID)
        {
            return Zones.FirstOrDefault(z => z.Id == zoneID);
        }

        public bool ContainsZone(int zoneID)
        {
            return Zones.Any(z => z.Id == zoneID);
        }
    }
}