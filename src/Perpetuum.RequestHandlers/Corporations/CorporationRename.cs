using System.Transactions;
using Perpetuum.Accounting;
using Perpetuum.Data;
using Perpetuum.Groups.Corporations;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Corporations
{
    public class CorporationRename : IRequestHandler
    {
        private readonly IAccountManager _accountManager;
        private readonly IAccountRepository _accountRepository;
        private readonly MtProductHelper _mtProductHelper;

        public CorporationRename(IAccountManager accountManager,IAccountRepository accountRepository,MtProductHelper mtProductHelper)
        {
            _accountManager = accountManager;
            _accountRepository = accountRepository;
            _mtProductHelper = mtProductHelper;
        }

        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var corpName = request.Data.GetOrDefault<string>(k.name).Trim();
                var nick = request.Data.GetOrDefault<string>(k.nick).Trim();
                var character = request.Session.Character;
            
                //get corp and role check
                var corporation = character.GetCorporation().CheckAccessAndThrowIfFailed(character, CorporationRole.CEO, CorporationRole.DeputyCEO);

                (corporation is PrivateCorporation).ThrowIfFalse(ErrorCodes.CorporationMustBePrivate);

                //nick check
                nick.Length.ThrowIfGreater(6, ErrorCodes.CorporationNickTooLong);
                nick.Length.ThrowIfLess(2, ErrorCodes.CorporationNickTooShort);
                nick.AllowExtras().ThrowIfFalse(ErrorCodes.CorporationNickNotAllowedCharacters);
                string.IsNullOrEmpty(nick).ThrowIfTrue(ErrorCodes.CorporationNickNotDefined);

                //name check
                corpName.Length.ThrowIfGreater(128, ErrorCodes.CorporationNameTooLong);
                corpName.Length.ThrowIfLessOrEqual(3, ErrorCodes.CorporationNameTooShort);
                corpName.AllowExtras().ThrowIfFalse(ErrorCodes.CorporationNameNotAllowedCharacters);
                string.IsNullOrEmpty(corpName).ThrowIfTrue(ErrorCodes.CorporationNameNotDefined);

                //existence check
                Corporation.IsNameOrNickTaken(corpName, nick).ThrowIfTrue(ErrorCodes.NameTaken);

                //write OLD name to alias history
                Corporation.WriteRenameHistory(corporation.Eid, character, corporation.CorporationName, corporation.CorporationNick);

                var product = _mtProductHelper.GetByAccountTransactionType(AccountTransactionType.CorporationRename);

                var account = _accountManager.Repository.Get(request.Session.AccountId).ThrowIfNull(ErrorCodes.AccountNotFound);
                var wallet = _accountManager.GetWallet(account,AccountTransactionType.CorporationRename);
                wallet.Balance -= product.price; 

                var e = new AccountTransactionLogEvent(account,AccountTransactionType.CorporationRename) {Credit = wallet.Balance, CreditChange = -product.price};
                _accountManager.LogTransaction(e);
            
                corporation.SetName(corpName, nick);

                _accountRepository.Update(account);

                Transaction.Current.OnCommited(() =>
                {

                    //force cache reload
                    CorporationData.RemoveFromCache(corporation.Eid);

                    //send to members
                    var corpInfo = corporation.GetInfoDictionaryForMember(character);
                    Message.Builder.SetCommand(Commands.CorporationGetMyInfo).WithData(corpInfo).ToCharacters(corporation.GetCharacterMembers()).Send();
                });
                
                scope.Complete();
            }
        }
    }
}
