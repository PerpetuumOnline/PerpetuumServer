using System.Collections.Generic;
using Perpetuum.Data;

namespace Perpetuum.Services.Sparks
{
    public class SparkExtensionsReader : ISparkExtensionsReader
    {
        public IEnumerable<SparkExtension> GetAllBySparkID(int sparkID)
        {
            var records = Db.Query().CommandText("select * from sparkextensions where sparkid = @sparkID").SetParameter("@sparkID",sparkID).Execute();
            var list = new List<SparkExtension>();

            foreach (var record in records)
            {
                var se = new SparkExtension();
                se.SparkId = record.GetValue<int>("sparkid");
                var extensionId = record.GetValue<int>("extensionid");
                var extensionLevel = record.GetValue<int>("extensionlevel");
                se.Extension = new Extension(extensionId, extensionLevel);
                list.Add(se);
            }

            return list;
        }
    }
}