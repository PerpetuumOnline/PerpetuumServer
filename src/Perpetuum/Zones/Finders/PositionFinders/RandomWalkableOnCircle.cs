using Perpetuum.Units;

namespace Perpetuum.Zones.Finders.PositionFinders
{
    /// <summary>
    /// Finds a random position on the edge of a circle, with some tolerance
    /// </summary>
    public class RandomWalkableOnCircle : RandomWalkableAroundPositionFinder
    {
        private readonly int _edgeTolerance;
        public RandomWalkableOnCircle(IZone zone, Position origin, int range, int edgeTolerance, Unit unit) : this(zone, origin, range, edgeTolerance, unit.Slope) { }
        public RandomWalkableOnCircle(IZone zone, Position origin, int range, int edgeTolerance, double slope = 4.0) : base(zone, origin, range, slope)
        {
            _edgeTolerance = edgeTolerance;
        }

        protected override Position GetRandomPos(IZone zone)
        {
            var random = FastRandom.NextDouble(0.0, 1.0);
            return _origin.OffsetInDirection(random, _maxRange);
        }

        protected override bool CheckResult(IZone zone, Position position)
        {
            var check = zone.FindWalkableArea(position, _zoneArea, AREA_TEST_SIZE) != null;
            return check && position.TotalDistance2D(_origin) > _maxRange - _edgeTolerance;
        }
    }
}
