using System.Collections.Generic;
using System.Linq;
using Perpetuum.Players;

namespace Perpetuum.Zones.Teleporting
{
    public class MobileWorldTeleport : MobileTeleport
    {
        private readonly TeleportWorldTargetHelper _worldTargetHelper;

        private TeleportDescription _usedTeleportDescription;

        public MobileWorldTeleport(TeleportWorldTargetHelper worldTargetHelper,TeleportDescriptionBuilder.Factory descriptionBuilderFactory) : base(descriptionBuilderFactory)
        {
            _worldTargetHelper = worldTargetHelper;
        }

        private int WorkingRange
        {
            get { return (int)(ED.Config.item_work_range ?? DistanceConstants.MOBILE_WORLD_TELEPORT_RANGE); }
        }

        public override void AcceptVisitor(TeleportVisitor visitor)
        {
            visitor.VisitMobileWorldTeleport(this);
        }

        public override IEnumerable<TeleportDescription> GetTeleportDescriptions()
        {
            if (_usedTeleportDescription != null)
                return new[] { _usedTeleportDescription };

            return _worldTargetHelper.GetWorldTargets(Zone, CurrentPosition, Eid, TeleportRange, WorkingRange);
        }

        public override Dictionary<string, object> ToDictionary()
        {
            var result = base.ToDictionary();
            result.Add(k.set, _usedTeleportDescription != null); //the device got fixed to a location
            return result;
        }

        public void Activate(Player player, TeleportDescription description)
        {
            if (_usedTeleportDescription != null)
                return;

            _usedTeleportDescription = description;

            var gang = player.Gang;
            if (gang == null)
                return;

            var result = ToDictionary();
            Message.Builder.SetCommand(Commands.TeleportTargetSet).WithData(result).ToCharacters(gang.GetMembers()).Send();
        }

        public override void CheckDeploymentAndThrow(IZone zone, Position spawnPosition)
        {
            var testlist = _worldTargetHelper.GetWorldTargets(zone, spawnPosition, Eid, TeleportRange, WorkingRange).ToList();

            (testlist.Count == 0).ThrowIfTrue(ErrorCodes.NoTeleportTargetsWereFound);

            base.CheckDeploymentAndThrow(zone, spawnPosition);
        }
    }
}
