using System.Collections.Generic;

namespace Perpetuum.Services.Looting
{
    public class CompositeLootGenerator : ILootGenerator
    {
        private readonly ILootGenerator[] _generators;

        public CompositeLootGenerator(params ILootGenerator[] generators)
        {
            _generators = generators;
        }

        public IEnumerable<LootItem> Generate()
        {
            var items = new List<LootItem>();

            foreach (var generator in _generators)
            {
                items.AddRange(generator.Generate());
            }

            return items;
        }
    }
}