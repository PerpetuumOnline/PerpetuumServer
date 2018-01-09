using System;
using Perpetuum.Units;

namespace Perpetuum.Zones.Finders.PositionFinders
{
    public class ClosestWalkablePositionFinder : ZonePositionFinder
    {
        private readonly double _slope = 4.0;
        private readonly Position _origin;

        public ClosestWalkablePositionFinder(IZone zone,Position origin,Unit unit) : this(zone,origin,unit.Slope)
        {

        }

        public ClosestWalkablePositionFinder(IZone zone,Position origin,double slope = 4.0) : base(zone)
        {
            _slope = slope;
            _origin = origin;
        }

        protected override bool Find(IZone zone, out Position result)
        {
            if (zone.IsWalkable(_origin,_slope) || Math.Abs(_slope) < double.Epsilon)
            {
                result = _origin;
                return true;
            }

            var dx = 1;
            var dy = 0;

            var x = _origin.intX;
            var y = _origin.intY;

            var segmentLength = 1;
            var segmentPassed = 0;

            while (true)
            {
                x += dx;
                y += dy;

                var p = new Position(x, y);

                if (!p.IsValid(zone.Size))
                {
                    result = _origin;
                    return false;
                }

                if (zone.IsWalkable(p,_slope))
                {
                    result = p.Center;
                    return true;
                }

                segmentPassed++;
                if (segmentPassed < segmentLength)
                    continue;

                segmentPassed = 0;

                var tmp = dx;
                dx = -dy;
                dy = tmp;

                if (dy == 0)
                    segmentLength++;
            }
        }
    }
}