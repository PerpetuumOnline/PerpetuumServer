using System.Data;
using Perpetuum.Data;

namespace Perpetuum.Services.ProductionEngine.CalibrationPrograms
{

    /// <summary>
    /// Default calibration data for dynamic calibration programs
    /// 
    /// </summary>
    public class DynamicCalibrationTemplate
    {
        public readonly double MaterialEfficiency;
        public readonly double TimeEfficiency;
        public readonly int TargetDefinition;

        public DynamicCalibrationTemplate(IDataRecord record)
        {
            MaterialEfficiency = record.GetValue<double>("materialefficiency");
            TimeEfficiency = record.GetValue<double>("timeefficiency");
            TargetDefinition = record.GetValue<int>("targetdefinition");
        }
    }
}
