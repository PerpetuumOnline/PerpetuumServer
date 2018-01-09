namespace Perpetuum.Zones.Terrains.Materials.Minerals.Generators
{
    public interface IMineralNodeGenerator
    {
        [CanBeNull]
        MineralNode Generate(MineralLayer layer);
    }
}