using System;
using Perpetuum.Units;

namespace Perpetuum.Zones.Movements
{
    public class RandomMovement : Movement
    {
        private readonly Position _origin;
        private readonly double _range;

        public RandomMovement(Position origin,double range)
        {
            _origin = origin;
            _range = range; 
        }

        private void SelectRandomDirection(Unit unit)
        {
            var nextDirection = FastRandom.NextDouble();
            unit.Direction = nextDirection;
        }

        public override void Start(Unit unit)
        {
            unit.CurrentSpeed = 1.0;
            SelectRandomDirection(unit);
        }

        public override void Update(Unit unit, TimeSpan elapsed)
        {
            var zone = unit.Zone;
            if (zone == null)
                return;

            var distanceTaken = Math.Min(unit.Speed * elapsed.TotalSeconds, 1.0);

            var nextPosition = unit.CurrentPosition.OffsetInDirection(unit.Direction, distanceTaken);
            var isInHomeRange = nextPosition.IsInRangeOf2D(_origin,_range);
            var isWalkable = unit.IsWalkable(nextPosition);

            if (!isInHomeRange || !isWalkable)
            {
                SelectRandomDirection(unit);
                return;
            }

            unit.CurrentPosition = nextPosition;
//            zone.CreateAlignedDebugBeam(BeamType.blue_20sec,nextPosition);
        }
    }
}