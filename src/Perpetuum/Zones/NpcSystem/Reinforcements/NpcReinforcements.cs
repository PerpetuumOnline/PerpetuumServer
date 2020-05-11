using Perpetuum.Zones.Terrains.Materials;
using System;
using System.Linq;
using System.Text;

namespace Perpetuum.Zones.NpcSystem.Reinforcements
{
    public class NpcReinforcements : INpcReinforcements
    {
        // Sorted array of OreNpcData by threshold
        private readonly INpcReinforcementWave[] _presences;


        public NpcReinforcements(INpcReinforcementWave[] presences)
        {
            _presences = presences.OrderBy(s => Array.IndexOf(presences, s.Threshold)).ToArray();
        }

        public INpcReinforcementWave GetNextPresence(double percentageOfFieldConsumed)
        {
            for (var i = 0; i < _presences.Length; i++)
            {
                if (_presences[i].Threshold > percentageOfFieldConsumed)
                {
                    if (_presences[i].Spawned)
                    {
                        return null;
                    }
                    return _presences[i];
                }
            }
            return null;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine("OreNPCSpawn {");
            for (var i = 0; i < _presences.Length; i++)
            {
                sb.AppendLine(_presences[i].ToString());
            }
            sb.AppendLine("}");
            return sb.ToString();
        }
    }
}
