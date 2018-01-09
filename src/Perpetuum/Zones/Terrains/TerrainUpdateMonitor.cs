using System;
using System.Collections.Immutable;
using System.Drawing;

namespace Perpetuum.Zones.Terrains
{
    public abstract class TerrainUpdateInfo
    {
        public LayerType Type { get; }

        public Point Position { get; }

        protected TerrainUpdateInfo(LayerType type, Point position)
        {
            Position = position;
            Type = type;
        }

        public virtual bool IntersectsWith(Area area)
        {
            return area.Contains(Position);
        }

        public abstract Packet CreateUpdatePacket(ITerrain terrain);

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Position.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)Type;
                return hashCode;
            }
        }
    }

    public class AreaUpdateInfo : TerrainUpdateInfo
    {
        public Area Area { get; }

        public AreaUpdateInfo(LayerType type, Area area)
            : base(type, area.Center)
        {
            Area = area;
        }

        public override bool IntersectsWith(Area area)
        {
            return Area.IntersectsWith(area);
        }

        public override Packet CreateUpdatePacket(ITerrain terrain)
        {
            var packet = terrain.BuildLayerUpdatePacket(Type, Area);
            return packet;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = base.GetHashCode();
                hashCode = (hashCode * 397) ^ Area.GetHashCode();
                return hashCode;
            }
        }
    }

    public class TileUpdateInfo : TerrainUpdateInfo
    {
        public TileUpdateInfo(LayerType type, Point position)
            : base(type, position)
        {
        }

        public override bool IntersectsWith(Area area)
        {
            return area.Contains(Position);
        }

        public override Packet CreateUpdatePacket(ITerrain terrain)
        {
            return terrain.BuildLayerUpdatePacket(Type, new Area(Position.X, Position.Y, Position.X, Position.Y));
        }
    }

    public class TerrainUpdateMonitor : IDisposable
    {
        private readonly IZone _zone;
        private ImmutableHashSet<TerrainUpdateInfo> _infos = ImmutableHashSet<TerrainUpdateInfo>.Empty;

        public TerrainUpdateMonitor(IZone zone)
        {
            _zone = zone;

            SubscribeLayerUpdatedEvents(zone.Terrain.Altitude);
            SubscribeLayerUpdatedEvents(zone.Terrain.Blocks);
            SubscribeLayerUpdatedEvents(zone.Terrain.Controls);
            SubscribeLayerUpdatedEvents(zone.Terrain.Plants);
        }

        private void SubscribeLayerUpdatedEvents(ILayer layer)
        {
            var n = layer as INotifyLayerUpdated;
            if (n == null)
                return;

            n.Updated += OnLayerUpdated;
            n.AreaUpdated += OnLayerAreaUpdated;
        }

        private void UnsubscribeLayerUpdatedEvents(ILayer layer)
        {
            var n = layer as INotifyLayerUpdated;
            if (n == null)
                return;

            n.Updated -= OnLayerUpdated;
            n.AreaUpdated -= OnLayerAreaUpdated;
        }

        private void OnLayerAreaUpdated(ILayer layer, Area area)
        {
            OnAreaUpdated(layer.LayerType, area);
        }

        private void OnLayerUpdated(ILayer layer, int x, int y)
        {
            OnTileUpdated(layer.LayerType, x, y);
        }

        public void Dispose()
        {
            UnsubscribeLayerUpdatedEvents(_zone.Terrain.Altitude);
            UnsubscribeLayerUpdatedEvents(_zone.Terrain.Blocks);
            UnsubscribeLayerUpdatedEvents(_zone.Terrain.Controls);
            UnsubscribeLayerUpdatedEvents(_zone.Terrain.Plants);

            Notify();
        }

        private void Notify()
        {
            if ( _infos.Count == 0 )
                return;

            foreach (var player in _zone.Players)
            {
                player.Session.EnqueueLayerUpdates(_infos);
            }

            foreach (var info in _infos)
            {
                if (info.Type != LayerType.Altitude)
                    continue;

                switch (info)
                {
                    case AreaUpdateInfo areaUpdateInfo:
                    {
                        _zone.Terrain.Slope.UpdateSlopeByArea(areaUpdateInfo.Area);
                        break;
                    }
                    case TileUpdateInfo tileUpdateInfo:
                    {
                        _zone.Terrain.Slope.UpdateSlopeByArea(Area.FromRadius(tileUpdateInfo.Position, 1));
                        break;
                    }
                }
            }
        }

        private void OnAreaUpdated(LayerType layerType, Area area)
        {
            var info = new AreaUpdateInfo(layerType, area);
            AddUpdateInfo(info);
        }

        private void OnTileUpdated(LayerType layerType, int x, int y)
        {
            var info = new TileUpdateInfo(layerType,new Point(x,y));
            AddUpdateInfo(info);
        }

        private void AddUpdateInfo(TerrainUpdateInfo info)
        {
            ImmutableInterlocked.Update(ref _infos, infos => infos.Add(info));
        }
    }
}