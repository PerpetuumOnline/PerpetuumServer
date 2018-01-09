using Perpetuum.Accounting;
using Perpetuum.Accounting.Characters;
using Perpetuum.Data;
using Perpetuum.Host.Requests;
using Perpetuum.Services.ExtensionService;

namespace Perpetuum.RequestHandlers.Extensions
{


    /// <summary>
    /// Reset character extension for account credit ... not used yet
    /// </summary>
    public class ExtensionResetCharacter : IRequestHandler
    {
        private readonly IAccountManager _accountManager;
        private readonly IAccountRepository _accountRepository;
        private readonly MtProductHelper _mtProductHelper;

        public ExtensionResetCharacter(IAccountManager accountManager,IAccountRepository accountRepository,MtProductHelper mtProductHelper)
        {
            _accountManager = accountManager;
            _accountRepository = accountRepository;
            _mtProductHelper = mtProductHelper;
        }

        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var account = _accountManager.Repository.Get(request.Session.AccountId).ThrowIfNull(ErrorCodes.AccountNotFound);

                var character = Character.Get(request.Data.GetOrDefault<int>(k.characterID));
            
                if ( character == Character.None )
                    throw new PerpetuumException(ErrorCodes.CharacterNotFound);

                //only characters that belong to the issuers account
                if (character.AccountId != account.Id)
                {
                    throw new PerpetuumException(ErrorCodes.AccessDenied);
                }

                (character.IsDocked).ThrowIfFalse(ErrorCodes.CharacterHasToBeDocked);

                var product = _mtProductHelper.GetByAccountTransactionType(AccountTransactionType.ExtensionReset);
                var wallet = _accountManager.GetWallet(account,AccountTransactionType.ExtensionReset);

                wallet.Balance -= product.price; 

                var e = new AccountTransactionLogEvent(account,AccountTransactionType.ExtensionReset)
                {
                    Credit = wallet.Balance, 
                    CreditChange = -product.price
                };

                _accountManager.LogTransaction(e);

                _accountRepository.Update(account);

                //current extensions
                var extensionCollection = character.GetExtensions();

                //default extensions
                var defaultExtensionHandler = new CharacterDefaultExtensionHelper(character);
            
                foreach (var extension in extensionCollection)
                {
                    //returns 0 if the extension is not starter extension
                    //returns the minimum level if the extension is starter
                    int newLevel;
                    defaultExtensionHandler.IsStartingExtension( extension, out newLevel);
                
                    var resultExtension = new Extension(extension.id, newLevel);
                
                    character.SetExtension(resultExtension);
                }

                character.DeleteAllSpentPoints();

                //ezt nem csinalja, csak nyersen reseteli az extensionoket
                /*
            ExtensionHelper.DeleteAllNonForeverPenaltyPoints(character);
            */

                // %%% itt hagytuk abba

                Message.Builder.FromRequest(request).WithOk().Send();
                
                scope.Complete();
            }

        }




    }
}
