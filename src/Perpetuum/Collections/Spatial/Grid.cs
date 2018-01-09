using System;
using System.Collections.Generic;
using System.Drawing;

namespace Perpetuum.Collections.Spatial
{
    public class Grid<TCell> where TCell:Cell
    {
        private readonly TCell[] _cells;
        private readonly Dictionary<TCell,List<TCell>> _neighbours = new Dictionary<TCell, List<TCell>>();

        private int _width;
        private int _height;
        private readonly int _numCellsX;
        private readonly int _numCellsY;
        private readonly int _cellSizeX;
        private readonly int _cellSizeY;

        public Grid(int width, int height, int cellsX, int cellsY,Func<Area,TCell> cellFactory)
        {
            _width = width;
            _height = height;
            _numCellsX = cellsX;
            _numCellsY = cellsY;

            _cellSizeX = width / cellsX;
            _cellSizeY = height / cellsY;

            _cells = new TCell[_numCellsX * _numCellsY];

            for (var y = 0; y < _numCellsY; y++)
            {
                for (var x = 0; x < _numCellsX; x++)
                {
                    var area = Area.FromRectangle(x * _cellSizeX, y * _cellSizeY, _cellSizeX, _cellSizeY);
                    var cell = cellFactory(area);
                    var index = GetCellCoordIndex(x, y);
                    _cells[index] = cell;
                }
            }

            for (var y = 0; y < _numCellsY; y++)
            {
                for (var x = 0; x < _numCellsX; x++)
                {
                    var index = GetCellCoordIndex(x, y);
                    var cell = _cells[index];

                    _neighbours[cell] = new List<TCell>();

                    var p = new Point(x, y);
                    foreach (var np in p.GetNeighbours())
                    {
                        if (np.X < 0 || np.X >= _numCellsX || np.Y < 0 || np.Y >= _numCellsY)
                            continue;

                        _neighbours[cell].Add(_cells[GetCellCoordIndex(np.X,np.Y)]);
                    }
                }
            }
        }

        private int GetCellCoordIndex(int x, int y)
        {
            return x + y * _numCellsX;
        }

        [CanBeNull]
        public TCell GetCell(Point p)
        {
            return GetCell(p.X, p.Y);
        }

        [CanBeNull]
        public TCell GetCell(int x, int y)
        {
            var cx = x / _cellSizeX;
            var cy = y / _cellSizeY;

            if (cx < 0 || cx >= _numCellsX || cy < 0 || cy >= _numCellsY)
                return null;

            var index = GetCellCoordIndex(cx, cy);
            return _cells[index];
        }

        public IEnumerable<TCell> GetCells()
        {
            return _cells;
        }

        public IEnumerable<TCell> FloodFill(int x,int y,Func<TCell,bool> predicate)
        {
            var s = new Stack<TCell>();
            var first = GetCell(x,y);
            s.Push(first);

            var closed = new HashSet<TCell> { first };

            while (s.Count > 0)
            {
                var current = s.Pop();
                yield return current;

                foreach (var neighbour in _neighbours[current])
                {
                    if (closed.Contains(neighbour))
                        continue;

                    closed.Add(neighbour);

                    if ( !predicate(neighbour) )
                        continue;

                    s.Push(neighbour);
                }
            }
        }
    }

}