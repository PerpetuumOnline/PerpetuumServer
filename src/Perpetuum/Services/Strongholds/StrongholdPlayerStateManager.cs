using Perpetuum.Data;
using Perpetuum.ExportedTypes;
using Perpetuum.Players;
using Perpetuum.Units;
using Perpetuum.Zones;
using System;

namespace Perpetuum.Services.Strongholds
{
    public interface IStrongholdPlayerStateManager
    {
        void OnPlayerEnterZone(Player player);
        void OnPlayerExitZone(Player player);
    }

    public class StrongholdPlayerStateManager : IStrongholdPlayerStateManager
    {
        private readonly TimeSpan MAX = TimeSpan.FromMinutes(60);
        private readonly TimeSpan MIN = TimeSpan.FromSeconds(30);
        private readonly IZone _zone;

        public StrongholdPlayerStateManager(IZone zone)
        {
            _zone = zone;
            MAX = TimeSpan.FromMinutes(_zone.Configuration.TimeLimitMinutes ?? 60);
        }

        public void OnPlayerEnterZone(Player player)
        {
            var now = DateTime.UtcNow;
            var effectEnd = player.DynamicProperties.GetOrAdd(k.strongholdDespawnTime, now.Add(MAX));
            var effectDuration = (effectEnd - now).Max(MIN);
            ApplyDespawn(player, effectDuration, effectEnd);
        }

        public void OnPlayerExitZone(Player player)
        {
            player.ClearStrongholdDespawn();
            player.DynamicProperties.Remove(k.strongholdDespawnTime);
        }

        private void ApplyDespawn(Player player, TimeSpan remaining, DateTime endTime)
        {
            using (var scope = Db.CreateTransaction())
            {
                player.DynamicProperties.Update(k.strongholdDespawnTime, endTime);
                player.Save();
                scope.Complete();
            }
            player.SetStrongholdDespawn(remaining, (u) =>
            {
                using (var scope = Db.CreateTransaction())
                {
                    if (u is Player p)
                    {
                        var dockingBase = p.Character.GetHomeBaseOrCurrentBase();
                        p.DockToBase(dockingBase.Zone, dockingBase);
                        p.DynamicProperties.Remove(k.strongholdDespawnTime);
                        p.Save();
                    }
                    scope.Complete();
                }
            });
        }
    }

    public class StrongholdPlayerDespawnHelper : UnitDespawnHelper
    {
        private static readonly EffectType DespawnEffect = EffectType.effect_stronghold_despawn_timer;

        private StrongholdPlayerDespawnHelper(TimeSpan despawnTime) : base(despawnTime) { }

        private bool _canceled = false;
        public void Cancel(Unit unit)
        {
            _canceled = true;
            RemoveDespawnEffect(unit);
        }

        public bool HasEffect(Unit unit)
        {
            return unit.EffectHandler.ContainsToken(_effectToken);
        }

        private bool _detectedEffectApplied = false;
        private bool EffectLive(Unit unit)
        {
            var effectRunning = HasEffect(unit);
            if (!_detectedEffectApplied)
            {
                _detectedEffectApplied = effectRunning;
            }
            return _detectedEffectApplied == effectRunning;
        }

        public override void Update(TimeSpan time, Unit unit)
        {
            _timer.Update(time).IsPassed(() =>
            {
                if (_canceled || EffectLive(unit))
                    return;

                DespawnStrategy?.Invoke(unit);
            });
        }

        private void RemoveDespawnEffect(Unit unit)
        {
            unit.EffectHandler.RemoveEffectByToken(_effectToken);
        }

        private void ApplyDespawnEffect(Unit unit)
        {
            var effectBuilder = unit.NewEffectBuilder().SetType(DespawnEffect).WithDuration(_despawnTime).WithToken(_effectToken);
            unit.ApplyEffect(effectBuilder);
        }

        public new static StrongholdPlayerDespawnHelper Create(Unit unit, TimeSpan despawnTime)
        {
            var helper = new StrongholdPlayerDespawnHelper(despawnTime);
            helper.ApplyDespawnEffect(unit);
            return helper;
        }
    }
}



