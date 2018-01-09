using System;
using Perpetuum.Accounting.Characters;
using Perpetuum.Common;
using Perpetuum.Common.Loggers.Transaction;
using Perpetuum.Groups.Corporations;
using Perpetuum.Log;
using Perpetuum.Wallets;

namespace Perpetuum.Services.Insurance
{
    public class InsurancePayOut
    {
        private readonly ICentralBank _centralBank;

        public InsurancePayOut(ICentralBank centralBank)
        {
            _centralBank = centralBank;
        }

        public void PayOut(InsuranceDescription insurance,int definition)
        {
            if (!insurance.IsInsured)
                return;

            var b = TransactionLogEvent.Builder().SetTransactionType(TransactionType.InsurancePayOut)
                .SetCreditChange(insurance.payOutPrice)
                .SetCharacter(insurance.character)
                .SetItem(definition, 0);

            IWallet<double> wallet;
            if (insurance.corporationEid != null)
            {
                var corporation = Corporation.GetOrThrow((long)insurance.corporationEid);
                wallet = new CorporationWallet(corporation);
                wallet.Balance += insurance.payOutPrice;
                b.SetCreditBalance(wallet.Balance).SetCorporation(corporation);
                corporation.LogTransaction(b);
                Logger.Info($"insurance paid to corp:{insurance.corporationEid} amount:{insurance.payOutPrice}");
            }
            else
            {
                wallet = insurance.character.GetWallet(TransactionType.InsurancePayOut);
                wallet.Balance += insurance.payOutPrice;
                b.SetCreditBalance(wallet.Balance);
                insurance.character.LogTransaction(b);
                Logger.Info($"insurance paid to character:{insurance.character.Id} amount:{insurance.payOutPrice}");
            }

            _centralBank.SubAmount(insurance.payOutPrice, TransactionType.InsurancePayOut);
        }
    }

    public class InsuranceDescription
    {
        public Character character;
        public long eid;
        public DateTime endDate;
        public InsuranceType type;
        public long? corporationEid;
        public double payOutPrice;

        public bool IsInsured => (endDate > DateTime.Now);

        public override string ToString()
        {
            return $"eid:{eid} type:{type} enddate:{endDate} characterID:{character.Id} corporationEid:{corporationEid} payOutPrice:{payOutPrice}";
        }
    }

}