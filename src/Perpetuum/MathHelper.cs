using System;
using System.Numerics;

namespace Perpetuum
{
    public static class MathHelper
    {
        public const double PI2 = Math.PI * 2;

        public static double DirectionTo(double x0,double y0,double x1,double y1)
        {
            var x = x1 - x0;
            var y = y1 - y0;

            if ( Math.Abs(x) < double.Epsilon )
                return y > 0 ? 0.5 : 0;

            if ( Math.Abs(y) < double.Epsilon )
                return x > 0 ? 0.25 : 0.75;

            var direction = (Math.Atan(y / x) + Math.PI / 2) / Math.PI * 0.5;

            if (x < 0)
                direction += 0.5;

            return NormalizeDirection(direction);
        }

        public static Vector2 DirectionToVector(double direction, double magnitude = 1.0)
        {
            var a = direction * Math.PI * 2;

            var x = Math.Sin(a) * magnitude;
            var y = Math.Cos(a) * magnitude;

            return new Vector2((float)x, (float)-y);
        }

        private static double NormalizeDirection(double direction)
        {
            return direction - Math.Floor(direction);
        }

        public static void NormalizeDirection(ref double direction)
        {
            direction = direction - Math.Floor(direction);
        }

        /// <summary>
        /// tensioned ease in ease out curve 
        /// tension 0 = linear
        /// </summary>
        /// <param name="value"></param>
        /// <param name="tension">0... N</param>
        /// <returns></returns>
        public static double TensionedEaseInEaseOut(double value, double tension)
        {
            value = value.Clamp();

            tension = Math.Max(1, tension + 1);

            if (value < 0.5)
            {
                return Math.Pow((value * 2), tension) / 2;
            }

            return 1 - (Math.Pow((2 - value * 2), tension)) / 2;
        }

        /// <summary>
        /// Reversed tension ease in ease out
        /// </summary>
        /// <param name="value"></param>
        /// <param name="tension"></param>
        /// <returns></returns>
        public static double ReverseTensionedEaseInEaseOut(double value, double tension)
        {
            value = value.Clamp();

            tension = Math.Max(1, tension + 1);

            if (value < 0.5)
            {
                return Math.Pow(value * 2, 1 / tension) / 2;
            }

            return 1 - Math.Pow(((value - 1) * -2), 1 / tension) / 2;
        }

        /// <summary>
        /// This function calculates a near-far type of falloff function.
        /// Returns double which represents the effect.
        /// near = 1
        /// far  = 0
        /// A smooth sine transition is calculated between the near far range. 
        /// </summary>
        /// <param name="nearRadius"></param>
        /// <param name="farRadius"></param>
        /// <param name="originX"></param>
        /// <param name="originY"></param>
        /// <param name="targetX"></param>
        /// <param name="targetY"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        public static double DistanceFalloff(int nearRadius, int farRadius, double originX, double originY, double targetX, double targetY)
        {
            if (nearRadius > farRadius)
            {
                nearRadius = farRadius;
            }

            var distance = Math.Sqrt(((targetX - originX) * (targetX - originX)) + ((targetY - originY) * (targetY - originY)));

            double result = 0;

            //out of far range => 0 returns
            if (distance < farRadius)
            {
                if (distance <= nearRadius)
                {
                    //within the near range
                    return 1;
                }

                result = (Math.Sin(((distance - nearRadius) / (farRadius - nearRadius) * Math.PI) + Math.PI / 2) + 1) / 2;
            }

            return result;
        }


        /// <summary>
        /// Xa,Ya is point 1 on the line segment.
        /// Xb,Yb is point 2 on the line segment.
        /// Xp,Yp is the point.
        /// </summary>
        /// <param name="xa"></param>
        /// <param name="ya"></param>
        /// <param name="xb"></param>
        /// <param name="yb"></param>
        /// <param name="xp"></param>
        /// <param name="yp"></param>
        /// <returns></returns>
        public static double DistanceFromLineSegment(double xa, double ya, double xb, double yb, double xp, double yp)
        {
            var xu = xp - xa;
            var yu = yp - ya;
            var xv = xb - xa;
            var yv = yb - ya;
            if (xu * xv + yu * yv < 0)
            {
                return Math.Sqrt(Math.Pow(xp - xa, 2) + Math.Pow(yp - ya, 2));
            }


            xu = xp - xb;
            yu = yp - yb;
            xv = -xv;
            yv = -yv;
            if (xu * xv + yu * yv < 0)
            {
                return Math.Sqrt(Math.Pow(xp - xb, 2) + Math.Pow(yp - yb, 2));
            }

            return
                Math.Abs((xp * (ya - yb) + yp * (xb - xa) + (xa * yb - xb * ya)) / Math.Sqrt(Math.Pow(xb - xa, 2) + Math.Pow(yb - ya, 2)));
        }
    }
}
