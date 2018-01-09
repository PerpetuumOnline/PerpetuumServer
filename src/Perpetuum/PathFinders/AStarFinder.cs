using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using Perpetuum.Collections;

namespace Perpetuum.PathFinders
{
    public class AStarFinder : PathFinder
    {

        private readonly Heuristic _heuristic;
        private readonly PathFinderNodePassableHandler _passableHandler;

        public AStarFinder(Heuristic heuristic,PathFinderNodePassableHandler passableHandler)
        {
            _heuristic = heuristic;
            _passableHandler = passableHandler;
            Weight = 10;
        }

        public int Weight { get; set; }

        public override Point[] FindPath(Point start, Point end,CancellationToken cancellationToken)
        {
            if (!_passableHandler(end.X, end.Y))
                return null;

            if (start == end)
                return EmptyPath;

            var openList = new PriorityQueue<Node>(500);
            var closedList = new Dictionary<int, bool>();

            var startNode = new Node(start.X, start.Y);
            openList.Enqueue(startNode);
            closedList[startNode.GetHashCode()] = true;

            Node node;
            while (openList.TryDequeue(out node) && !cancellationToken.IsCancellationRequested)
            {
                if (!OnProcessNode(node))
                    break;

                if (node.Location == end)
                    return Backtrace(node);

                foreach (var neighbor in GetNeighbors(node))
                {
                    if (closedList.ContainsKey(neighbor.GetHashCode()))
                        continue;

                    closedList[neighbor.GetHashCode()] = true;

                    var newG = node.g + (int)(Weight * (neighbor.Location.X - node.Location.X == 0 || neighbor.Location.Y - node.Location.Y == 0 ? 1 : SQRT2));
                    var newH = _heuristic.Calculate(neighbor.Location.X, neighbor.Location.Y, end.X, end.Y) * Weight;

                    neighbor.g = newG;
                    neighbor.f = newG + newH;
                    neighbor.parent = node;

                    openList.Enqueue(neighbor);
                    OnPathFinderDebug(neighbor, PathFinderNodeType.Open);
                }
            }

            return null;
        }

        private Point[] Backtrace(Node node)
        {
            var stack = new Stack<Point>();

            while (node != null)
            {
                stack.Push(node.Location);
                OnPathFinderDebug(node,PathFinderNodeType.Path);
                node = node.parent;
            }

            return stack.ToArray();
        }

        private static readonly sbyte[,] _n = { { -1, -1 }, { 0, -1 }, { 1, -1 }, { -1, 0 }, { 1, 0 }, { -1, 1 }, { 0, 1 }, { 1, 1 } };

        private IEnumerable<Node> GetNeighbors(Node node)
        {
            for (var i = 0; i < 8; i++)
            {
                var nx = node.Location.X + _n[i, 0];
                var ny = node.Location.Y + _n[i, 1];

                if (_passableHandler(nx, ny))
                {
                    yield return new Node(nx,ny);
                }
            }
        }

        private class Node : PathFinderNode,IComparable<Node>
        {
            public int g;
            public int f;
            public Node parent;

            public Node(int x, int y) : base(x, y)
            {
            }

            public int CompareTo(Node other)
            {
                return f - other.f;
            }

            public override string ToString()
            {
                return $"{base.ToString()}, G: {g}, F: {f}";
            }
        }
    }
}
    