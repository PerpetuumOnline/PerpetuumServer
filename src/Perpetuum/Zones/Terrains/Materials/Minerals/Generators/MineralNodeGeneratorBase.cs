using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Perpetuum.Units;
using Perpetuum.Units.DockingBases;
using Perpetuum.Zones.Finders.PositionFinders;
using Perpetuum.Zones.Teleporting;

namespace Perpetuum.Zones.Terrains.Materials.Minerals.Generators
{
    public abstract class MineralNodeGeneratorBase : IMineralNodeGenerator
    {
        private readonly IZone _zone;

        protected MineralNodeGeneratorBase(IZone zone)
        {
            _zone = zone;
        }

        public int Radius { private get; set; }
        public int MaxTiles { protected get; set; }
        public int TotalAmount { private get; set; }
        public double MinThreshold { private get; set; }

        public MineralNode Generate(MineralLayer layer)
        {
            var startPosition = FindStartPosition(layer);
            var noise = GenerateNoise(startPosition);
            var normalizedNoise = NormalizeNoise(noise);
            var node = CreateMineralNode(layer,normalizedNoise);
            return node;
        }

        private Dictionary<Point, double> NormalizeNoise(Dictionary<Point, double> noise)
        {
            var max = noise.Values.Max();

            var result = new Dictionary<Point, double>();

            foreach (var kvp in noise)
            {
                result[kvp.Key] = (kvp.Value/max).Clamp().Normalize(MinThreshold,1.0);
            }

            return result;
        }
       
        protected abstract Dictionary<Point,double> GenerateNoise(Position startPosition);

        protected bool IsValid(Point location)
        {
            if (!_zone.Size.Contains(location.X, location.Y))
                return false;

            var blockingInfo = _zone.Terrain.Blocks.GetValue(location.X, location.Y);
            if (blockingInfo.Island)
                return false;

            var controlInfo = _zone.Terrain.Controls.GetValue(location.X, location.Y);
            if (controlInfo.PBSTerraformProtected)
                return false;

            if (_zone.Configuration.Terraformable)
            {
                if (_zone.Terrain.IsBlocked(location.X, location.Y))
                    return false;
            }
            else
            {
                if (!_zone.IsWalkable(location))
                    return false;
            }

            return true;
        }

        private MineralNode CreateMineralNode(MineralLayer layer,Dictionary<Point,double> tiles)
        {
            int minx = int.MaxValue, miny = int.MaxValue, maxx = 0, maxy = 0;

            var sum = 0.0;

            foreach (var t in tiles)
            {
                var p = t.Key;

                minx = Math.Min(p.X, minx);
                miny = Math.Min(p.Y, miny);
                maxx = Math.Max(p.X, maxx);
                maxy = Math.Max(p.Y, maxy);

                sum += t.Value;
            }

            var area = new Area(minx, miny, maxx, maxy);
            var node = layer.CreateNode(area);

            foreach (var t in tiles)
            {
                var u = (t.Value / sum) * TotalAmount;
                u *= FastRandom.NextFloat(0.9f, 1.1f);
                node.SetValue(t.Key, (uint) u);
            }

            return node;
        }

        private bool IsInRangeOfBaseOrTeleports(Point location, double dist)
        {
            if (_zone.Units.OfType<DockingBase>().WithinRange(location.ToPosition(), dist).Any())
                return true;

            if (_zone.Units.OfType<Teleport>().WithinRange(location.ToPosition(), dist).Any())
                return true;
            return false;
        }

        private bool CheckSpecialOreKeepOuts(MineralLayer layer, Position startPosition)
        {
            if (layer.Configuration.Type == MaterialType.Epriton)
            {
                return IsInRangeOfBaseOrTeleports(startPosition, DistanceConstants.MINERAL_DISTANCE_FROM_BASE_MIN);
            }
            else if (layer.Configuration.Type == MaterialType.FluxOre)
            {
                return IsInRangeOfBaseOrTeleports(startPosition, DistanceConstants.MINERAL_DISTANCE_FROM_BASE_MIN * 2.0);
            }
            return false;
        }

        private Position FindStartPosition(MineralLayer layer)
        {
            var finder = new RandomPassablePositionFinder(_zone);

            while (true)
            {
                if (!finder.Find(out Position startPosition))
                    continue;

                if (!IsValid(startPosition))
                    continue;

                if (CheckSpecialOreKeepOuts(layer, startPosition))
                    continue;

                var n = layer.GetNearestNode(startPosition);
                if (n == null)
                    return startPosition;

                var d = n.Area.Distance(startPosition);
                if (d < Radius * 2)
                {
                    // ha tul kozel van akkor keresunk ujat
                    continue;
                }

                return startPosition;
            }
        }
    }
}