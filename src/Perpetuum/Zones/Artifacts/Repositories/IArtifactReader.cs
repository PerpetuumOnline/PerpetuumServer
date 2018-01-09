using System.Collections.Generic;
using Perpetuum.ExportedTypes;

namespace Perpetuum.Zones.Artifacts.Repositories
{
    public interface IArtifactReader
    {
        IEnumerable<Artifact> GetArtifacts();
        [CanBeNull]
        ArtifactInfo GetArtifactInfo(ArtifactType type);
        IEnumerable<IArtifactLoot> GetArtifactLoots(ArtifactType type);
    }
}