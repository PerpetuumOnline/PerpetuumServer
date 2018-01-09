namespace Perpetuum.Zones.Artifacts.Generators.Loot
{
    public interface IArtifactLootGenerator
    {
        [CanBeNull]
        ArtifactLootItems GenerateLoot(Artifact artifact);
    }
}