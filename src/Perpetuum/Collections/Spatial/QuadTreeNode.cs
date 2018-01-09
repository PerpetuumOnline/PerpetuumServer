using System.Collections.Generic;

namespace Perpetuum.Collections.Spatial
{
    public class QuadTreeNode<T>
    {
        private const int CAPACITY = 4;
        private readonly Area _area;
        private readonly List<QuadTreeItem<T>> _items = new List<QuadTreeItem<T>>(CAPACITY);

        private QuadTreeNode<T>[] _nodes;

        public QuadTreeItem<T>[] GetItems()
        {
            return _items.ToArray();
        }

        public QuadTreeNode<T>[] GetNodes()
        {
            return _nodes;
        }

        public Area Area
        {
            get { return _area; }
        }

        public QuadTreeNode(Area area)
        {
            _area = area;
        }

        public bool TryAdd(int x, int y, T value,out QuadTreeItem<T> item)
        {
            if (!_area.Contains(x, y))
            {
                item = null;
                return false;
            }

            if (_items.Count < CAPACITY)
            {
                item = new QuadTreeItem<T>(this,x,y,value);
                _items.Add(item);
                return true;
            }

            if (_nodes == null)
            {
                _nodes = new QuadTreeNode<T>[CAPACITY];
                var w = _area.Width / 2;
                var h = _area.Height / 2;

                _nodes[0] = new QuadTreeNode<T>(Area.FromRectangle(_area.X1, _area.Y1, w, h));
                _nodes[1] = new QuadTreeNode<T>(Area.FromRectangle(_area.X1 + w, _area.Y1, w, h));
                _nodes[2] = new QuadTreeNode<T>(Area.FromRectangle(_area.X1, _area.Y1 + h, w, h));
                _nodes[3] = new QuadTreeNode<T>(Area.FromRectangle(_area.X1 + w, _area.Y1 + h, w, h));
            }

            for (var i = 0; i < 4; i++)
            {
                if (_nodes[i].TryAdd(x, y, value,out item))
                    return true;
            }

            item = null;
            return false;
        }

        public void Remove(QuadTreeItem<T> item)
        {
            _items.Remove(item);
        }
    }
}