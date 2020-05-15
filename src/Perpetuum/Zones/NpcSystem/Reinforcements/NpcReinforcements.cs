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

        /// <summary>
        /// Get the next unspawned presence in the list with the next greatest threshold
        /// </summary>
        /// <param name="percentageOfFieldConsumed">percentage expressed as [0.0-1.0]</param>
        /// <returns>INpcReinforcementWave or null</returns>
        public INpcReinforcementWave GetNextPresence(double percentageOfFieldConsumed)
        {
            for (var i = 0; i < _presences.Length; i++)
            {
                if (_presences[i].Threshold > percentageOfFieldConsumed)
                {
                    return _presences[i].Spawned ? null : _presences[i];
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
