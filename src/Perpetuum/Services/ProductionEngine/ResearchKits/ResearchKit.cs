using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Items;

namespace Perpetuum.Services.ProductionEngine.ResearchKits
{
    public class ResearchKit : Item
    {
        private readonly IProductionDataAccess _productionDataAccess;

        public ResearchKit(IProductionDataAccess productionDataAccess)
        {
            _productionDataAccess = productionDataAccess;
        }

        public int GetResearchLevel()
        {
            return ED.Options.Level;
        }

        public static int GetResearchLevelByDefinition(int definition)
        {
            return EntityDefault.Get(definition).Options.Level;
        }

        public int MyResearchLevel
        {
            get { return this.ED.Options.Level; }

        }

        public virtual ErrorCodes IsMatchingWithItem(Item sourceItem)
        {
            if (sourceItem.IsCategory(CategoryFlags.cf_random_items))
            {
                return ErrorCodes.OnlyMissionResearchKitAccepted;
            }

            var researchKitLevel = GetResearchLevel();

            var itemLevel = _productionDataAccess.GetResearchLevel(sourceItem.Definition);

            return itemLevel <= researchKitLevel ? ErrorCodes.NoError : ErrorCodes.ResearchLevelMismatch;
        }


        public virtual void GetCalibrationDefaults(EntityDefault entityDefault,  out int materialEfficiency, out int timeEfficiency)
        {
            _productionDataAccess.GetCalibrationDefault(entityDefault,out materialEfficiency,out timeEfficiency);
        }

        public virtual bool IsMissionRelated
        {
            get { return false; }
        }
    }
}
