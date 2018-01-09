using System.Drawing;
using System.Linq;
using Perpetuum.Data;
using Perpetuum.ExportedTypes;
using Perpetuum.Log;
using Perpetuum.Players;
using Perpetuum.Zones.Artifacts.Repositories;
using Perpetuum.Zones.Terrains;

namespace Perpetuum.Zones.Artifacts.Generators
{
    public class PersistentArtifactGenerator : IArtifactGenerator
    {
        private const int MAX_ARTIFACTS_ON_ZONE = 10;
        private static readonly ILookup<int, ArtifactSpawnRate> _artifactSpawnRates = Database.CreateLookupCache<int, ArtifactSpawnRate>("artifactspawninfo", "zoneid", r => new ArtifactSpawnRate(r.GetValue<ArtifactType>("artifacttype"), r.GetValue<double>("rate")), null);

        private readonly IZone _zone;
        private readonly IArtifactRepository _artifactRepository;
        private readonly Player _player;

        public PersistentArtifactGenerator(IZone zone,IArtifactRepository artifactRepository,Player player)
        {
            _zone = zone;
            _artifactRepository = artifactRepository;
            _player = player;
        }

        public void GenerateArtifacts()
        {
            if (!HasArtifacts()) 
                return;

            var artifactsCount = _artifactRepository.GetArtifacts().Count(a => a.Info.isPersistent && a.Character == _player.Character);

            while (artifactsCount < MAX_ARTIFACTS_ON_ZONE)
            {
                var artifactType = GetNextArtifactType();
                var info = _artifactRepository.GetArtifactInfo(artifactType);
                var position = FindArtifactPosition(_zone).ToPosition();

                var artifact = new Artifact(info, position, _player.Character);
                _artifactRepository.InsertArtifact(artifact);

                Logger.Info($"[Artifact] Created. zone:{_zone.Id} artifact:{artifact} player:{_player.InfoString}");

                artifactsCount++;
            }
        }

        private ArtifactType GetNextArtifactType()
        {
            if (!HasArtifacts())
                return ArtifactType.undefined;

            var spawnRates = _artifactSpawnRates[_zone.Id].ToArray();

            var sumRate = spawnRates.Sum(r => r.rate);
            var minRate = 0.0;
            var chance = FastRandom.NextDouble();

            foreach (var spawnRate in spawnRates)
            {
                var rate = spawnRate.rate / sumRate;
                var maxRate = rate + minRate;

                if (minRate < chance && chance <= maxRate)
                {
                    return spawnRate.artifactType;
                }

                minRate += rate;
            }

            return ArtifactType.undefined;
        }

        private static Point FindArtifactPosition(IZone zone)
        {
            if (!zone.Configuration.Terraformable)
            {
                return zone.GetRandomPassablePosition();
            }

            // gamman keresunk teruletet
            var p = zone.FindWalkableArea(zone.Size.ToArea(), 20);
            return p.RandomElement();
        }

        private bool HasArtifacts()
        {
            return _artifactSpawnRates.Contains(_zone.Id);
        }

        private class ArtifactSpawnRate
        {
            public readonly ArtifactType artifactType;
            public readonly double rate;

            public ArtifactSpawnRate(ArtifactType artifactType, double rate)
            {
                this.artifactType = artifactType;
                this.rate = rate;
            }
        }
    }
}