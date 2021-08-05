using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Perpetuum.Builders;
using Perpetuum.Groups.Corporations;
using Perpetuum.IO;
using Perpetuum.Log;
using Perpetuum.Modules.Weapons;
using Perpetuum.Units;
using Perpetuum.Zones.Terrains;

namespace Perpetuum.Zones
{
    public interface ILayerFileIO
    {
        T[] LoadLayerData<T>(IZone zone, string name) where T : struct;
        void SaveLayerToDisk<T>(IZone zone, ILayer<T> layer) where T : struct;
    }

    public static class LayerFileIOExtensions
    {
        public static T[] Load<T>(this ILayerFileIO dataIO,IZone zone,LayerType layerType) where T : struct
        {
            return dataIO.LoadLayerData<T>(zone,layerType.ToString());
        }
    }

    public class LayerFileIO : ILayerFileIO
    {
        private readonly IFileSystem _fileSystem;

        public LayerFileIO(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public T[] LoadLayerData<T>(IZone zone,string name) where T : struct
        {
            var path = zone.CreateTerrainDataFilename(name);
            var data = _fileSystem.ReadLayer<T>(path);
            Logger.Info("Layer data loaded. (" + name + ") zone:" + zone.Id);
            return data;
        }

        public void SaveLayerToDisk<T>(IZone zone, ILayer<T> layer) where T : struct
        {
            var baseFilename = zone.CreateTerrainDataFilename(layer.LayerType.ToString().ToLower(),"");

            using (var md5 = MD5.Create())
            {
                var tmpFn = baseFilename + "tmp" + DateTime.Now.Ticks + ".bin";
                var layerData = layer.RawData.ToByteArray();
                _fileSystem.WriteLayer(tmpFn,layerData);

                if (!md5.ComputeHash(layerData).SequenceEqual(md5.ComputeHash(_fileSystem.ReadLayerAsByteArray(tmpFn))))
                    return;

                _fileSystem.MoveLayerFile(tmpFn,baseFilename + "bin");
                Logger.Info("Layer saved. (" + baseFilename + ")");
            }
        }
    }

    public static partial class ZoneExtensions
    {
        public static List<Point> FindWalkableArea(this IZone zone, Area area, int size,double slope = 4.0)
        {
            area = area.Clamp(zone.Size);
            while (true)
            {
                Point startPosition;
                while (true)
                {
                    startPosition = area.GetRandomPosition();

                    if (!zone.Terrain.Blocks.GetValue(startPosition).Island && zone.IsWalkable(startPosition,slope))
                        break;
                }

                var p = FindWalkableArea(zone, startPosition, area, size,slope);
                if (p != null)
                    return p;

                Thread.Sleep(1);
            }
        }

        [CanBeNull]
        public static List<Point> FindWalkableArea(this IZone zone,Point startPosition, Area area, int size,double slope = 4.0)
        {
            var q = new Queue<Point>();
            q.Enqueue(startPosition);
            var closed = new HashSet<Point> {startPosition};

            var result = new List<Point>();
            Point position;
            while (q.TryDequeue(out position))
            {
                result.Add(position);

                if (result.Count >= size)
                {
                    // nyert
                    return result;
                }

                foreach (var np in position.GetNonDiagonalNeighbours())
                {
                    if (closed.Contains(np))
                        continue;

                    closed.Add(np);

                    if (!area.Contains(np) || !zone.IsWalkable(np,slope))
                        continue;

                    q.Enqueue(np);
                }
            }

            return null;
        }

        /// <summary>
        /// A 2d raycast check for a line segment in cellular world
        /// An implementation of Bresenham's line algorithm
        /// </summary>
        /// <param name="zone">this</param>
        /// <param name="start">Start point of line segment</param>
        /// <param name="end">End point of line segment</param>
        /// <param name="slope">Slope capability check for slope-based blocking</param>
        /// <returns>True if tiles checked are walkable</returns>
        public static bool CheckLinearPath(this IZone zone, Point start, Point end, double slope = 4.0)
        {
            var x = start.X;
            var y = start.Y;
            var deltaX = Math.Abs(end.X - x);
            var deltaY = Math.Abs(end.Y - y);
            var travelDist = deltaX + deltaY;
            var xIncrement = (end.X > x) ? 1 : -1;
            var yIncrement = (end.Y > y) ? 1 : -1;
            var error = deltaX - deltaY;
            deltaX *= 2;
            deltaY *= 2;
            
            for (var i = 0; i <= travelDist; i++)
            {
                if (!zone.IsWalkable(x, y, slope))
                {
                    return false;
                }

                if (error > 0)
                {
                    x += xIncrement;
                    error -= deltaY;
                }
                else
                {
                    y += yIncrement;
                    error += deltaX;
                }
            }
            return true;
        }

        public static bool IsTerrainConditionsMatchInRange(this IZone zone, Position centerPosition, int range, double slope)
        {
            var totalTiles = range * range * 4;

            var illegalsFound = 0;

            for (var j = centerPosition.intY - range; j < centerPosition.intY + range; j++)
            {
                for (var i = centerPosition.intX - range; i < centerPosition.intX + range; i++)
                {
                    var cPos = new Position(i, j);

                    if (centerPosition.IsInRangeOf2D(cPos, range))
                    {
                        var blockInfo = zone.Terrain.Blocks.GetValue(i, j);

                        if (blockInfo.Height > 0 || blockInfo.NonNaturally || blockInfo.Plant || !zone.Terrain.Slope.CheckSlope(cPos.intX, cPos.intY, slope))
                        {
                            illegalsFound++;
                        }
                    }
                }
            }

            var troubleFactor = illegalsFound / (double)totalTiles;

            Logger.Info("trouble factor: " + troubleFactor);

            if (troubleFactor > 0.5)
            {
                Logger.Warning("illegal tiles coverage: " + troubleFactor * 100 + "%");
                return false;
            }

            return true;
        }

        public static void DoAoeDamageAsync(this IZone zone,IBuilder<DamageInfo> damageBuilder)
        {
            Task.Run(() => DoAoeDamage(zone, damageBuilder));
        }

        public static void DoAoeDamage(this IZone zone,IBuilder<DamageInfo> damageBuilder)
        {
            var damageInfo = damageBuilder.Build();
            var units = zone.Units.WithinRange(damageInfo.sourcePosition, damageInfo.Range);

            foreach (var unit in units)
            {
                var losResult = zone.IsInLineOfSight(damageInfo.attacker, unit, false);
                if (losResult.hit)
                    continue;

                unit.TakeDamage(damageInfo);
            }

            using (new TerrainUpdateMonitor(zone))
            {
                zone.DamageToPlantOnArea(damageInfo);
            }
        }

        public static string CreateTerrainDataFilename(this IZone zone, string name, string extension = "bin")
        {
            return CreateTerrainDataFilename(zone.Id, name, extension);
        }

        public static string CreateTerrainDataFilename(int zoneId, string name, string extension = "bin")
        {
            return $"{name.ToLower()}.{zoneId:0000}.{extension}".ToLower();
        }

        private const int MAX_SAMPLES = 1200;

        public static Position FindPassablePointInRadius(this IZone zone, Position origin, int radius)
        {
            var counter = 0;
            while (true)
            {
                counter++;
                if (counter > MAX_SAMPLES)
                    return default(Position);

                var randomPos = origin.GetRandomPositionInRange2D(0, radius).Clamp(zone.Size);
                if (zone.Terrain.IsPassable(randomPos))
                    return randomPos;
            }
        }

        public static bool IsValidPosition(this IZone zone, int x, int y)
        {
            return x >= 0 && x < zone.Size.Width && y >= 0 && y < zone.Size.Height;
        }

        public static void UpdateCorporation(this IZone zone,CorporationCommand command,Dictionary<string,object> data)
        {
            zone.CorporationHandler.HandleCorporationCommand(command,data);
        }

        public static Position ToWorldPosition(this IZone zone,Position position)
        {
            var zx = zone.Configuration.WorldPosition.X;
            var zy = zone.Configuration.WorldPosition.Y;
            return position.GetWorldPosition(zx,zy);
        }
    }
}