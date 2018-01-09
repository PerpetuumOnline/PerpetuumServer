using System.Collections.Generic;
using System.Linq;
using Perpetuum.Services.Looting;
using Perpetuum.Zones.Artifacts.Repositories;

namespace Perpetuum.Zones.Artifacts.Generators.Loot
{
    public class ArtifactLootGenerator : IArtifactLootGenerator
    {
        private readonly IArtifactRepository _artifactRepository;

        public ArtifactLootGenerator(IArtifactRepository artifactRepository)
        {
            _artifactRepository = artifactRepository;
        }

        public ArtifactLootItems GenerateLoot(Artifact artifact)
        {
            var artifactType = artifact.Info.type;
            var info = _artifactRepository.GetArtifactInfo(artifactType);
            if (info == null)
                return null;

            var result = new List<LootItem>();

            var loots = _artifactRepository.GetArtifactLoots(artifactType).ToArray();

            if (loots.Length <= 0)
                return null;

            do
            {
                foreach (var loot in loots)
                {
                    var chance = FastRandom.NextDouble();
                    if (chance > loot.Chance)
                        continue;

                    var builder = loot.GetLootItemBuilder();
                    var lootItem = builder.Build();
                    result.Add(lootItem);
                }

            } while (result.Count < info.minimumLoot);

            return new ArtifactLootItems(artifact.Position, result);
        }
    }
}