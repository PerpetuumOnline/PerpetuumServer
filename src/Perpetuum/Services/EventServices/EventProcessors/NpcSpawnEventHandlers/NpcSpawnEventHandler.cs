using Perpetuum.ExportedTypes;
using Perpetuum.Log;
using Perpetuum.Services.EventServices.EventMessages;
using Perpetuum.Zones;
using Perpetuum.Zones.NpcSystem.Presences;
using Perpetuum.Zones.NpcSystem.Reinforcements;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Perpetuum.Services.EventServices.EventProcessors.NpcSpawnEventHandlers
{
    /// <summary>
    /// Common functions and event handling procedure for NpcReinforcement related events
    /// </summary>
    /// <typeparam name="T">EventMessage</typeparam>
    public abstract class NpcSpawnEventHandler<T> : EventProcessor where T : IEventMessage
    {
        protected abstract TimeSpan SPAWN_DELAY { get; }
        protected abstract TimeSpan SPAWN_LIFETIME { get; }
        protected abstract int MAX_SPAWN_DIST { get; }
        protected readonly IZone _zone;

        protected readonly INpcReinforcementsRepository _npcReinforcementsRepo;
        public NpcSpawnEventHandler(IZone zone, INpcReinforcementsRepository reinforcementsRepo)
        {
            _zone = zone;
            _npcReinforcementsRepo = reinforcementsRepo;
        }

        protected abstract IEnumerable<INpcReinforcements> GetActiveReinforcments(Presence presence);

        protected abstract bool CheckMessage(IEventMessage inMsg, out T msg);

        protected abstract void CheckReinforcements(T msg);

        protected abstract bool CheckState(T msg);

        protected abstract void CleanupAllReinforcements(T msg);

        protected abstract Position FindSpawnPosition(T msg, int maxRange);

        protected abstract INpcReinforcementWave GetNextWave(T msg);

        protected virtual void OnSpawning(Presence pres, T msg) { }

        protected virtual Position GetHomePos(T msg, Position spawnPos)
        {
            return spawnPos;
        }

        protected void DoBeams(Position beamLocation)
        {
            _zone.CreateBeam(BeamType.npc_egg_beam, b => b.WithPosition(beamLocation).WithDuration(SPAWN_DELAY));
            _zone.CreateBeam(BeamType.teleport_storm, b => b.WithPosition(beamLocation).WithDuration(SPAWN_DELAY));
        }

        protected void DoSpawning(INpcReinforcementWave wave, Position homePosition, Position spawnPosition, T msg)
        {
            var pres = _zone.AddDynamicPresenceToPosition(wave.PresenceId, homePosition, spawnPosition, SPAWN_LIFETIME);
            OnSpawning(pres, msg);
            pres.PresenceExpired += OnPresenceExpired;
            wave.SetActivePresence(pres);
        }

        protected void OnPresenceExpired(Presence presence)
        {
            var activeReinforcements = GetActiveReinforcments(presence);
            foreach (var reinforcements in activeReinforcements)
            {
                var wave = reinforcements.GetActiveWaveOfPresence(presence);
                ExpireWave(wave);
            }
        }

        protected void ExpireWave(INpcReinforcementWave wave)
        {
            wave.ActivePresence.PresenceExpired -= OnPresenceExpired;
            wave.DeactivatePresence();
        }

        private bool _spawning = false;

        public override void HandleMessage(IEventMessage value)
        {
            if (CheckMessage(value, out T msg))
            {
                if (CheckState(msg))
                    return;

                if (_spawning)
                    return;

                CheckReinforcements(msg);

                var wave = GetNextWave(msg);
                if (wave == null)
                    return; // Presence not found for this message state or already spawned

                var spawnPos = FindSpawnPosition(msg, MAX_SPAWN_DIST);
                if (spawnPos == Position.Empty)
                    return; // Failed to find valid spawn location, try again on next cycle

                var homePos = GetHomePos(msg, spawnPos);

                DoBeams(homePos);

                _spawning = true;
                Task.Delay(SPAWN_DELAY).ContinueWith(t =>
                {
                    try
                    {
                        DoSpawning(wave, homePos, spawnPos, msg);
                    }
                    catch (Exception ex)
                    {
                        Logger.Exception(ex);
                    }
                    finally
                    {
                        _spawning = false;
                    }
                });
            }
        }
    }
}
