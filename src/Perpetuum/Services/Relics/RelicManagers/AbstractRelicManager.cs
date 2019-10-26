using Perpetuum.Zones;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using Perpetuum.Log;
using System.Threading;
using Perpetuum.Threading;

namespace Perpetuum.Services.Relics
{
    public abstract class AbstractRelicManager : IRelicManager
    {
        //Constants
        private const double ACTIVATION_RANGE = 3; //30m
        private const double RESPAWN_PROXIMITY = 10.0 * ACTIVATION_RANGE;
        private readonly TimeSpan THREAD_TIMEOUT = TimeSpan.FromSeconds(4);


        protected abstract IZone Zone { get; }
        protected abstract ReaderWriterLockSlim Lock { get; }

        protected int _max_relics = 0;
        protected List<IRelic> _relics;

        private readonly TimeSpan _relicRefreshRate = TimeSpan.FromSeconds(19.95);

        //DB-accessing objects
        protected RelicLootGenerator relicLootGenerator;

        //Timers for update
        private TimeSpan _refreshElapsed;
        private TimeSpan _respawnElapsed;
        protected TimeSpan _respawnRandomized;

        //Interface implementation

        public virtual void Start()
        {
            //Inject max relics on first start
            while (GetRelicCount() < _max_relics)
            {
                SpawnRelic();
            }
        }

        public virtual void Stop()
        {
            //TODO cleanup if using DB to cache relics
        }

        public virtual void Update(TimeSpan time)
        {
            //Minimum tick rate
            _refreshElapsed += time;
            if (_refreshElapsed < _relicRefreshRate)
                return;

            //Update Relic lifespans, refresh beams and remove dead relics
            UpdateRelics();

            _respawnElapsed += _refreshElapsed;
            _refreshElapsed = TimeSpan.Zero;

            //check if time to spawn a new Relic
            if (_respawnElapsed > _respawnRandomized)
            {
                if (GetRelicCount() < _max_relics)
                {
                    SpawnRelic();
                    _respawnRandomized = RollNextSpawnTime();
                }
                _respawnElapsed = TimeSpan.Zero;
            }
        }

        public bool ForceSpawnRelicAt(int x, int y)
        {
            bool success = false;
            try
            {
                var info = GetNextRelicType();
                if (info == null)
                {
                    return false;
                }
                Position position = new Position(x, y);
                AddRelicToZone(info, position);
                success = true;
            }
            catch (Exception e)
            {
                Logger.Warning("Failed to spawn Relic by ForceSpawnRelicAt()");
                Logger.Warning(e.Message);
            }
            return success;
        }

        public List<Dictionary<string, object>> GetRelicListDictionary()
        {
            using (Lock.Read(THREAD_TIMEOUT))
                return DoGetRelicListDictionary();
        }

        protected virtual List<Dictionary<string, object>> DoGetRelicListDictionary()
        {
            return _relics.Select(r => r.ToDebugDictionary()).ToList();
        }
        


        //Abstract methods for extension of behaviour'
        protected abstract void RefreshBeam(IRelic relic);

        protected abstract Point FindRelicPosition(RelicInfo info);

        protected abstract RelicInfo GetNextRelicType();

        protected abstract TimeSpan RollNextSpawnTime();

        protected virtual IRelic MakeRelic(RelicInfo info, Position position)
        {
            return AbstractRelic.BuildAndAddToZone(info, Zone, position, relicLootGenerator.GenerateLoot(info));
        }

        //Common methods for all RelicManagers
        protected int GetRelicCount()
        {
            using (Lock.Read(THREAD_TIMEOUT))
                return _relics.Count;
        }

        private void SpawnRelic()
        {
            //Get Next Relictype based on the distribution of their probabilities on this zone
            var maxAttempts = 100;
            var attempts = 0;
            RelicInfo info = null;
            while (info == null)
            {
                info = GetNextRelicType();
                if (info.HasStaticPosistion && IsSpawnTooClose(info.GetPosition())) //The selected Relic type is static!  We must check if another relic is in this location
                {
                    info = null;
                }
                attempts++;
                if (attempts > maxAttempts)
                {
                    Logger.Error("Could not get RelicInfo for next Relic on Zone: " + Zone.Id);
                    return;
                }
            }

            attempts = 0;
            Point pt = FindRelicPosition(info);
            while (IsSpawnTooClose(pt) || !IsValidPos(pt))
            {
                pt = FindRelicPosition(info);
                attempts++;
                if (attempts > maxAttempts)
                {
                    Logger.Error("Could not get Position for next Relic on Zone: " + Zone.Id);
                    return;
                }
            }
            AddRelicToZone(info, pt.ToPosition());
        }

        private bool IsValidPos(Point pt)
        {
            return Zone.IsWalkable(pt);
        }

        private void AddRelicToZone(RelicInfo info, Position position)
        {
            using (Lock.Write(THREAD_TIMEOUT))
            {
                var r = MakeRelic(info, position);
                if (r != null)
                {
                    _relics.Add(r);
                }
            }
        }

        private bool IsSpawnTooClose(Point point)
        {
            using (Lock.Read(THREAD_TIMEOUT))
            {
                foreach (var r in _relics)
                {
                    if (RESPAWN_PROXIMITY > point.Distance(r.GetPosition()))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private void UpdateRelics()
        {
            using (Lock.Write(THREAD_TIMEOUT))
            {
                foreach (var r in _relics)
                {
                    if (!r.IsAlive())
                    {
                        r.RemoveFromZone();
                    }
                }
                _relics.RemoveAll(r => !r.IsAlive());
            }
            using (Lock.Read(THREAD_TIMEOUT))
            {
                foreach (var r in _relics)
                {
                    RefreshBeam(r);
                }
            }
        }


    }
}
