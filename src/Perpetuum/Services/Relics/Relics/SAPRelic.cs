using Perpetuum.ExportedTypes;
using Perpetuum.Players;
using Perpetuum.Services.EventServices.EventMessages;
using Perpetuum.Zones;
using Perpetuum.Zones.Intrusion;

namespace Perpetuum.Services.Relics
{
    public class SAPRelic : AbstractRelic
    {
        private Outpost _outpost;

        [CanBeNull]
        public static IRelic BuildAndAddToZone(RelicInfo info, IZone zone, Position position, RelicLootItems lootItems, Outpost outpost)
        {
            var relic = (SAPRelic)CreateUnitWithRandomEID(DefinitionNames.RELIC_SAP);
            if (relic == null)
                return null;
            relic.Init(info, zone, position, lootItems);
            relic.SetOutpost(outpost);
            relic.AddToZone(zone, position);
            return relic;
        }

        private void SetOutpost(Outpost outpost)
        {
            _outpost = outpost;
        }

        public override void PopRelic(Player player)
        {
            player.ApplyPvPEffect();
            _outpost.PublishSAPEvent(new StabilityAffectingEvent(_outpost, player, this.Definition, this.Eid, 1));
            base.PopRelic(player);
        }
    }
}
