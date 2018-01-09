using System.Collections.Generic;
using System.Drawing;

namespace Perpetuum.Collections.Spatial
{

    public class Grid
    {
        public static int TilesPerGrid = 64;

        protected static readonly Dictionary<GridDistricts, CellCoord> Neighbours = new Dictionary<GridDistricts, CellCoord>
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

        public static Size CalculateGridSize(Size size)
        {
            return new Size(size.Width / TilesPerGrid, size.Height / TilesPerGrid);
        }
    }
}