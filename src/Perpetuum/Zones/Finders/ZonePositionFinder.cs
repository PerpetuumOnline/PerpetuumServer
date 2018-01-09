namespace Perpetuum.Zones.Finders
{
    public abstract class ZonePositionFinder : IPositionFinder
    {
        private readonly IZone _zone;

        protected ZonePositionFinder(IZone zone)
        {
            _zone = zone;
        }

        public bool Find(out Position result)
        {
            return Find(_zone, out result);
        }

        protected abstract bool Find(IZone zone, out Position result);
    }
}