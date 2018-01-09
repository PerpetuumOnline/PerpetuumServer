using Perpetuum.Common.Loggers;
using Perpetuum.Common.Loggers.Transaction;
using Perpetuum.Data;

namespace Perpetuum.Accounting.Characters
{
    public class CharacterTransactionLogger : DbLogger<TransactionLogEvent>,ICharacterTransactionLogger
    {
        protected override void BuildCommand(TransactionLogEvent e,DbQuery query)
        {
            const string sqlCmd = @"insert charactertransactions (characterid,transactiontype,amount,definition,quantity, currentcredit, othercharacter,containereid) 
                                                                 values 
                                                                (@characterId,@transactionType,@amount,@definition,@quantity,@currentCredit,@otherCharacter,@containerEid)";

            query.CommandText(sqlCmd)
                .SetParameter("characterId", e.CharacterID)
                .SetParameter("transactionType", e.TransactionType)
                .SetParameter("amount", e.CreditChange)
                .SetParameter("currentCredit", e.CreditBalance)
                .SetParameter("otherCharacter", e.InvolvedCharacterID == 0 ? (object)null : e.InvolvedCharacterID)
                .SetParameter("containerEid", e.ContainerEid == 0L ? (object)null : e.ContainerEid);

            if (e.ItemDefinition > 0)
            {
                query.SetParameter("definition", e.ItemDefinition);
                query.SetParameter("quantity", e.ItemQuantity);
            }
            else
            {
                query.SetParameter("definition", null);
                query.SetParameter("quantity", null);
            }
        }
    }
}