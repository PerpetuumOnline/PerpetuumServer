using System;
using System.Transactions;
using Perpetuum.Accounting.Characters;
using Perpetuum.Common.Loggers.Transaction;
using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;

using Perpetuum.Items;
using Perpetuum.Log;
using Perpetuum.Players;
using Perpetuum.Units;
using Perpetuum.Zones;
using Perpetuum.Zones.Beams;

namespace Perpetuum.Deployers
{
    public abstract class ItemDeployerBase : Item
    {
        public abstract void Deploy(IZone zone, Player player);

        protected static void LogTransaction(Player player,Item deployableItem)
        {
            var b = TransactionLogEvent.Builder()
                .SetTransactionType(TransactionType.ItemDeploy)
                .SetCharacter(player.Character)
                .SetItem(deployableItem);

            player.Character.LogTransaction(b);
        }

        protected EntityDefault DeployableItemEntityDefault
        {
            get { return ED.Config.TargetEntityDefault; }
        }
    }
    
    /// <summary>
    /// Deploys a unit into the zone by using an item to the player's current position
    /// </summary>
    public class ItemDeployer : ItemDeployerBase
    {
        private readonly IEntityServices _entityServices;
        private static readonly TimeSpan _deployBeamDuration = TimeSpan.FromSeconds(5);

        public ItemDeployer(IEntityServices entityServices)
        {
            _entityServices = entityServices;
        }

        protected virtual ErrorCodes CanDeploy(IZone zone,Unit unit,Position spawnPosition,Player player)
        {
            return ErrorCodes.NoError;
        }

        public override void Deploy(IZone zone, Player player)
        {
            var spawnPosition = player.CurrentPosition.Center;
            var deployableItem = CreateDeployableItem(zone, spawnPosition, player);

            var error = CanDeploy(zone, deployableItem, spawnPosition, player);
            if (error != ErrorCodes.NoError)
                throw new PerpetuumException(error);

            deployableItem.Save();

            LogTransaction(player,deployableItem);

            Transaction.Current.OnCommited(() =>
            {
                var deployBeamBuilder = Beam.NewBuilder()
                    .WithType(BeamType.dock_in)
                    .WithSource(player)
                    .WithTarget(deployableItem)
                    .WithState(BeamState.Hit)
                    .WithDuration(_deployBeamDuration);

                deployableItem.AddToZone(zone,spawnPosition,ZoneEnterType.Deploy, deployBeamBuilder);

                Logger.Info($"Item deployed on zone {zone.Id}. ({typeof (Unit).Name}) deployer = {player.InfoString}");
            });
        }

        protected virtual Unit CreateDeployableItem(IZone zone, Position spawnPosition, Player player)
        {
            var unit = (Unit)_entityServices.Factory.CreateWithRandomEID(DeployableItemEntityDefault);
            unit.Owner = player.Character.Eid;
            return unit;
        }
    }
}