using System;
using Perpetuum.Deployers;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Players;
using Perpetuum.Units;

namespace Perpetuum.Zones.Teleporting
{
    public class MobileTeleportDeployer : ItemDeployer
    {
        private readonly TeleportWorldTargetHelper _worldTargetHelper;

        public MobileTeleportDeployer(IEntityServices entityServices,TeleportWorldTargetHelper worldTargetHelper) : base(entityServices)
        {
            _worldTargetHelper = worldTargetHelper;
        }

        public TeleportWorldTargetHelper WorldTargetHelper => _worldTargetHelper;

        protected override Unit CreateDeployableItem(IZone zone, Position spawnPosition, Player player)
        {
            var mobileTeleport = (MobileTeleport)base.CreateDeployableItem(zone, spawnPosition, player);

            mobileTeleport.CheckDeploymentAndThrow(zone,spawnPosition);

            mobileTeleport.SetDespawnTime(MobileTeleportDespawnTime);
            mobileTeleport.SetCooldownInterval(MobileTeleportCooldown);

            return mobileTeleport;
        }

        private TimeSpan MobileTeleportCooldown
        {
            get
            {
                var m = GetPropertyModifier(AggregateField.mobile_teleport_cooldown);
                return TimeSpan.FromMilliseconds(m.Value);
            }
        }

        private TimeSpan MobileTeleportDespawnTime
        {
            get
            {
                var m = GetPropertyModifier(AggregateField.despawn_time);
                return TimeSpan.FromMilliseconds((int)m.Value);
            }
        }

        public int WorkingRange
        {
            get
            {
                var targetDefinition = DeployableItemEntityDefault;
                var dc = targetDefinition.Config;

                if (dc.item_work_range == null)
                {
                    return (int) DistanceConstants.MOBILE_WORLD_TELEPORT_RANGE;
                }

                return (int) dc.item_work_range;
            }
        }
    }
}