using System.Data;
using System.Diagnostics;

namespace Perpetuum.Data
{
    public interface IDataRecordStepper
    {
        T GetNextValue<T>();
    }

    /// <summary>
    /// Steps through the columns of an sql record. Useful when a large amount of selected columns present in a record 
    /// </summary>
    public struct DataRecordStepper : IDataRecordStepper
    {
        private readonly IDataRecord _record;
        private int _index;

        public DataRecordStepper(IDataRecord record)
        {
            _record = record;
            _index = 0;
        }

        public T GetNextValue<T>()
        {
            Debug.Assert(_record.FieldCount > _index,"Invalid index. i:" + _index + " > c:" + _record.FieldCount);
            return _record.GetValue<T>(_index++);
        }
    }
}