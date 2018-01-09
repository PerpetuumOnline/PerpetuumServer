using System.Collections.Generic;
using Perpetuum.Data;
using Perpetuum.Wallets;

namespace Perpetuum.Groups.Corporations
{
    public class CorporationWallet : Wallet<double>
    {
        public CorporationWallet(Corporation corporation)
        {
            Corporation = corporation;
        }

        public Corporation Corporation { get; private set; }

        protected override void SetBalance(double value)
        {
            Db.Query().CommandText("update corporations set wallet=@amount where EID=@corporationEID")
                .SetParameter("@corporationEID", Corporation.Eid)
                .SetParameter("@amount", value)
                .ExecuteNonQuery();
        }

        protected override double GetBalance()
        {
            return Db.Query().CommandText("select wallet from corporations where EID=@corporationEID")
                .SetParameter("@corporationEID", Corporation.Eid)
                .ExecuteScalar<double>();
        }

        protected override void OnBalanceUpdating(double currentCredit, double desiredCredit)
        {
            desiredCredit.ThrowIfLess(0, ErrorCodes.CorporationNotEnoughMoney);
        }

        protected override void OnCommited(double startBalance)
        {
            var result = new Dictionary<string, object> {{ k.wallet, (long)GetBalance() }};
            Message.Builder.SetCommand(Commands.CorporationGetMyInfo)
                .WithData(result)
                .ToCharacters(Corporation.GetCharacterMembers()).Send();
        }
    }
}