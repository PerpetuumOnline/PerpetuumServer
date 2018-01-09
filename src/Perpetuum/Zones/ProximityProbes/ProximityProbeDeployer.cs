using System.Linq;
using Perpetuum.Deployers;
using Perpetuum.EntityFramework;
using Perpetuum.Groups.Corporations;
using Perpetuum.Players;
using Perpetuum.Units;

namespace Perpetuum.Zones.ProximityProbes
{
    public class VisibilityBasedProbeDeployer : ItemDeployer
    {
        private readonly IEntityServices _entityServices;

        public VisibilityBasedProbeDeployer(IEntityServices entityServices) : base(entityServices)
        {
            _entityServices = entityServices;
        }

        protected override Unit CreateDeployableItem(IZone zone, Position spawnPosition, Player player)
        {
            zone.Configuration.Protected.ThrowIfTrue(ErrorCodes.OnlyUnProtectedZonesAllowed);

            var corporation = player.Character.GetPrivateCorporationOrThrow();

            var maxProbes = corporation.GetMaximumProbeAmount();
            corporation.GetProximityProbeEids().Count().ThrowIfGreaterOrEqual(maxProbes, ErrorCodes.MaximumAmountOfProbesReached);

            var probe = (ProximityProbeBase)_entityServices.Factory.CreateWithRandomEID(DeployableItemEntityDefault);
            probe.Owner = corporation.Eid;
            var zoneStorage = zone.Configuration.GetStorage();
            probe.Parent = zoneStorage.Eid;
            probe.Save();

            zone.UnitService.AddUserUnit(probe,spawnPosition);

            var initialMembers = corporation.GetMembersWithAnyRoles(CorporationRole.CEO, CorporationRole.DeputyCEO).Select(cm => cm.character).ToList();
            initialMembers.Add(player.Character);
            
            probe.InitProbe( initialMembers.Distinct() );
            return probe;
        }
    }
}