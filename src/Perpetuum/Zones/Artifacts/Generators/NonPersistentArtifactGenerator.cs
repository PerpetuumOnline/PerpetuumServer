using System;
using System.Linq;
using Perpetuum.Data;
using Perpetuum.Log;
using Perpetuum.Players;
using Perpetuum.Zones.Artifacts.Repositories;

namespace Perpetuum.Zones.Artifacts.Generators
{
    public class NonPersistentArtifactGenerator : IArtifactGenerator
    {
        private readonly IZone _zone;
        private readonly IArtifactRepository _repository;
        private readonly Player _player;

        public NonPersistentArtifactGenerator(IZone zone,IArtifactRepository repository, Player player)
        {
            _zone = zone;
            _repository = repository;
            _player = player;
        }

        public void GenerateArtifacts()
        {
            var runningMissionArtifactTargets = _player.MissionHandler.GetArtifactTargets().Where(t => t.MyTarget.ZoneId == _zone.Id).ToArray();
            
            var typesAlreadyCreated = new int[0];

            if (runningMissionArtifactTargets.Length > 0)
            {
                var artifactTypes = runningMissionArtifactTargets.Select(m => (int)m.GetArtifactType()).ArrayToString();
                var cmd = $"select artifacttype from artifacts where zoneid = @zoneId and characterid = @characterId and artifacttype in ({artifactTypes})";
                typesAlreadyCreated = Db.Query().CommandText(cmd)
                                              .SetParameter("@zoneId",_zone.Id)
                                              .SetParameter("@characterId",_player.Character.Id)
                                              .Execute()
                                              .Select(r => r.GetValue<int>(0)).ToArray();
            }

            var artifactTypesToDelete = _nonPersistentIds.Value.Except(typesAlreadyCreated).ToArray();

            if (!artifactTypesToDelete.IsNullOrEmpty())
            {
                var cmd = $"delete from artifacts where zoneid = @zoneId and characterid = @characterId and artifacts.artifacttype in ({artifactTypesToDelete.ArrayToString()})";
                var count = Db.Query().CommandText(cmd)
                                   .SetParameter("@zoneId",_zone.Id)
                                   .SetParameter("@characterId",_player.Character.Id)
                                   .ExecuteNonQuery();

                Logger.Info($"[Artifact] Non active mission artifact deleted. zone:{_zone.Id} player:{_player.InfoString} count:{count}");
            }

            foreach (var missionTargetFindArtifact in runningMissionArtifactTargets.Where(m => !typesAlreadyCreated.Contains((int)m.GetArtifactType())))
            {
                var artifactType = missionTargetFindArtifact.GetArtifactType();
                var position = missionTargetFindArtifact.GetPosition();
                var range = missionTargetFindArtifact.GetRange();

                var artifactTypeInfo = _repository.GetArtifactInfo(artifactType);
                if ( artifactTypeInfo == null )
                    continue;

                range = range - artifactTypeInfo.goalRange;

                var resultPosition = _zone.FindPassablePointInRadius(position, range);
                var artifact = new Artifact(artifactTypeInfo, resultPosition,_player.Character);
                _repository.InsertArtifact(artifact);

                Logger.Info($"[Artifact] Created for mission. zone:{_zone.Id} id:{missionTargetFindArtifact.MyZoneMissionInProgress.missionId} player:{_player.InfoString}");
            }
        }

        private readonly Lazy<int[]> _nonPersistentIds = new Lazy<int[]>(GetNonPersistentArtifactIds);

        private static int[] GetNonPersistentArtifactIds()
        {
            return Db.Query().CommandText("select id from artifacttypes where persistent=0").Execute().Select(r => r.GetValue<int>(0)).ToArray();
        }
    }
}