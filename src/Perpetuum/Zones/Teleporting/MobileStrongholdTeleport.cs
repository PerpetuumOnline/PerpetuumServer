using System.Collections.Generic;
using System.Linq;
using Perpetuum.Players;

namespace Perpetuum.Zones.Teleporting
{
    public class MobileStrongholdTeleport : MobileTeleport
    {
        private readonly StrongholdTeleportTargetHelper _strongholdTeleportTargetHelper;

        private TeleportDescription _usedTeleportDescription;

        public MobileStrongholdTeleport(StrongholdTeleportTargetHelper targetHelper, TeleportDescriptionBuilder.Factory descriptionBuilderFactory) : base(descriptionBuilderFactory)
        {
            _strongholdTeleportTargetHelper = targetHelper;
        }

        public override void AcceptVisitor(TeleportVisitor visitor)
        {
            visitor.VisitMobileStrongholdTeleport(this);
        }

        public override IEnumerable<TeleportDescription> GetTeleportDescriptions()
        {
            if (_usedTeleportDescription != null)
                return new[] { _usedTeleportDescription };

            return _strongholdTeleportTargetHelper.GetStrongholdTargets(Zone, Eid, TeleportRange);
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
            var testlist = _strongholdTeleportTargetHelper.GetStrongholdTargets(Zone, Eid, TeleportRange).ToList();

            (testlist.Count == 0).ThrowIfTrue(ErrorCodes.NoTeleportTargetsWereFound);

            base.CheckDeploymentAndThrow(zone, spawnPosition);
        }
    }
}
