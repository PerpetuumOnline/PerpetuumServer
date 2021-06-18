using Perpetuum.Data;
using System.Collections.Generic;

namespace Perpetuum.Zones.NpcSystem
{
    public static class NpcEp
    {
        private static readonly IDictionary<int, int> _npcEp = new Dictionary<int, int>();

        public static int GetEpForNpc(Npc npc)
        {
            var def = npc.Definition;
            if (def <= 0)
                return 0;

            return _npcEp.GetOrAdd(def, () =>Db.Query().CommandText("GetNpcKillEp").SetParameter("@definition", def).ExecuteScalar<int>());
        }
    }
}
