using System.Collections.Generic;
using Perpetuum.Items;

namespace Perpetuum.Services.ProductionEngine.CalibrationPrograms
{
    public abstract class CalibrationProgramBase : Item
    {
        public abstract  int TargetDefinition { get;  }
        public abstract int MaterialEfficiencyPoints { get; set; }
        public abstract int TimeEfficiencyPoints { get; set; }
        public abstract List<ProductionComponent> Components { get; }
        public abstract bool HasComponents { get; }
        public abstract bool IsMissionRelated { get; }

    }
}
