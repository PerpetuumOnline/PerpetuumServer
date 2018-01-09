using System.Collections.Generic;
using System.Data;
using System.Linq;
using Perpetuum.Data;
using Perpetuum.Services.ExtensionService;

namespace Perpetuum.Services.Sparks
{
    public class SparkRepository : ISparkRepository
    {
        private readonly ISparkExtensionsReader _sparkExtensionsReader;
        private readonly IExtensionReader _extensionReader;

        public SparkRepository(ISparkExtensionsReader sparkExtensionsReader,IExtensionReader extensionReader)
        {
            _sparkExtensionsReader = sparkExtensionsReader;
            _extensionReader = extensionReader;
        }

        public Spark Get(int id)
        {
            var record = Db.Query().CommandText("select * from sparks where id = @id").SetParameter("@id", id).ExecuteSingleRow();
            if (record == null)
                return null;

            var spark = CreateSparkFromRecord(record);
            return spark;
        }

        private Spark CreateSparkFromRecord(IDataRecord record)
        {
            var spark = new Spark();
            spark.id = record.GetValue<int>("id");
            spark.sparkName = record.GetValue<string>("sparkname");
            spark.unlockPrice = record.GetValue<int?>("unlockprice");
            spark.energyCredit = record.GetValue<int?>("energycredit");
            spark.standingLimit = record.GetValue<double?>("standinglimit");
            spark.allianceEid = record.GetValue<long?>("allianceeid");
            spark.definition = record.GetValue<int?>("definition");
            spark.quantity = record.GetValue<int?>("quantity");
            spark.changePrice = record.GetValue<int>("changeprice");
            spark.displayOrder = record.GetValue<int>("displayorder");
            spark.unlockable = record.GetValue<bool>("unlockable");
            spark.defaultSpark = record.GetValue<bool>("defaultspark");
            spark.icon = record.GetValue<string>("icon");
            spark.hidden = record.GetValue<bool>("hidden");
            spark.Extensions = _sparkExtensionsReader.GetAllBySparkID(spark.id).ToList();
            spark.RelatedExtensions = new List<Extension>();

            foreach (var sparkExtension in spark.Extensions)
            {
                var extensionInfo = _extensionReader.GetExtensionByID(sparkExtension.Extension.id);
                if (extensionInfo == null)
                    continue;

                spark.RelatedExtensions.Add(sparkExtension.Extension);
            }

            return spark;
        }

        public IEnumerable<Spark> GetAll()
        {
            return Db.Query().CommandText("select * from sparks").Execute().Select(CreateSparkFromRecord).ToArray();
        }
    }
}