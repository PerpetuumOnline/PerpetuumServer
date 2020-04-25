using System.Collections.Generic;
using System.Linq;
using Perpetuum.ExportedTypes;
using Perpetuum.Services.ProductionEngine.CalibrationPrograms;

namespace Perpetuum.Services.ProductionEngine
{
    public interface IProductionDataAccess
    {
        IDictionary<int,int> Prototypes { get; }
        IDictionary<int, ItemResearchLevel> ResearchLevels { get; }
        ILookup<int, ProductionComponent> ProductionComponents { get; }
        IDictionary<CategoryFlags, double> ProductionDurations { get; }
        IDictionary<CategoryFlags, ProductionCost> ProductionCostByCategory { get; }
        IDictionary<int, ProductionCost> ProductionCostByTechLevel { get; }
        IDictionary<int, CalibrationDefault> CalibrationDefaults { get; }

        ProductionDecalibration GetDecalibration(int targetDefinition);
    }
}