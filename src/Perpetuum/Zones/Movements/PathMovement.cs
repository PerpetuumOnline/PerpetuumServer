using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using Perpetuum.Units;

namespace Perpetuum.Zones.Movements
{
    public class PathMovement : Movement
    {
        private readonly Queue<WaypointMovement> _path = new Queue<WaypointMovement>();
        private WaypointMovement _movement;

        public PathMovement(IEnumerable<Point> path)
        {
            foreach (var point in path.Skip(1))
            {
                _path.Enqueue(new WaypointMovement(Vector2.Add(point.ToVector2(),new Vector2(0.5f,0.5f))));
            }
        }

        private bool _arrived = false;

        public bool Arrived
        {
            get { return _arrived; }
        }

        public override void Start(Unit unit)
        {
            _arrived = false;
            base.Start(unit);
        }

        public override void Update(Unit unit, TimeSpan elapsed)
        {
            if (_movement == null)
            {
                if (_path.Count == 0)
                {
                    unit.StopMoving();
                    _arrived = true;
                    return;
                }

                _movement = _path.Dequeue();
                _movement.Start(unit);
            }

            _movement.Update(unit,elapsed);

            if (_movement.Arrived)
                _movement = null;
        }
    }
} 