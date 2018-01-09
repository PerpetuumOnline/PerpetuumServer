using System.Collections.Generic;
using System.Data;
using System.Data.Common;

namespace Perpetuum.Data
{
    public static class DataReaderExtensions
    {
        public static IEnumerable<IDataRecord> ToEnumerable(this IDataReader reader)
        {
            var e  = new DbEnumerator(reader);

            while (e.MoveNext())
            {
                yield return (IDataRecord)e.Current;
            }
        }
    }
}