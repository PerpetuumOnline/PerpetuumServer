using System.Data;
using Perpetuum.Data;

namespace Perpetuum.Services.ProductionEngine.CalibrationPrograms
{

    /// <summary>
    /// Default calibration of a CPRG
    /// 
    /// </summary>
    public class CalibrationDefault
    {
        public double materialEfficiency;
        public double timeEfficiency;

        public CalibrationDefault(IDataRecord record)
        {
            materialEfficiency = record.GetValue<double>(k.materialEfficiency.ToLower());
            timeEfficiency = record.GetValue<double>(k.timeEfficiency.ToLower());
        }
    }
}
