namespace Perpetuum.Zones.Artifacts.Repositories
{
    public interface IArtifactRepository : IArtifactReader
    {
        void InsertArtifact(Artifact artifact);
        void DeleteArtifact(Artifact artifact);
    }
}