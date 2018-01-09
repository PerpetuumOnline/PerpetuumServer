using System;
using System.Drawing;
using System.Numerics;

namespace Perpetuum.Zones.Terrains
{
    public static class LayerExtensions
    {
        public static bool IsValidPosition(this ILayer layer, int x, int y)
        {
            return x >= 0 && x < layer.Width && y >= 0 && y < layer.Height;
        }

        public static T GetValue<T>(this ILayer<T> layer, Point position)
        {
            return layer.GetValue(position.X, position.Y);
        }

        public static T GetValue<T>(this ILayer<T> layer, Position position)
        {
            return layer.GetValue((int) position.X, (int) position.Y);
        }

        public static T GetValue<T>(this ILayer<T> layer, Vector3 position)
        {
            return layer.GetValue((int) position.X, (int) position.Y);
        }

        public static void SetValue<T>(this ILayer<T> layer, Position position, T value)
        {
            layer.SetValue((int) position.X,(int) position.Y,value);
        }

        public static void UpdateAll<T>(this ILayer<T> layer, Func<int, int, T,T> updater)
        {
            for (var y = 0; y < layer.Height; y++)
            {
                for (var x = 0; x < layer.Height; x++)
                {
                    var current = layer[x, y];
                    var updated = updater(x, y, current);
                    if (current.Equals(updated))
                        continue;
                    layer[x, y] = updated;
                }
            }
        }

        public static void UpdateValue<T>(this ILayer<T> layer, Position position, Func<T, T> updater)
        {
            layer.UpdateValue((int) position.X,(int) position.Y,updater);
        }

        public static void UpdateValue<T>(this ILayer<T> layer, int x, int y, Func<T, T> updater)
        {
            if ( !layer.IsValidPosition(x,y))
                return;

            var current = layer[x, y];
            var updated = updater(current);
            if (current.Equals(updated))
                return;

            layer[x,y] = updated;
        }
    }
}