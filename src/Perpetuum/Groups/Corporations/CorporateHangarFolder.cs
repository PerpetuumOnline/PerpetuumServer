using Perpetuum.Containers;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Items;

namespace Perpetuum.Groups.Corporations
{
    /// <summary>
    /// A utility container to sort items
    /// This container can only be found in CorporateHangar containers
    /// </summary>
    public class CorporateHangarFolder : Container
    {
        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
                base.AcceptVisitor(visitor);
        }

        [CanBeNull]
        private CorporateHangar ParentHangar
        {
            get { return GetOrLoadParentEntity() as CorporateHangar; }
        }

        public override void OnLoadFromDb()
        {
            if (ParentHangar == null)
            {
                var corporateHangar = GetOrThrow(Parent);
                corporateHangar.AddChild(this);
            }

            if (ParentHangar != null) 
                SetLogging(ParentHangar.IsLogging(), null);

            base.OnLoadFromDb();
        }

        public override void AddItem(Item item, long issuerEid, bool doStack)
        {
            CorporateHangar.CheckAllowedTypesForAddAndThrowIfFailed(item);
            // owner = corporation!
            base.AddItem(item,Owner, doStack);
        }

        public override void ReloadItems(long? ownerEid)
        {
            base.ReloadItems(Owner);
        }

        protected override bool IsPersonalContainer
        {
            get { return false; }
        }

        public static CorporateHangarFolder CreateCorporateHangarFolder()
        {
            return (CorporateHangarFolder)Factory.CreateWithRandomEID(DefinitionNames.CORPORATE_HANGAR_FOLDER);
        }
    }
}
