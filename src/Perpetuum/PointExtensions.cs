using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;

namespace Perpetuum
{
    public static class PointExtensions
    {
        private static readonly int[,] _neighbours = { { -1, -1 }, { 0, -1 }, { 1, -1 }, { -1, 0 }, { 1, 0 }, { -1, 1 }, { 0, 1 }, { 1, 1 } };
        private static readonly int[,] _nonDiagonalNeighbours = { { 0, 1 }, { 1, 0 }, { 0, -1 }, { -1, 0 } };


        public static Position ToPosition(this Point p)
        {
            return new Position(p.X + 0.5, p.Y + 0.5);
        }

        public static Position ToPosition(this PointF p)
        {
            return new Position(p.X,p.Y);
        }

        public static IEnumerable<Point> GetNonDiagonalNeighbours(this Point point)
        {
            for (var i = 0; i < 4; i++)
            {
                var nx = point.X + _nonDiagonalNeighbours[i, 0];
                var ny = point.Y + _nonDiagonalNeighbours[i, 1];

                yield return new Point(nx, ny);
            }
        }

        public static IEnumerable<Point> GetNeighbours(this Point point)
        {
            for (var i = 0; i < 8; i++)
            {
                var nx = point.X + _neighbours[i, 0];
                var ny = point.Y + _neighbours[i, 1];

                yield return new Point(nx, ny);
            }
        }

        public static IEnumerable<Vector2> GetNeighbours(this Vector2 v)
        {
            for (var i = 0; i < 8; i++)
            {
                var nx = v.X + _neighbours[i, 0];
                var ny = v.Y + _neighbours[i, 1];

                yield return new Vector2(nx, ny);
            }
        }


        public static IEnumerable<Point> GetNeighbours(this Point point,int size)
        {
            for (int y = -size; y <= size; y++)
            {
                for (int x = -size; x <= size; x++)
                {
                    var nx = point.X + x;
                    var ny = point.Y + y;

                    yield return new Point(nx, ny);
                }
            }
        }

        public static IEnumerable<Vector2> GetNeighbours(this Vector2 v,int size)
        {
            for (int y = -size; y <= size; y++)
            {
                for (int x = -size; x <= size; x++)
                {
                    var nx = v.X + x;
                    var ny = v.Y + y;

                    yield return new Vector2(nx, ny);
                }
            }
        }

        public static Point GetNearestPoint(this Point point, IEnumerable<Point> points)
        {
            var nearestPoint = Point.Empty;
            var nearestDistSq = int.MaxValue;

            foreach (var p in points)
            {
                var distSqr = SqrDistance(point, p);
                if (distSqr >= nearestDistSq)
                    continue;

                nearestPoint = p;
                nearestDistSq = distSqr;
            }

            return nearestPoint;
        }

        public static bool IsInRange(this Point p1, Point p2,double range)
        {
            return p1.SqrDistance(p2) <= range * range;
        }

        public static double Distance(this Point p1, Point p2)
        {
            return Math.Sqrt(SqrDistance(p1, p2));
        }

        public static int SqrDistance(this Point p1, Point p2)
        {
            return SqrDistance(p1, p2.X, p2.Y);
        }

        public static int SqrDistance(this Point p1,int x,int y)
        {
            var dx = p1.X - x;
            var dy = p1.Y - y;
            return dx * dx + dy * dy;
        }

        [UsedImplicitly]
        public static double DirectionTo(this Point from,Point to)
        {
            var dx = to.X - from.X;
            var dy = to.Y - from.Y;

            if (dx == 0)
                return dy > 0 ? 0.5 : 0;

            if (dy == 0)
                return dx > 0 ? 0.25 : 0.75;

            // - PI/2 ... + PI/2
            var angle = Math.Atan((double)dy / dx);
            //0 ... PI
            var radians = (angle + Math.PI / 2);
            //0 ... PI      =>     0 ... 128
            var direction = (radians / Math.PI * 0.5);

            if (dx < 0)
            {
                direction = (0.5 + direction);
            }

            MathHelper.NormalizeDirection(ref direction);
            return direction;
        }

        private const double PI2 = Math.PI * 2;

        public static Point OffsetInDirection(this Point p,double direction, double distance)
        {
            var angleRadians = direction * PI2;

            var deltaX = Math.Sin(angleRadians) * distance;
            var deltaY = Math.Cos(angleRadians) * distance;

            return new Point((int) (p.X + deltaX), (int) (p.Y - deltaY));
        }

        public static IEnumerable<Point> FloodFill(this Point p,Func<Point,bool> validator = null)
        {
            var q = new Queue<Point>();
            q.Enqueue(p);

            var closed = new HashSet<Point> { p };

            Point current;
            while (q.TryDequeue(out current))
            {
                yield return current;

                foreach (var np in current.GetNeighbours())
                {
                    if ( closed.Contains(np) )
                        continue;

                    closed.Add(np);

                    if (validator != null && !validator(np))
                        continue;

                    q.Enqueue(np);
                }
            }
        }

        public static Vector2 ToVector2(this Point p)
        {
            return new Vector2(p.X,p.Y);
        }
    }
}
