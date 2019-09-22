using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Perpetuum.EntityFramework;
using Perpetuum.Log;
using Perpetuum.Services.Looting;
using Perpetuum.Units;
using Perpetuum.Zones.Finders.PositionFinders;
using Perpetuum.Zones.NpcSystem.Presences;

namespace Perpetuum.Zones.NpcSystem.Flocks
{
    public interface INpcGroup
    {
        string Name { get; }
        IEnumerable<Npc> Members { get; }
        void AddDebugInfoToDictionary(IDictionary<string, object> dictionary);
    }

    public delegate Flock FlockFactory(IFlockConfiguration flockConfiguration,Presence presence);

    public class Flock
    {
        public ILootService LootService { get; set; }
        public IEntityServices EntityService { get; set; }

        private ImmutableList<Npc> _members = ImmutableList<Npc>.Empty;

        public IFlockConfiguration Configuration { get; }
        public Presence Presence { get; }

        public NpcBossInfo BossInfo { get { return Configuration.BossInfo; } }
        public bool IsBoss { get { return BossInfo != null; } }

        public int Id => Configuration.ID;

        public int HomeRange => Configuration.HomeRange;

        public Flock(IFlockConfiguration configuration, Presence presence)
        {
            Configuration = configuration;
            Presence = presence;

            SpawnOrigin = SpawnOriginSelector(presence);
        }

        public Position SpawnOrigin { get; }

        public IReadOnlyCollection<Npc> Members => _members;

        private void AddMember(Npc npc)
        {
            ImmutableInterlocked.Update(ref _members, m => m.Add(npc));
            npc.Dead += OnMemberDead;
        }

        private void RemoveMember(Npc npc)
        {
            ImmutableInterlocked.Update(ref _members, m => m.Remove(npc));
        }


        protected virtual void OnMemberDead(Unit killer,Unit npc)
        {
            RemoveMember((Npc) npc);

            if (_members.Count <= 0)
            {
                OnAllMembersDead();
            }
        }

        public event Action<Flock> AllMembersDead;

        private void OnAllMembersDead()
        {
            AllMembersDead?.Invoke(this);
        }

        public virtual IDictionary<string, object> ToDictionary()
        {
            var dictionary = new Dictionary<string, object> {
                                                                    {k.ID, Configuration.ID},
                                                                    {k.presenceID, Presence.Configuration.ID},
                                                                    {k.definition, Configuration.EntityDefault.Definition},
                                                                    {k.name, Configuration.Name},
                                                                    {k.spawnRangeMin, Configuration.SpawnRange.Min},
                                                                    {k.spawnRangeMax, Configuration.SpawnRange.Max},
                                                                    {k.flockMemberCount, Configuration.FlockMemberCount},
                                                                    {k.respawnSeconds, Configuration.RespawnTime.Seconds},
                                                                    {k.homeRange, HomeRange},
                                                                    {k.totalSpawnCount, Configuration.TotalSpawnCount},
                                                                    {k.spawnOriginX,SpawnOrigin.intX},
                                                                    {k.spawnOriginY,SpawnOrigin.intY}
                                                            };
            return dictionary;
        }

        public int MembersCount => _members.Count;

        public virtual void Update(TimeSpan time)
        {
        }

        public void SpawnAllMembers()
        {
            var totalToSpawn = Configuration.FlockMemberCount - MembersCount;
            for (var i = 0; i < totalToSpawn; i++)
            {
                CreateMemberInZone();
            }

            Log($"{Configuration.FlockMemberCount} NPCs created");
        }

        protected virtual void CreateMemberInZone()
        {
            var npc = (Npc)EntityService.Factory.Create(Configuration.EntityDefault, EntityIDGenerator.Random);
            npc.Behavior = GetBehavior();
            npc.SpecialType = Configuration.SpecialType;
            npc.BossInfo = BossInfo;

            var gen = new CompositeLootGenerator(
                new LootGenerator(LootService.GetNpcLootInfos(npc.Definition)),
                new LootGenerator(LootService.GetFlockLootInfos(Id))
            );

            npc.LootGenerator = gen;
            npc.HomeRange = HomeRange;
            npc.HomePosition = SpawnOrigin;
            npc.CallForHelp = Configuration.IsCallForHelp;

            var zone = Presence.Zone;
            var spawnPosition = GetSpawnPosition(SpawnOrigin);
            var finder = new ClosestWalkablePositionFinder(zone, spawnPosition, npc);
            if (!finder.Find(out spawnPosition))
            {
                Log($"invalid spawnposition in CreateMemberInZone: {spawnPosition} {Configuration.Name} {Presence.Configuration.name} zone:{zone.Id}");
            }

            OnNpcCreated(npc);

            npc.AddToZone(zone, spawnPosition, ZoneEnterType.NpcSpawn);

            AddMember(npc);
            Log($"member spawned to zone:{zone.Id} EID:{npc.Eid}");
        }

        private Position GetSpawnPosition(Position spawnOrigin)
        {
            var spawnRangeMin = Configuration.SpawnRange.Min;
            var spawnRangeMax = Configuration.SpawnRange.Max.Min(HomeRange);

            var spawnPosition = spawnOrigin.GetRandomPositionInRange2D(spawnRangeMin, spawnRangeMax).Clamp(Presence.Zone.Size);
            return spawnPosition;
        }

        public event Action<Npc> NpcCreated;

        protected virtual void OnNpcCreated(Npc npc)
        {
            NpcCreated?.Invoke(npc);
        }

        private NpcBehavior GetBehavior()
        {
            if (Configuration.BehaviorType == NpcBehaviorType.Aggressive && Presence is DynamicPresence)
            {
                // hogy ne tamadjanak be mindenkit rogton
                return NpcBehavior.Create(NpcBehaviorType.Neutral);
            }

            return NpcBehavior.Create(Configuration.BehaviorType);
        }

        protected void Log(string message)
        {
            Logger.Info($"[Flock] ({ToString()}) - {message}");
        }

        public override string ToString()
        {
            return $"{Configuration.Name}:{Configuration.ID}";
        }

        public void RemoveAllMembersFromZone(bool withTeleportExit = false)
        {
            foreach (var npc in Members)
            {
                if (withTeleportExit)
                    npc.States.Teleport = true;

                npc.RemoveFromZone();
                RemoveMember(npc);
            }
        }

        private Position SpawnOriginSelector(Presence presence)
        {
            switch (presence)
            {
                case DynamicPresence dp:
                {
                    return dp.DynamicPosition;
                }
                case RandomPresence rp:
                {
                    return rp.SpawnOriginForRandomPresence;
                }
                case RoamingPresence roaming:
                {
                    return roaming.SpawnOrigin;
                }
            }

            return Configuration.SpawnOrigin;
        }

    }
}