using System.Collections.Generic;
using System.Linq;
using Perpetuum.Data;

namespace Perpetuum.Services.ProductionEngine.CalibrationPrograms
{
  


    /// <summary>
    /// Calibration program that was creates as an item
    /// Artifact loot currently. 
    /// There are cprgs with different material/timeefficiency, and it technically overrides the values on save
    /// </summary>
    public class DynamicCalibrationProgram : CalibrationProgram
    {
        private static readonly IDictionary<int, DynamicCalibrationTemplate> DynamicCalibrationTemplates;

        static DynamicCalibrationProgram()
        {
            DynamicCalibrationTemplates = Database.CreateCache<int,DynamicCalibrationTemplate>("dynamiccalibrationtemplates", "definition", r => new DynamicCalibrationTemplate(r));
        }

        public DynamicCalibrationProgram(IProductionDataAccess productionDataAccess) : base(productionDataAccess)
        {
        }

        //ez amikor ramba csinalod meghivodik VIGYAZAT!
        public override void OnInsertToDb()
        {
            base.OnInsertToDb();

            MaterialEfficiencyPoints = (int)Template.MaterialEfficiency;
            TimeEfficiencyPoints = (int)Template.TimeEfficiency;
        }

        public static bool IsDefinitionDynamic(int targetDefinition)
        {
            return DynamicCalibrationTemplates.Values.Any(t => t.TargetDefinition == targetDefinition);
        }

        public static int GetDynamicTemplateDefinition(int targetDefinition)
        {
            return DynamicCalibrationTemplates.Where(p => p.Value.TargetDefinition == targetDefinition).Select(o => o.Key).FirstOrDefault();
        }

        private DynamicCalibrationTemplate Template
        {
            get
            {
                //Ensure.IsTrue(DynamicCalibrationTemplates.ContainsKey(Definition));  %%%
                return DynamicCalibrationTemplates[Definition];
            }
        }

        public override int TargetDefinition
        {
            get { return Template.TargetDefinition; }
        }

       

        public override Dictionary<string, object> ToDictionary()
        {
            var info = base.ToDictionary();

            var targetDefinition = TargetDefinition;

            if (targetDefinition != 0 && HasComponents)
            {
                info[k.targetDefinition] = targetDefinition;
            }

            return info;
        }

        public override void CheckTargetForForgeAndThrowIfFailed(CalibrationProgram target)
        {
            var dynamicTarget = target as DynamicCalibrationProgram;
            if (dynamicTarget != null)
            {
                TargetDefinition.ThrowIfNotEqual(dynamicTarget.TargetDefinition, ErrorCodes.CPRGPointsToDifferentItems);
                return;
            }

            base.CheckTargetForForgeAndThrowIfFailed(target);
        }
    }
}