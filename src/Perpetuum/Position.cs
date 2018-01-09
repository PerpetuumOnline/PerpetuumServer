using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using Perpetuum.Collections.Spatial;

namespace Perpetuum
{
    [Serializable]
    public struct Position : IEquatable<Position>,IComparable<Position>
    {
        public static readonly Position Empty = new Position();

        private double _x;
        private double _y;
        private double _z;

        public double X { get { return _x; } }
        public double Y { get { return _y; } }
        public double Z { get { return _z; } }

        public Position(double x, double y, double z = 0.0)
        {
            _x = x;
            _y = y;
            _z = z;
        }

        public int intX
        {
            get { return (int)_x;}
        }

        public int intY
        {
            get { return (int)_y;}
        }

        public int intZ
        {
            get { return (int)_z;}
        }

        public Position Center
        {
            get { return new Position(intX + 0.5, intY + 0.5, _z); }
        }

        public bool IsValid(Size size)
        {
            return size.Contains(intX, intY) && intZ >= 0 && intZ < short.MaxValue;
        }

        public void Normalize()
        {
            var m = Length;
            if ( m <= 0.0 )
                return;

            _x /= m;
            _y /= m;
            _z /= m;
        }

        public double Length
        {
            get
            {
                return Math.Sqrt(_x * _x + _y * _y + _z * _z);
            }
        }

        public double lengthDouble2D
        {
            get
            {
                return Math.Sqrt(_x * _x + _y * _y );
            }
        }

        public bool IsNeighbouring2DInt(Position p)
        {
            var dx = Math.Abs(p.intX - intX);
            var dy = Math.Abs(p.intY - intY);

            if (dx == 1 && dy == 1)
                return true;

            return (dx + dy) <= 1;
        }

        public bool IsTileChange(Position p)
        {
            return (Math.Abs(p.intX - intX) > 0 || Math.Abs(p.intY - intY) > 0);
        }

        public double TotalDistance3D(Position p)
        {
            return Math.Sqrt(SqrDistance3D(p));
        }

        public double SqrDistance3D(Position p)
        {
            var ax = p._x - _x;
            var ay = p._y - _y;
            var az = (p._z - _z)/4.0;
            return ax*ax + ay*ay + az*az;
        }
        
        [System.Diagnostics.Contracts.Pure]
        public double TotalDistance2D(Position p)
        {
            return TotalDistance2D((int) p.X, (int) p.Y);
        }

        [System.Diagnostics.Contracts.Pure]
        public double TotalDistance2D(Point p)
        {
            return TotalDistance2D(p.X, p.Y);
        }

        [System.Diagnostics.Contracts.Pure]
        public double TotalDistance2D(int x, int y)
        {
            var ax = x - _x;
            var ay = y - _y;
            return Math.Sqrt(ax * ax + ay * ay);
        }

        public bool IsEqual2D(Position tPos)
        {
            return (tPos.intX == intX && tPos.intY == intY);
        }
        
        public int CompareTo(Position other)
        {
            if ( intX == other.intX )
            {
                return intY - other.intY;
            }

            return intX - other.intX;
        }

        public override string ToString()
        {
            return $"{X};{Y};{Z}";
        }

        public string ToDoubleString2D()
        {
            return $"X={_x:F} Y={_y:F}";
        }

        public double DirectionTo(Position targetPosition)
        {
            double direction;
            var dx = targetPosition._x - _x;
            var dy = targetPosition._y - _y;

            if (dx.IsZero())
            {
                direction = dy > 0 ? 0.5 : 0;
                return direction;
            }

            if (dy.IsZero())
            {
                direction = dx > 0 ? 0.25 : 0.75;
                return direction;
            }

            // - PI/2 ... + PI/2
            var angle = Math.Atan(dy / dx);

            //0 ... PI
            var radians = (angle + Math.PI / 2);

            //0 ... PI      =>     0 ... 128
            direction = (radians / Math.PI * 0.5);

            if (dx < 0)
            {
                direction = (0.5 + direction);
            }
           
            MathHelper.NormalizeDirection(ref direction);
            return direction;
        }

        public static double GetAngle(Position p)
        {
            var x = p.X;
            var y = p.Y;

            if (Math.Abs(x) < double.Epsilon)
                return y > 0 ? 0.5 : 0;

            if (Math.Abs(y) < double.Epsilon)
                return x > 0 ? 0.25 : 0.75;

            var direction = (Math.Atan(y / x) + Math.PI / 2) / Math.PI * 0.5;

            if (x < 0)
                direction += 0.5;

            MathHelper.NormalizeDirection(ref direction);
            return direction;
        }

        private const double PI2 = Math.PI * 2;

        public Position OffsetInDirection(double direction, double distance)
        {
            var angleRadians = direction * PI2;

            var deltaX = Math.Sin(angleRadians) * distance;
            var deltaY = Math.Cos(angleRadians) * distance;

            return new Position(_x + deltaX, _y - deltaY, _z);
        }

        public Position GetRandomPositionInRange2D(IntRange range)
        {
            return GetRandomPositionInRange2D(range.Min, range.Max);
        }

        [System.Diagnostics.Contracts.Pure]
        public Position GetRandomPositionInRange2D(double minRange, double maxRange)
        {
            var randomAngleRadians = FastRandom.NextDouble() * Math.PI * 2;

            var offSetX = Math.Sin(randomAngleRadians);
            var offSetY = Math.Cos(randomAngleRadians);
            var distance = FastRandom.NextDouble(minRange,maxRange);
            return new Position(_x + (offSetX * distance), _y + (offSetY * distance), _z);
        }

        public Position GetPositionTowards2D(Position position, double distance)
        {
            var xDiff = position._x - _x;
            var yDiff = position._y - _y;
            var totalDistance = TotalDistance2D(position);

            if (totalDistance.IsZero()) 
                return position;

            var xNorm = xDiff/totalDistance;
            var yNorm = yDiff/totalDistance;
            return new Position(_x + (xNorm*distance), _y + (yNorm*distance), _z);
        }

        public bool IsWithinRangeOf2D(Position sourcePosition, double range)
        {
            return IsWithinRangeOf2D(sourcePosition._x, sourcePosition._y, range);
        }

        public bool IsWithinRangeOf2D(double cX, double cY, double range)
        {
            var ax = cX - _x;
            var ay = cY - _y;
            var dist = (ax * ax) + (ay * ay);
            return (range * range > dist);
        }

        public bool IsWithinOrEqualRange(double cX, double cY, double range)
        {
            var ax = cX - _x;
            var ay = cY - _y;
            var dist = (ax * ax) + (ay * ay);
            return (range * range >= dist);
        }

        [System.Diagnostics.Contracts.Pure]
        public bool IsInRangeOf2D(Position sourcePosition, double range)
        {
            return IsInRangeOf2D(sourcePosition._x, sourcePosition._y, range);
        }

        public bool IsInRangeOf2D(double cX, double cY, double range)
        {
            var ax = cX - _x;
            var ay = cY - _y;
            var dist = (ax * ax) + (ay * ay);
            return (range * range >= dist);
        }

        public bool IsInRangeOf3D(Position sourcePosition, double range)
        {
            var dx = sourcePosition._x - _x;
            var dy = sourcePosition._y - _y;
            var dz = (sourcePosition._z - _z)/4.0;
            var dist = (dx*dx) + (dy*dy) + (dz*dz);
            return (range * range >= dist);
        }

        public Position RotateAroundOrigo(double radians)
        {
            var xRotated = Math.Cos(radians) * _x - Math.Sin(radians) * _y;
            var yRotated = Math.Sin(radians) * _x + Math.Cos(radians) * _y;

            return new Position(xRotated , yRotated , _z);
        }

        public static Position RotateCWWithTurns(Position sourcePosition, int rotationTurns)
        {
            if (rotationTurns == 0) return sourcePosition;
            
            while (rotationTurns > 0)
            {
                sourcePosition = sourcePosition.Rotate90CW();
                rotationTurns--;
            }

            return sourcePosition;
        }

        public Position Rotate90CW()
        {
            return new Position(-1 * _y, _x, _z);
        }

        public static Position RotateCCWWithTurns(Position sourcePosition, int rotationTurns)
        {
            if (rotationTurns == 0) return sourcePosition;

            while (rotationTurns > 0)
            {
                sourcePosition = sourcePosition.Rotate90CCW();
                rotationTurns--;
            }

            return sourcePosition;
        }

        public Position Rotate90CCW()
        {
            return new Position(_y, -1 * _x, _z);
        }

        public Position Clamp(Size size)
        {
            return new Position(_x.Clamp(0, size.Width - 1), _y.Clamp(0, size.Height - 1), _z.Clamp(0, short.MaxValue));
        }

        #region Operators

        public static Position operator +(Position p,double d)
        {
            return new Position(p._x + d, p._y + d, p._z + d);
        }

        public static Position operator -(Position p, double d)
        {
            return new Position(p._x - d, p._y - d, p._z - d);
        }

        public static Position operator +(Position a,Position b)
        {
            return new Position(a._x + b._x, a._y + b._y, a._z + b._z);
        }

        public static Position operator +(Position a,Vector2 v)
        {
            return new Position(a._x + v.X, a._y + v.Y,a.Z);
        }

        public static Position operator -(Position a, Position b)
        {
            return new Position(a._x - b._x, a._y - b._y, a._z - b._z);
        }

        public static Position operator /(Position p, double num)
        {
            return new Position(p._x / num, p._y / num, p._z / num);
        }

        public static Position operator /(Position p, int num)
        {
            return new Position(p._x / num, p._y / num, p._z / num);
        }

        public static Position operator *(Position p, double  num)
        {
            return new Position(p._x * num, p._y * num, p._z * num);
        }

        public static Position operator *(Position p, int num)
        {
            return new Position(p._x * num, p._y * num, p._z * num);
        }

        public static implicit operator Point(Position p)
        {
            return p.ToPoint();
        }

        public static implicit operator Position(Vector2 v)
        {
            return new Position(v.X,v.Y);
        }

        public static explicit operator Position(Vector3 v)
        {
            return new Position(v.X,v.Y,v.Z);
        }

        public static bool operator ==(Position p1, Position p2)
        {
            return p1.Equals(p2);
        }

        public static bool operator !=(Position p1, Position p2)
        {
            return !(p1 == p2);
        }

        #endregion

        public bool Equals(Position other)
        {
            return GetUlongHashCode() == other.GetUlongHashCode();
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (obj.GetType() != typeof (Position)) return false;
            return Equals((Position) obj);
        }

        public override int GetHashCode()
        {
            return GetUlongHashCode().GetHashCode();
        }

        public Vector2 ToVector2()
        {
            return new Vector2((float) _x,(float) _y);
        }

        public Vector3 ToVector3()
        {
            return new Vector3((float) _x,(float) _y,(float) _z);
        }

        public Point ToPoint()
        {
            return new Point((int) _x,(int) _y);
        }

        public PointF ToPointF()
        {
            return new PointF((float) _x,(float) _y);
        }

        public ulong GetUlongHashCode()
        {
            unchecked
            {
                var hx = (ulong)intX & 0x7fff;
                var hy = (ulong)intY & 0x7fff;
                var hz = (ulong)intZ & 0x7fff;
                return hx << 40 | hy << 16 | hz;
            }
        }

        private static readonly int[,] _nonDiagonalNeighbours = { { 0, 1 }, { 1, 0 }, { 0, -1 }, { -1, 0 } };

        public IEnumerable<Position> NonDiagonalNeighbours
        {
            get
            {
                for (var i = 0; i < 4; i++)
                {
                    var nx = intX + _nonDiagonalNeighbours[i, 0];
                    var ny = intY + _nonDiagonalNeighbours[i, 1];

                    yield return new Position(nx, ny);
                }
            }
        }
        private static readonly int[,] _neighbours = { { -1, -1 }, { 0, -1 }, { 1, -1 }, { -1, 0 }, { 1, 0 }, { -1, 1 }, { 0, 1 }, { 1, 1 } };

        public IEnumerable<Position> GetEightNeighbours(Size size)
        {
            return EightNeighbours.Where(np => np.IsValid(size));
        }

        public IEnumerable<Position> EightNeighbours
        {
            get
            {
                for (var i = 0; i < 8; i++)
                {
                    var nx = _x + _neighbours[i, 0];
                    var ny = _y + _neighbours[i, 1];

                    yield return new Position(nx,ny);
                }
            }
        }

        public Position AddToZ(double offset)
        {
            return new Position(X,Y,Z + offset);
        }

        public double DirectionTo(Vector2 v)
        {
            return MathHelper.DirectionTo(_x, _y, v.X, v.Y);
        }

        public static Position Abs(Position p)
        {
            return new Position(Math.Abs(p.X),Math.Abs(p.Y),Math.Abs(p.Z));
        }

        public Position GetWorldPosition(double zoneX,double zoneY)
        {
            var wx = zoneX + X;
            var wy = zoneY + Y;
            return new Position(wx,wy);
        }

        public CellCoord ToCellCoord()
        {
            return CellCoord.FromXY((int)X,(int)Y);
        }

        public double DirectionTo(Point point)
        {
            return DirectionTo(point.ToPosition());
        }

        public bool IsInRangeOf2D(Point point,double range)
        {
            return IsInRangeOf2D(point.ToPosition(),range);
        }


    }
}


