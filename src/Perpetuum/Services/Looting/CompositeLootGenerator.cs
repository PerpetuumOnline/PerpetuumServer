using System.Collections.Generic;
using System.Linq;

namespace Perpetuum.Services.Looting
{
    public class CompositeLootGenerator : ILootGenerator
    {
        private readonly ILootGenerator[] _generators;

        public CompositeLootGenerator(params ILootGenerator[] generators)
        {
            _generators = generators;
        }

        public IReadOnlyCollection<LootGeneratorItemInfo> GetInfos()
        {
            var infos = _generators.Select(x => x.GetInfos());
            return infos.SelectMany(infoset => infoset).ToList().AsReadOnly();
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