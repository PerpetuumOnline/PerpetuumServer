using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;

namespace Perpetuum.Containers.SystemContainers
{
    /// <summary>
    /// System level containers for facilities, etc
    /// </summary>
    public class DefaultSystemContainer : SystemContainer
    {
        public static DefaultSystemContainer Create()
        {
            return (DefaultSystemContainer)Factory.CreateWithRandomEID(DefinitionNames.SYSTEM_CONTAINER);
        }
    }
}
