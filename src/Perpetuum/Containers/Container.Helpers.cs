using Perpetuum.Accounting.Characters;
using Perpetuum.EntityFramework;
using Perpetuum.Items;
using Perpetuum.Units.DockingBases;
using Perpetuum.Units.FieldTerminals;

namespace Perpetuum.Containers
{
    partial class Container
    {
        public static void GetContainersWithItems(Character character, long sourceContainerEid, long targetContainerEid, out Container sourceContainer, out Container targetContainer)
        {
            // a sourcet mindig betoltjuk
            sourceContainer = GetWithItems(sourceContainerEid, character, ContainerAccess.Remove);

            if (sourceContainerEid == targetContainerEid)
            {
                // ha ugyanaz a target mint a source akkor access-t nezunk es kilepunk
                targetContainer = sourceContainer;
                targetContainer.CheckAccessAndThrowIfFailed(character, ContainerAccess.Add);
                return;
            }

            // megkeressuk,h a target nem-e gyereke a sourcenak
            targetContainer = (Container) sourceContainer.GetItem(targetContainerEid, true);

            if (targetContainer != null)
            {
                // ha igen akkor csak accesst nezunk
                targetContainer.CheckAccessAndThrowIfFailed(character, ContainerAccess.Add);
                return;
            }

            // nem gyereke ezert betoltjuk a targetet
            targetContainer = GetWithItems(targetContainerEid, character, ContainerAccess.Add);

            // itt megnezzuk,h a targetnek nem gyereke-e a source
            var tmpContainer = (Container) targetContainer.GetItem(sourceContainerEid,true);
            if (tmpContainer == null)
                return;

            //  ha igen akkor access-t nezunk es a gyerek lesz a source
            sourceContainer = tmpContainer;
            sourceContainer.CheckAccessAndThrowIfFailed(character, ContainerAccess.Remove);
        }

        public static Container GetWithItems(long containerEid, Character character, ContainerAccess access)
        {
            var container = GetOrThrow(containerEid);

            container.Quantity.ThrowIfNotEqual(1, ErrorCodes.ItemHasToBeSingle);
            container.IsRepackaged.ThrowIfTrue(ErrorCodes.ContainerHasToBeUnPacked);
            container.CheckAccessAndThrowIfFailed(character, access);
            container.ReloadItems(character);

            return container;
        }

        public static Container GetWithItems(long containerEid, Character character)
        {
            var container = GetOrThrow(containerEid);
            container.ReloadItems(character);
            return container;
        }

        public new static Container GetOrThrow(long containerEid)
        {
            return (Container) Repository.LoadOrThrow(containerEid);
        }

        public static PublicContainer GetFromStructure(long strucureEid)
        {
            var entity = Services.Repository.Load(strucureEid);

            if (entity is DockingBase dockingBase)
            {
                return dockingBase.GetPublicContainer();
            }

            if ( entity is FieldTerminal fieldTerminal)
            {
                return fieldTerminal.GetPublicContainer();
            }

            return null;
        }
    }

    public class ContainerHelper
    {
        private readonly ItemHelper _itemHelper;

        public ContainerHelper(ItemHelper itemHelper)
        {
            _itemHelper = itemHelper;
        }

        public PublicContainer GetFromStructure(long strucureEid)
        {
            var entity = _itemHelper.LoadItem(strucureEid);

            if (entity is DockingBase dockingBase)
            {
                return dockingBase.GetPublicContainer();
            }

            if (entity is FieldTerminal fieldTerminal)
            {
                return fieldTerminal.GetPublicContainer();
            }

            return null;
        }
    }

}
