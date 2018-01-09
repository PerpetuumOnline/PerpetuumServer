using System.Transactions;
using Perpetuum.Accounting;
using Perpetuum.Accounting.Characters;
using Perpetuum.Data;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Characters
{
    public class CharacterRename : IRequestHandler
    {
        private readonly IAccountManager _accountManager;
        private readonly IAccountRepository _accountRepository;
        private readonly IReadOnlyRepository<int,CharacterProfile> _characterProfiles;
        private readonly MtProductHelper _mtProductHelper;

        public CharacterRename(IAccountManager accountManager,IAccountRepository accountRepository,IReadOnlyRepository<int,CharacterProfile> characterProfiles,MtProductHelper mtProductHelper)
        {
            _accountManager = accountManager;
            _accountRepository = accountRepository;
            _characterProfiles = characterProfiles;
            _mtProductHelper = mtProductHelper;
        }

        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var account = _accountManager.Repository.Get(request.Session.AccountId).ThrowIfNull(ErrorCodes.AccountNotFound);

                var nick = request.Data.GetOrDefault<string>(k.nick).Trim();
                var targetCharacter = Character.Get( request.Data.GetOrDefault<int>(k.characterID));

                if( targetCharacter == Character.None )
                    throw new PerpetuumException(ErrorCodes.CharacterNotFound); 
            
                //only characters that belong to the issuers account
                if (targetCharacter.AccountId != account.Id)
                {
                    throw new PerpetuumException(ErrorCodes.AccessDenied);
                }

                //sosem fordulhat elo, de azert megis
                if (!targetCharacter.IsActive)
                {
                    throw new PerpetuumException(ErrorCodes.AccessDenied);
                }

                //check nick and other conditions
                Character.CheckNickAndThrowIfFailed(nick, request.Session.AccessLevel,account);

                //withdrw credit
                var product = _mtProductHelper.GetByAccountTransactionType(AccountTransactionType.CharacterRename);
                var wallet = _accountManager.GetWallet(account,AccountTransactionType.CharacterRename);
                wallet.Balance -= product.price; 

                var e = new AccountTransactionLogEvent(account,AccountTransactionType.CharacterRename)
                {
                    Credit = wallet.Balance,
                    CreditChange = -product.price
                };

                _accountManager.LogTransaction(e);

                //do the heavy lifting
                var oldNick = targetCharacter.Nick;
                targetCharacter.Nick = nick;

                //log
                Db.Query().CommandText("INSERT dbo.characternickhistory ( characterid, accountid, nick ) VALUES  ( @characterid, @accountid, @nick )")
                    .SetParameter("@characterid", targetCharacter.Id)
                    .SetParameter("@accountid", account.Id)
                    .SetParameter("@nick", oldNick)
                    .ExecuteNonQuery();

                _accountRepository.Update(account);

                Transaction.Current.OnCommited(() =>
                {
                    var character = request.Session.Character;

                    if (_characterProfiles is CachedReadOnlyRepository<int,CharacterProfile> cached)
                        cached.Remove(character.Id);

                    var profile = character.GetFullProfile();
                    Message.Builder
                        .FromRequest(request)
                        .SetCommand(Commands.CharacterGetMyProfile)
                        .WithData(profile).Send();
                });
                
                scope.Complete();
            }
        }
    }
}
