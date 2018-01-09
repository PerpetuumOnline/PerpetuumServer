using System.Collections.Generic;
using System.Drawing;

namespace Perpetuum.Collections.Spatial
{
    public class QuadTree<T>
    {
        private readonly QuadTreeNode<T> _root;

        public QuadTreeNode<T> Root
        {
            get { return _root; }
        }

        public QuadTree(Area area)
        {
            _root = new QuadTreeNode<T>(area);
        }

        public QuadTreeItem<T> Add(Point position, T value)
        {
            return Add(position.X, position.Y, value);
        }

        public QuadTreeItem<T> Add(int x, int y, T value)
        {
            QuadTreeItem<T> item;
            if (!_root.TryAdd(x, y, value, out item))
                return null;

            return item;
        }

        public IEnumerable<QuadTreeItem<T>> Query(Area area)
        {
            var q = new Queue<QuadTreeNode<T>>();
            q.Enqueue(_root);

            QuadTreeNode<T> node;
            while (q.TryDequeue(out node))
            {
                if ( !area.IntersectsWith(node.Area) )
                    continue;

                foreach (var item in node.GetItems())
                {
                    if ( area.Contains(item.X,item.Y))
                        yield return item;
                }

                var nodes = node.GetNodes();
                if (nodes == null)
                    continue;

                for (var i = 0; i < 4; i++)
                {
                    q.Enqueue(nodes[i]);
                }
            }
        }
    }
}