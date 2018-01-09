using System;
using System.Linq;
using System.Transactions;
using Perpetuum.Data;
using Perpetuum.Deployers;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Players;
using Perpetuum.Zones.Beams;
using Perpetuum.Zones.PBS;

namespace Perpetuum.Zones.Gates
{
    public class GateDeployer : ItemDeployer
    {
        private readonly IEntityServices _entityServices;

        public GateDeployer(IEntityServices entityServices) : base(entityServices)
        {
            _entityServices = entityServices;
        }

        public override void Deploy(IZone zone, Player player)
        {
            zone.Configuration.IsAlpha.ThrowIfTrue(ErrorCodes.OnlyUnProtectedZonesAllowed);
            
            zone.IsUnitWithCategoryInRange(CategoryFlags.cf_gate, player.CurrentPosition, TypeExclusiveRange).ThrowIfTrue(ErrorCodes.GatesAround);
            
            CheckBlockingAndThrow(zone,player.CurrentPosition);

            PBSHelper.CheckWallPlantingAndThrow(zone, zone.Units.ToArray(), player.CurrentPosition, player.CorporationEid);

            var privateCorporation = player.Character.GetPrivateCorporationOrThrow();
            
            var gate = (Gate)_entityServices.Factory.CreateWithRandomEID(ED.Config.TargetEntityDefault);
            gate.Owner = privateCorporation.Eid;

            var position = player.CurrentPosition.Center;

            zone.UnitService.AddUserUnit(gate,position);

            Transaction.Current.OnCommited(() =>
            {
                var beamBuilder = Beam.NewBuilder()
                    .WithType(BeamType.dock_in)
                    .WithSource(player)
                    .WithTarget(gate)
                    .WithState(BeamState.Hit)
                    .WithDuration(TimeSpan.FromSeconds(5));

                gate.AddToZone(zone,position,ZoneEnterType.Deploy, beamBuilder);
            });
        }

        private int TypeExclusiveRange => ED.Config.typeExclusiveRange ?? 30;

        private int ItemWorkRange => (int) (ED.Config.item_work_range ?? 1);

        private void CheckBlockingAndThrow(IZone zone, Position position)
        {
            var area = Area.FromRadius(position, ItemWorkRange);
            var blockInfos = zone.Terrain.Blocks.GetArea(area);
            blockInfos.Any(b=>b.NonNaturally).ThrowIfTrue(ErrorCodes.BlockedTileWasFoundInConstructionRadius);
        }
    }
}