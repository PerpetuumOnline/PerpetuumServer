using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Perpetuum.Common.Loggers.Transaction;
using Perpetuum.Data;
using Perpetuum.Groups.Corporations;
using Perpetuum.Log;

namespace Perpetuum.Zones.Intrusion
{
    /// <summary>
    /// Static intrustion helper
    /// </summary>
    public class IntrusionHelper
    {
        /// <summary>
        /// Stores the income so the server can periodically empty/transfer the credit to the given corporation
        /// </summary>
        public static void AddOwnerIncome(long corporationEid, double amount)
        {
            Db.Query().CommandText("addownerincome")
                   .SetParameter("@corpEID", corporationEid)
                   .SetParameter("@amount", amount)
                   .ExecuteNonQuery();
        }

        public static Task DoSiegeCorporationSharePayOutAsync()
        {
            return Task.Run(() => DoSiegeCorporationSharePayOut());
        }

        /// <summary>
        /// This function is called periodically and pays the owned base's income to a corporation
        /// </summary>
        private static void DoSiegeCorporationSharePayOut()
        {
            using (var scope = Db.CreateTransaction())
            {
                try
                {
                    var timeBehind = DateTime.Now.AddHours(-5); //real

                    var records = Db.Query().CommandText("select corporationeid,amount from ownerincome where lastflush < @dayBehind")
                                        .SetParameter("@dayBehind", timeBehind)
                                        .Execute()
                                        .Select(r =>
                                            new
                                            {
                                                corporation = Corporation.GetOrThrow(r.GetValue<long>(0)),
                                                amount = r.GetValue<double>(1)
                                            })
                                        .Where(r => r.amount > 0.0);

                    foreach (var record in records)
                    {
                        var corporation = record.corporation;

                        var wallet = new CorporationWallet(corporation);
                        wallet.Balance += record.amount;

                        corporation.LogTransaction(TransactionLogEvent.Builder()
                                                                      .SetCorporation(corporation)
                                                                      .SetTransactionType(TransactionType.BaseIncome)
                                                                      .SetCreditBalance(wallet.Balance)
                                                                      .SetCreditChange(record.amount));

                        Db.Query().CommandText("update ownerincome set amount=0,lastflush=@now where corporationeid=@corpEID")
                                .SetParameter("@now", DateTime.Now)
                                .SetParameter("@corpEID", corporation.Eid)
                                .ExecuteNonQuery();

                        Logger.Info("owner income added to corp: " + corporation.Eid + " amount: " + record.amount);
                    }

                    scope.Complete();
                }
                catch (Exception ex)
                {
                    Logger.Exception(ex);
                }
            }
        }


        #region intrusion log

        public static IDictionary<string, object> GetMySitesLog(int offsetInDays, long corporationeid)
        {
            var later = DateTime.Now.AddDays(-offsetInDays);
            var earlier = later.AddDays(-Outpost.INTRUSION_LOGS_LENGTH);

            const string sqlCmd = @"SELECT siteeid,stability,eventtime,owner,winnercorporationeid,oldstability,sapdefinition,eventtype,oldowner
                                    FROM  intrusionsitelog
                                    WHERE eventtime between @earlier AND @later and owner=@corporationeid";

            var result = Db.Query().CommandText(sqlCmd)
                                .SetParameter("@later", later)
                                .SetParameter("@earlier", earlier)
                                .SetParameter("@corporationeid", corporationeid)
                                .Execute()
                                .RecordsToDictionary("e");
            return result;
        }

        #endregion
    }
}
