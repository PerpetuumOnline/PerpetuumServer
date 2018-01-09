using System;
using System.Collections.Generic;
using System.Drawing;
using Perpetuum.Collections;
using Perpetuum.Items;

namespace Perpetuum.Zones.Terrains.Materials.Minerals
{
    public class MineralExtractor : MineralLayerVisitor
    {
        private readonly Point _location;
        private readonly uint _amount;
        private readonly MaterialHelper _materialHelper;

        private readonly List<ItemInfo> _items = new List<ItemInfo>();

        public List<ItemInfo> Items => _items;

        public MineralExtractor(Point location,uint amount,MaterialHelper materialHelper)
        {
            _location = location;
            _amount = amount;
            _materialHelper = materialHelper;
        }

        public override void VisitGravelLayer(GravelLayer layer)
        {
            var node = layer.GetNode(_location);
            if (node == null)
                return;

            var amounts = Math.Min(node.GetValue(_location), _amount);

            var m = _materialHelper.GetMaterialInfo(node.Type);
            _items.Add(m.ToItem((int)amounts));
        }

        public override void VisitOreLayer(OreLayer layer)
        {
            var node = layer.GetNode(_location);
            if (node == null)
                return;

            var amounts = node.DecreaseValue(_location, _amount);
            var m = _materialHelper.GetMaterialInfo(node.Type);
            _items.Add(m.ToItem((int)amounts));
        }

        private struct MineralDistance : IComparable<MineralDistance>
        {
            public readonly Point location;
            private readonly int _sqrDistance;

            public MineralDistance(Point location, int sqrDistance)
            {
                this.location = location;
                _sqrDistance = sqrDistance;
            }

            public int CompareTo(MineralDistance other)
            {
                return other._sqrDistance - _sqrDistance;
            }
        }

        public override void VisitLiquidLayer(LiquidLayer layer)
        {
            var node = layer.GetNode(_location);
            if (node == null)
                return;

            var pq = new PriorityQueue<MineralDistance>(node.Area.Ground);

            for (var y = node.Area.Y1; y <= node.Area.Y2; y++)
            {
                for (var x = node.Area.X1; x <= node.Area.X2; x++)
                {
                    if ( !node.HasValue(x,y))
                        continue;

                    var d = _location.SqrDistance(x, y);
                    pq.Enqueue(new MineralDistance(new Point(x, y), d));
                }
            }

            uint extractedAmounts = 0;
            MineralDistance md;
            while (pq.TryDequeue(out md))
            {
                var need = _amount - extractedAmounts;
                extractedAmounts += node.DecreaseValue(md.location, need);

                if (extractedAmounts >= _amount)
                    break;
            }

            var m = _materialHelper.GetMaterialInfo(node.Type);
            _items.Add(m.ToItem((int) extractedAmounts));
        }



    }
}