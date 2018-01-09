using System.Linq;
using Perpetuum.Services.MissionEngine.MissionTargets;
using Perpetuum.Zones.Artifacts.Repositories;

namespace Perpetuum.Zones.Artifacts.Generators
{
    public class MissionArtifactGenerator: IArtifactGenerator
    {
        private readonly FindArtifactZoneTarget _target;
        private readonly IArtifactRepository _repository;

        public MissionArtifactGenerator(FindArtifactZoneTarget target, IArtifactRepository repository)
        {
            _target = target;
            _repository = repository;
        }

        public void GenerateArtifacts()
        {
            var artifactType = _target.GetArtifactType();
            var artifactInfo = _repository.GetArtifactInfo(artifactType);

            if (!_repository.GetArtifacts().Where(a => a.MissionGuid == _target.MyZoneMissionInProgress.missionGuid && artifactInfo == a.Info).IsNullOrEmpty()) 
                return;

            var position = _target.GetPosition();
            var range = _target.GetRange();
            
            var resultPosition = _target.Zone.FindPassablePointInRadius(position, range);

            var artifact = new Artifact(artifactInfo, resultPosition, _target.Player.Character)
            {
                MissionGuid = _target.MyZoneMissionInProgress.missionGuid
            };

            _repository.InsertArtifact(artifact);
        }
    }
}