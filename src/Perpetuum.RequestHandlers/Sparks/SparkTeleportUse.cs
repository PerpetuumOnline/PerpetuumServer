using System.Linq;
using System.Transactions;
using Perpetuum.Accounting.Characters;
using Perpetuum.Common.Loggers.Transaction;
using Perpetuum.Data;
using Perpetuum.ExportedTypes;
using Perpetuum.Host.Requests;
using Perpetuum.Items;
using Perpetuum.Players;
using Perpetuum.Robots;
using Perpetuum.Services.ExtensionService;
using Perpetuum.Services.Sparks.Teleports;

namespace Perpetuum.RequestHandlers.Sparks
{
    public class SparkTeleportUse : IRequestHandler
    {
        private readonly IExtensionReader _extensionReader;
        private readonly SparkTeleportHelper _sparkTeleportHelper;

        public SparkTeleportUse(IExtensionReader extensionReader,SparkTeleportHelper sparkTeleportHelper)
        {
            _extensionReader = extensionReader;
            _sparkTeleportHelper = sparkTeleportHelper;
        }

        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var id = request.Data.GetOrDefault<int>(k.ID);
                var character = request.Session.Character;

                character.IsDocked.ThrowIfFalse(ErrorCodes.CharacterHasToBeDocked);
                character.CheckNextAvailableUndockTimeAndThrowIfFailed();
                CheckExtensionLevelAndThrowIfFailed(character);

                var sparkTeleport = _sparkTeleportHelper.Get(id);
                var currentDockingBase = character.GetCurrentDockingBase();

                character.SubtractFromWallet(TransactionType.SparkTeleportUse, SparkTeleport.SPARK_TELEPORT_USE_FEE);

                if ( sparkTeleport.DockingBase == currentDockingBase)
                    throw new PerpetuumException(ErrorCodes.YouAreHereAlready);

                sparkTeleport.DockingBase.IsDockingAllowed(character).ThrowIfError();
                sparkTeleport.DockingBase.DockIn(character,Player.NormalUndockDelay);

                var robot = sparkTeleport.DockingBase.GetPublicContainerWithItems(character)
                    .GetFullTree()
                    .OfType<Robot>()
                    .FirstOrDefault(r => !r.IsRepackaged && ItemExtensions.HaveAllEnablerExtensions(r, character));

                character.SetActiveRobot(robot);

                Transaction.Current.OnCommited(() =>
                {
                    currentDockingBase.LeaveChannel(character);

                    Message.Builder.FromRequest(request)
                        .WithData(sparkTeleport.ToDictionary())
                        .Send();
                });
                
                scope.Complete();
            }
        }
        
        private void CheckExtensionLevelAndThrowIfFailed(Character character)
        {
            var liveSparkTeleports = _sparkTeleportHelper.GetAllSparkTeleports(character);
            var alreadySpent = _sparkTeleportHelper.GetCostFromDescriptions(liveSparkTeleports);
            var maxCount = _sparkTeleportHelper.GetMaxSparkTeleportCount(character);
            if (alreadySpent <= maxCount)
                return; //

            var gex = PerpetuumException.Create(ErrorCodes.SparkTeleportExtensionLevelTooLow);

            var extension = _extensionReader.GetExtensionByName(ExtensionNames.SPARK_TELEPORT_COUNT_BASIC);
            if (extension != null)
                gex.SetData(k.extensionID, extension.id);

            throw gex;
        }
    }
}
