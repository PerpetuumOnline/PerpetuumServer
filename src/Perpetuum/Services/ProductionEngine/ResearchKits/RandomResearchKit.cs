using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Items;
using Perpetuum.Log;

namespace Perpetuum.Services.ProductionEngine.ResearchKits
{
    /// <summary>
    /// Used by the mission system
    /// </summary>
    public class RandomResearchKit : ResearchKit
    {
        public RandomResearchKit(IProductionDataAccess productionDataAccess) : base(productionDataAccess) { }

        /// <summary>
        /// If source item is mission related then it's ok.
        /// No level match
        /// </summary>
        /// <param name="sourceItem"></param>
        /// <returns></returns>
        public override ErrorCodes IsMatchingWithItem(Item sourceItem)
        {
            if (sourceItem.IsCategory(CategoryFlags.cf_generic_random_items))
            {
                return ErrorCodes.NoError;
            }

            return ErrorCodes.OnlyMissionItemAccepted;

        }

        public override void GetCalibrationDefaults(EntityDefault entityDefault,  out int materialEfficiency, out int timeEfficiency)
        {
            Logger.Info("Random research kit processing. Using fake efficiencies. CPRG:" + this.Eid );

            //fake it for the mission
            materialEfficiency = 100;
            timeEfficiency = 100;
        }

        public override bool IsMissionRelated
        {
            get { return true; }
        }

        
    }

}
