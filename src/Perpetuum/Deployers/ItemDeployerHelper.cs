using System.Linq;
using Perpetuum.EntityFramework;

namespace Perpetuum.Deployers
{
    public class ItemDeployerHelper
    {
        private readonly IEntityDefaultReader _entityDefaultReader;

        public ItemDeployerHelper(IEntityDefaultReader entityDefaultReader)
        {
            _entityDefaultReader = entityDefaultReader;
        }

        public int GetDeployerItemDefinition(int definition)
        {
            var e = _entityDefaultReader.GetAll().FirstOrDefault(ed => ed.Config.targetDefinition == definition) ?? EntityDefault.None;
            return e.Config.definition;
        }
    }
}