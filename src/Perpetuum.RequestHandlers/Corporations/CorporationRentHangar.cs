using System;
using Perpetuum.Common.Loggers.Transaction;
using Perpetuum.Data;
using Perpetuum.Groups.Corporations;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Corporations
{
    public class CorporationRentHangar : IRequestHandler
    {
        private readonly ICorporationManager _corporationManager;

        public CorporationRentHangar(ICorporationManager corporationManager)
        {
            _corporationManager = corporationManager;
        }

        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var baseEID = request.Data.GetOrDefault<long>(k.baseEID);
                var character = request.Session.Character;

                character.IsDocked.ThrowIfFalse(ErrorCodes.CharacterHasToBeDocked);

                var dockingBase = character.GetCurrentDockingBase();
                if (dockingBase.Eid != baseEID)
                    throw new PerpetuumException(ErrorCodes.FacilityOutOfReach);

                var corporation = character.GetPrivateCorporationOrThrow();

                var role = corporation.GetMemberRole(character);
                role.IsAnyRole(CorporationRole.CEO, CorporationRole.HangarOperator, CorporationRole.DeputyCEO).ThrowIfFalse(ErrorCodes.InsufficientPrivileges);

                if (!role.IsAnyRole(CorporationRole.CEO))
                {
                    _corporationManager.IsInJoinOrLeave(character).ThrowIfError();
                }

                var mainHangar = dockingBase.GetPublicCorporationHangarStorage();

                var rentInfo = mainHangar.GetCorporationHangarRentInfo();

                var wallet = new CorporationWallet(corporation);
                wallet.Balance -= rentInfo.price;

                var b = TransactionLogEvent.Builder()
                    .SetCorporation(corporation)
                    .SetTransactionType(TransactionType.hangarRent)
                    .SetCreditBalance(wallet.Balance)
                    .SetCreditChange(-rentInfo.price);

                corporation.LogTransaction(b);

                var corporateHangar = CorporateHangar.Create();
                corporateHangar.Owner = corporation.Eid;
                mainHangar.AddChild(corporateHangar);

                //set lease start time
                corporateHangar.LeaseStart = DateTime.Now;
                corporateHangar.LeaseEnd = DateTime.Now + rentInfo.period;
                corporateHangar.IsLeaseExpired = false;
                corporateHangar.SetLogging(true, character); //set logging on by default

                mainHangar.Save();

                dockingBase.AddCentralBank(TransactionType.hangarRent, rentInfo.price);
                Message.Builder.FromRequest(request).WithData(corporateHangar.ToDictionary()).Send();
                
                scope.Complete();
            }
        }
    }
}