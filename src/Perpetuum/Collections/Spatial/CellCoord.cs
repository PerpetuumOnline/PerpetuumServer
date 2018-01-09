using System;
using System.Collections.Generic;

namespace Perpetuum.Collections.Spatial
{
    public struct CellCoord : IEquatable<CellCoord>
    {
        public int x;
        public int y;

        public CellCoord(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public GridDistricts ComputeDistrict(CellCoord cell)
        {
            var district = GridDistricts.Undefined;

            if (Math.Abs(x - cell.x) > 1 || Math.Abs(y - cell.y) > 1)
            {
                return GridDistricts.All;
            }

            if (x < cell.x)
            {
                district |= GridDistricts.Left;
            }
            else if (x > cell.x)
            {
                district |= GridDistricts.Right;
            }

            if (y < cell.y)
            {
                district |= GridDistricts.Upper;
            }
            else if (y > cell.y)
            {
                district |= GridDistricts.Lower;
            }

            return district;
        }

        public Area ToArea()
        {
            var x1 = x * Grid.TilesPerGrid;
            var y1 = y * Grid.TilesPerGrid;
            var x2 = x1 + Grid.TilesPerGrid - 1;
            var y2 = y1 + Grid.TilesPerGrid - 1;

            return new Area(x1, y1, x2, y2);
        }

        public override string ToString()
        {
            return $"{x}:{y}";
        }

        public bool Equals(CellCoord other)
        {
            return other.x == x && other.y == y;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;

            return obj is CellCoord && Equals((CellCoord)obj);
        }

        public override int GetHashCode()
        {
            return ObjectHelper.CombineHashCodes(x, y);
        }

        public static bool operator ==(CellCoord left, CellCoord right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(CellCoord left, CellCoord right)
        {
            return !left.Equals(right);
        }

        public static CellCoord operator +(CellCoord a, CellCoord b)
        {
            return new CellCoord(a.x + b.x, a.y + b.y);
        }

        public static CellCoord FromXY(int x, int y)
        {
            var cx = x / Grid.TilesPerGrid;
            var cy = y / Grid.TilesPerGrid;

            return new CellCoord(cx, cy);
        }

        private static readonly IDictionary<GridDistricts, CellCoord> _neighbours = new Dictionary<GridDistricts, CellCoord>
        {
            {GridDistricts.LeftUpper, new CellCoord(-1, -1)},
            {GridDistricts.Upper, new CellCoord(0, -1)},
            {GridDistricts.RightUpper,new CellCoord(1, -1)},
            {GridDistricts.Left,new CellCoord(-1, 0)},
            {GridDistricts.Center, new CellCoord(0,0)},
            {GridDistricts.Right,new CellCoord(1, 0)},
            {GridDistricts.LeftLower,new CellCoord(-1, 1)},
            {GridDistricts.Lower,new CellCoord(0, 1)},
            {GridDistricts.RightLower,new CellCoord(1, 1)}
        };

        public IEnumerable<CellCoord> GetNeighbours(GridDistricts gridDistricts = GridDistricts.All)
        {
            var mask = 0x80;
            do
            {
                var district = gridDistricts & (GridDistricts)mask;

                if (district == GridDistricts.Undefined)
                    continue;

                var n = _neighbours[district];

                n.x += x;
                n.y += y;

                yield return n;

            } while ((mask >>= 1) > 0);
        }

        public bool IsNeighbouring(CellCoord coord)
        {
            return (Math.Abs(coord.x - x) <= 1 & Math.Abs(coord.y - y) <= 1);
        }
    }
}