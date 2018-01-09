using System.Collections.Generic;
using System.Linq;

namespace Perpetuum.Zones.Artifacts.Generators
{
    public class CompositeArtifactGenerator : IArtifactGenerator
    {
        private readonly List<IArtifactGenerator> _generators;

        public CompositeArtifactGenerator(params IArtifactGenerator[] generators)
        {
            _generators = generators.ToList();
        }

        public void GenerateArtifacts()
        {
            foreach (var generator in _generators)
            {
                generator.GenerateArtifacts();
            }
        }

        public void AddGenerator(IArtifactGenerator generator)
        {
            _generators.Add(generator);
        }
    }
}