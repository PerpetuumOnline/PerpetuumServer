using System;
using System.Collections.Generic;
using Perpetuum.Data;
using Perpetuum.ExportedTypes;

namespace Perpetuum.Zones.Artifacts.Repositories
{
    public abstract class ArtifactRepository : IArtifactRepository
    {
        private readonly IArtifactReader _artifactReader;

        public ArtifactRepository(IArtifactReader artifactReader)
        {
            _artifactReader = artifactReader;
        }

        public abstract void InsertArtifact(Artifact artifact);
        public abstract void DeleteArtifact(Artifact artifact);
        public static void DeleteArtifactsByMissionGuid(Guid guid)
        {
            Db.Query().CommandText("delete artifacts where missionguid=@guid")
                           .SetParameter("@guid", guid)
                           .ExecuteNonQuery();
        }

        public IEnumerable<Artifact> GetArtifacts()
        {
            return _artifactReader.GetArtifacts();
        }

        public ArtifactInfo GetArtifactInfo(ArtifactType type)
        {
            return _artifactReader.GetArtifactInfo(type);
        }

        public IEnumerable<IArtifactLoot> GetArtifactLoots(ArtifactType type)
        {
            return _artifactReader.GetArtifactLoots(type);
        }
    }
}