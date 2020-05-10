using Perpetuum.Data;
using Perpetuum.Zones.Terrains.Materials;
using System.Data;
using System.Linq;

namespace Perpetuum.Zones.NpcSystem.OreNPCSpawns
{
    public class OreNpcRepository : IOreNpcRepository
    {
        public IOreNpcSpawn CreateOreNPCSpawn(MaterialType materialType)
        {
            var records = Db.Query().CommandText("SELECT materialType, presenceId, threshold FROM npcorespawn WHERE @material=materialType")
                .SetParameter("@material", materialType)
                .Execute()
                .Select(CreatePresenceThresholdRecords).ToArray();
            return new OreNpcSpawn(materialType, records);
        }

        private static IOreNpcData CreatePresenceThresholdRecords(IDataRecord record)
        {
            var materialID = record.GetValue<int>("materialType");
            var material = (MaterialType)materialID;
            var presence = record.GetValue<int>("presenceId");
            var threshold = record.GetValue<double>("threshold");
            var pair = new OreNpcData(material, presence, threshold);
            return pair;
        }

    }
}