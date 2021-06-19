using Perpetuum.Data;
using System.Collections.Concurrent;

namespace Perpetuum.Zones.NpcSystem
{
    public static class NpcEp
    {
        private static readonly ConcurrentDictionary<int, int> _npcEp = new ConcurrentDictionary<int, int>();

        public static int GetEpForNpc(Npc npc)
        {
            var definition = npc.Definition;
            if (definition <= 0)
                return 0;

            return _npcEp.GetOrAdd(definition, (d) => Db.Query().CommandText("GetNpcKillEp").SetParameter("@definition", d).ExecuteScalar<int>());
        }
    }
}
