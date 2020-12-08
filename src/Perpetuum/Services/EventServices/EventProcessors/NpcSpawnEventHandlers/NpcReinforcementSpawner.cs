using Perpetuum.Services.EventServices.EventMessages;
using Perpetuum.Units;
using Perpetuum.Zones;
using Perpetuum.Zones.Finders.PositionFinders;
using Perpetuum.Zones.NpcSystem;
using Perpetuum.Zones.NpcSystem.Flocks;
using Perpetuum.Zones.NpcSystem.Presences;
using Perpetuum.Zones.NpcSystem.Reinforcements;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Perpetuum.Services.EventServices.EventProcessors.NpcSpawnEventHandlers
{
    public class NpcReinforcementSpawner : NpcSpawnEventHandler<NpcReinforcementsMessage>
    {
        protected override TimeSpan SPAWN_DELAY { get { return TimeSpan.FromSeconds(10); } }
        protected override TimeSpan SPAWN_LIFETIME { get { return TimeSpan.FromMinutes(15); } }
        protected override int MAX_SPAWN_DIST { get { return 10; } }

        private readonly IDictionary<NpcBossInfo, INpcReinforcements> _reinforcementsByNpc = new Dictionary<NpcBossInfo, INpcReinforcements>();
        public NpcReinforcementSpawner(IZone zone, INpcReinforcementsRepository reinforcementsRepo) : base(zone, reinforcementsRepo) { }

        protected override IEnumerable<INpcReinforcements> GetActiveReinforcments(Presence presence)
        {
            return _reinforcementsByNpc.Where(p => p.Value.HasActivePresence(presence)).Select(p => p.Value);
        }

        protected override bool CheckMessage(EventMessage inMsg, out NpcReinforcementsMessage msg)
        {
            if (inMsg is NpcReinforcementsMessage message && _zone.Id == message.ZoneId)
            {
                msg = message;
                return true;
            }
            else
            {
                msg = null;
                return false;
            }
        }

        protected override void CheckReinforcements(NpcReinforcementsMessage msg)
        {
            var info = msg.Npc.BossInfo;
            if (!_reinforcementsByNpc.ContainsKey(info))
            {
                var reinforcements = _npcReinforcementsRepo.CreateNpcBossAddSpawn(info, msg.ZoneId);
                _reinforcementsByNpc.Add(info, reinforcements);
            }
        }

        protected override bool CheckState(NpcReinforcementsMessage msg)
        {
            if (msg.Npc.BossInfo.IsDead)
            {
                CleanupAllReinforcements(msg);
                return true;
            }
            UpdateAggro(msg);
            return false;
        }

        private void UpdateAggro(NpcReinforcementsMessage msg)
        {
            var info = msg.Npc.BossInfo;
            if (_reinforcementsByNpc.ContainsKey(info))
            {
                var activeWaves = _reinforcementsByNpc[info].GetAllActiveWaves().Where(w => w.ActivePresence != null);
                foreach (var wave in activeWaves)
                {
                    SpreadAggro(wave.ActivePresence, msg.Npc);
                }
            }
        }

        protected override void CleanupAllReinforcements(NpcReinforcementsMessage msg)
        {
            var info = msg.Npc.BossInfo;
            if (_reinforcementsByNpc.ContainsKey(info))
            {
                var activeWaves = _reinforcementsByNpc[info].GetAllActiveWaves();
                foreach (var wave in activeWaves)
                {
                    ExpireWave(wave);
                }
                _reinforcementsByNpc.Remove(info);
            }
        }

        protected override Position FindSpawnPosition(NpcReinforcementsMessage msg, int maxRange)
        {
            var finder = new RandomWalkableAroundPositionFinder(_zone, msg.Npc.CurrentPosition, maxRange);
            if (finder.Find(out Position result))
            {
                return result;
            }
            return msg.Npc.CurrentPosition;
        }

        protected override INpcReinforcementWave GetNextWave(NpcReinforcementsMessage msg)
        {
            var npc = msg.Npc;
            var percent = 1.0 - npc.ArmorPercentage;
            return _reinforcementsByNpc[npc.BossInfo].GetNextPresence(percent);
        }

        protected override void OnSpawning(Presence pres, NpcReinforcementsMessage msg)
        {
            SpreadAggro(pres, msg.Npc);
        }

        private void SpreadAggro(Presence presenceToAggro, Npc npcWithAggro)
        {
            foreach (var npc in presenceToAggro.Flocks.GetMembers())
            {
                foreach (var threat in npcWithAggro.ThreatManager.Hostiles)
                {
                    npc.AddDirectThreat(threat.unit, threat.Threat + FastRandom.NextDouble(5, 10));
                }
            }
        }
    }
}
