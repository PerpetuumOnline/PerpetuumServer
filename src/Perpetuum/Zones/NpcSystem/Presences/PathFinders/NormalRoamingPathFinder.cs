using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Perpetuum.Collections;

namespace Perpetuum.Zones.NpcSystem.Presences.PathFinders
{
    public class NormalRoamingPathFinder : IRoamingPathFinder
    {
        private readonly IZone _zone;
        private static readonly int[,] _n = { { -1, -1 }, { 0, -1 }, { 1, -1 }, { -1, 0 }, { 1, 0 }, { -1, 1 }, { 0, 1 }, { 1, 1 } };
        private double _direction;

        public NormalRoamingPathFinder(IZone zone)
        {
            _zone = zone;
        }

        public Point FindSpawnPosition(RoamingPresence presence)
        {
            var point = _zone.SafeSpawnPoints.GetAll().RandomElement();
            return point.Location;
        }

        public Point FindNextRoamingPosition(RoamingPresence presence)
        {
            var homeRange = presence.Flocks.Max(f => f.HomeRange);
            var rangeMax = homeRange * 2;

            var length = rangeMax * MathHelper.PI2;
            var radians = 1.0 / length;
            var zigzag = radians * 4;

            var pq = new PriorityQueue<Node>(rangeMax * rangeMax * 2);

            var startNode = new Node(presence.CurrentRoamingPosition);
            pq.Enqueue(startNode);

            var closed = new Dictionary<int, bool> { { startNode.GetHashCode(), true } };

            var randomRadians = FastRandom.NextDouble(-zigzag, zigzag);
            _direction += randomRadians;
            MathHelper.NormalizeDirection(ref _direction);

            var farPosition = startNode.position.OffsetInDirection(_direction, rangeMax);

            Node currentNode;
            while (pq.TryDequeue(out currentNode))
            {
                var distance = startNode.Distance(currentNode);
                if (distance >= homeRange)
                {
                    _direction = startNode.position.DirectionTo(currentNode.position);

                    if (!IsRoamingPosition(currentNode.position))
                    {
                        _direction += radians;
                        MathHelper.NormalizeDirection(ref _direction);
                    }

                    return currentNode.position;
                }

                for (var i = 0; i < 8; i++)
                {
                    var neighbor = new Node(_n[i, 0] + currentNode.position.X, _n[i, 1] + currentNode.position.Y);

                    if (closed.ContainsKey(neighbor.GetHashCode()))
                        continue;

                    closed[neighbor.GetHashCode()] = true;

                    if (!_zone.IsWalkableForNpc(neighbor.position))
                        continue;

                    neighbor.cost = CalculateCost(farPosition, neighbor.position);
                    pq.Enqueue(neighbor);
                }
            }

            return startNode.position;
        }

        private int CalculateCost(Point from, Point position)
        {
            var dx = Math.Abs(position.X - from.X);
            var dy = Math.Abs(position.Y - from.Y);

            var cost = dx + dy;

            if (!IsRoamingPosition(position))
            {
                cost += 10000;
            }

            return cost;
        }

        private bool IsRoamingPosition(Point roamingPosition)
        {
            var isRoaming = _zone.Terrain.Controls[roamingPosition.X,roamingPosition.Y].Roaming;
            var isWalkable = _zone.IsWalkable(roamingPosition.X, roamingPosition.Y);
            return isRoaming && isWalkable;
        }

        private struct Node : IComparable<Node>
        {
            public readonly Point position;
            public int cost;

            public Node(int x, int y) : this(new Point(x, y))
            {
            }

            public Node(Point position)
            {
                this.position = position;
                cost = 0;
            }

            public double Distance(Node node)
            {
                return Math.Sqrt(position.SqrDistance(node.position));
            }

            public int CompareTo(Node other)
            {
                return (cost - other.cost);
            }

            public override int GetHashCode()
            {
                return (position.Y << 16) + position.X;
            }

            public override string ToString()
            {
                return $"Position: {position}, Cost: {cost}";
            }
        }
    }
}