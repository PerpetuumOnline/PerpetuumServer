using System.Linq;
using Perpetuum.Zones.NpcSystem.Flocks;

namespace Perpetuum.Zones.NpcSystem.Presences
{
    public class RandomFlockSelector : IRandomFlockSelector
    {
        private readonly IRandomFlockReader _randomFlockReader;

        public RandomFlockSelector(IRandomFlockReader randomFlockReader)
        {
            _randomFlockReader = randomFlockReader;
        }

        [CanBeNull]
        public IFlockConfiguration SelectRandomFlockByPresence(RandomPresence presence)
        {
            var flockInfos = _randomFlockReader.GetByPresence(presence);
            
            var sumRate = flockInfos.Sum(r => r.rate);
            var minRate = 0.0;
            var chance = FastRandom.NextDouble();

            foreach (var flockRate in flockInfos)
            {
                var rate = flockRate.rate / sumRate;
                var maxRate = rate + minRate;

                if (minRate < chance && chance <= maxRate)
                {
                    return presence.FlockConfigurationRepository.Get(flockRate.flockID);
                }

                minRate += rate;
            }

            return null;
        }
    }
}