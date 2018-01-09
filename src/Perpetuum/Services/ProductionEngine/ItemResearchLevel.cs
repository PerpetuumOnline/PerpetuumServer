using System.Collections.Generic;

namespace Perpetuum.Services.ProductionEngine
{
    public class ItemResearchLevel
    {
        public int definition;
        public int researchLevel;
        public int? calibrationProgramDefinition;

        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
                       {
                           {k.definition, definition},
                           {k.researchLevel, researchLevel},
                           {k.calibrationProgram, calibrationProgramDefinition},
                       };
        }
    }
}
