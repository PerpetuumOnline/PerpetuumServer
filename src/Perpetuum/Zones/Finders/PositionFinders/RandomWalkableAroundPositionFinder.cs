using Perpetuum.Units;

namespace Perpetuum.Zones.Finders.PositionFinders
{
    /// <summary>
    /// Find a Random walkable location within some defined circle
    /// </summary>
    public class RandomWalkableAroundPositionFinder : ZonePositionFinder
    {
        protected const int AREA_TEST_SIZE = 3000;
        protected const int MAX_INTERATIONS = 100;

        protected readonly int _maxRange;
        protected readonly double _slope = 4.0;
        protected readonly Position _origin;
        protected readonly Area _zoneArea;

        public RandomWalkableAroundPositionFinder(IZone zone, Position origin, int range, Unit unit) : this(zone, origin, range, unit.Slope) { }
        public RandomWalkableAroundPositionFinder(IZone zone, Position origin, int range, double slope = 4.0) : base(zone)
        {
            _slope = slope;
            _origin = origin;
            _maxRange = range;
            _zoneArea = zone.Size.ToArea();
        }

        protected Position FindClosestWalkable(IZone zone, Position pos)
        {
            var finder = new ClosestWalkablePositionFinder(zone, pos, _slope);
            if(finder.Find(out Position p))
            {
                return p;
            }
            return Position.Empty;
        }

        protected virtual bool CheckResult(IZone zone, Position position)
        {
            return zone.FindWalkableArea(position, _zoneArea, AREA_TEST_SIZE) != null;
        }

        protected virtual Position GetRandomPos(IZone zone)
        {
            return zone.FindPassablePointInRadius(_origin, _maxRange);
        }

        protected override bool Find(IZone zone, out Position result)
        {
            result = Position.Empty;
            for (int i = 0; i < MAX_INTERATIONS; i++)
            {
                var randPos = GetRandomPos(zone);
                var walkablePos = FindClosestWalkable(zone, randPos);
                if (CheckResult(zone, walkablePos))
                {
                    result = walkablePos;
                    return true;
                }
            }
            return false;
        }
    }
}