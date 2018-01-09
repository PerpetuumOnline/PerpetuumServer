using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Perpetuum.Data;
using Perpetuum.Zones.Terrains;

namespace Perpetuum.Zones
{
    public static partial class ZoneExtensions
    {
        public const double MIN_SLOPE = 4.0;

        public static IEnumerable<Position> GetPassablePositionFromDb(this IZone zone)
        {
            return Db.Query().CommandText("select x,y from passablemappoints where zoneid=@zoneID")
                           .SetParameter("@zoneID", zone.Id)
                           .Execute()
                           .Select(r => new Position(r.GetValue<int>(0), r.GetValue<int>(1))).ToList();
        }

        public static bool IsWalkableForNpc(this IZone zone,Point position, double slope = MIN_SLOPE)
        {
            return zone.IsWalkableForNpc(position.X, position.Y, slope);
        }

        public static bool IsWalkableForNpc(this IZone zone, int x, int y, double slope = MIN_SLOPE)
        {
            if (!zone.IsWalkable(x, y, slope))
                return false;

            return !zone.Terrain.Controls[x, y].NpcRestricted;
        }

        public static bool IsWalkable(this IZone zone, int x, int y, double slope = MIN_SLOPE)
        {
            return zone.IsWalkable(new Position(x, y), slope);
        }

        public static bool IsWalkable(this IZone zone, Point point, double slope = MIN_SLOPE)
        {
            return zone.IsWalkable(point.ToPosition(),slope);
        }

        public static bool IsWalkable(this IZone zone, Position position, double slope = MIN_SLOPE)
        {
            if (zone == null)
                return false;

            if (!position.IsValid(zone.Size))
                return false;

            var isBlocked = zone.Terrain.IsBlocked(position);
            if (isBlocked)
                return false;

            var validSlope = zone.Terrain.Slope.CheckSlope(position.intX, position.intY, slope);
            return validSlope;
        }

        public static Position GetPosition(this IZone zone, Position position)
        {
            return zone.GetPosition(position.intX,position.intY);
        }

        public static Position GetPosition(this IZone zone, int x, int y)
        {
            return new Position(x,y,zone.GetZ(x,y));
        }

        public static Position FixZ(this IZone zone, Position position)
        {
            if (zone == null)
                return position;

            return new Position(position.X, position.Y,zone.GetZ(position));
        }

        public static double GetZ(this IZone zone,Position position)
        {
            return zone.GetZ(position.intX,position.intY);
        }

        public static double GetZ(this IZone zone, int x, int y)
        {
            return zone?.Terrain.Altitude.GetAltitude(x, y) ?? 0.0;
        }

        public static Area CreateArea(this IZone zone, Position position, int range)
        {
            return Area.FromRadius(position,range).Clamp(zone.Size);
        }

        public static void SaveLayers(this IZone zone)
        {
        }
    }
}
