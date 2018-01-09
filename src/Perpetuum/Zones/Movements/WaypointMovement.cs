using System;
using System.Numerics;
using Perpetuum.Units;

namespace Perpetuum.Zones.Movements
{
    public class WaypointMovement : Movement
    {
        private readonly Vector2 _target;

        public WaypointMovement(Vector2 target)
        {
            _target = target;
        }

        public Vector2 Target
        {
            get { return _target; }
        }

        public override void Start(Unit unit)
        {
            Arrived = false;
            unit.Direction = unit.CurrentPosition.DirectionTo(_target);
            unit.CurrentSpeed = 1.0;

            _velocity = Vector2.Subtract(_target,unit.CurrentPosition.ToVector2());
            _distance = Vector2.Abs(_velocity);
            _velocity = Vector2.Normalize(_velocity);

            base.Start(unit);
        }

        private Vector2 _velocity;
        private Vector2 _distance;

        public override void Update(Unit unit, TimeSpan elapsed)
        {
            if ( Arrived )
                return;
            
            var d = (float) (unit.Speed * elapsed.TotalSeconds);
            var v = Vector2.Multiply(_velocity,d);

            _distance -= Vector2.Abs(v);

            if (_distance.X <= 0.0f && _distance.Y <= 0.0f)
            {
                Arrived = true;
                unit.CurrentPosition = _target;
            }
            else
            {
                unit.CurrentPosition += v;
            }

//            unit.Zone.CreateAlignedDebugBeam(BeamType.orange_20sec, unit.CurrentPosition);
        }

        public bool Arrived { get; private set; }
    }
}