using System.Collections.Generic;
using System.Linq;
using Perpetuum.ExportedTypes;

namespace Perpetuum.EntityFramework
{
    public interface IEntityDefaultReader
    {
        bool Exists(int definition);
        EntityDefault Get(int definition);
        EntityDefault GetByEid(long eid);
        bool TryGet(int definition, out EntityDefault entityDefault);
        IEnumerable<EntityDefault> GetAll();
    }

    public static class EntityDefaultReaderExtensions
    {
        [NotNull]
        public static EntityDefault GetByName(this IEntityDefaultReader reader, string name)
        {
            return reader.GetAll().FirstOrDefault(ed => ed.Name == name) ?? EntityDefault.None;
        }

        public static IEnumerable<EntityDefault> GetByCategoryFlags(this IEnumerable<EntityDefault> entityDefaults,CategoryFlags categoryFlags)
        {
            return entityDefaults.Where(ed => ed.CategoryFlags.IsCategory(categoryFlags));
        }

        public static int[] GetDefinitionsByCategoryFlag(this IEnumerable<EntityDefault> entityDefaults,CategoryFlags categoryFlags)
        {
            return entityDefaults.GetByCategoryFlags(categoryFlags).Select(ed => ed.Definition).ToArray();
        }
    }
}
