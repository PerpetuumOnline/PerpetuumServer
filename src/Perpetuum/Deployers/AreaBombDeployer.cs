using System.Linq;
using Perpetuum.EntityFramework;
using Perpetuum.Players;
using Perpetuum.Units;
using Perpetuum.Units.DockingBases;
using Perpetuum.Zones;
using Perpetuum.Zones.Eggs;
using Perpetuum.Zones.Teleporting;

namespace Perpetuum.Deployers
{
    /// <summary>
    /// Item which deploys the AreaBomb
    /// </summary>
    public class AreaBombDeployer : ItemDeployer
    {
        public AreaBombDeployer(IEntityServices entityServices) : base(entityServices)
        {
        }

        protected override ErrorCodes CanDeploy(IZone zone, Unit unit, Position spawnPosition, Player player)
        {
            if (zone.Configuration.Protected)
                return ErrorCodes.OnlyUnProtectedZonesAllowed;

            if (!zone.Configuration.Terraformable)
            {
                if (zone.Units.OfType<DockingBase>().WithinRange(spawnPosition, DistanceConstants.AREA_BOMB_DISTANCE_TO_STATIONS).Any())
                    return ErrorCodes.NotDeployableNearObject;

                if (zone.Units.OfType<TeleportColumn>().WithinRange(spawnPosition, DistanceConstants.AREA_BOMB_DISTANCE_TO_TELEPORTS).Any())
                    return ErrorCodes.NotDeployableNearObject;
            }
            else
            {
                if (zone.Units.OfType<AreaBomb>().WithinRange(spawnPosition, DistanceConstants.GAMMA_BOMB_STACK_DISTANCE).Any())
                    return ErrorCodes.TooCloseToOtherDevice;
            }

            return base.CanDeploy(zone, unit, spawnPosition, player);
        }
    }
}