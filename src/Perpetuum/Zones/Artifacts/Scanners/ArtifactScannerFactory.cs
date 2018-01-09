using System.Linq;
using Perpetuum.Players;
using Perpetuum.Zones.Artifacts.Generators;
using Perpetuum.Zones.Artifacts.Generators.Loot;
using Perpetuum.Zones.Artifacts.Repositories;

namespace Perpetuum.Zones.Artifacts.Scanners
{
    public class ArtifactScannerFactory
    {
        private readonly IZone _zone;

        public ArtifactScannerFactory(IZone zone)
        {
            _zone = zone;
        }

        public IArtifactScanner CreateArtifactScanner(Player player)
        {
            IArtifactRepository artifactRepository;
            IArtifactGenerator artifactGenerator;
            IArtifactLootGenerator artifactLootGenerator;

            if (_zone is TrainingZone)
            {
                artifactGenerator = new NullArtifactGenerator();

                artifactRepository = new TrainingZoneArtifactRepository();
                artifactLootGenerator = new TrainingArtifactLootGenerator(new ArtifactLootGenerator(artifactRepository));
            }
            else
            {

                var reader = new ZoneArtifactReader(player,ArtifactReadMode.Persistent);

                var cr = new CompositeArtifactReader(reader);

                var targets = player.MissionHandler.GetArtifactTargets();
                var uniqueGuids = targets.Select(t => t.MyZoneMissionInProgress.missionGuid).Distinct().ToList();
                foreach (var missionGuid in uniqueGuids)
                {
                    cr.AddReader( new MissionArtifactReader(missionGuid));
                }
                
                artifactRepository = new ZoneArtifactRepository(_zone,cr);

                var cg = new CompositeArtifactGenerator(new PersistentArtifactGenerator(_zone, artifactRepository, player));

                foreach (var target in targets)
                {
                    cg.AddGenerator(new MissionArtifactGenerator(target,artifactRepository));
                }

                artifactGenerator = cg;
                artifactLootGenerator = new ArtifactLootGenerator(artifactRepository);
            }

            var scanner = new ArtifactScanner(_zone, artifactRepository, artifactGenerator ,artifactLootGenerator);
            return scanner;
        }

       
    }
}