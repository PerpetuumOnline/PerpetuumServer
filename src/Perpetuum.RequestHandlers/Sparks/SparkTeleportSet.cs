using System.Linq;
using System.Transactions;
using Perpetuum.Common.Loggers.Transaction;
using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Services.Sparks.Teleports;
using Perpetuum.Zones;

namespace Perpetuum.RequestHandlers.Sparks
{
    public class SparkTeleportSet : IRequestHandler
    {
        private readonly SparkTeleportHelper _sparkTeleportHelper;

        public SparkTeleportSet(SparkTeleportHelper sparkTeleportHelper)
        {
            _sparkTeleportHelper = sparkTeleportHelper;
        }

        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var character = request.Session.Character;
                character.IsDocked.ThrowIfFalse(ErrorCodes.CharacterHasToBeDocked);

                var dockingBase = character.GetCurrentDockingBase();
                dockingBase.IsDockingAllowed(character).ThrowIfError();
                if (dockingBase.Zone is TrainingZone)
                    throw new PerpetuumException(ErrorCodes.TrainingCharacterInvolved);

                var sparkTeleports = _sparkTeleportHelper.GetAllSparkTeleports(character).ToArray();
                if (sparkTeleports.Any(d => d.DockingBase == dockingBase))
                    throw new PerpetuumException(ErrorCodes.BaseAlreadySparkTeleportDestination);

                var alreadySpent = _sparkTeleportHelper.GetCostFromDescriptions(sparkTeleports);
                var maxCount = _sparkTeleportHelper.GetMaxSparkTeleportCount(character);
                if (alreadySpent + dockingBase.Zone.Configuration.SparkCost > maxCount)
                    throw new PerpetuumException(ErrorCodes.NotEnoughSparkTeleportSlots);
            
                character.SubtractFromWallet(TransactionType.SparkTeleportPlace,SparkTeleport.SPARK_TELEPORT_PLACE_FEE);

                _sparkTeleportHelper.CreateSparkTeleport(dockingBase, character);

                Transaction.Current.OnCommited(() =>
                {
                    var info = _sparkTeleportHelper.GetSparkTeleportDescriptionInfos(character);
                    Message.Builder.FromRequest(request).WithData(info).Send();
                });
                
                scope.Complete();
            }
        }
    }
}