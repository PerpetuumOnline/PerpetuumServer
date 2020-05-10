using Perpetuum.Zones.Terrains.Materials;
using System;
using System.Linq;
using System.Text;

namespace Perpetuum.Zones.NpcSystem.OreNPCSpawns
{
    public class OreNpcSpawn : IOreNpcSpawn
    {
        // Sorted array of OreNpcData by threshold
        private readonly IOreNpcData[] _presences;
        public MaterialType OreType { get; private set; }

        public OreNpcSpawn(MaterialType oreType, IOreNpcData[] presences)
        {
            OreType = oreType;
            _presences = presences.OrderBy(s => Array.IndexOf(presences, s.Threshold)).ToArray();
        }

        public IOreNpcData GetNextPresence(double percentageOfFieldConsumed)
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
