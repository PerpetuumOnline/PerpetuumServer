using System;
using System.Drawing;

namespace Perpetuum
{
    public static class SizeExtensions
    {
        public static bool Contains(this Size size,Point p)
        {
            return Contains(size, p.X,p.Y);
        }

        public static bool Contains(this Size size,int x, int y)
        {
            return x >= 0 && x < size.Width && y >= 0 && y < size.Height;
        }

        public static Point GetCenter(this Size size)
        {
            return new Point(size.Width / 2,size.Height / 2);
        }

        public static Area ToArea(this Size size)
        {
            return Area.FromRectangle(0,0,size.Width,size.Height);
        }

        public static Position GetRandomPosition(this Size size, int margin)
        {
            var minX = 0 + margin;
            var maxX = size.Width - margin;

            var minY = 0 + margin;
            var maxY = size.Height - margin;

            return new Position(FastRandom.NextInt(minX, maxX), FastRandom.NextInt(minY, maxY));
        }

        [System.Diagnostics.Contracts.Pure]
        public static int Ground(this Size size)
        {
            return size.Width * size.Height;
        }

        [System.Diagnostics.Contracts.Pure]
        public static T[] CreateArray<T>(this Size size)
        {
            return new T[size.Width * size.Height];
        }

        [System.Diagnostics.Contracts.Pure]
        public static T[,] Create2DArray<T>(this Size size)
        {
            return new T[size.Width, size.Height];
        }

        [System.Diagnostics.Contracts.Pure]
        public static double Diagonal(this Size size)
        {
            return Math.Sqrt((size.Width * size.Width) + (size.Height * size.Height));
        }
    }
}
