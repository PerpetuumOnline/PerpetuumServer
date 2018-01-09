using System.Linq;
using Perpetuum.Accounting;
using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Log;
using Perpetuum.Services.ExtensionService;

namespace Perpetuum.RequestHandlers.Extensions
{
    public class ExtensionRemoveLevel : IRequestHandler
    {
        private readonly IAccountManager _accountManager;
        private readonly IAccountRepository _accountRepository;
        private readonly IExtensionReader _extensionReader;

        public ExtensionRemoveLevel(IAccountManager accountManager,IAccountRepository accountRepository,IExtensionReader extensionReader)
        {
            _accountManager = accountManager;
            _accountRepository = accountRepository;
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

           
                //only docked
                character.IsDocked.ThrowIfFalse(ErrorCodes.CharacterHasToBeDocked);
            
                var extensionInfo = _extensionReader.GetExtensionByID(extensionId).ThrowIfNull(ErrorCodes.ExtensionNotFound);

                //validate extension
            
                var currentExtensionLevel = character.GetExtensionLevel(extensionId);

                //this extension is not learnt for this character yet
                currentExtensionLevel.ThrowIfLessOrEqual(0, ErrorCodes.ItemNotFound);

                var defaultExtensionHandler = new CharacterDefaultExtensionHelper(character);

                if (defaultExtensionHandler.IsStartingExtension(extensionInfo,out int minimumLevel))
                {
                    (currentExtensionLevel == minimumLevel).ThrowIfTrue(ErrorCodes.ExtensionMinimumReached);
                }


                if (currentExtensionLevel == 1)
                {
                    //extension is getting deleted
                }


                // these extensions need the current one on a specific level as a requirement 
                var prerequiredOf = _extensionReader.GetPrerequiredExtensionsOf(extensionId);

                var learntExtensions = character.GetExtensions().ToList();

                foreach (var requiresExtension in prerequiredOf)
                {
                    foreach (var learntExtension in learntExtensions)
                    {
                        if (learntExtension.id != requiresExtension.id)
                            continue;

                        if (requiresExtension.level < currentExtensionLevel)
                            continue;

                        //extension cant be downgraded because
                        Logger.DebugWarning($"extension:{_extensionReader.GetExtensionName(requiresExtension.id)} requires:{_extensionReader.GetExtensionName(extensionId)} on level:{requiresExtension.level} current level:{currentExtensionLevel}  requests:{requiresExtension.id}->this:{extensionId}");
                        throw PerpetuumException.Create(ErrorCodes.ExtensionIsRequired)
                            .SetData("thisNeeds", requiresExtension.id)
                            .SetData("level",requiresExtension.level)
                            .SetData("downgrading",extensionId);
                    }
                }

                var spentId = 0;
                var spentPoints = 0;
                character.GetTableIndexForAccountExtensionSpent(extensionId, currentExtensionLevel, ref spentId, ref spentPoints);

                if (spentId == 0)
                {
                    isAdmin.ThrowIfFalse(ErrorCodes.ExtensionLevelCantBeRemoved);
                }

                //add negative spent points
                var spentPoints1 = -1 * spentPoints;
                var extensionLevel = currentExtensionLevel - 1;
                _accountManager.AddExtensionPointsSpent(account,character,spentPoints1,extensionId,extensionLevel);

                //insert log
                _accountManager.InsertExtensionRemoveLog(account,character,extensionId,currentExtensionLevel,spentPoints);
                character.SetExtension(new Extension(extensionId, currentExtensionLevel - 1));

                var price = spentPoints/60;

                //var priceBase = AccountWallet.GetProductPrice(AccountTransactionType.extensionRemoveLevel.ToString().ToLower());
                //var price = currentExtensionLevel * priceBase; //current level * base price
            
                var wallet = _accountManager.GetWallet(account,AccountTransactionType.ExtensionRemoveLevel);
                wallet.Balance -= price;
                var e = new AccountTransactionLogEvent(account,AccountTransactionType.ExtensionRemoveLevel) { Credit = wallet.Balance, CreditChange = -price };
                _accountManager.LogTransaction(e);

                _accountRepository.Update(account);

                var result = _accountManager.GetEPData(account,character);

                //The amount of EP gained
                result.Add("pointsReturned", spentPoints);
                result.Add(k.extensionID, extensionId);
                result.Add(k.extensionLevel, currentExtensionLevel - 1);

                Message.Builder.FromRequest(request).WithData(result).Send();
                
                scope.Complete();
            }
        }




        #region old handler trialos

        /*
        public void HandleRequest_old(IRequest request)
        {
            var account = request.account;
            var character = request.character;
            var extensionId = request.GetValueOrDefault<int>(k.extensionID);
            var isAdmin = request.accessLevel.IsAccessRole(AccessRoles.extensionOperator);

            account.ValidUntil.ThrowIfLessOrEqual(DateTime.Now, ErrorCodes.AccountExpired);
            character.IsDocked.ThrowIfFalse(ErrorCodes.CharacterHasToBeDocked);

            var extensionInfo = ExtensionHelper.Extensions.GetOrDefault(extensionId).ThrowIfNull(ErrorCodes.ExtensionNotFound);

            //validate extension


            DateTime trialPeriodEnd;
            ExtensionHelper.IsExtensionRemoveAllowed(account, out trialPeriodEnd).ThrowIfFalse(ErrorCodes.ExtensionRemoveTimeOut);

            ExtensionHelper.CountExtensionRemoveLogEntries(account, TimeConstants.EXTENSION_REMOVE_MINUTES_BACK)
                           .ThrowIfGreaterOrEqual(ExtensionHelper.MAX_EXTENSION_REMOVE_PER_PERIOD, ErrorCodes.ExtensionMaximumRemovedPerPeriod);

            var currentExtensionLevel = character.GetExtensionLevel(extensionId);

            //this extension is not learnt for this character yet
            currentExtensionLevel.ThrowIfLessOrEqual(0, ErrorCodes.ItemNotFound);

            //extension is getting deleted
            if (currentExtensionLevel == 1)
            {
                character.AddToWallet(TransactionType.ExtensionPriceRefund, extensionInfo.price);
            }

            //not below
            if (extensionInfo.freezeLimit != null)
            {
                if (currentExtensionLevel >= (int)extensionInfo.freezeLimit)
                {
                    isAdmin.ThrowIfFalse(ErrorCodes.ExtensionFrozen);
                }
            }

            var spentId = 0;
            var spentPoints = 0;
            ExtensionHelper.GetTableIndexForAccountExtensionSpent(extensionId, currentExtensionLevel, character, ref spentId, ref spentPoints);

            if (spentId == 0)
            {
                isAdmin.ThrowIfFalse(ErrorCodes.ExtensionLevelCantBeRemoved);
            }

            //add negative spent points

            if (!character.IsInTraining())
            {
                ExtensionHelper.AddExtensionPointsSpent(account, character, -1*spentPoints, extensionId, currentExtensionLevel - 1);
                ExtensionHelper.InsertRemoveLog(account, character, extensionId, currentExtensionLevel, spentPoints);
            
            }

            character.SetExtension(new Extension(extensionId, currentExtensionLevel - 1));

            var result = ExtensionHelper.GetEPData(account, character);

            //The amount of EP gained
            result.Add("pointsReturned", spentPoints);
            result.Add(k.extensionID, extensionId);
            result.Add(k.extensionLevel, currentExtensionLevel - 1);

            Message.Builder.FromRequest(request).WithData(result).Send();
        }

        */

        #endregion
    }
}
