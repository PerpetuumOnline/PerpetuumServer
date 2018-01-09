using System.Threading;
using Perpetuum.Zones.Terrains;

namespace Perpetuum.Zones.Finders.PositionFinders
{
    public class RandomPassablePositionFinder : ZonePositionFinder
    {
        public RandomPassablePositionFinder(IZone zone) : base(zone)
        {
        }

        protected override bool Find(IZone zone, out Position result)
        {
            bool isPassable;
            do
            {
                result = FindPositionWithinIsland(zone);
                isPassable = zone.Terrain.IsPassable(result);
            } while (!isPassable);

            return true;
        }

        private static Position FindPositionWithinIsland(IZone zone)
        {
            var counter = 0;
            while (true)
            {
                var xo = FastRandom.NextInt(0, zone.Size.Width - 1);
                var yo = FastRandom.NextInt(0, zone.Size.Height - 1);
                var p = new Position(xo, yo);

                if (!zone.Terrain.Blocks[xo, yo].Island)
                    return p;

                counter++;
                if (counter % 50 == 0)
                {
                    Thread.Sleep(1);
                }
            }
        }
    }
}