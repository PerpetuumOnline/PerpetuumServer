
using Perpetuum.Log;

namespace Perpetuum.Common.Loggers.Transaction
{
    public class TransactionLogEvent : ILogEvent
    {
        public TransactionType TransactionType { get; set; }

        public double CreditBalance { get; set; }
        public double CreditChange { get; set; }

        public int CharacterID { get; set; }
        public int InvolvedCharacterID { get; set; }

        public int ItemDefinition { get; set; }
        public int ItemQuantity { get; set; }

        public long CorporationEid { get; set; }
        public long InvolvedCorporationEid { get; set; }
        public long ContainerEid { get; set; }

        public static TransactionLogEventBuilder Builder()
        {
            return new TransactionLogEventBuilder();
        }
    }
}