using Perpetuum.Data;
using Perpetuum.Zones.Terrains.Materials;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;


namespace Perpetuum.Zones.NpcSystem.OreNPCSpawns
{
    public class OreNPCRepository : IOreNPCRepository
    {
        private readonly Lazy<IDictionary<MaterialType, IOreNPCSpawn>> _cache;

        public OreNPCRepository()
        {
            _cache = new Lazy<IDictionary<MaterialType, IOreNPCSpawn>>(InitCache);
        }

        private IDictionary<MaterialType, IOreNPCSpawn> InitCache()
        {
            var records = Db.Query().CommandText("SELECT materialType, presenceId, threshold FROM npcorespawn")
                .Execute()
                .Select(CreatePresenceThresholdRecords).ToArray();
            var dict = new Dictionary<MaterialType, IList<PresenceThreshold>>();
            foreach (var record in records)
            {
                if (!dict.ContainsKey(record.OreType))
                {
                    dict[record.OreType] = new List<PresenceThreshold>();
                }
                dict[record.OreType].Add(record);
            }
            var cacheDict = new Dictionary<MaterialType, IOreNPCSpawn>();
            foreach (KeyValuePair<MaterialType, IList<PresenceThreshold>> entry in dict)
            {
                cacheDict[entry.Key] = new OreNPCSpawn(entry.Key, entry.Value.ToArray());
            }
            return cacheDict;
        }

        public int GetPresenceForOreAndThreshold(MaterialType materialType, double threshold)
        {
            return _cache.Value[materialType].GetPresenceFor(threshold);
        }

        private static PresenceThreshold CreatePresenceThresholdRecords(IDataRecord record)
        {
            var materialID = record.GetValue<int>("materialType");
            var material = (MaterialType)materialID;
            var presence = record.GetValue<int>("presenceId");
            var threshold = record.GetValue<double>("threshold");
            var pair = new PresenceThreshold(material, presence, threshold);
            return pair;
        }

    }
}