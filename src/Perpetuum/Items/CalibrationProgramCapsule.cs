using Perpetuum.Data;
using Perpetuum.EntityFramework;
using System.Collections.Generic;
using System.Data;

namespace Perpetuum.Items
{

    public class CalibrationProgramCapsule : Item
    {
        public static IDictionary<int, CalibrationCapsuleRecord> CalibrationTemplateItemLookup;

        static CalibrationProgramCapsule()
        {
            CalibrationTemplateItemLookup = Database.CreateCache<int, CalibrationCapsuleRecord>("calibrationtemplateitems", "definition", r => new CalibrationCapsuleRecord(r));
        }


        IEntityDefaultReader _entityDefaultReader;


        public CalibrationProgramCapsule(IEntityDefaultReader entityDefaultReader) : base()
        {
            _entityDefaultReader = entityDefaultReader;
        }


        public EntityDefault Activate()
        {
            CalibrationCapsuleRecord record = CalibrationTemplateItemLookup.GetOrDefault(Definition);
            return _entityDefaultReader.Get(record.TargetDefinition);
        }

    }


    public class CalibrationCapsuleRecord
    {
        public readonly int TargetDefinition;

        public CalibrationCapsuleRecord(IDataRecord record)
        {
            TargetDefinition = record.GetValue<int>("targetdefinition");
        }
    }
}
