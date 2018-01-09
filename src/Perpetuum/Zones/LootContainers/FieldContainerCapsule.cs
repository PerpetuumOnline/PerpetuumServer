using GenXY.Framework.Geometry;
using Perpetuum.Deployers;
using Perpetuum.EntityFramework;
using Perpetuum.Players;
using Perpetuum.Units;

namespace Perpetuum.Zones.LootContainers
{
    public sealed class FieldContainerCapsule : ItemDeployer
    {
        public FieldContainerCapsule(EntityDefault entityDefault) : base(entityDefault)
        {
        }

        public int PinCode { get; set; }

        protected override Unit CreateDeployableItem(IZone zone, Position spawnPosition, Player player)
        {
            return LootContainer.Create().SetType(LootContainerType.Field).SetOwner(player).SetPinCode(PinCode).Build(zone, spawnPosition);
        }
    }
}
