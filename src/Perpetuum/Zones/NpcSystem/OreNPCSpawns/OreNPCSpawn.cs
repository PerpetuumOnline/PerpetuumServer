using Perpetuum.Zones.Terrains.Materials;
using System;
using System.Linq;
using System.Text;

namespace Perpetuum.Zones.NpcSystem.OreNPCSpawns
{
    public class PresenceThreshold
    {
        public MaterialType OreType { get; private set; }
        public int Presence { get; private set; }
        public double Threshold { get; private set; }
        public PresenceThreshold(MaterialType oreType, int presenceID, double threshold)
        {
            OreType = oreType;
            Presence = presenceID;
            Threshold = threshold;
        }

        public override string ToString()
        {
            return $"{Threshold}:{Presence}";
        }
    }

    public class OreNPCSpawn : IOreNPCSpawn
    {
        //Sorted array of presence threhsold pairs
        private PresenceThreshold[] _presences;
        public MaterialType OreType { get; private set; }

        public OreNPCSpawn(MaterialType oreType, PresenceThreshold[] presences)
        {
            OreType = oreType;
            _presences = presences.OrderBy(s => Array.IndexOf(presences, s.Threshold)).ToArray();
        }

        public int GetPresenceFor(double percentageOfFieldConsumed)
        {
            for (var i = 0; i < _presences.Length; i++)
            {
                if(_presences[i].Threshold > percentageOfFieldConsumed)
                {
                    return _presences[i].Presence;
                }
            }
            return -1; // Can not find presence
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine("OreNPCSpawn {");
            for(var i =0; i<_presences.Length; i++)
            {
                sb.AppendLine(_presences[i].ToString());
            }
            sb.AppendLine("}");
            return sb.ToString();
        }

    }
}
