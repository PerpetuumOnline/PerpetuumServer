using System.Linq;
using Perpetuum.Accounting;
using Perpetuum.Common.Loggers.Transaction;
using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Services.ExtensionService;

namespace Perpetuum.RequestHandlers.Extensions
{
    public class ExtensionBuyForPoints : IRequestHandler
    {
        private readonly IAccountManager _accountManager;
        private readonly ExtensionPoints _extensionPoints;
        private readonly IExtensionReader _extensionReader;

        public ExtensionBuyForPoints(IAccountManager accountManager,ExtensionPoints extensionPoints,IExtensionReader extensionReader)
        {
            _accountManager = accountManager;
            _extensionPoints = extensionPoints;
            _extensionReader = extensionReader;
        }

        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var account = _accountManager.Repository.Get(request.Session.AccountId).ThrowIfNull(ErrorCodes.AccountNotFound);

                var character = request.Session.Character;
                var extensionId = request.Data.GetOrDefault<int>(k.extensionID);
                var isAdmin = request.Session.AccessLevel.IsAdminOrGm();

                character.IsDocked.ThrowIfFalse(ErrorCodes.CharacterHasToBeDocked);

                var extensionInfo = _extensionReader.GetExtensionByID(extensionId).ThrowIfNull(ErrorCodes.ExtensionNotFound);

                // req extension check
                if (!extensionInfo.RequiredExtensions.All(pr => character.CheckLearnedExtension(pr)))
                {
                    isAdmin.ThrowIfFalse(ErrorCodes.PrerequireExtensionError);
                }

                //the level he/she wants to learn
                var extensionLevel = character.GetExtensionLevel(extensionId) + 1;
                extensionLevel.ThrowIfGreater(10, ErrorCodes.ExtensionFullyLearnt);

                var extensionRank = extensionInfo.rank;
                var extensionPointCost = _extensionPoints.GetNominalExtensionPoints(extensionLevel, extensionRank);

                //extension price
                if (extensionLevel == 1)
                {
                    // ha lvl 1 akkor leszedjuk penzzel a komat
                    character.SubtractFromWallet(TransactionType.extensionLearn, extensionInfo.price);
                }

                //training character -> no EP pool usage 
                //                   -> free extension start
                if (!character.IsInTraining())
                {
                    //points check
                    var availablePoints = _accountManager.CalculateCurrentEp(account);
                    if (availablePoints < extensionPointCost)
                    {
                        isAdmin.ThrowIfFalse(ErrorCodes.NotEnoughExtensionPoints);
                    }

                    //spent extension
                    _accountManager.AddExtensionPointsSpent(account,character,extensionPointCost,extensionId,extensionLevel);
                }

                //write to sql
                character.IncreaseExtensionLevel(extensionId, extensionLevel);

                var result = _accountManager.GetEPData(account,character);
                result.Add(k.extensionID, extensionId);
                result.Add(k.extensionLevel, extensionLevel);

                Message.Builder.FromRequest(request).WithData(result).Send();
                
                scope.Complete();
            }
        }
    }
}
