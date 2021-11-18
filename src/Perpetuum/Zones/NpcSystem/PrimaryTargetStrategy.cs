using Perpetuum.Collections;
using Perpetuum.Zones.Locking.Locks;
using System.Collections.Generic;
using System.Linq;

namespace Perpetuum.Zones.NpcSystem
{
    public enum PrimaryLockStrategy
    {
        Random,
        Hostile,
        Closest,
        OptimalRange
    }

    public delegate bool TargetSelectionStrategy(Npc npc, UnitLock[] locks);

    public static class Strategies
    {
        public static Dictionary<PrimaryLockStrategy, TargetSelectionStrategy> All = new Dictionary<PrimaryLockStrategy, TargetSelectionStrategy>()
        {
            { PrimaryLockStrategy.Random, TargetRandom },
            { PrimaryLockStrategy.Hostile, TargetMostHated },
            { PrimaryLockStrategy.Closest, TargetClosest },
            { PrimaryLockStrategy.OptimalRange, TargetWithinOptimal }
        };

        public static TargetSelectionStrategy GetStrategy(PrimaryLockStrategy strategyType)
        {
            return All.GetOrDefault(strategyType);
        }

        public static bool TryInvokeStrategy(PrimaryLockStrategy strategyType, Npc npc, UnitLock[] locks)
        {
            var strat = GetStrategy(strategyType);
            if (strat == null)
                return false;
            return strat(npc, locks);
        }

        private static bool TargetMostHated(Npc npc, UnitLock[] locks)
        {
            var hostiles = npc.ThreatManager.Hostiles;
            var hostileLocks = locks.Where(u => hostiles.Any(h => h.unit.Eid == u.Target.Eid));
            var mostHostileLock = hostileLocks.OrderByDescending(u => hostiles.Where(h => h.unit.Eid == u.Target.Eid).FirstOrDefault()?.Threat ?? 0).FirstOrDefault();
            return TrySetPrimaryLock(npc, mostHostileLock);
        }

        private static bool TargetWithinOptimal(Npc npc, UnitLock[] locks)
        {
            return TrySetPrimaryLock(npc, locks.Where(k => k.Target.GetDistance(npc) < npc.BestCombatRange).RandomElement());
        }

        private static bool TargetClosest(Npc npc, UnitLock[] locks)
        {
            return TrySetPrimaryLock(npc, locks.OrderBy(u => u.Target.GetDistance(npc)).First());
        }

        private static bool TargetRandom(Npc npc, UnitLock[] locks)
        {
            return TrySetPrimaryLock(npc, locks.RandomElement());
        }

        private static bool TrySetPrimaryLock(Npc npc, Lock l)
        {
            if (l == null) return false;
            npc.SetPrimaryLock(l);
            return true;
        }
    }



    public class PrimaryLockSelectionStrategySelector
    {
        private readonly WeightedCollection<PrimaryLockStrategy> _selection;
        public PrimaryLockSelectionStrategySelector(WeightedCollection<PrimaryLockStrategy> selection)
        {
            _selection = selection;
        }
        public bool TryUseStrategy(Npc npc, UnitLock[] locks)
        {
            var stratType = _selection.GetRandom();
            return Strategies.TryInvokeStrategy(stratType, npc, locks);
        }
        public static PrimaryLockSelectionStrategyBuilder Create()
        {
            return new PrimaryLockSelectionStrategyBuilder();
        }

        public class PrimaryLockSelectionStrategyBuilder
        {
            private readonly WeightedCollection<PrimaryLockStrategy> _selection;
            public PrimaryLockSelectionStrategyBuilder()
            {
                _selection = new WeightedCollection<PrimaryLockStrategy>();
            }
            public PrimaryLockSelectionStrategyBuilder WithStrategy(PrimaryLockStrategy strategy, int weight = 1)
            {
                _selection.Add(strategy, weight);
                return this;
            }

            public PrimaryLockSelectionStrategySelector Build()
            {
                return new PrimaryLockSelectionStrategySelector(_selection);
            }
        }
    }
}
