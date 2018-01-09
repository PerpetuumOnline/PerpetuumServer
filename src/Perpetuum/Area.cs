using System;
using System.Collections.Generic;
using System.Drawing;

namespace Perpetuum
{
    [Serializable]
    public struct Area : IEquatable<Area>
    {
        public static readonly Area Empty = new Area();

        private readonly int _x1;
        private readonly int _y1;
        private readonly int _x2;
        private readonly int _y2;

        public Area(int x1, int y1, int x2, int y2)
        {
            if (x1 < x2)
            {
                _x1 = x1;
                _x2 = x2;
            }
            else
            {
                _x1 = x2;
                _x2 = x1;
            }

            if (y1 < y2)
            {
                _y1 = y1;
                _y2 = y2;
            }
            else
            {
                _y1 = y2;
                _y2 = y1;
            }
        }

        public static Area FromRectangle(int x, int y, int width, int height)
        {
            return new Area(x, y, x + width - 1, y + height - 1);
        }

        public static Area FromRadius(Position position, int radius)
        {
            return FromRadius(position.intX, position.intY, radius);
        }

        public static Area FromRadius(Point position, int radius)
        {
            return FromRadius(position.X, position.Y, radius);
        }
        
        public static Area FromRadius(int x, int y,int radius)
        {
            return new Area(x - radius, y - radius, x + radius, y + radius);
        }

        public int X1
        {
            get { return _x1; }
        }

        public int Y1
        {
            get { return _y1; }
        }

        public int X2
        {
            get { return _x2; }
        }

        public int Y2
        {
            get { return _y2; }
        }

        public int Width
        {
            get { return (_x2 - _x1) + 1; }
        }

        public int Height
        {
            get { return (_y2 - _y1) + 1; }
        }

        public Position CenterPrecise
        {
            get { return new Position((_x1 + _x2) / 2.0, (_y1 + _y2) / 2.0); }
        }

        public Point Center
        {
            get { return new Position((Width >> 1) + _x1, (Height >> 1) + _y1); }
        }

        public int Ground
        {
            get { return Width * Height; }
        }

        public double Diagonal
        {
            get { return Math.Sqrt((Width * Width) + (Height * Height)); }
        }

        [Pure]
        public int GetOffset(int x, int y)
        {
            return x + y * Width;
        }

        public bool Contains(Point target)
        {
            return Contains(target.X, target.Y);
        }

        public bool Contains(Position target)
        {
            return Contains(target.intX, target.intY);
        }

        public bool Contains(Area area)
        {
            return Contains(area.X1, area.Y1) && Contains(area.X2, area.Y2);
        }

        [Pure]
        public bool ContainsInInnerCircle(int x, int y)
        {
            var dx = Center.X - x;
            var dy = Center.Y - y;
            var d = dx*dx + dy*dy;
            var r = Width / 2;
            return r * r >= d;
        }

        [Pure]
        public bool Contains(int x, int y)
        {
            return x >= _x1 && x <= _x2 && y >= _y1 && y <= _y2;
        }

        public override string ToString()
        {
            return $"X1 = {X1} Y1 = {Y1} X2 = {X2} Y2 = {Y2} Width = {Width} Height = {Height}";
        }

        public Area Clamp(Size size)
        {
            return Clamp(size.Width, size.Height);
        }

        public Area Clamp(int width,int height)
        {
            return new Area(_x1.Clamp(0, width - 1),
                            _y1.Clamp(0, height - 1),
                            _x2.Clamp(0, width - 1),
                            _y2.Clamp(0, height - 1));
        }

        public void ForEachXY(Action<int,int> m)
        {
            for (var j = Y1; j <= Y2; j++)
            {
                for (var i = X1; i <= X2; i++)
                {
                    m(i, j);
                }
            }
        }

        public IEnumerable<Area> Slice(int size)
        {
            return Slice(size, size);
        }

        private IEnumerable<Area> Slice(int w,int h)
        {
            var y1 = _y1;
            do
            {
                var y2 = y1 + h;

                if (y2 >= _y2)
                    y2 = _y2;

                var x1 = _x1;
                do
                {
                    var x2 = x1 + w;

                    if (x2 >= _x2)
                        x2 = _x2;

                    yield return new Area(x1, y1, x2, y2);
                    x1 = x2;
                } while (x1 < _x2);
                y1 = y2;

            } while (y1 < _y2);
        }

        public Point GetRandomPosition()
        {
            var x = FastRandom.NextInt(_x1,_x2);
            var y = FastRandom.NextInt(_y1,_y2);
            return new Point(x, y);
        }

        public Area AddBorder(int border)
        {
            return new Area(_x1 - border, _y1 - border, _x2 + border,_y2 + border);
        }

        public IEnumerable<Position> GetPositions()
        {
            for (var j = _y1; j <= _y2; j++)
            {
                for (var i = _x1; i <= _x2; i++)
                {
                    yield return new Position(i,j);
                }
            }
        }

        public double Distance(Point p)
        {
            return Distance(p.X,p.Y);
        }

        public double Distance(int x, int y)
        {
            return Math.Sqrt(SqrDistance(x, y));
        }

        public double SqrDistance(Point p)
        {
            return SqrDistance(p.X,p.Y);
        }

        public double SqrDistance(int x, int y)
        {
            var dx = 0;
            if (X1 > x) dx = X1 - x;
            else 
            if (X2 < x) dx = x - X2;

            var dy = 0;
            if (Y1 > y) dy = Y1 - y;
            else 
            if (Y2 < y) dy = y - Y2;

            return dx*dx + dy*dy;
        }

        public double Distance(Area area)
        {
            var d = SqrDistance(area);
            return Math.Sqrt(d);
        }

        public double SqrDistance(Area area)
        {
            if (IntersectsWith(area))
                return 0.0;

            var mostLeft = _x1 < area.X1 ? this : area;
            var mostRight = area.X2 < _x1 ? this : area;

            var xdiff = mostLeft.X1 == mostRight.X2 ? 0 : mostRight.X1 - mostLeft.X2;
            xdiff = Math.Max(xdiff, 0);

            var upper = Y1 < area.Y1 ? this : area;
            var lower = area.Y1 < Y1 ? this : area;

            var ydiff = upper.Y1 == lower.Y1 ? 0 : lower.Y1 - upper.Y2;
            ydiff = Math.Max(ydiff, 0);

            return xdiff * xdiff + ydiff * ydiff;
        }

        [Pure]
        public bool IntersectsWith(Area area)
        {
            return area.X1 <= X2 && X1 <= area.X2 && area.Y1 <= Y2 && Y1 <= area.Y2;
        }

        [Pure]
        public Area Intersect(Area area)
        {
            return Intersect(area, this);
        }

        public static Area Intersect(Area a, Area b)
        {
            var x1 = Math.Max(a.X1, b.X1);
            var x2 = Math.Min(a.X2, b.X2);
            var y1 = Math.Max(a.Y1, b.Y1);
            var y2 = Math.Min(a.Y2, b.Y2);

            if (x2 >= x1 && y2 >= y1)
                return new Area(x1, y1, x2, y2);

            return Empty;
        }

        public Area Union(Area area)
        {
            return Union(area, this);
        }

        public static Area Union(Area a, Area b)
        {
            var x1 = Math.Min(a.X1, b.X1);
            var x2 = Math.Max(a.X2, b.X2);
            var y1 = Math.Min(a.Y1, b.Y1);
            var y2 = Math.Max(a.Y2, b.Y2);

            return new Area(x1, y1, x2, y2);
        }


        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = _x1;
                hashCode = (hashCode*397) ^ _y1;
                hashCode = (hashCode*397) ^ _x2;
                hashCode = (hashCode*397) ^ _y2;
                return hashCode;
            }
        }

        public static bool operator ==(Area a, Area b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(Area a, Area b)
        {
            return !(a == b);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;

            return obj is Area && Equals((Area)obj);
        }

        public bool Equals(Area area)
        {
            return _x1 == area._x1 && _y1 == area._y1 && _x2 == area._x2 && _y2 == area._y2;
        }
    }
}
