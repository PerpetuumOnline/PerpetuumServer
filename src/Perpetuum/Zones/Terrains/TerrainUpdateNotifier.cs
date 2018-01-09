using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Perpetuum.Collections.Spatial;
using Perpetuum.Log;
using Perpetuum.Players;

namespace Perpetuum.Zones.Terrains
{
    public class TerrainUpdateNotifier
    {
        private const int VISIBLE_RANGE = 90;
        private const int TILES_PER_CELL = 32;

        private readonly IZone _zone;
        private readonly Player _player;
        private int _dirty;

        private readonly Grid<UpdateHolderCell> _grid;
        private ImmutableList<TerrainUpdateInfo>  _newUpdates = ImmutableList<TerrainUpdateInfo>.Empty;

        private class UpdateHolderCell : Cell
        {
            public HashSet<LayerType> DirtyLayers { get; private set; }
            public LinkedList<TerrainUpdateInfo> Updates { get; private set; }

            public UpdateHolderCell(Area boundingBox) : base(boundingBox)
            {
                DirtyLayers = new HashSet<LayerType>();
                Updates = new LinkedList<TerrainUpdateInfo>();
            }

            private bool IsEmpty
            {
                get { return DirtyLayers.Count == 0 && Updates.Count == 0; }
            }

            public void Update(ITerrain terrain, Player player, Area visibleArea)
            {
                if (IsEmpty)
                    return;

                if (!visibleArea.IntersectsWith(BoundingBox))
                {
                    // nem lathato cella
                    if (Updates.Count <= 0)
                        return;

                    // ha van benne update akkor csak a tipust tartjuk meg es az osszes update-et toroljuk
                    DirtyLayers.AddMany(Updates.Select(u => u.Type).Distinct());
                    Updates.Clear();
                    return;
                }

                if (DirtyLayers.Count > 0)
                {
                    // itt az egesz cellat elkuldjuk ha valamelyik tipus modosult
                    foreach (var type in DirtyLayers)
                    {
                        var packet = terrain.BuildLayerUpdatePacket(type, BoundingBox);
                        player.Session.SendPacket(packet);
                    }

                    //torlunk mindent
                    Updates.Clear();
                    DirtyLayers.Clear();
                    return;
                }

                // itt a kis teruleteket vizsgaljuk meg,h lathato-e
                // ha igen akkor kikuldjuk es toroljuk a cellabol
                Updates.RemoveAll(info =>
                {
                    if (!info.IntersectsWith(visibleArea))
                        return false;

                    var packet = info.CreateUpdatePacket(terrain);
                    player.Session.SendPacket(packet);
                    return true;
                });
            }
        }

        public TerrainUpdateNotifier(IZone zone, Player player, LayerType[] layerTypes)
        {
            _zone = zone;
            _player = player;

            var tilesPerCellX = zone.Size.Width / TILES_PER_CELL;
            var tilesPerCellY = zone.Size.Height / TILES_PER_CELL;

            _grid = new Grid<UpdateHolderCell>(zone.Size.Width,zone.Size.Height,tilesPerCellX,tilesPerCellY,area =>
            {
                var cell = new UpdateHolderCell(area);

                foreach (var type in layerTypes)
                {
                    cell.DirtyLayers.Add(type);
                }

                return cell;
            });
        }

        public void EnqueueNewUpdates(IEnumerable<TerrainUpdateInfo> infos)
        {
            ImmutableInterlocked.Update(ref _newUpdates, (u) =>
            {
                var b = u.ToBuilder();
                b.AddRange(infos);
                return b.ToImmutable();
            });
        }

        private bool _updatingGrid;

        public void Update()
        {
            if ( _updatingGrid )
                return;

            DequeueNewUpdates();

            if ( Interlocked.CompareExchange(ref _dirty,0,1) == 0)
                return;

            _updatingGrid = true;

            Task.Run(() =>
            {
                UpdateGrid();
            }).ContinueWith(t =>
            {
                _updatingGrid = false;
            }).LogExceptions();
        }

        private void DequeueNewUpdates()
        {
            if (_newUpdates.Count == 0)
                return;

            try
            {
                ImmutableList<TerrainUpdateInfo> infos = null;
                ImmutableInterlocked.Update(ref _newUpdates, u =>
                {
                    infos = u;
                    return ImmutableList<TerrainUpdateInfo>.Empty;
                });

                if ( infos == null )
                    return;

                foreach (var info in infos)
                {
                    Debug.Assert(info != null, "info != null");

                    if (info == null)
                    {
                        Logger.Error("TerrainUpdateNotifier: terrain update info is null");
                        continue;
                    }

                    var cell = _grid.GetCell(info.Position);
                    cell.Updates.AddLast(info);
                }
            }
            finally
            {
                Interlocked.Exchange(ref _dirty, 1);
            }
        }

        private Area GetVisibleArea()
        {
            return Area.FromRadius(_player.CurrentPosition, VISIBLE_RANGE);
        }

        public void ForceUpdateGrids()
        {
            Interlocked.Exchange(ref _dirty, 1);
        }

        private void UpdateGrid()
        {
            var visibleArea = GetVisibleArea();

            foreach (var cell in _grid.GetCells())
            {
                cell.Update(_zone.Terrain,_player, visibleArea);
            }
        }
    }
}