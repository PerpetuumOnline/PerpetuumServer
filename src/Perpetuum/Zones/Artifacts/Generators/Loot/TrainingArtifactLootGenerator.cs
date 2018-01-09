namespace Perpetuum.Zones.Artifacts.Generators.Loot
{
    public class TrainingArtifactLootGenerator : IArtifactLootGenerator
    {
        private const int MIN_RANGE = 2;
        private const int MAX_RANGE = 10;
        private readonly IArtifactLootGenerator _lootGenerator;

        public TrainingArtifactLootGenerator(IArtifactLootGenerator lootGenerator)
        {
            _lootGenerator = lootGenerator;
        }

        public ArtifactLootItems GenerateLoot(Artifact artifact)
        {
            var lootItems = _lootGenerator.GenerateLoot(artifact);

            if (lootItems == null)
                return null;

            var randomPosition = lootItems.Position.GetRandomPositionInRange2D(MIN_RANGE, MAX_RANGE);
            return new ArtifactLootItems(randomPosition,lootItems.LootItems);
        }
    }
}