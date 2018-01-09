using System;
using System.Linq;
using Perpetuum.EntityFramework;
using Perpetuum.Log;
using Perpetuum.Units;
using Perpetuum.Zones.Environments;
using Perpetuum.Zones.Terrains;

namespace Perpetuum.Zones
{
    partial class ZoneExtensions
    {
        private static void DrawEnvironmentByDefinitionAndPosition(this IZone zone,Unit unit)
        {
            //enviroment load
            var description = EntityEnvironment.LoadEnvironmentSql(unit.Definition);
            //draw extra environment
            if (description.Equals(default(EntityEnvironmentDescription))) 
                return;

            if (description.blocksTiles == null || description.blocksTiles.Count <= 0)
                return;

            foreach (var tile in description.blocksTiles)
            {
                var offsetPosition = unit.CurrentPosition + tile.ToPosition();
                if (!offsetPosition.IsValid(zone.Size))
                    continue;

                zone.Terrain.Blocks.UpdateValue(offsetPosition, bi =>
                {
                    return new BlockingInfo
                    {
                        Obstacle = true,
                        Height = Math.Max(bi.Height, tile.data),
                    };
                });
            }
        }

        public static void DrawEnvironmentByUnit(this IZone zone, Unit unit)
        {
            var description = EntityEnvironment.LoadEnvironmentSql(unit.Definition);

            var turns = (int)(Math.Round(unit.Orientation, 2) / 0.25);

            DrawEnvironmentWithMirrorAndTurns(zone, unit.CurrentPosition, description, turns, false, false, BlockingFlags.Obstacle);
        }

        public static void DrawBlockingByDefinition(this IZone zone,EntityDefault entityDefault)
        {
            Logger.Info($"drawing blocking for definition:{entityDefault.Definition} {entityDefault.Name} on zone:{zone.Id}");

            var units = zone.Units.Where(u => u.ED == entityDefault).ToList();

            foreach (var unit in units)
            {
                zone.DrawEnvironmentByUnit(unit);
            }

            Logger.Info($"{units.Count} {entityDefault.Name} blocked.");
        }

        public static void CleanBlockingByDefinition(this IZone zone,EntityDefault entityDefault)
        {
            Logger.Info($"clean blocking for definition:{entityDefault.Definition} {entityDefault.Name} on zone:{zone.Id}");

            var units = zone.Units.Where(u => u.ED == entityDefault).ToList();

            foreach (var unit in units)
            {
                Logger.Info($"cleaning position: {unit.CurrentPosition.ToDoubleString2D()}");
                //enviroment load
                var description = EntityEnvironment.LoadEnvironmentSql(entityDefault.Definition);
                //clean environment
                CleanEnvironmentFromLayers(zone, unit.CurrentPosition, description);
            }

            Logger.Info($"{units.Count} {entityDefault.Name} cleaned.");
        }

        public static void CleanEnvironmentByUnit(this IZone zone, Unit unit)
        {
            var description = EntityEnvironment.LoadEnvironmentSql(unit.Definition);
            var turns = (int)(Math.Round(unit.Orientation, 2) / 0.25);
            CleanEnvironmentWithMirrorAndTurns(zone, unit.CurrentPosition, description, turns, false, false);
        }

        public static void DrawEnvironmentForDecor(this IZone zone, Position position, EntityEnvironmentDescription description, int rotationTurns, bool flipX, bool flipY)
        {
            DrawEnvironmentWithMirrorAndTurns(zone, position, description, rotationTurns, flipX, flipY, BlockingFlags.Decor);
        }

        private static void DrawEnvironmentWithMirrorAndTurns(this IZone zone, Position position, EntityEnvironmentDescription description, int rotationTurns, bool flipX, bool flipY, BlockingFlags blockingFlag)
        {
            if (description.Equals(default(EntityEnvironmentDescription)))
                return;

            if (description.blocksTiles == null || description.blocksTiles.Count <= 0)
                return;

            var terrain = zone.Terrain;
            var originAltitude = terrain.Altitude.GetAltitudeAsDouble(position);

            using (new TerrainUpdateMonitor(zone))
            {
                foreach (var tile in description.blocksTiles)
                {
                    var tx = tile.x;
                    var ty = tile.y;

                    if (flipX)
                        tx *= -1;

                    if (flipY)
                        ty *= -1;

                    var tilePos = new Position(tx, ty);
                    var rotatedPos = Position.RotateCWWithTurns(tilePos, rotationTurns);

                    var offsetPosition = new Position(position.intX + rotatedPos.intX, position.intY + rotatedPos.intY);

                    if (!offsetPosition.IsValid(zone.Size))
                        continue;

                    zone.Terrain.Blocks.UpdateValue(offsetPosition,bi =>
                    {
                        var altitude = terrain.Altitude.GetAltitudeAsDouble(offsetPosition);
                        var altDiff = (int)(altitude - originAltitude);
                        var resultingBlockingHeight = (byte)((tile.data - altDiff).Clamp(0, 255));
                        return new BlockingInfo(blockingFlag, Math.Max(bi.Height, resultingBlockingHeight));
                    });
                }
            }
        }

        private static void CleanEnvironmentFromLayers(this IZone zone, Position position, EntityEnvironmentDescription description)
        {
            if (description.Equals(default(EntityEnvironmentDescription)))
                return;

            if (description.blocksTiles == null || description.blocksTiles.Count <= 0)
                return;

            foreach (var tile in description.blocksTiles)
            {
                var offsetPosition = new Position(position.intX + tile.x, position.intY + tile.y);
                if (!offsetPosition.IsValid(zone.Size))
                    continue;

                zone.Terrain.Blocks.SetValue(offsetPosition,new BlockingInfo());
            }
        }

        private static void CleanEnvironmentWithMirrorAndTurns(this IZone zone, Position position, EntityEnvironmentDescription description, int rotationTurns, bool flipX, bool flipY)
        {
            if (description.Equals(default(EntityEnvironmentDescription)))
                return;


            if (description.blocksTiles == null || description.blocksTiles.Count <= 0)
                return;

            var bi = new BlockingInfo();
            using (new TerrainUpdateMonitor(zone))
            {
                foreach (var tile in description.blocksTiles)
                {
                    var tx = tile.x;
                    var ty = tile.y;

                    if (flipX)
                    {
                        tx *= -1;
                    }

                    if (flipY)
                    {
                        ty *= -1;
                    }

                    var tilePos = new Position(tx, ty);
                    var rotatedPos = Position.RotateCWWithTurns(tilePos, rotationTurns);

                    var offsetPosition = new Position(position.intX + rotatedPos.intX, position.intY + rotatedPos.intY);
                    if (offsetPosition.IsValid(zone.Size))
                    {
                        zone.Terrain.Blocks.SetValue(offsetPosition,bi);
                    }
                }
            }
        }
    }
}
