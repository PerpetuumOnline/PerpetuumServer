using System.Linq;
using Perpetuum.EntityFramework;
using Perpetuum.Log;
using Perpetuum.Services.ProductionEngine.CalibrationPrograms;

namespace Perpetuum.Services.ProductionEngine
{
    public static class ProductionDataAccessExtensions
    {
        public static int GetPrototypePair(this IProductionDataAccess dataAccess,int definition)
        {
            return !dataAccess.Prototypes.ContainsKey(definition) ? definition : dataAccess.Prototypes[definition];
        }

        public static int GetOriginalDefinitionFromPrototype(this IProductionDataAccess dataAccess,int prototypeDefinition)
        {
            var x = dataAccess.Prototypes.FirstOrDefault(p => p.Value == prototypeDefinition).Key;
            return x == 0 ? prototypeDefinition : x;
        }

        public static bool IsPrototypeDefinition(this IProductionDataAccess dataAccess,int definition)
        {
            var res = dataAccess.Prototypes.Any(t => t.Value == definition);
            Logger.Info("input is prototype: " + res);
            return res;
        }

        public static int GetResultingDefinitionFromCalibrationDefinition(this IProductionDataAccess dataAccess,int definition)
        {
            var target = (dataAccess.ResearchLevels.Values.Where(ir => ir.calibrationProgramDefinition == definition).Select(ir => ir.definition)).FirstOrDefault();
            if (target == 0)
            {
                Logger.Error("no target definition was found for calibration program: " + EntityDefault.Get(definition).Name + " " + definition);
            }

            return target;
        }

        public static ItemResearchLevel GetItemReserchLevelByCalibrationProgram(this IProductionDataAccess dataAccess,CalibrationProgram calibrationProgram)
        {
            return dataAccess.ResearchLevels.Values.FirstOrDefault(i => i.calibrationProgramDefinition == calibrationProgram.Definition);
        }

        public static bool IsItemResearchable(this IProductionDataAccess dataAccess,int definition)
        {
            return dataAccess.ResearchLevels.ContainsKey(definition);
        }

        public static int GetResearchLevel(this IProductionDataAccess dataAccess, int definition)
        {
            return dataAccess.ResearchLevels.TryGetValue(definition, out ItemResearchLevel researchLevel) ? researchLevel.researchLevel : 0;
        }

        public static double GetProductionDuration(this IProductionDataAccess dataAccess,int targetDefinition)
        {
            if (!EntityDefault.TryGet(targetDefinition, out EntityDefault ed))
            {
                Logger.Error("definition was not found: " + targetDefinition);
                return 1.0;
            }

            foreach (var cf in ed.CategoryFlags.GetCategoryFlagsTree())
            {
                if (dataAccess.ProductionDurations.TryGetValue(cf, out double durationModifier))
                {
                    return durationModifier;
                }
            }

            Logger.Error("consistency error. production duration modifier was not found for definition: " + targetDefinition + " " + ed.CategoryFlags);
            return 1.0;
        }

        public static void GetCalibrationDefault(this IProductionDataAccess dataAccess,EntityDefault ed, out int materialEfficiency, out int timeEfficiency)
        {
            dataAccess.GetCalibrationDefault(ed.Definition,out materialEfficiency,out timeEfficiency);
        }

        public static void GetCalibrationDefault(this IProductionDataAccess dataAccess,int definition, out int materialEfficiency, out int timeEfficiency)
        {
            if (dataAccess.CalibrationDefaults.TryGetValue(definition, out CalibrationDefault calibrationDefault))
            {
                materialEfficiency = (int)calibrationDefault.materialEfficiency;
                timeEfficiency = (int)calibrationDefault.timeEfficiency;
            }
            else
            {
                materialEfficiency = 50;
                timeEfficiency = 50;
#if DEBUG
                Logger.Error("consistency error. no default was found for calibration program: " + definition);
#endif
            }
        }

    }
}