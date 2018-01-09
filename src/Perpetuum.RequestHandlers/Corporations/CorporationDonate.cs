using Perpetuum.Accounting.Characters;
using Perpetuum.Common.Loggers.Transaction;
using Perpetuum.Data;
using Perpetuum.Groups.Corporations;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Corporations
{
    public class CorporationDonate : IRequestHandler
    {
        private readonly ICorporationManager _corporationManager;
        private readonly CharacterWalletFactory _characterWalletFactory;

        public CorporationDonate(ICorporationManager corporationManager,CharacterWalletFactory characterWalletFactory)
        {
            _corporationManager = corporationManager;
            _characterWalletFactory = characterWalletFactory;
        }


        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var character = request.Session.Character;
                var amount = request.Data.GetOrDefault<long>(k.amount);

                if (amount <= 0)
                    return;

                character.IsInTraining().ThrowIfTrue(ErrorCodes.TrainingCharacterInvolved);

                character.IsPrivilegedTransactionsAllowed().ThrowIfError();

                Corporation corporation = character.GetPrivateCorporationOrThrow();
                corporation.IsActive.ThrowIfFalse(ErrorCodes.CorporationNotExists);

                var ceo = corporation.CEO;
                if (ceo != character)
                {
                    _corporationManager.IsJoinPeriodExpired(character, corporation.Eid).ThrowIfFalse(ErrorCodes.corporationTransactionsFrozen);
                }

                var characterWallet = _characterWalletFactory(character, TransactionType.characterDonate);
                characterWallet.Balance -= amount;

                var builder = TransactionLogEvent.Builder()
                    .SetCharacter(character)
                    .SetTransactionType(TransactionType.characterDonate)
                    .SetCreditBalance(characterWallet.Balance)
                    .SetCreditChange(-amount)
                    .SetCorporation(corporation);

                character.LogTransaction(builder);

                var corporationWallet = new CorporationWallet(corporation);
                corporationWallet.Balance += amount;

                builder.SetCreditBalance(corporationWallet.Balance)
                    .SetCreditChange(amount);

                corporation.LogTransaction(builder);
                
                scope.Complete();
            }
        }
    }
}