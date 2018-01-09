using System.Collections.Generic;
using System.Linq;
using Perpetuum.Data;

namespace Perpetuum.Zones.Terrains.Materials.Minerals
{
    public interface IMineralConfiguration
    {
        int ZoneId { get; }
        MaterialType Type { get; }
        MineralExtractionType ExtractionType { get; }
        int MaxNodes { get; }
        int MaxTilesPerNode { get; }
        int TotalAmountPerNode { get; }
        double MinThreshold { get; }
    }

    public interface IMineralConfigurationReader
    {
        IEnumerable<IMineralConfiguration> ReadAll();
    }

    public class MineralConfigurationReader : IMineralConfigurationReader
    {
        private Dictionary<MaterialType, MineralExtractionType> _extractionTypes;

        public IEnumerable<IMineralConfiguration> ReadAll()
        {
            if (_extractionTypes == null)
            {
                _extractionTypes = Db.Query().CommandText("select name,extractionType from minerals").Execute().ToDictionary(r =>
                {
                    return r.GetValue<string>("name").ToEnum<MaterialType>();
                },r =>
                {
                    return (MineralExtractionType)r.GetValue<int>("extractionType");
                });
            }

            var records = Db.Query().CommandText("select * from mineralconfigs").Execute();

            foreach (var record in records)
            {
                var materialType = (MaterialType) record.GetValue<int>("materialtype");
                var mc = new MineralConfiguration
                {
                    ZoneId = record.GetValue<int>("zoneId"),
                    Type = materialType,
                    MaxNodes = record.GetValue<int>("maxnodes"),
                    MaxTilesPerNode = record.GetValue<int>("maxtilespernode"),
                    TotalAmountPerNode = record.GetValue<int>("totalamountpernode"),
                    MinThreshold = record.GetValue<double>("minthreshold"),
                    ExtractionType = _extractionTypes[materialType],
                };

                yield return mc;
            }
        }
    }

    public class MineralConfiguration : IMineralConfiguration
    {
        public int ZoneId { get; set; }
        public MaterialType Type { get; set; }
        public MineralExtractionType ExtractionType { get; set; }

        public int MaxNodes { get; set; }
        public int MaxTilesPerNode { get; set; }
        public int TotalAmountPerNode { get; set; }
        public double MinThreshold { get; set; }

        public override string ToString()
        {
            return $"Type: {Type}, ExtractionType: {ExtractionType}";
        }
    }
}