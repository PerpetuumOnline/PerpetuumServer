using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Perpetuum.Timers;
using Perpetuum.Units;
using Perpetuum.Zones.NpcSystem.Presences;

namespace Perpetuum.Zones.NpcSystem.Flocks
{
    public class NormalFlock : Flock
    {
        private readonly ConcurrentQueue<TimeTracker> _spawnTimes = new ConcurrentQueue<TimeTracker>();

        private TimeSpan _elapsedTime = TimeSpan.Zero;

        public double respawnMultiplierLow;
        public double respawnMultiplier = 1.0;

        private TimeSpan RespawnTime => Configuration.RespawnTime.Multiply(respawnMultiplier);

        public NormalFlock(IFlockConfiguration configuration, Presence presence) : base(configuration, presence)
        {
            respawnMultiplierLow = configuration.RespawnMultiplierLow;
        }

        public override IDictionary<string, object> ToDictionary()
        {
            var dictionary = base.ToDictionary();

            dictionary.Add(k.respawnMultiplier, respawnMultiplier);
            dictionary.Add(k.respawnMultiplierLow, respawnMultiplierLow);

            return dictionary;
        }

        private bool _allowMultiplierChange;

        private void ModifyRespawnMultiplier()
        {
            if (Configuration.FlockMemberCount <= 1) 
                return;

            if (!_allowMultiplierChange) 
                return;

            if (MembersCount == 0)
            {
                //all dead
                respawnMultiplier = (respawnMultiplier * 0.9).Clamp(respawnMultiplierLow);
                _allowMultiplierChange = false;

                Log("respawn multiplier descreased: " + respawnMultiplier);
            }

            if (MembersCount != Configuration.FlockMemberCount) 
                return;

            //all alive
            respawnMultiplier = (respawnMultiplier * 1.1).Clamp(respawnMultiplierLow);
            _allowMultiplierChange = false;
            Log("respawn multiplier increased: " + respawnMultiplier);
        }

        protected override void OnMemberDead(Unit killer,Unit npc)
        {
            base.OnMemberDead(killer,npc);

            _allowMultiplierChange = true;
            _spawnTimes.Enqueue(new TimeTracker(GetRespawnTime()));
        }

        private TimeSpan GetRespawnTime()
        {
            if (IsBoss)
            {
                return BossInfo.GetNextSpawnTime(RespawnTime);
            }
            return RespawnTime;
        }

        public override void Update(TimeSpan time)
        {
            _elapsedTime += time;

            if (!Presence.Configuration.IsRespawnAllowed) 
                return;

            RespawnAllDeadNpcs(time);
        }

        private TimeTracker _nextSpawnTime;

        private void RespawnAllDeadNpcs(TimeSpan time)
        {
            if (IsMaxSpawnCountReached)
                return;

            ModifyRespawnMultiplier();

            //kinn van-e minden kello member?
            if (MembersCount >= Configuration.FlockMemberCount)
                return;

            if (_nextSpawnTime == null)
                _nextSpawnTime = GetNextSpawnTime();

            _nextSpawnTime.Update(time);

            if (!_nextSpawnTime.Expired) 
                return;

            CreateMemberInZone();
            _nextSpawnTime = null;
        }

        private TimeTracker GetNextSpawnTime()
        {
            return !_spawnTimes.TryDequeue(out TimeTracker tracker) ? new TimeTracker(TimeSpan.Zero) : tracker;
        }

        protected override void CreateMemberInZone()
        {
            //tehet meg ennek a flocknak be?
            if (IsMaxSpawnCountReached)
                return;

            BossInfo?.OnRespawn();
            base.CreateMemberInZone();
        }

        public override string ToString()
        {
            return $"{base.ToString()} respawnMult:{respawnMultiplier}";
        }

        private bool IsMaxSpawnCountReached
        {
            get
            {
                if (Configuration.TotalSpawnCount == 0)
                    return false; //spawn forever

                return MembersCount >= Configuration.TotalSpawnCount;
            }
        }
    }
}