using System;
using Perpetuum.Units;

namespace Perpetuum.Zones.Movements
{
    public abstract class Movement
    {
        public static readonly Movement None = new NullMovement();

        public virtual void Start(Unit unit)
        {
            
        }

        public abstract void Update(Unit unit, TimeSpan elapsed);

        private class NullMovement : Movement
        {
            public override void Update(Unit unit, TimeSpan elapsed)
            {
            }
        }
    }
}