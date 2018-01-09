
using Perpetuum.IDGenerators;

namespace Perpetuum.EntityFramework
{
    public interface IEntityServices
    {
        IEntityDefaultReader Defaults { get; }
        IEntityFactory Factory { get; }
        IEntityRepository Repository { get; }
    }

    public interface IEntityFactory
    {
        Entity Create(string definitionName,IIDGenerator<long> idGenerator);
        Entity Create(int definition,IIDGenerator<long> idGenerator);
        Entity Create(EntityDefault entityDefault,IIDGenerator<long> idGenerator);
    }

    public static class EntityFactoryExtensions
    {
        public static Entity CreateWithRandomEID(this IEntityFactory factory,int definition)
        {
            return factory.Create(definition, EntityIDGenerator.Random);
        }

        public static Entity CreateWithRandomEID(this IEntityFactory factory,EntityDefault ed)
        {
            return factory.Create(ed, EntityIDGenerator.Random);
        }

        public static Entity CreateWithRandomEID(this IEntityFactory factory,string definitionName)
        {
            return factory.Create(definitionName,EntityIDGenerator.Random);
        }
    }
}