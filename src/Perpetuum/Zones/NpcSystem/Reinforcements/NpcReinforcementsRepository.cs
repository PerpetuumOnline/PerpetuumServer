using Perpetuum.Data;
using Perpetuum.Zones.Terrains.Materials;
using System.Data;
using System.Linq;

namespace Perpetuum.Zones.NpcSystem.Reinforcements
{
    public class NpcReinforcementsRepository : INpcReinforcementsRepository
    {
        public INpcReinforcements CreateOreNPCSpawn(MaterialType materialType)
        {
            var records = Db.Query().CommandText("SELECT threshold, presenceId from npcreinforcements WHERE targetId=@target AND reinforcementType=@type")
                .SetParameter("@target", materialType)
                .SetParameter("@type", ReinforcementType.Minerals)
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

        public INpcReinforcements CreateNpcBossAddSpawn(NpcBossInfo npcBossInfo)
        {
            throw new System.NotImplementedException(); //TODO new feature =)
        }
    }
}