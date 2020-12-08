using Perpetuum.Data;
using Perpetuum.Zones.Terrains.Materials;
using System.Data;
using System.Linq;

namespace Perpetuum.Zones.NpcSystem.Reinforcements
{
    public class NpcReinforcementsRepository : INpcReinforcementsRepository
    {
        private const string queryStr = "SELECT threshold, presenceId from npcreinforcements WHERE targetId=@target AND reinforcementType=@type AND (zoneId IS NULL OR zoneId=@zone);";

        public INpcReinforcements CreateOreNPCSpawn(MaterialType materialType, int zoneId)
        {
            var records = Db.Query().CommandText(queryStr)
                .SetParameter("@target", materialType)
                .SetParameter("@type", ReinforcementType.Minerals)
                .SetParameter("@zone", zoneId)
                .Execute()
                .Select(CreateFromRecord).ToArray();
            return new NpcReinforcements(records);
        }

        private static INpcReinforcementWave CreateFromRecord(IDataRecord record)
        {
            var presence = record.GetValue<int>("presenceId");
            var threshold = record.GetValue<double>("threshold");
            var pair = new NpcReinforcementWave(presence, threshold);
            return pair;
        }

        public INpcReinforcements CreateNpcBossAddSpawn(NpcBossInfo npcBossInfo, int zoneId)
        {
            var records = Db.Query().CommandText(queryStr)
                .SetParameter("@target", npcBossInfo.FlockId)
                .SetParameter("@type", ReinforcementType.Boss)
                .SetParameter("@zone", zoneId)
                .Execute()
                .Select(CreateFromRecord).ToArray();
            return new NpcReinforcements(records);
        }
    }
}