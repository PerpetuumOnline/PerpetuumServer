namespace Perpetuum.Zones.Terrains.Materials.Minerals.Generators
{
    public interface IMineralNodeGeneratorFactory
    {
        [CanBeNull]
        RandomWalkMineralNodeGenerator Create();
    }
}