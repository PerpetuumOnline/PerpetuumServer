using Perpetuum.Common.Loggers;
using Perpetuum.Common.Loggers.Transaction;
using Perpetuum.Data;

namespace Perpetuum.Groups.Corporations
{
    public class CorporationTransactionLogger : DbLogger<TransactionLogEvent>
    {
        protected override void BuildCommand(TransactionLogEvent e,DbQuery query)
        {
            const string insertSqlCommand = @"insert corporationtransactions (corporationEID,memberID,amount,transactiontype,quantity,definition,targetMemberID,currentwallet,involvedCorporationEID) 
                                                                              values 
                                                                             (@corporationEID,@memberID,@amount,@transactiontype,@quantity,@definition,@targetMemberID,@currentWallet,@involvedCorporationEID)";

            query.CommandText(insertSqlCommand)
                .SetParameter("@corporationEID", e.CorporationEid)
                .SetParameter("@memberID", e.CharacterID == 0 ? (object)null : e.CharacterID)
                .SetParameter("@targetMemberID", e.InvolvedCharacterID == 0 ? (object)null : e.InvolvedCharacterID)
                .SetParameter("@amount", e.CreditChange)
                .SetParameter("@currentWallet", e.CreditBalance)
                .SetParameter("@transactiontype", e.TransactionType)
                .SetParameter("@involvedCorporationEID", e.InvolvedCorporationEid == 0L ? (object)null : e.InvolvedCorporationEid);

            if (e.ItemDefinition > 0)
            {
                query.SetParameter("definition", e.ItemDefinition).SetParameter("quantity", e.ItemQuantity);
            }
            else
            {
                query.SetParameter("definition", null).SetParameter("quantity", null);
            }
        }
    }
}