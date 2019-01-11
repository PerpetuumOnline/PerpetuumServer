using System;
using System.Collections.Generic;
using Perpetuum.Accounting.Characters;

namespace Perpetuum.Accounting
{
    public interface IAccountManager
    {
        IAccountRepository Repository { get; }

        IAccountWallet GetWallet(Account account, AccountTransactionType transactionType);

        void LogTransaction(AccountTransactionLogEvent e);
        IEnumerable<AccountTransactionLogEvent> GetTransactionHistory(Account account, TimeSpan offset,TimeSpan length);

        int GetActiveCharactersCount(Account account);

        int GetLockedEpByAccount(Account account);
        int CalculateCurrentEp(Account account);
        IDictionary<string, object> GetEPData(Account account, Character character);
        void FreeLockedEp(Account account, int amount);

        void InsertExtensionRemoveLog(Account account, Character character, int extensionId, int extensionLevel,int points);
        void AddExtensionPointsSpent(Account account, Character character, int spentPoints, int extensionID,int extensionLevel);
        void ExtensionSubscriptionStart(Account account, DateTime startTime, DateTime endTime, int multiplierBonus);
        //void ExtensionSubscriptionExtend(Account account, DateTime extendedValidUntil);

        void AddExtensionPoints(Account account, int pointsToInject);
        int AddExtensionPointsBoostAndLog(Account account, Character character, EpForActivityType activityType,int points);

        void InsertPenaltyPoint(Account account, AccountExtensionPenaltyType penaltyType, int points, bool forever);
        IEnumerable<EpForActivityLogEvent> GetEpForActivityHistory(Account account, DateTime earlier, DateTime later);

        void PackageGenerateAll(Account account);
    }
}