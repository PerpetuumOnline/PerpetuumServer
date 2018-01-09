using System.Collections.Generic;
using System.Data;
using System.Linq;
using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Log;
using Perpetuum.Services.ProductionEngine.CalibrationPrograms;

namespace Perpetuum.Services.ProductionEngine
{
    public class ProductionDataAccess : IProductionDataAccess
    {
        private readonly IEntityDefaultReader _entityDefaultReader;
        private IDictionary<int, int> _prototypes;
        private IDictionary<int, ItemResearchLevel> _researchlevels;
        private ILookup<int, ProductionComponent> _productionComponents;
        private IDictionary<CategoryFlags, double> _productionDurations;
        private IDictionary<int, CalibrationDefault> _calibrationDefaults;
        private IDictionary<CategoryFlags, ProductionDecalibration> _productionDecalibrations;

        public ProductionDataAccess(IEntityDefaultReader entityDefaultReader)
        {
            _entityDefaultReader = entityDefaultReader;
        }

        public void Init()
        {
            _prototypes = Database.CreateCache<int, int>("prototypes", "definition", "prototype");
            _researchlevels = Database.CreateCache<int, ItemResearchLevel>("itemresearchlevels", "definition", r =>
            {
                var level = new ItemResearchLevel
                {
                    definition = r.GetValue<int>(k.definition),
                    researchLevel = r.GetValue<int>(k.researchLevel.ToLower()),
                    calibrationProgramDefinition = r.GetValue<int?>(k.calibrationProgram.ToLower())
                };
                return level;
            },ItemResearchLevelFilter);

            _productionComponents = Database.CreateLookupCache<int, ProductionComponent>("components", "definition", r =>
            {
                var ed = _entityDefaultReader.Get(r.GetValue<int>(k.componentDefinition.ToLower()));
                var amount = r.GetValue<int>(k.componentAmount.ToLower());
                return new ProductionComponent(ed, amount);
            }, r => _entityDefaultReader.Exists(r.GetValue<int>(k.definition)));

            _productionDurations = Database.CreateCache<CategoryFlags, double>("productionduration", "category", "durationmodifier");
            _calibrationDefaults = Database.CreateCache<int, CalibrationDefault>("calibrationdefaults", "definition", r => new CalibrationDefault(r));
            _productionDecalibrations = Database.CreateCache<CategoryFlags, ProductionDecalibration>("productiondecalibration", "categoryflag", r =>
            {
                var distorsionMin = r.GetValue<double>(k.distorsionMin.ToLower());
                var distorsionMax = r.GetValue<double>(k.distorsionMax.ToLower());
                var decrease = r.GetValue<double>("decrease");
                return new ProductionDecalibration(distorsionMin,distorsionMax,decrease);
            });
        }

        public bool ItemResearchLevelFilter(IDataRecord record)
        {
            var definition = record.GetValue<int>(k.definition);

            if (!_entityDefaultReader.Exists(definition) || !record.GetValue<bool>(k.enabled))
            {
                return false;
            }

            var calibrationPrg = record.GetValue<int?>(k.calibrationProgram.ToLower());
            if (calibrationPrg == null)
                return true;

            var cprgED = _entityDefaultReader.Get((int)calibrationPrg);
            if (cprgED.CategoryFlags.IsCategory(CategoryFlags.cf_calibration_programs))
                return true;

            Logger.Error("illegal calibration program was defined for definition:" + definition + " calibration program def:" + cprgED.Name + " " + cprgED.Definition);
            return false;
        }



        public IDictionary<int, int> Prototypes => _prototypes;
        public IDictionary<int, ItemResearchLevel> ResearchLevels => _researchlevels;
        public ILookup<int, ProductionComponent> ProductionComponents => _productionComponents;
        public IDictionary<CategoryFlags, double> ProductionDurations => _productionDurations;
        public IDictionary<int, CalibrationDefault> CalibrationDefaults => _calibrationDefaults;

        public ProductionDecalibration GetDecalibration(int targetDefinition)
        {
            if (!_entityDefaultReader.TryGet(targetDefinition, out EntityDefault ed))
            {
                Logger.Error("consistency error! definition was not found for production line. definition:" + targetDefinition);
                return ProductionDecalibration.Default;
            }

            return GetDecalibration(ed);
        }

        public ProductionDecalibration GetDecalibration(EntityDefault target)
        {
            foreach (var flagInTree in target.CategoryFlags.GetCategoryFlagsTree())
            {
                if (_productionDecalibrations.TryGetValue(flagInTree, out ProductionDecalibration productionDecalibration))
                    return productionDecalibration;
            }

            return ProductionDecalibration.Default;
        }
    }
}