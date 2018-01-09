using System.Collections.Generic;
using System.Drawing;
using Perpetuum.Builders;
using Perpetuum.Log;

namespace Perpetuum.Zones.Terrains
{
    public class PassableMapBuilder : IBuilder<ILayer<bool>>
    {
        private readonly ILayer<BlockingInfo> _blocksLayer;
        private readonly SlopeLayer _slopeLayer;
        private readonly IEnumerable<Position> _startPositions;

        private Size _size;

        public PassableMapBuilder(ILayer<BlockingInfo> blocksLayer,SlopeLayer slopeLayer,IEnumerable<Position> startPositions)
        {
            _blocksLayer = blocksLayer;
            _slopeLayer = slopeLayer;
            _startPositions = startPositions;

            _size = new Size(blocksLayer.Width, blocksLayer.Height);
        }

        public ILayer<bool> Build()
        {
            foreach (var position in _startPositions)
            {
                if (!IsPassable(position))
                    continue;

                var m = Generate(position);
                if (m != null)
                    return m;
            }

            return null;
        }

        private ILayer<bool> Generate(Position start)
        {
            var map = new Layer<bool>(LayerType.Passable,_blocksLayer.Width,_blocksLayer.Height);

            var q = new Queue<Position>();
            q.Enqueue(start);

            var closedList = new Dictionary<ulong, bool> {{start.GetUlongHashCode(), true}};

            var counter = 0L;
            Position p;
            while (q.TryDequeue(out p))
            {
                map.SetValue(p,true);

                foreach (var np in p.NonDiagonalNeighbours)
                {
                    if (closedList.ContainsKey(np.GetUlongHashCode()))
                        continue;

                    closedList.Add(np.GetUlongHashCode(), true);

                    if ( !IsPassable(np) )
                        continue;

                    q.Enqueue(np);
                }

                counter++;
            }

            Logger.Info(counter + " outline points were tested for passable map.");
            return map;
        }

        private bool IsPassable(Position position)
        {
            if (!position.IsValid(_size))
                return false;

            // plants excluded
            var bi = _blocksLayer.GetValue(position);
            if (bi.NonNaturally)
                return false;

            var validSlope = _slopeLayer.CheckSlope(position);
            return validSlope;
        }
    }
}