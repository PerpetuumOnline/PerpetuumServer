using System;
using System.Collections.Generic;
using Perpetuum.Common.Loggers.Transaction;
using Perpetuum.Containers;
using Perpetuum.Data;
using Perpetuum.Groups.Corporations;
using Perpetuum.Host.Requests;

namespace Perpetuum.RequestHandlers.Corporations
{
    public class CorporationHangarPayRent : IRequestHandler
    {
        public void HandleRequest(IRequest request)
        {
            using (var scope = Db.CreateTransaction())
            {
                var character = request.Session.Character;
                var hangarEID = request.Data.GetOrDefault<long>(k.eid);

                var corporation = character.GetCorporation();
                var corporateHangar = corporation.GetHangar(hangarEID, character, ContainerAccess.LogList);

                var hangarStorage = corporateHangar.GetHangarStorage();
                var rentInfo = hangarStorage.GetCorporationHangarRentInfo();

                var wallet = new CorporationWallet(corporation);
                wallet.Balance -= rentInfo.price;

                var b = TransactionLogEvent.Builder().SetCorporation(corporation).SetTransactionType(TransactionType.hangarRent).SetCreditBalance(wallet.Balance).SetCreditChange(-rentInfo.price);
                corporation.LogTransaction(b);

                if (corporateHangar.IsLeaseExpired)
                {
                    corporateHangar.LeaseStart = DateTime.Now;
                    corporateHangar.LeaseEnd = DateTime.Now + rentInfo.period;
                }
                else
                {
                    corporateHangar.LeaseStart += rentInfo.period;
                    corporateHangar.LeaseEnd += rentInfo.period;
                }

                corporateHangar.IsLeaseExpired = false;
                corporateHangar.Save();

                hangarStorage.GetParentDockingBase().AddCentralBank(TransactionType.hangarRent, rentInfo.price);

                var result = new Dictionary<string, object>
                {
                    {k.eid, hangarEID},
                    {k.name, corporateHangar.Name},
                    {k.price, rentInfo.price},
                    {k.rentPeriod, (int)rentInfo.period.TotalDays},
                    {k.leaseStart, corporateHangar.LeaseStart},
                    {k.leaseEnd, corporateHangar.LeaseEnd},
                    {k.leaseExpired, 0}
                };

                Message.Builder.FromRequest(request).WithData(result).Send();
                
                scope.Complete();
            }
        }
    }
}