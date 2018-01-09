using System;
using Perpetuum.Common.Loggers.Transaction;
using Perpetuum.Data;
using Perpetuum.Threading.Process;

namespace Perpetuum.Common
{
    public interface ICentralBank
    {
        void AddAmount(double amount, TransactionType transactionType);
        void SubAmount(double amount, TransactionType transactionType);
    }

    public class CentralBank : Process,ICentralBank
    {
        public void AddAmount(double amount, TransactionType transactionType)
        {
            amount = Math.Abs(amount);

            Db.Query().CommandText("centralBank_add")
                .SetParameter("@amount", amount)
                .SetParameter("@transactionType", (int)transactionType)
                .ExecuteNonQuery();
        }

        public void SubAmount(double amount, TransactionType transactionType)
        {
            amount = Math.Abs(amount);

            Db.Query().CommandText("centralBank_sub")
                .SetParameter("@amount", amount)
                .SetParameter("@transactionType", (int)transactionType)
                .ExecuteNonQuery();
        }

        public override void Update(TimeSpan time)
        {
            Db.Query().CommandText("centralBank_addLog").SetParameter("@day",DateTime.Today).ExecuteNonQuery();
        }
    }
}