using System.Collections.Generic;
using System.Linq;

namespace Perpetuum.Zones.Artifacts.Repositories
{
    public class CompositeArtifactReader : ArtifactReader
    {
        private readonly List<IArtifactReader> _readers;

        public CompositeArtifactReader(params IArtifactReader[] readers)
        {
            this._readers = readers.ToList();
        }

        public CompositeArtifactReader(IEnumerable<IArtifactReader> readers)
        {
            _readers = readers.ToList();
        }

        public override IEnumerable<Artifact> GetArtifacts()
        {
            return _readers.SelectMany(r => r.GetArtifacts());
        }

        public void AddReader(IArtifactReader reader)
        {
            _readers.Add(reader);
        }

    }
}