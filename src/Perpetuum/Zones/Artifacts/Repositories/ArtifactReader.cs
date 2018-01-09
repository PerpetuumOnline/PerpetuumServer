using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Perpetuum.Accounting.Characters;
using Perpetuum.Data;
using Perpetuum.ExportedTypes;

namespace Perpetuum.Zones.Artifacts.Repositories
{
    public abstract class ArtifactReader : IArtifactReader
    {

        public abstract IEnumerable<Artifact> GetArtifacts();

        private static readonly ILookup<ArtifactType, ArtifactLoot> _artifactLoots = Database.CreateLookupCache<ArtifactType, ArtifactLoot>("artifactloot", "artifacttype", r => new ArtifactLoot(r));
        private static readonly IDictionary<ArtifactType, ArtifactInfo> _artifactInfos = Database.CreateCache<ArtifactType, ArtifactInfo>("artifacttypes", "id", ArtifactInfo.GenerateArtifactInfo);
        public ArtifactInfo GetArtifactInfo(ArtifactType type)
        {
            return _artifactInfos[type];
        }

        public IEnumerable<IArtifactLoot> GetArtifactLoots(ArtifactType type)
        {
            return _artifactLoots.GetOrEmpty(type);
        }

        protected Artifact CreateArtifactFromRecord(IDataRecord record)
        {
            var id = record.GetValue<int>("id");
            var artifactType = (ArtifactType)record.GetValue<int>("artifacttype");
            var info = GetArtifactInfo(artifactType);

            var x = record.GetValue<int>("positionx");
            var y = record.GetValue<int>("positiony");
            var position = new Position(x, y);

            var characterId = record.GetValue<int>("characterid");

            var a = new Artifact(id, info, position, Character.Get(characterId));

            var missionGuid = record.GetValue<Guid?>("missionGuid");
            if (missionGuid != null)
                a.MissionGuid = (Guid)missionGuid;

            return a;
        }


    }
}