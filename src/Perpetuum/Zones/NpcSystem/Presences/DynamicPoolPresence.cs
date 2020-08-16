using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Perpetuum.Accounting.Characters;
using Perpetuum.ExportedTypes;
using Perpetuum.Log;
using Perpetuum.Players;
using Perpetuum.Services.RiftSystem;
using Perpetuum.StateMachines;
using Perpetuum.Timers;
using Perpetuum.Units;
using Perpetuum.Zones.NpcSystem.Flocks;

namespace Perpetuum.Zones.NpcSystem.Presences
{
    public sealed class DynamicPoolPresence : DynamicPresence
    {
        private readonly IRandomFlockReader _randomFlockReader;
        private readonly StackFSM _fsm = new StackFSM();
        public Character Summoner { private get; set; }

        public DynamicPoolPresence(IZone zone, IPresenceConfiguration configuration,IRandomFlockReader randomFlockReader) : base(zone, configuration)
        {
            _randomFlockReader = randomFlockReader;
        }

        public void Init(int totalWaves)
        {
            var infos = _randomFlockReader.GetByPresence(this);

            _fsm.Push(CreatePortalState());

            var boss = infos.Where(i => i.lastWave).RandomElement();
            if (boss != null)
            {
                _fsm.Push(CreateWaveState(new[] { boss }, true));
            }

            var normalFlocks = infos.Where(i => !i.lastWave).ToArray();
            if (normalFlocks.Length <= 0)
                return;

            for (var i = 0; i < totalWaves; i++)
            {
                _fsm.Push(CreateWaveState(normalFlocks.RandomElement(Configuration.MaxRandomFlock), false));
            }
        }

        private IState CreatePortalState()
        {
            var first = true;
            return AnonymousState.Create(t =>
            {
                if (!first)
                    return;

                first = false;

                var portal = (DespawningPortal)Unit.CreateUnitWithRandomEID(DefinitionNames.RANDOM_RIFT_PORTAL);
                portal.SetDespawnTime(TimeSpan.FromMinutes(5));
                portal.AddToZone(Zone, DynamicPosition);
            });
        }

        private IState CreateWaveState(IEnumerable<RandomFlockInfo> flockInfos,bool isLastWave)
        {
            var first = true;
            return AnonymousState.Create(t =>
            {
                if (!first)
                    return;

                first = false;

                Task.Run(() =>
                {
                    foreach (var flockInfo in flockInfos)
                    {
                        var f = SpawnFlock(flockInfo.flockID);
                        f.AllMembersDead += flock =>
                        {
                            var membersCount = Flocks.MembersCount();
                            if (membersCount > 0)
                                return;

                            _fsm.Pop();

                            if (!isLastWave)
                                _fsm.Push(CreateDelayState(TimeSpan.FromSeconds(5)));
                        };

                        Logger.DebugInfo($"Flock spawned: {f}");
                    }
                });
            });
        }

        private IState CreateDelayState(TimeSpan delay)
        {
            var timer = new TimeTracker(delay);

            return AnonymousState.Create(t =>
            {
                timer.Update(t);

                if ( !timer.Expired )
                    return;

                _fsm.Pop();
            });
        }

        private Flock SpawnFlock(int flockID)
        {
            var flock = CreateAndAddFlock(flockID);
            flock.SpawnAllMembers();

            var summoner = GetSummonerPlayer();
            if (summoner != null)
            {
                foreach (var npc in Flocks.GetMembers())
                {
                    npc.AddDirectThreat(summoner, 40 + FastRandom.NextDouble(0.0, 3.0));
                    npc.Tag(summoner, LifeTime);
                }
            }

            Zone.CreateBeam(BeamType.teleport_storm, builder => builder.WithPosition(DynamicPosition.GetRandomPositionInRange2D(0, 3)).WithDuration(100000));
            return flock;
        }


        protected override void OnUpdate(TimeSpan time)
        {
            base.OnUpdate(time);
            _fsm.Update(time);
        }

        [CanBeNull]
        private Player GetSummonerPlayer()
        {
            return Zone.GetPlayer(Summoner);
        }
    }
}