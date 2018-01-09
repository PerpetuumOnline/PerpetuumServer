namespace Perpetuum.Zones.Terrains.Materials.Minerals.Generators
{
    public class MineralNodeGeneratorFactory : IMineralNodeGeneratorFactory
    {
        public static readonly IMineralNodeGeneratorFactory None = new NullGenerator();
        private readonly IZone _zone;

        public MineralNodeGeneratorFactory(IZone zone)
        {
            _zone = zone;
        }

        public RandomWalkMineralNodeGenerator Create()
        {
            return new RandomWalkMineralNodeGenerator(_zone);
        }

        private class NullGenerator : IMineralNodeGeneratorFactory
        {
            public RandomWalkMineralNodeGenerator Create()
            {
                return null;
            }
        }
    }
}