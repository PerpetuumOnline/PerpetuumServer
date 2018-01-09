using System;
using System.Collections.Generic;
using Perpetuum.Log;

namespace Perpetuum.Accounting
{
    public class AccountTransactionLogEvent : ILogEvent
    {
        public AccountTransactionType TransactionType { get; private set; }

        public AccountTransactionLogEvent(Account account,AccountTransactionType transactionType)
        {
            Account = account;
            TransactionType = transactionType;
        }

        public Account Account { get; set; }
        public int? Definition { get; set; }
        public int? Quantity { get; set; }
        public long? Eid { get; set; }
        public int Credit { get; set; }
        public int CreditChange { get; set; }
        public DateTime Created { get; set; } = DateTime.Now;

        public IDictionary<string, object> ToDictionary()
        {
            var d = new Dictionary<string, object>
            {
                {k.transactionType, (int) TransactionType}, 
                {k.definition, Definition}, 
                {k.quantity, Quantity}, 
                {k.credit, Credit}, 
                {k.creditChange, CreditChange}, 
                {k.created, Created}
            };

            return d;
        }

    }
}