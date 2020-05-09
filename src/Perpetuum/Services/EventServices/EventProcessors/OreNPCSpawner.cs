using Perpetuum.ExportedTypes;
using Perpetuum.Services.EventServices.EventMessages;
using Perpetuum.Zones;
using Perpetuum.Zones.NpcSystem.OreNPCSpawns;
using Perpetuum.Zones.NpcSystem.Presences;
using Perpetuum.Zones.Terrains.Materials.Minerals;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Perpetuum.Services.EventServices.EventProcessors
{
    /// <summary>
    /// EventListener for each zone, receives messages for mineralnode mined
    /// </summary>
    public class OreNPCSpawner : EventProcessor<EventMessage>
    {
        private IZone _zone;
        private readonly IDictionary<MineralNode, DynamicPresence> orePresences = new Dictionary<MineralNode, DynamicPresence>();
        private readonly IOreNPCRepository _oreNPCRepository;
        private readonly IEnumerable<IMineralConfiguration> _mineralConfigs;

        public OreNPCSpawner(IZone zone, IOreNPCRepository oreNPCRepository, IMineralConfigurationReader mineralConfigurationReader)
        {
            _zone = zone;
            _oreNPCRepository = oreNPCRepository;
            _mineralConfigs = mineralConfigurationReader.ReadAll().Where(c => c.ZoneId == zone.Id);
        }

        private KeyValuePair<MineralNode, DynamicPresence> FindEntryByValue(Presence presence)
        {
            foreach (var pair in orePresences)
            {
                if (pair.Value == presence)
                {
                    return pair;
                }
            }
            return new KeyValuePair<MineralNode, DynamicPresence>(null, null);
        }

        private bool IsEntryNull(KeyValuePair<MineralNode, DynamicPresence> pair)
        {
            return pair.Key == null;
        }

        private void OnPresenceExpired(Presence presence)
        {
            var pair = FindEntryByValue(presence);
            if (IsEntryNull(pair))
            {
                return;
            }
            pair.Value.PresenceExpired -= OnPresenceExpired;
            orePresences.Remove(pair.Key);
        }

        private void RemoveEntry(MineralNode node)
        {
            if (orePresences.ContainsKey(node))
            {
                if (orePresences[node] != null)
                {
                    orePresences[node].PresenceExpired -= OnPresenceExpired;
                }
                orePresences.Remove(node);
            }
        }

        public override void OnNext(EventMessage value)
        {
            if (value is OreNpcSpawnMessage msg && _zone.Id == msg.GetZoneID())
            {
                var node = msg.GetMineralNode();
                if (orePresences.ContainsKey(node))
                {
                    if (msg.GetOreNodeState() == OreNodeState.Removed)
                    {
                        //Node has been removed from zone - remove from our cache
                        RemoveEntry(node);
                    }
                    //There is an active presence
                    return;
                }
                var current = Convert.ToInt32(node.GetTotalAmount());
                var total = _mineralConfigs.Where(c => c.Type == node.Type).First().TotalAmountPerNode;
                var percent = (current / (double)total).Clamp(0.0, 1.0);
                var radius = (int)(node.Area.Diagonal / 2.0);
                var spawnPos = _zone.FindPassablePointInRadius(node.Area.Center.ToPosition(), radius);
                var presenceID = _oreNPCRepository.GetPresenceForOreAndThreshold(node.Type, percent);
                var pres = _zone.AddDynamicPresenceToPosition(presenceID, spawnPos, TimeSpan.FromSeconds(30));
                pres.PresenceExpired += OnPresenceExpired;
                orePresences.Add(node, pres);
                _zone.CreateBeam(BeamType.teleport_storm, b => b.WithPosition(spawnPos).WithDuration(TimeSpan.FromSeconds(100)));
            }

        }
    }
}
