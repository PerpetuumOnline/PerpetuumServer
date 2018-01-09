using System;
using System.Collections.Generic;
using System.Numerics;

namespace Perpetuum
{
    public static class Vector2Extensions
    {
        public static bool IsInRange(this Vector2 source, Vector2 target, float range)
        {
            return Vector2.DistanceSquared(source, target) <= range * range;
        }

        public static IEnumerable<Vector3> LineTo(this Vector3 source,Vector3 target)
        {
            var v = target - source;
            var n = Vector3.Normalize(v);
            var d = v.Length();

            for (var i = 0;i < d;i++)
            {
                yield return source + (n * i);
            }
        }

        public static IEnumerable<Vector2> LineTo(this Vector2 source, Vector2 target)
        {
            var v = target - source;
            var n = Vector2.Normalize(v);
            var d = v.Length();

            for (var i = 0; i < d; i++)
            {
                yield return source + (n * i);
            }
        }

        public static double GetAngle(this Vector2 v)
        {
            if (Math.Abs(v.X) < double.Epsilon)
                return v.Y > 0 ? 0.5 : 0;

            if (Math.Abs(v.Y) < double.Epsilon)
                return v.X > 0 ? 0.25 : 0.75;

            var direction = (Math.Atan(v.Y / v.X) + Math.PI / 2) / Math.PI * 0.5;

            if (v.X < 0)
                direction += 0.5;

            MathHelper.NormalizeDirection(ref direction);
            return direction;
        }

    }
}
