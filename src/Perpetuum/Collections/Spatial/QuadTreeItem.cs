namespace Perpetuum.Collections.Spatial
{
    public class QuadTreeItem<T>
    {
        public QuadTreeNode<T> Node { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public T Value { get; set; }

        public QuadTreeItem(QuadTreeNode<T> node,int x, int y, T value)
        {
            Node = node;
            X = x;
            Y = y;
            Value = value;
        }

        public void Remove()
        {
            Node.Remove(this);
        }
    }
}