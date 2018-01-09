using Perpetuum.Common.Loggers;
using Perpetuum.Data;

namespace Perpetuum.Accounting
{
    public class AccountTransactionLogger : DbLogger<AccountTransactionLogEvent>
    {
        protected override void BuildCommand(AccountTransactionLogEvent logEvent, DbQuery query)
        {
            query.CommandText("insert into accounttransactionlog (accountId,transactionType,definition,quantity,eid,credit,creditChange,created) values (@accountId,@transactionType,@definition,@quantity,@eid,@credit,@creditChange,@created)")
                .SetParameter("@accountId",logEvent.Account.Id)
                .SetParameter("@transactionType", (int)logEvent.TransactionType)
                .SetParameter("@definition", logEvent.Definition)
                .SetParameter("@quantity", logEvent.Quantity)
                .SetParameter("@eid", logEvent.Eid)
                .SetParameter("@credit", logEvent.Credit)
                .SetParameter("@creditChange", logEvent.CreditChange)
                .SetParameter("@created", logEvent.Created);
        }
    }
}